using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Behaviors;
using ForEach.Conduit.Tests.Handlers;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests;

public class StreamPipelineBehaviorTests
{
    private static IDispatcher Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConduit();
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    private static async Task<List<T>> Collect<T>(IAsyncEnumerable<T> source)
    {
        var list = new List<T>();
        await foreach (var item in source)
            list.Add(item);
        return list;
    }

    [Fact]
    public async Task SingleBehavior_WrapsStreamExecution()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new CountingStreamBehavior<NumberStreamQuery, int>(log, "b1"));
        });

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().Equal(1, 2, 3);
        log.Should().Equal("b1:before", "b1:after:3");
    }

    [Fact]
    public async Task NoBehavior_StreamWorksAsNormal()
    {
        var dispatcher = Build(s =>
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>());

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task MultipleBehaviors_RunInRegistrationOrder_OuterFirst()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new CountingStreamBehavior<NumberStreamQuery, int>(log, "outer"));
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new CountingStreamBehavior<NumberStreamQuery, int>(log, "inner"));
        });

        await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        log.Should().Equal(
            "outer:before",
            "inner:before",
            "inner:after:3",
            "outer:after:3");
    }

    [Fact]
    public async Task Behavior_CanTransformItems()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new MultiplyingStreamBehavior<NumberStreamQuery>(10));
        });

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().Equal(10, 20, 30);
    }

    [Fact]
    public async Task MultipleBehaviors_TransformationsCompose()
    {
        // outer multiplies by 10, inner multiplies by 2 → outer sees 20, 40, 60
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new MultiplyingStreamBehavior<NumberStreamQuery>(10));
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new MultiplyingStreamBehavior<NumberStreamQuery>(2));
        });

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        // handler: [1,2,3] → inner×2: [2,4,6] → outer×10: [20,40,60]
        items.Should().Equal(20, 40, 60);
    }

    [Fact]
    public async Task Behavior_CanShortCircuit_ReturnsEmptySequence()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new ShortCircuitStreamBehavior<NumberStreamQuery, int>(blocked: true));
        });

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().BeEmpty();
    }

    [Fact]
    public async Task Behavior_ShortCircuit_DoesNotCallNext()
    {
        var behavior = new ShortCircuitStreamBehavior<NumberStreamQuery, int>(blocked: true);
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddSingleton<IStreamPipelineBehavior<NumberStreamQuery, int>>(behavior);
        });

        await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        behavior.NextWasCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Behavior_NotBlocked_CallsNextAndYieldsItems()
    {
        var behavior = new ShortCircuitStreamBehavior<NumberStreamQuery, int>(blocked: false);
        var dispatcher = Build(s =>
        {
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
            s.AddSingleton<IStreamPipelineBehavior<NumberStreamQuery, int>>(behavior);
        });

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        behavior.NextWasCalled.Should().BeTrue();
        items.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Stream_WithBehavior_NoHandler_StillThrowsSynchronously()
    {
        var dispatcher = Build(s =>
            s.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
                _ => new CountingStreamBehavior<NumberStreamQuery, int>(new List<string>(), "b")));

        // Behavior is registered but handler is missing — should throw before the behavior runs
        var act = () => dispatcher.Stream<int>(new NumberStreamQuery());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No IStreamQueryHandler*");
    }

    [Fact]
    public async Task AddStreamPipelineBehavior_Closed_Extension_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
        services.AddStreamPipelineBehavior<NumberStreamQuery, int, PassThroughStreamBehavior<NumberStreamQuery, int>>();
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        // PassThroughStreamBehavior doesn't change items — handler output flows through
        items.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task AddStreamPipelineBehavior_Builder_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddStreamQueryHandler<NumberStreamQuery, int, NumberStreamHandler>()
            .AddStreamPipelineBehavior<NumberStreamQuery, int, PassThroughStreamBehavior<NumberStreamQuery, int>>();
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task AddStreamPipelineBehavior_FactoryRegistration_Works()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>();
        services.AddScoped<IStreamPipelineBehavior<NumberStreamQuery, int>>(
            _ => new MultiplyingStreamBehavior<NumberStreamQuery>(4));
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var items = await Collect(dispatcher.Stream<int>(new NumberStreamQuery()));

        items.Should().Equal(4, 8, 12);
    }
}

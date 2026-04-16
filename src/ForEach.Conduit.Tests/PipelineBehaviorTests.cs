using ForEach.Conduit.Commands;
using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Behaviors;
using ForEach.Conduit.Tests.Commands;
using ForEach.Conduit.Tests.Events;
using ForEach.Conduit.Tests.Handlers;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests;

/// <summary>Short-circuits returning a typed failure.</summary>
internal sealed class ShortCircuitBehavior<TReq, TResult> : IPipelineBehavior<TReq, ValueResult<TResult>>
{
    public static readonly Error ShortCircuitError = new("ShortCircuit", "blocked by behavior");

    public ValueTask<ValueResult<TResult>> Handle(TReq request, Func<ValueTask<ValueResult<TResult>>> next, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<TResult>.Failure(ShortCircuitError));
}

public class PipelineBehaviorTests
{
    private static IDispatcher Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConduit();
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task SingleBehavior_Void_WrapsHandlerExecution()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TimedCommand>, TimedHandler>();
            s.AddScoped<IPipelineBehavior<TimedCommand, ValueResult>>(
                _ => new OrderRecordingBehavior<TimedCommand, ValueResult>(log, "b1"));
        });

        await dispatcher.Send(new TimedCommand());

        log.Should().Equal("b1:before", "b1:after");
    }

    [Fact]
    public async Task SingleBehavior_WithResult_WrapsHandlerExecution()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<MultiplyCommand, int>, MultiplyHandler>();
            s.AddScoped<IPipelineBehavior<MultiplyCommand, ValueResult<int>>>(
                _ => new OrderRecordingBehavior<MultiplyCommand, ValueResult<int>>(log, "b1"));
        });

        var result = await dispatcher.Send(new MultiplyCommand(5));

        result.Value.Should().Be(10);
        log.Should().Equal("b1:before", "b1:after");
    }

    [Fact]
    public async Task SingleBehavior_Query_WrapsHandlerExecution()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<IQueryHandler<PrefixQuery, string>, PrefixHandler>();
            s.AddScoped<IPipelineBehavior<PrefixQuery, ValueResult<string>>>(
                _ => new OrderRecordingBehavior<PrefixQuery, ValueResult<string>>(log, "bq"));
        });

        var result = await dispatcher.Query(new PrefixQuery("hello"));

        result.Value.Should().Be("result:hello");
        log.Should().Equal("bq:before", "bq:after");
    }

    [Fact]
    public async Task MultipleBehaviors_RunInRegistrationOrder()
    {
        var log = new List<string>();
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TimedCommand>, TimedHandler>();
            s.AddScoped<IPipelineBehavior<TimedCommand, ValueResult>>(
                _ => new OrderRecordingBehavior<TimedCommand, ValueResult>(log, "outer"));
            s.AddScoped<IPipelineBehavior<TimedCommand, ValueResult>>(
                _ => new OrderRecordingBehavior<TimedCommand, ValueResult>(log, "inner"));
        });

        await dispatcher.Send(new TimedCommand());

        log.Should().Equal("outer:before", "inner:before", "inner:after", "outer:after");
    }

    [Fact]
    public async Task Behavior_CanShortCircuit_VoidCommand()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TimedCommand>, TimedHandler>();
            s.AddScoped<IPipelineBehavior<TimedCommand, ValueResult>, ShortCircuitBehavior<TimedCommand>>();
        });

        var result = await dispatcher.Send(new TimedCommand());

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("ShortCircuit");
    }

    [Fact]
    public async Task Behavior_CanShortCircuit_CommandWithResult()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<MultiplyCommand, int>, MultiplyHandler>();
            s.AddScoped<IPipelineBehavior<MultiplyCommand, ValueResult<int>>,
                ShortCircuitBehavior<MultiplyCommand, int>>();
        });

        var result = await dispatcher.Send(new MultiplyCommand(5));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("ShortCircuit");
    }

    [Fact]
    public async Task NotificationBehavior_WrapsHandlerFanOut()
    {
        var log = new List<string>();
        var handler = new AuditedEventHandler();
        var dispatcher = Build(s =>
        {
            s.AddSingleton<INotificationHandler<AuditedEvent>>(handler);
            s.AddScoped<INotificationPipelineBehavior<AuditedEvent>>(
                _ => new NotificationOrderBehavior(log, "nb"));
        });

        await dispatcher.Publish(new AuditedEvent());

        log.Should().Equal("nb:before", "nb:after");
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task MultipleNotificationBehaviors_RunInRegistrationOrder()
    {
        var log = new List<string>();
        var handler = new AuditedEventHandler();
        var dispatcher = Build(s =>
        {
            s.AddSingleton<INotificationHandler<AuditedEvent>>(handler);
            s.AddScoped<INotificationPipelineBehavior<AuditedEvent>>(
                _ => new NotificationOrderBehavior(log, "outer"));
            s.AddScoped<INotificationPipelineBehavior<AuditedEvent>>(
                _ => new NotificationOrderBehavior(log, "inner"));
        });

        await dispatcher.Publish(new AuditedEvent());

        log.Should().Equal("outer:before", "inner:before", "inner:after", "outer:after");
    }

    [Fact]
    public async Task NoBehavior_HandlerCalledDirectly()
    {
        var dispatcher = Build(s =>
            s.AddScoped<ICommandHandler<MultiplyCommand, int>, MultiplyHandler>());

        var result = await dispatcher.Send(new MultiplyCommand(6));
        result.Value.Should().Be(12);
    }
}

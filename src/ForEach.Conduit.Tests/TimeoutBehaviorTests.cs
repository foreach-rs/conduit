using ForEach.Conduit.Behaviors;
using ForEach.Conduit.Commands;
using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Commands;
using ForEach.Conduit.Tests.Handlers;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests;

public class TimeoutBehaviorTests
{
    private const int ShortTimeoutMs  = 50;   // behavior deadline
    private const int SlowHandlerMs   = 2000; // effectively "never finishes in time"
    private const int FastHandlerMs   = 0;    // completes immediately

    private static IDispatcher Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConduit();
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task VoidCommand_ExceedsTimeout_ReturnsTimeoutError()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
                _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Send(new SlowCommand(SlowHandlerMs));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task VoidCommand_ErrorMessage_ContainsRequestTypeName()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
                _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Send(new SlowCommand(SlowHandlerMs));

        result.Error!.Value.Message.Should().Contain(nameof(SlowCommand));
    }

    [Fact]
    public async Task VoidCommand_ErrorMessage_ContainsTimeoutMs()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
                _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Send(new SlowCommand(SlowHandlerMs));

        result.Error!.Value.Message.Should().Contain($"{ShortTimeoutMs}ms");
    }

    [Fact]
    public async Task VoidCommand_CompletesBeforeTimeout_ReturnsSuccess()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
                _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromSeconds(10)));
        });

        var result = await dispatcher.Send(new SlowCommand(FastHandlerMs));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VoidCommand_CallerCancels_ThrowsOperationCanceledException()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
                _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromSeconds(10)));
        });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(ShortTimeoutMs));

        await dispatcher.Invoking(d => d.Send(new SlowCommand(SlowHandlerMs), cts.Token).AsTask())
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task TypedCommand_ExceedsTimeout_ReturnsTimeoutError()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommandWithResult, string>, SlowCommandWithResultHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommandWithResult, ValueResult<string>>>(
                _ => new TimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Send(new SlowCommandWithResult(SlowHandlerMs));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task TypedCommand_ExceedsTimeout_ValueIsDefault()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommandWithResult, string>, SlowCommandWithResultHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommandWithResult, ValueResult<string>>>(
                _ => new TimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Send(new SlowCommandWithResult(SlowHandlerMs));

        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task TypedCommand_CompletesBeforeTimeout_ReturnsValue()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommandWithResult, string>, SlowCommandWithResultHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommandWithResult, ValueResult<string>>>(
                _ => new TimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromSeconds(10)));
        });

        var result = await dispatcher.Send(new SlowCommandWithResult(FastHandlerMs));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("done");
    }

    [Fact]
    public async Task TypedCommand_CallerCancels_ThrowsOperationCanceledException()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<SlowCommandWithResult, string>, SlowCommandWithResultHandler>();
            s.AddScoped<IPipelineBehavior<SlowCommandWithResult, ValueResult<string>>>(
                _ => new TimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromSeconds(10)));
        });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(ShortTimeoutMs));

        await dispatcher.Invoking(d => d.Send(new SlowCommandWithResult(SlowHandlerMs), cts.Token).AsTask())
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Query_ExceedsTimeout_ReturnsTimeoutError()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<IQueryHandler<SlowQuery, int>, SlowQueryHandler>();
            s.AddScoped<IPipelineBehavior<SlowQuery, ValueResult<int>>>(
                _ => new TimeoutBehavior<SlowQuery, int>(TimeSpan.FromMilliseconds(ShortTimeoutMs)));
        });

        var result = await dispatcher.Query(new SlowQuery(SlowHandlerMs));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task Query_CompletesBeforeTimeout_ReturnsValue()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<IQueryHandler<SlowQuery, int>, SlowQueryHandler>();
            s.AddScoped<IPipelineBehavior<SlowQuery, ValueResult<int>>>(
                _ => new TimeoutBehavior<SlowQuery, int>(TimeSpan.FromSeconds(10)));
        });

        var result = await dispatcher.Query(new SlowQuery(FastHandlerMs));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Query_CallerCancels_ThrowsOperationCanceledException()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<IQueryHandler<SlowQuery, int>, SlowQueryHandler>();
            s.AddScoped<IPipelineBehavior<SlowQuery, ValueResult<int>>>(
                _ => new TimeoutBehavior<SlowQuery, int>(TimeSpan.FromSeconds(10)));
        });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(ShortTimeoutMs));

        await dispatcher.Invoking(d => d.Query(new SlowQuery(SlowHandlerMs), cts.Token).AsTask())
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task AddTimeoutBehavior_VoidCommand_Extension_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<ICommandHandler<SlowCommand>, SlowCommandHandler>();
        services.AddTimeoutBehavior<SlowCommand>(TimeSpan.FromMilliseconds(ShortTimeoutMs));
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new SlowCommand(SlowHandlerMs));

        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task AddTimeoutBehavior_TypedCommand_Extension_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<ICommandHandler<SlowCommandWithResult, string>, SlowCommandWithResultHandler>();
        services.AddTimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromMilliseconds(ShortTimeoutMs));
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new SlowCommandWithResult(SlowHandlerMs));

        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task AddTimeoutBehavior_Builder_VoidCommand_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddCommandHandler<SlowCommand, SlowCommandHandler>()
            .AddTimeoutBehavior<SlowCommand>(TimeSpan.FromMilliseconds(ShortTimeoutMs));
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new SlowCommand(SlowHandlerMs));

        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public async Task AddTimeoutBehavior_Builder_TypedCommand_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddCommandHandler<SlowCommandWithResult, string, SlowCommandWithResultHandler>()
            .AddTimeoutBehavior<SlowCommandWithResult, string>(TimeSpan.FromMilliseconds(ShortTimeoutMs));
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new SlowCommandWithResult(SlowHandlerMs));

        result.Error!.Value.Code.Should().Be("Timeout");
    }

    [Fact]
    public void ErrorTimeout_HasCorrectCode()
    {
        var error = Error.Timeout("something timed out");
        error.Code.Should().Be("Timeout");
    }

    [Fact]
    public void ErrorTimeout_HasCorrectMessage()
    {
        var error = Error.Timeout("op took too long");
        error.Message.Should().Be("op took too long");
    }
}

using ForEach.Conduit.Behaviors;
using ForEach.Conduit.Commands;
using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Tests.Commands;
using ForEach.Conduit.Tests.Handlers;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.Retry;

namespace ForEach.Conduit.Tests;

public class ResilienceBehaviorTests
{
    private static IDispatcher Build(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConduit();
        configure(services);
        return services.BuildServiceProvider().GetRequiredService<IDispatcher>();
    }

    [Fact]
    public async Task VoidCommand_TransientFailure_RetrySucceeds()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            // Fails 2 times, succeeds on 3rd — maxAttempts=3 means 2 retries
            s.AddRetryBehavior<TransientCommand>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VoidCommand_TransientFailure_SingleAttemptFails_WithoutRetry()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            // maxAttempts=1 means no retries at all
            s.AddRetryBehavior<TransientCommand>(maxAttempts: 1, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 2));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Transient");
    }

    [Fact]
    public async Task VoidCommand_AllAttemptsExhausted_ReturnsFinalFailure()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            // Only 2 total attempts but handler fails 5 times
            s.AddRetryBehavior<TransientCommand>(maxAttempts: 2, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 5));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Transient");
    }

    [Fact]
    public async Task VoidCommand_NoFailures_ReturnsSuccess()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            s.AddRetryBehavior<TransientCommand>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 0));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VoidCommand_ExceptionThrown_RetrySucceeds()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<ThrowingTransientCommand>, ThrowingTransientCommandHandler>();
            s.AddRetryBehavior<ThrowingTransientCommand>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new ThrowingTransientCommand(ThrowCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VoidCommand_ExceptionThrown_AllAttemptsExhausted_Throws()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<ThrowingTransientCommand>, ThrowingTransientCommandHandler>();
            // Only 1 attempt; handler throws every time
            s.AddRetryBehavior<ThrowingTransientCommand>(maxAttempts: 1, baseDelay: TimeSpan.Zero);
        });

        // Polly re-throws the exception when attempts are exhausted
        await dispatcher.Invoking(d => d.Send(new ThrowingTransientCommand(ThrowCount: 5)).AsTask())
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task VoidCommand_ShouldRetryPredicate_MatchingError_RetrySucceeds()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            s.AddRetryBehavior<TransientCommand>(
                maxAttempts: 3,
                baseDelay: TimeSpan.Zero,
                shouldRetry: e => e.Code == "Transient");
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task VoidCommand_ShouldRetryPredicate_NonMatchingError_DoesNotRetry()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();
            // Predicate does NOT match "Transient" code — should not retry
            s.AddRetryBehavior<TransientCommand>(
                maxAttempts: 3,
                baseDelay: TimeSpan.Zero,
                shouldRetry: e => e.Code == "SomethingElse");
        });

        var result = await dispatcher.Send(new TransientCommand(FailCount: 1));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Transient");
    }

    [Fact]
    public async Task TypedCommand_TransientFailure_RetrySucceeds()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommandWithResult, string>, TransientCommandWithResultHandler>();
            s.AddRetryBehavior<TransientCommandWithResult, string>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommandWithResult(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("done");
    }

    [Fact]
    public async Task TypedCommand_AllAttemptsExhausted_ReturnsFailure()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommandWithResult, string>, TransientCommandWithResultHandler>();
            s.AddRetryBehavior<TransientCommandWithResult, string>(maxAttempts: 2, baseDelay: TimeSpan.Zero);
        });

        var result = await dispatcher.Send(new TransientCommandWithResult(FailCount: 5));

        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Transient");
    }

    [Fact]
    public async Task TypedCommand_ShouldRetryPredicate_MatchingError_RetrySucceeds()
    {
        var dispatcher = Build(s =>
        {
            s.AddScoped<ICommandHandler<TransientCommandWithResult, string>, TransientCommandWithResultHandler>();
            s.AddRetryBehavior<TransientCommandWithResult, string>(
                maxAttempts: 3,
                baseDelay: TimeSpan.Zero,
                shouldRetry: e => e.Code == "Transient");
        });

        var result = await dispatcher.Send(new TransientCommandWithResult(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task NamedPipeline_VoidCommand_RetrySucceeds()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<ICommandHandler<TransientCommand>, TransientCommandHandler>();

        services.AddResiliencePipeline<string, ValueResult>("test-retry", builder =>
            builder.AddRetry(new RetryStrategyOptions<ValueResult>
            {
                MaxRetryAttempts = 2,
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
                ShouldHandle     = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null ||
                    (args.Outcome.Result is { IsSuccess: false }))
            }));

        services.AddResilienceBehavior<TransientCommand>("test-retry");

        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new TransientCommand(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task NamedPipeline_TypedCommand_RetrySucceeds()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddScoped<ICommandHandler<TransientCommandWithResult, string>, TransientCommandWithResultHandler>();

        services.AddResiliencePipeline<string, ValueResult<string>>("test-typed-retry", builder =>
            builder.AddRetry(new RetryStrategyOptions<ValueResult<string>>
            {
                MaxRetryAttempts = 2,
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
                ShouldHandle     = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null ||
                    (args.Outcome.Result is { IsSuccess: false }))
            }));

        services.AddResilienceBehavior<TransientCommandWithResult, string>("test-typed-retry");

        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new TransientCommandWithResult(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("done");
    }

    [Fact]
    public async Task Builder_AddRetryBehavior_VoidCommand_RetrySucceeds()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddCommandHandler<TransientCommand, TransientCommandHandler>()
            .AddRetryBehavior<TransientCommand>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new TransientCommand(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Builder_AddRetryBehavior_TypedCommand_RetrySucceeds()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddCommandHandler<TransientCommandWithResult, string, TransientCommandWithResultHandler>()
            .AddRetryBehavior<TransientCommandWithResult, string>(maxAttempts: 3, baseDelay: TimeSpan.Zero);
        var dispatcher = services.BuildServiceProvider().GetRequiredService<IDispatcher>();

        var result = await dispatcher.Send(new TransientCommandWithResult(FailCount: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("done");
    }

    [Fact]
    public async Task DirectConstruction_VoidBehavior_WrapsNextDelegate()
    {
        var pipeline = new ResiliencePipelineBuilder<ValueResult>()
            .AddRetry(new RetryStrategyOptions<ValueResult>
            {
                MaxRetryAttempts = 2,
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
                ShouldHandle     = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null || !args.Outcome.Result.IsSuccess)
            })
            .Build();

        var behavior = new ResiliencePipelineBehavior<TransientCommand>(pipeline);

        var callCount = 0;
        var result = await behavior.Handle(
            new TransientCommand(FailCount: 2),
            () =>
            {
                callCount++;
                return callCount <= 2
                    ? ValueTask.FromResult(ValueResult.Failure(new Error("Transient", "fail")))
                    : ValueTask.FromResult(ValueResult.Success());
            });

        result.IsSuccess.Should().BeTrue();
        callCount.Should().Be(3);
    }

    [Fact]
    public async Task DirectConstruction_TypedBehavior_WrapsNextDelegate()
    {
        var pipeline = new ResiliencePipelineBuilder<ValueResult<string>>()
            .AddRetry(new RetryStrategyOptions<ValueResult<string>>
            {
                MaxRetryAttempts = 2,
                Delay            = TimeSpan.Zero,
                BackoffType      = DelayBackoffType.Constant,
                UseJitter        = false,
                ShouldHandle     = args => ValueTask.FromResult(
                    args.Outcome.Exception is not null || !args.Outcome.Result.IsSuccess)
            })
            .Build();

        var behavior = new ResiliencePipelineBehavior<TransientCommandWithResult, string>(pipeline);

        var callCount = 0;
        var result = await behavior.Handle(
            new TransientCommandWithResult(FailCount: 2),
            () =>
            {
                callCount++;
                return callCount <= 2
                    ? ValueTask.FromResult(ValueResult<string>.Failure(new Error("Transient", "fail")))
                    : ValueTask.FromResult(ValueResult<string>.Success("done"));
            });

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("done");
        callCount.Should().Be(3);
    }

    [Fact]
    public void ErrorCircuitOpen_HasCorrectCode()
    {
        var error = Error.CircuitOpen("circuit tripped");
        error.Code.Should().Be("CircuitOpen");
    }

    [Fact]
    public void ErrorCircuitOpen_HasCorrectMessage()
    {
        var error = Error.CircuitOpen("service unavailable");
        error.Message.Should().Be("service unavailable");
    }

    [Fact]
    public void ErrorCircuitOpen_IsValid()
    {
        var error = Error.CircuitOpen("tripped");
        error.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ErrorTimeout_IsValid()
    {
        var error = Error.Timeout("timed out");
        error.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ErrorCircuitOpen_NoException()
    {
        var error = Error.CircuitOpen("tripped");
        error.Exception.Should().BeNull();
    }
}

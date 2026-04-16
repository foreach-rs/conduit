using ForEach.Conduit.Pipeline;
using Polly;
using Polly.CircuitBreaker;

namespace ForEach.Conduit.Behaviors;

/// <summary>
/// Wraps void-command dispatch in a Polly <see cref="ResiliencePipeline{ValueResult}"/>.
///
/// <para>
/// Supports the full set of Polly v8 strategies: retry with exponential back-off and jitter,
/// circuit breaker (shared singleton state), rate limiter, hedging, timeout, and fallback.
/// Because the pipeline is typed to <see cref="ValueResult"/>, <c>ShouldHandle</c> predicates
/// can inspect both exceptions AND result failures — no exceptions need to escape the
/// handler for retry to work.
/// </para>
///
/// <para>
/// The behavior captures a pre-built <see cref="ResiliencePipeline{ValueResult}"/> instance.
/// For circuit breakers that need shared state across all requests, register the pipeline as a
/// singleton and inject it via <see cref="Microsoft.Extensions.Resilience"/>:
/// <code>
/// services.AddResiliencePipeline&lt;ValueResult&gt;("orders-cb", builder =>
///     builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions&lt;ValueResult&gt;
///     {
///         FailureRatio      = 0.5,
///         SamplingDuration  = TimeSpan.FromSeconds(10),
///         MinimumThroughput = 5,
///         BreakDuration     = TimeSpan.FromSeconds(30),
///         ShouldHandle      = args => ValueTask.FromResult(
///             args.Outcome.Exception is not null ||
///             args.Outcome.Result is { IsSuccess: false })
///     }));
///
/// services.AddResilienceBehavior&lt;PlaceOrderCommand&gt;("orders-cb");
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TRequest">The command type this behavior applies to.</typeparam>
public sealed class ResiliencePipelineBehavior<TRequest>(ResiliencePipeline<ValueResult> pipeline)
    : IPipelineBehavior<TRequest, ValueResult>
{
    /// <inheritdoc/>
    public ValueTask<ValueResult> Handle(
        TRequest request,
        Func<ValueTask<ValueResult>> next,
        CancellationToken cancellationToken = default)
        => pipeline.ExecuteAsync(
            static (state, _) => state(),
            next,
            cancellationToken);
}

/// <summary>
/// Wraps typed-command and query dispatch in a Polly
/// <see cref="ResiliencePipeline{T}"/> where <c>T</c> is <see cref="ValueResult{TResult}"/>.
///
/// <para>
/// <c>ShouldHandle</c> predicates can inspect both exceptions and result failures.
/// Configure via <see cref="Microsoft.Extensions.Resilience"/>:
/// <code>
/// services.AddResiliencePipeline&lt;ValueResult&lt;OrderDto&gt;&gt;("orders-retry", builder =>
///     builder.AddRetry(new RetryStrategyOptions&lt;ValueResult&lt;OrderDto&gt;&gt;
///     {
///         MaxRetryAttempts = 3,
///         UseJitter        = true,
///         ShouldHandle     = args => ValueTask.FromResult(
///             args.Outcome.Exception is not null ||
///             args.Outcome.Result?.Error?.Code == "Transient")
///     }));
///
/// services.AddResilienceBehavior&lt;GetOrderQuery, OrderDto&gt;("orders-retry");
/// </code>
/// </para>
/// </summary>
/// <typeparam name="TRequest">The command or query type this behavior applies to.</typeparam>
/// <typeparam name="TResult">The inner result type.</typeparam>
public sealed class ResiliencePipelineBehavior<TRequest, TResult>(
    ResiliencePipeline<ValueResult<TResult>> pipeline)
    : IPipelineBehavior<TRequest, ValueResult<TResult>>
{
    /// <inheritdoc/>
    public ValueTask<ValueResult<TResult>> Handle(
        TRequest request,
        Func<ValueTask<ValueResult<TResult>>> next,
        CancellationToken cancellationToken = default)
        => pipeline.ExecuteAsync(
            static (state, _) => state(),
            next,
            cancellationToken);
}

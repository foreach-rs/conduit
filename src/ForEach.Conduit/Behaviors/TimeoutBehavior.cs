using ForEach.Conduit.Pipeline;

namespace ForEach.Conduit.Behaviors;

/// <summary>
/// Pipeline behavior that enforces an execution timeout on void commands.
///
/// When the timeout expires before the handler completes, the linked
/// <see cref="CancellationToken"/> is cancelled (signalling the handler to stop)
/// and a <see cref="ValueResult.Failure"/> with an <see cref="Error.Timeout"/> error
/// is returned instead of propagating an exception.
///
/// If the caller's own <see cref="CancellationToken"/> fires first, the
/// <see cref="OperationCanceledException"/> is re-thrown as normal — it is not
/// treated as a timeout.
///
/// Register via the DI helpers:
/// <code>
/// services.AddTimeoutBehavior&lt;PlaceOrderCommand&gt;(TimeSpan.FromSeconds(30));
/// // or via the fluent builder:
/// services.AddConduitHandlers()
///         .AddTimeoutBehavior&lt;PlaceOrderCommand&gt;(TimeSpan.FromSeconds(30));
/// </code>
/// </summary>
/// <typeparam name="TRequest">The command type this behavior applies to.</typeparam>
public sealed class TimeoutBehavior<TRequest>(TimeSpan timeout)
    : IPipelineBehavior<TRequest, ValueResult>
{
    /// <inheritdoc/>
    public async ValueTask<ValueResult> Handle(
        TRequest request,
        Func<ValueTask<ValueResult>> next,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            return await next().AsTask().WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ValueResult.Failure(
                Error.Timeout(
                    $"Handler for {typeof(TRequest).Name} timed out after {(int)timeout.TotalMilliseconds}ms."));
        }
    }
}

/// <summary>
/// Pipeline behavior that enforces an execution timeout on commands and queries
/// that return a typed result.
///
/// When the timeout expires, a <see cref="ValueResult{TResult}.Failure"/> with an
/// <see cref="Error.Timeout"/> error is returned. The caller's cancellation token
/// cancelling is distinguished from a timeout and re-throws as normal.
///
/// Register via the DI helpers:
/// <code>
/// services.AddTimeoutBehavior&lt;GetProductQuery, ProductDto&gt;(TimeSpan.FromSeconds(10));
/// services.AddTimeoutBehavior&lt;CreateOrderCommand, OrderId&gt;(TimeSpan.FromSeconds(30));
/// </code>
/// </summary>
/// <typeparam name="TRequest">The command or query type this behavior applies to.</typeparam>
/// <typeparam name="TResult">The inner result type.</typeparam>
public sealed class TimeoutBehavior<TRequest, TResult>(TimeSpan timeout)
    : IPipelineBehavior<TRequest, ValueResult<TResult>>
{
    /// <inheritdoc/>
    public async ValueTask<ValueResult<TResult>> Handle(
        TRequest request,
        Func<ValueTask<ValueResult<TResult>>> next,
        CancellationToken cancellationToken = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        try
        {
            return await next().AsTask().WaitAsync(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ValueResult<TResult>.Failure(
                Error.Timeout(
                    $"Handler for {typeof(TRequest).Name} timed out after {(int)timeout.TotalMilliseconds}ms."));
        }
    }
}

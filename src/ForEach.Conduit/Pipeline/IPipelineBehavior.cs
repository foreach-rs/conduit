namespace ForEach.Conduit.Pipeline;

/// <summary>
/// Middleware for the dispatch pipeline. Behaviors wrap handler execution and run
/// in registration order — first registered is the outermost (runs first, returns last).
/// </summary>
/// <typeparam name="TRequest">The type of the request (command or query).</typeparam>
/// <typeparam name="TResponse">The type of the response (typically a <see cref="ValueResult"/>).</typeparam>
public interface IPipelineBehavior<in TRequest, TResponse>
{
    /// <summary>
    /// Handles the request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <param name="next">A delegate to invoke the next behavior in the pipeline or the final handler.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{TResponse}"/> representing the result of the operation.</returns>
    ValueTask<TResponse> Handle(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken = default);
}
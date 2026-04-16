namespace ForEach.Conduit.Queries;

/// <summary>
/// Handles a streaming query, returning an asynchronous sequence of <typeparamref name="T"/> items.
/// </summary>
public interface IStreamQueryHandler<in TQuery, out T>
    where TQuery : IStreamQuery
{
    /// <summary>
    /// Handles the streaming query.
    /// </summary>
    /// <param name="query">The streaming query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An asynchronous sequence of items.</returns>
    IAsyncEnumerable<T> Handle(
        TQuery query,
        CancellationToken cancellationToken = default);
}
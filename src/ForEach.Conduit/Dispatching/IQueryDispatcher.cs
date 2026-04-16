using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Dispatching;

/// <summary>
/// Dispatches queries and streaming queries to their single registered handler.
/// </summary>
public interface IQueryDispatcher
{
    /// <summary>Dispatches a query to its single registered handler.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> containing the result of the query.</returns>
    ValueTask<ValueResult<TResult>> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Dispatches a streaming query and returns an async sequence of items.
    /// Errors are signaled via exceptions (standard IAsyncEnumerable contract).
    /// Throws <see cref="InvalidOperationException"/> synchronously if no handler is registered.
    /// </summary>
    /// <typeparam name="T">The type of the items in the stream.</typeparam>
    /// <param name="query">The streaming query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items.</returns>
    IAsyncEnumerable<T> Stream<T>(
        IStreamQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>Task-based convenience wrapper for <see cref="Query{TResult}(IQuery{TResult}, CancellationToken)"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{Result}"/> containing the result of the query.</returns>
    Task<Result<TResult>> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default);
}
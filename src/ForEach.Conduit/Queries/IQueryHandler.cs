namespace ForEach.Conduit.Queries;

/// <summary>
/// Defines a handler for a query that returns a response.
/// </summary>
/// <typeparam name="TQuery">The type of the query.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery<TResponse>
{
    /// <summary>
    /// Handles the query.
    /// </summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> containing the response.</returns>
    ValueTask<ValueResult<TResponse>> Handle(
        TQuery query,
        CancellationToken cancellationToken = default);
}
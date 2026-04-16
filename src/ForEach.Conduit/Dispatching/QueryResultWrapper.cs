using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Dispatching;

internal abstract class QueryResultWrapper<TResult>
{
    public abstract ValueTask<ValueResult<TResult>> Execute(
        IQuery<TResult> query,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}
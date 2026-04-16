using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Dispatching;

internal abstract class StreamQueryWrapper<T>
{
    public abstract IAsyncEnumerable<T> Execute(
        IStreamQuery query,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>
/// Auth-style short-circuit: if <see cref="Blocked"/> is true, returns an empty sequence
/// without calling <c>next</c>. Tracks whether <c>next</c> was invoked.
/// </summary>
internal sealed class ShortCircuitStreamBehavior<TQuery, T>(bool blocked) : IStreamPipelineBehavior<TQuery, T>
    where TQuery : IStreamQuery
{
    public bool NextWasCalled { get; private set; }

    public IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        CancellationToken cancellationToken = default)
    {
        if (blocked)
            return AsyncEnumerable.Empty<T>();

        NextWasCalled = true;
        return next();
    }
}

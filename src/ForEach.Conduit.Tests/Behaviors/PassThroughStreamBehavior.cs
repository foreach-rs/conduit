using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>No-op stream behavior — passes all items through unchanged. Used to test DI registration helpers.</summary>
internal sealed class PassThroughStreamBehavior<TQuery, T> : IStreamPipelineBehavior<TQuery, T>
    where TQuery : IStreamQuery
{
    public bool WasCalled { get; private set; }

    public IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return next();
    }
}

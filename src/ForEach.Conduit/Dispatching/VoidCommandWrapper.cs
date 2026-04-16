using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Dispatching;

internal abstract class VoidCommandWrapper
{
    public abstract ValueTask<ValueResult> Execute(
        ICommand command,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}
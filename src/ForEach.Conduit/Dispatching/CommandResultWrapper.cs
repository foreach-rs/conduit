using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Dispatching;

internal abstract class CommandResultWrapper<TResult>
{
    public abstract ValueTask<ValueResult<TResult>> Execute(
        ICommand<TResult> command,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}
using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class SlowCommandHandler : ICommandHandler<SlowCommand>
{
    public async ValueTask<ValueResult> Handle(SlowCommand command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(command.DelayMs, cancellationToken).ConfigureAwait(false);
        return ValueResult.Success();
    }
}

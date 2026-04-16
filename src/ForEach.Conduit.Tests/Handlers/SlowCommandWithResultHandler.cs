using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class SlowCommandWithResultHandler : ICommandHandler<SlowCommandWithResult, string>
{
    public async ValueTask<ValueResult<string>> Handle(SlowCommandWithResult command, CancellationToken cancellationToken = default)
    {
        await Task.Delay(command.DelayMs, cancellationToken).ConfigureAwait(false);
        return ValueResult<string>.Success("done");
    }
}

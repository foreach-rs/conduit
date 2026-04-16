using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class CancelCheckHandler : ICommandHandler<CancelCheckCommand>
{
    public CancellationToken ReceivedToken { get; private set; }
    public ValueTask<ValueResult> Handle(CancelCheckCommand command, CancellationToken cancellationToken = default)
    {
        ReceivedToken = cancellationToken;
        return ValueTask.FromResult(ValueResult.Success());
    }
}
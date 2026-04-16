using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class PingHandler : ICommandHandler<PingCommand>
{
    public bool WasCalled { get; private set; }
    public ValueTask<ValueResult> Handle(PingCommand command, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return ValueTask.FromResult(ValueResult.Success());
    }
}
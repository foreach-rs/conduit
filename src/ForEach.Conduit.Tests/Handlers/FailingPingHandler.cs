using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class FailingPingHandler : ICommandHandler<PingCommand>
{
    public ValueTask<ValueResult> Handle(PingCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult.Failure(Error.Validation("intentional failure")));
}
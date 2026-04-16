using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class RegCmdHandler : ICommandHandler<RegCommand>
{
    public ValueTask<ValueResult> Handle(RegCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult.Success());
}
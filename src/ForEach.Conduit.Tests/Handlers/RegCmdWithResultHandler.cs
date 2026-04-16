using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class RegCmdWithResultHandler : ICommandHandler<RegCommandWithResult, int>
{
    public ValueTask<ValueResult<int>> Handle(RegCommandWithResult command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<int>.Success(1));
}
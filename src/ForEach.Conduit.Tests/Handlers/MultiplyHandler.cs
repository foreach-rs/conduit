using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class MultiplyHandler : ICommandHandler<MultiplyCommand, int>
{
    public ValueTask<ValueResult<int>> Handle(MultiplyCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<int>.Success(command.Value * 2));
}
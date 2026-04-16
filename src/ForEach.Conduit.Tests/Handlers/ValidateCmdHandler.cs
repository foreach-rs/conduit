using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

public sealed class ValidateCmdHandler : ICommandHandler<ValidateCommand>
{
    public ValueTask<ValueResult> Handle(ValidateCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult.Success());
}
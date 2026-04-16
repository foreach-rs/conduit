using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

public sealed class ValidateCmdWithResultHandler : ICommandHandler<ValidateCommandWithResult, string>
{
    public ValueTask<ValueResult<string>> Handle(ValidateCommandWithResult command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<string>.Success("ok"));
}
using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class EchoHandler : ICommandHandler<EchoCommand, string>
{
    public ValueTask<ValueResult<string>> Handle(EchoCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<string>.Success(command.Text));
}
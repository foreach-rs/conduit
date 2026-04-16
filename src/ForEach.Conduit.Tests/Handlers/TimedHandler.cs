using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class TimedHandler : ICommandHandler<TimedCommand>
{
    public ValueTask<ValueResult> Handle(TimedCommand command, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult.Success());
}
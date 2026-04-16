using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

/// <summary>
/// Returns <see cref="ValueResult.Failure"/> for the first <c>FailCount</c> calls,
/// then returns <see cref="ValueResult.Success"/> on every subsequent call.
/// Thread-safe via <see cref="Interlocked"/>.
/// </summary>
internal sealed class TransientCommandHandler : ICommandHandler<TransientCommand>
{
    private int _callCount;

    public ValueTask<ValueResult> Handle(TransientCommand command, CancellationToken cancellationToken = default)
    {
        var count = Interlocked.Increment(ref _callCount);
        return count <= command.FailCount
            ? ValueTask.FromResult(ValueResult.Failure(new Error("Transient", $"Transient failure #{count}")))
            : ValueTask.FromResult(ValueResult.Success());
    }
}

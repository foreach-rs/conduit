using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

/// <summary>
/// Returns <see cref="ValueResult{T}.Failure"/> for the first <c>FailCount</c> calls,
/// then returns <see cref="ValueResult{T}.Success"/> with <c>"done"</c>.
/// Thread-safe via <see cref="Interlocked"/>.
/// </summary>
internal sealed class TransientCommandWithResultHandler : ICommandHandler<TransientCommandWithResult, string>
{
    private int _callCount;

    public ValueTask<ValueResult<string>> Handle(TransientCommandWithResult command, CancellationToken cancellationToken = default)
    {
        var count = Interlocked.Increment(ref _callCount);
        return count <= command.FailCount
            ? ValueTask.FromResult(ValueResult<string>.Failure(new Error("Transient", $"Transient failure #{count}")))
            : ValueTask.FromResult(ValueResult<string>.Success("done"));
    }
}

using ForEach.Conduit.Commands;
using ForEach.Conduit.Tests.Commands;

namespace ForEach.Conduit.Tests.Handlers;

/// <summary>
/// Throws <see cref="InvalidOperationException"/> for the first <c>ThrowCount</c> calls,
/// then returns <see cref="ValueResult.Success"/>.
/// Thread-safe via <see cref="Interlocked"/>.
/// </summary>
internal sealed class ThrowingTransientCommandHandler : ICommandHandler<ThrowingTransientCommand>
{
    private int _callCount;

    public ValueTask<ValueResult> Handle(ThrowingTransientCommand command, CancellationToken cancellationToken = default)
    {
        var count = Interlocked.Increment(ref _callCount);
        if (count <= command.ThrowCount)
            throw new InvalidOperationException($"Transient exception #{count}");

        return ValueTask.FromResult(ValueResult.Success());
    }
}

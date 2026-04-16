using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

/// <summary>
/// Void command whose handler throws <see cref="InvalidOperationException"/> for the first
/// <paramref name="ThrowCount"/> calls, then succeeds.
/// </summary>
internal record ThrowingTransientCommand(int ThrowCount = 2) : ICommand;

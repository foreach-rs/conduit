using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

/// <summary>Typed command whose handler delays for <paramref name="DelayMs"/> milliseconds then returns "done".</summary>
internal record SlowCommandWithResult(int DelayMs = 2000) : ICommand<string>;

using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

/// <summary>Typed command whose handler fails <paramref name="FailCount"/> times before succeeding.</summary>
internal record TransientCommandWithResult(int FailCount = 2) : ICommand<string>;

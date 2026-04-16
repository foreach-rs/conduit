using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

/// <summary>Void command whose handler fails <paramref name="FailCount"/> times before succeeding.</summary>
internal record TransientCommand(int FailCount = 2) : ICommand;

using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

/// <summary>Void command whose handler delays for <paramref name="DelayMs"/> milliseconds.</summary>
internal record SlowCommand(int DelayMs = 2000) : ICommand;

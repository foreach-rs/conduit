using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

internal record EchoCommand(string Text) : ICommand<string>;
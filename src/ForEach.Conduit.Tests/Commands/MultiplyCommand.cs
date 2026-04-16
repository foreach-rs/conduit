using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Tests.Commands;

internal record MultiplyCommand(int Value) : ICommand<int>;
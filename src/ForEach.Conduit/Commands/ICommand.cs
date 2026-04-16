namespace ForEach.Conduit.Commands;

/// <summary>
/// Marker interface to represent a command.
/// </summary>
public interface ICommand;

/// <summary>
/// Marker interface to represent a command with a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommand<out TResponse> : ICommand;
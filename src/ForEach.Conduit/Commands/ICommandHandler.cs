namespace ForEach.Conduit.Commands;

/// <summary>
/// Defines a handler for a command that does not return a value.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> representing the result of the operation.</returns>
    ValueTask<ValueResult> Handle(
        TCommand command,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Defines a handler for a command that returns a response.
/// </summary>
/// <typeparam name="TCommand">The type of the command.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface ICommandHandler<in TCommand, TResponse>
    where TCommand : ICommand<TResponse>
{
    /// <summary>
    /// Handles the command.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> containing the response.</returns>
    ValueTask<ValueResult<TResponse>> Handle(
        TCommand command,
        CancellationToken cancellationToken = default);
}
using ForEach.Conduit.Commands;

namespace ForEach.Conduit.Dispatching;

/// <summary>
/// Dispatches commands to their single registered handler.
/// </summary>
public interface ICommandDispatcher
{
    /// <summary>Dispatches a void command to its single registered handler.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> representing the result of the operation.</returns>
    ValueTask<ValueResult> Send(
        ICommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Dispatches a command that returns a value to its single registered handler.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask{ValueResult}"/> containing the response.</returns>
    ValueTask<ValueResult<TResult>> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);

    /// <summary>Task-based convenience wrapper for <see cref="Send(ICommand, CancellationToken)"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{Result}"/> representing the result of the operation.</returns>
    Task<Result> SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default);

    /// <summary>Task-based convenience wrapper for <see cref="Send{TResult}(ICommand{TResult}, CancellationToken)"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <param name="command">The command.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task{Result}"/> containing the response.</returns>
    Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default);
}
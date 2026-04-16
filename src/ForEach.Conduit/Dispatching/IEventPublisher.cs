using ForEach.Conduit.Notifications;

namespace ForEach.Conduit.Dispatching;

/// <summary>
/// Publishes notifications (domain events) to all registered handlers.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a notification to all registered handlers sequentially.
    /// Handlers run in registration order; the first failure stops the chain and propagates.
    /// It is not an error to publish when no handlers are registered.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask Publish(
        INotification notification,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Publishes a notification to all registered handlers in parallel.
    /// All handlers run concurrently. Throws <see cref="AggregateException"/> if any fail;
    /// each inner exception is wrapped with the failing handler's type name for easy diagnosis.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task PublishParallel(
        INotification notification,
        CancellationToken cancellationToken = default);
}
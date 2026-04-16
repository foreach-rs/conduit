namespace ForEach.Conduit.Notifications;

/// <summary>
/// Handles a specific notification type. Multiple handlers for the same notification type
/// are all invoked — register as many as needed without conflict.
/// </summary>
public interface INotificationHandler<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the asynchronous operation.</returns>
    ValueTask Handle(
        TNotification notification,
        CancellationToken cancellationToken = default);
}
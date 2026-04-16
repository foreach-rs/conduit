using ForEach.Conduit.Notifications;

namespace ForEach.Conduit.Pipeline;

/// <summary>
/// Defines middleware that wraps the entire notification fan-out for a specific notification type.
///
/// <para>
/// <c>next</c> invokes all registered <see cref="INotificationHandler{TNotification}"/> instances
/// (sequentially for <c>Publish</c>, concurrently for <c>PublishParallel</c>).
/// Behaviors are applied in registration order; inner-most behavior calls <c>next</c> last.
/// </para>
///
/// <para>
/// Register open-generic behaviors (e.g. <c>LoggingNotificationBehavior&lt;&gt;</c>) via
/// <c>AddNotificationPipelineBehavior(typeof(LoggingNotificationBehavior&lt;&gt;))</c> to apply
/// them to every notification type. Register closed behaviors to target a specific type.
/// </para>
/// </summary>
public interface INotificationPipelineBehavior<in TNotification>
    where TNotification : INotification
{
    /// <summary>
    /// Handles the notification.
    /// </summary>
    /// <param name="notification">The notification.</param>
    /// <param name="next">A delegate to invoke the next behavior in the pipeline or the notification handlers.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="ValueTask"/> representing the result of the operation.</returns>
    ValueTask Handle(
        TNotification notification,
        Func<ValueTask> next,
        CancellationToken cancellationToken = default);
}
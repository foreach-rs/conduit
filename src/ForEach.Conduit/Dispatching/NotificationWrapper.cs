using ForEach.Conduit.Notifications;

namespace ForEach.Conduit.Dispatching;

internal abstract class NotificationWrapper
{
    /// <summary>Sequential publish — awaits each handler in order. First failure stops the chain.</summary>
    public abstract ValueTask Publish(
        INotification notification,
        IServiceProvider services,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Parallel publish — all handlers run concurrently. All complete before returning.
    /// Throws <see cref="AggregateException"/> if one or more handlers fail.
    /// </summary>
    public abstract Task PublishParallel(
        INotification notification,
        IServiceProvider services,
        CancellationToken cancellationToken = default);
}
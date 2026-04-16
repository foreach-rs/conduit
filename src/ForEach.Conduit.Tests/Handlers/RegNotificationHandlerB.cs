using ForEach.Conduit.Notifications;
using ForEach.Conduit.Tests.Notifications;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class RegNotificationHandlerB : INotificationHandler<RegNotification>
{
    public ValueTask Handle(RegNotification notification, CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Tests.Events;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class ThrowingEventHandler : INotificationHandler<UserCreatedEvent>
{
    public ValueTask Handle(UserCreatedEvent notification, CancellationToken cancellationToken = default) =>
        throw new InvalidOperationException("handler exploded");
}
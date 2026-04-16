using ForEach.Conduit.Notifications;
using ForEach.Conduit.Tests.Events;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class AuditedEventHandler : INotificationHandler<AuditedEvent>
{
    public bool WasCalled { get; private set; }
    public ValueTask Handle(AuditedEvent notification, CancellationToken cancellationToken = default)
    {
        WasCalled = true;
        return ValueTask.CompletedTask;
    }
}
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Tests.Events;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class FirstEventHandler : INotificationHandler<UserCreatedEvent>
{
    public List<string> Calls { get; } = [];
    public ValueTask Handle(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"first:{notification.Name}");
        return ValueTask.CompletedTask;
    }
}
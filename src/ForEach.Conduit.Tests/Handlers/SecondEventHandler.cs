using ForEach.Conduit.Notifications;
using ForEach.Conduit.Tests.Events;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class SecondEventHandler : INotificationHandler<UserCreatedEvent>
{
    public List<string> Calls { get; } = [];
    public ValueTask Handle(UserCreatedEvent notification, CancellationToken cancellationToken = default)
    {
        Calls.Add($"second:{notification.Name}");
        return ValueTask.CompletedTask;
    }
}
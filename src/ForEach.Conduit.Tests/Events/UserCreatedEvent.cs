using ForEach.Conduit.Notifications;

namespace ForEach.Conduit.Tests.Events;

internal record UserCreatedEvent(string Name) : INotification;
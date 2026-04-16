namespace ForEach.Conduit.Notifications;

/// <summary>
/// Marker interface for notifications (domain events, integration events, fan-out messages).
///
/// Unlike commands and queries which route to exactly one handler, a notification is
/// published to zero or more handlers. Missing handlers are not an error.
/// </summary>
public interface INotification;
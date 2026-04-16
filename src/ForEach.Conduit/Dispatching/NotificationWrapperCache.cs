using System.Collections.Concurrent;

namespace ForEach.Conduit.Dispatching;

internal static class NotificationWrapperCache
{
    private static readonly ConcurrentDictionary<Type, NotificationWrapper> Cache = new();

    public static NotificationWrapper GetOrCreate(
        Type notificationType) =>
        Cache.GetOrAdd(
            notificationType,
            t =>
            {
                var wrapperType = typeof(NotificationWrapper<>).MakeGenericType(t);
                return (NotificationWrapper)Activator.CreateInstance(wrapperType)!;
            });
}
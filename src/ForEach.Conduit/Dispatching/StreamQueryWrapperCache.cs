using System.Collections.Concurrent;

namespace ForEach.Conduit.Dispatching;

internal static class StreamQueryWrapperCache<T>
{
    private static readonly ConcurrentDictionary<Type, StreamQueryWrapper<T>> Cache = new();

    public static StreamQueryWrapper<T> GetOrCreate(
        Type queryType) =>
        Cache.GetOrAdd(
            queryType,
            t =>
            {
                var wrapperType = typeof(StreamQueryWrapper<,>).MakeGenericType(
                    t,
                    typeof(T));
                return (StreamQueryWrapper<T>)Activator.CreateInstance(wrapperType)!;
            });
}
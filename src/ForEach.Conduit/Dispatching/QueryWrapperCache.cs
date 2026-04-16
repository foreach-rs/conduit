using System.Collections.Concurrent;

namespace ForEach.Conduit.Dispatching;

internal static class QueryWrapperCache<TResult>
{
    private static readonly ConcurrentDictionary<Type, QueryResultWrapper<TResult>> Cache = new();

    public static QueryResultWrapper<TResult> GetOrCreate(
        Type queryType) =>
        Cache.GetOrAdd(
            queryType,
            t =>
            {
                var wrapperType = typeof(QueryResultWrapper<,>).MakeGenericType(
                    t,
                    typeof(TResult));
                return (QueryResultWrapper<TResult>)Activator.CreateInstance(wrapperType)!;
            });
}
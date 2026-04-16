using System.Collections.Concurrent;

namespace ForEach.Conduit.Dispatching;

internal static class CommandWrapperCache<TResult>
{
    private static readonly ConcurrentDictionary<Type, CommandResultWrapper<TResult>> Cache = new();

    public static CommandResultWrapper<TResult> GetOrCreate(
        Type commandType) =>
        Cache.GetOrAdd(
            commandType,
            t =>
            {
                var wrapperType = typeof(CommandResultWrapper<,>).MakeGenericType(
                    t,
                    typeof(TResult));
                return (CommandResultWrapper<TResult>)Activator.CreateInstance(wrapperType)!;
            });
}
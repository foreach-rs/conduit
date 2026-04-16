using System.Runtime.CompilerServices;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>
/// Records how many items flowed through the stream and adds "before"/"after" markers to a log.
/// </summary>
internal sealed class CountingStreamBehavior<TQuery, T>(List<string> log, string name)
    : IStreamPipelineBehavior<TQuery, T>
    where TQuery : IStreamQuery
{
    public async IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        log.Add($"{name}:before");
        var count = 0;
        await foreach (var item in next().WithCancellation(cancellationToken).ConfigureAwait(false))
        {
            count++;
            yield return item;
        }
        log.Add($"{name}:after:{count}");
    }
}

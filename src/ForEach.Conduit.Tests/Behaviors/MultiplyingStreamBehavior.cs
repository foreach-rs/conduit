using System.Runtime.CompilerServices;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>Per-item transformation: multiplies each <c>int</c> item by a factor.</summary>
internal sealed class MultiplyingStreamBehavior<TQuery>(int factor)
    : IStreamPipelineBehavior<TQuery, int>
    where TQuery : IStreamQuery
{
    public async IAsyncEnumerable<int> Handle(
        TQuery query,
        Func<IAsyncEnumerable<int>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in next().WithCancellation(cancellationToken).ConfigureAwait(false))
            yield return item * factor;
    }
}

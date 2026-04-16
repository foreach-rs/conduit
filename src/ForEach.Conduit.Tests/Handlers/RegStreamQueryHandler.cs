using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class RegStreamQueryHandler : IStreamQueryHandler<RegStreamQuery, int>
{
    public async IAsyncEnumerable<int> Handle(RegStreamQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return 1;
        await Task.Yield();
    }
}
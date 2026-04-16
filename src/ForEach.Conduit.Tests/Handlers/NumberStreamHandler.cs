using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class NumberStreamHandler : IStreamQueryHandler<NumberStreamQuery, int>
{
    public async IAsyncEnumerable<int> Handle(NumberStreamQuery query, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= 3; i++)
        {
            yield return i;
            await Task.Yield();
        }
    }
}
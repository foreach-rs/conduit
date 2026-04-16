using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class SlowQueryHandler : IQueryHandler<SlowQuery, int>
{
    public async ValueTask<ValueResult<int>> Handle(SlowQuery query, CancellationToken cancellationToken = default)
    {
        await Task.Delay(query.DelayMs, cancellationToken).ConfigureAwait(false);
        return ValueResult<int>.Success(42);
    }
}

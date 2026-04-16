using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class GetNumberHandler : IQueryHandler<GetNumberQuery, int>
{
    public ValueTask<ValueResult<int>> Handle(GetNumberQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<int>.Success(query.Value));
}
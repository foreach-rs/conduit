using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

public sealed class ValidateQueryHandler : IQueryHandler<ValidateQuery, int>
{
    public ValueTask<ValueResult<int>> Handle(ValidateQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<int>.Success(1));
}
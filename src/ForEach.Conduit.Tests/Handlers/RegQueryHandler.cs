using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class RegQueryHandler : IQueryHandler<RegQuery, string>
{
    public ValueTask<ValueResult<string>> Handle(RegQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<string>.Success("ok"));
}
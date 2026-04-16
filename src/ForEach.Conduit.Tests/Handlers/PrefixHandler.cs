using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests.Handlers;

internal sealed class PrefixHandler : IQueryHandler<PrefixQuery, string>
{
    public ValueTask<ValueResult<string>> Handle(PrefixQuery query, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult<string>.Success($"result:{query.Text}"));
}
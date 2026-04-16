using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Queries;

internal record PrefixQuery(string Text) : IQuery<string>;
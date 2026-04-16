using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Queries;

internal record GetNumberQuery(int Value) : IQuery<int>;
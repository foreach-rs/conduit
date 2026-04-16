using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Tests.Queries;

/// <summary>Query whose handler delays for <paramref name="DelayMs"/> milliseconds then returns 42.</summary>
internal record SlowQuery(int DelayMs = 2000) : IQuery<int>;

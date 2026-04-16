namespace ForEach.Conduit.Queries;

/// <summary>
/// Marker interface for streaming queries — queries that return an asynchronous sequence of items.
/// Use when the result set is too large to buffer entirely in memory, or when you want to
/// start processing results before the full set is available.
///
/// Unlike <see cref="IQuery{TResponse}"/>, streaming queries return
/// <see cref="IAsyncEnumerable{T}"/> directly without a Result wrapper.
/// Errors are signalled via exceptions (the standard C# async-enumerable contract).
/// </summary>
public interface IStreamQuery;
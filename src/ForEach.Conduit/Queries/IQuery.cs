namespace ForEach.Conduit.Queries;

/// <summary>
/// Marker interface to represent a query.
/// </summary>
public interface IQuery;

/// <summary>
/// Marker interface to represent a query with a response.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IQuery<out TResponse> : IQuery;
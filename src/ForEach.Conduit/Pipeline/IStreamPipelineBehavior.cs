using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Pipeline;

/// <summary>
/// Middleware for the streaming dispatch pipeline. Behaviors wrap the entire
/// <see cref="IAsyncEnumerable{T}"/> produced by the handler and run in registration
/// order — first registered is the outermost (starts first, finishes last).
///
/// <para>
/// <c>next()</c> returns the inner <see cref="IAsyncEnumerable{T}"/>.
/// Behaviors can intercept the stream before it starts, enumerate and transform items,
/// or short-circuit by returning an empty (or alternative) sequence without calling <c>next</c>.
/// </para>
///
/// <para>
/// Typical patterns:
/// <list type="bullet">
///   <item>Logging — count items yielded, record timing around enumeration.</item>
///   <item>Auth check — inspect the query before calling <c>next</c>; return
///       <c>AsyncEnumerable.Empty&lt;T&gt;()</c> or throw if unauthorized.</item>
///   <item>Per-item transformation — <c>await foreach</c> over <c>next()</c>
///       and <c>yield return</c> transformed items.</item>
///   <item>Deduplication / filtering — skip items matching a predicate.</item>
/// </list>
/// </para>
///
/// <para>
/// Implementations that enumerate the inner sequence must use <c>async IAsyncEnumerable&lt;T&gt;</c>
/// and mark the <see cref="CancellationToken"/> parameter with
/// <c>[<see cref="System.Runtime.CompilerServices.EnumeratorCancellationAttribute"/>]</c>.
/// </para>
///
/// Register via the DI helpers:
/// <code>
/// // Open-generic — applies to every stream query:
/// services.AddStreamPipelineBehavior(typeof(LoggingStreamBehavior&lt;,&gt;));
///
/// // Closed — applies to one stream query type only:
/// services.AddStreamPipelineBehavior&lt;ProductsQuery, Product, AuthStreamBehavior&gt;();
/// </code>
/// </summary>
/// <typeparam name="TQuery">The streaming query type.</typeparam>
/// <typeparam name="T">The item type produced by the stream.</typeparam>
public interface IStreamPipelineBehavior<in TQuery, T>
    where TQuery : IStreamQuery
{
    /// <summary>
    /// Handles the streaming query.
    /// </summary>
    /// <param name="query">The streaming query.</param>
    /// <param name="next">A delegate returning the next sequence in the pipeline or the handler's sequence.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of items.</returns>
    IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        CancellationToken cancellationToken = default);
}

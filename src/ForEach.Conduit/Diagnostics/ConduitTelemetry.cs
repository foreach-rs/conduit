using System.Diagnostics;
using System.Reflection;

namespace ForEach.Conduit.Diagnostics;

/// <summary>
/// OpenTelemetry / ActivitySource integration point for ForEach.Conduit.
///
/// The dispatcher automatically starts an <see cref="Activity"/> per dispatch when a
/// listener is registered. When no listener is attached (the default in non-OTel apps),
/// returns <see langword="null"/> immediately
/// and the dispatcher takes a zero-overhead fast path — no async state machine, no allocs.
///
/// Setup with OpenTelemetry SDK:
/// <code>
/// builder.Services.AddOpenTelemetry()
///     .WithTracing(t => t.AddSource(ConduitTelemetry.SourceName));
/// </code>
///
/// Available tags on every span:
/// <list type="bullet">
///   <item><c>conduit.request.type</c> — fully-qualified request type name</item>
///   <item><c>conduit.request.name</c> — simple type name (used as span name)</item>
///   <item><c>conduit.operation</c>    — "send", "query", "publish", or "publish.parallel"</item>
///   <item><c>error.type</c>           — error Code on failure (follows OTel conventions)</item>
/// </list>
/// </summary>
public static class ConduitTelemetry
{
    /// <summary>The source name to pass to <c>AddSource()</c> when configuring OpenTelemetry.</summary>
    private const string SourceName = "ForEach.Conduit";

    /// <summary>
    /// The <see cref="ActivitySource"/> used by the dispatcher.
    /// Also available for creating manual spans inside pipeline behaviors.
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(
        SourceName,
        Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0");
}
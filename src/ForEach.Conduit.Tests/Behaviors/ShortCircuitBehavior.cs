using ForEach.Conduit.Pipeline;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>Short-circuits without calling next, returning a fixed failure.</summary>
internal sealed class ShortCircuitBehavior<TReq> : IPipelineBehavior<TReq, ValueResult>
{
    public static readonly Error ShortCircuitError = new("ShortCircuit", "blocked by behavior");

    public ValueTask<ValueResult> Handle(TReq request, Func<ValueTask<ValueResult>> next, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(ValueResult.Failure(ShortCircuitError));
}
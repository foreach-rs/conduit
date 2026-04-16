using ForEach.Conduit.Pipeline;

namespace ForEach.Conduit.Tests.Behaviors;

internal sealed class RegPipelineBehavior<TReq, TResp> : IPipelineBehavior<TReq, TResp>
{
    public ValueTask<TResp> Handle(TReq request, Func<ValueTask<TResp>> next, CancellationToken cancellationToken = default) =>
        next();
}
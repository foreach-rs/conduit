using ForEach.Conduit.Pipeline;

namespace ForEach.Conduit.Tests.Behaviors;

/// <summary>Records the order in which it was called into a shared list.</summary>
internal sealed class OrderRecordingBehavior<TReq, TResp>(List<string> callLog, string name)
    : IPipelineBehavior<TReq, TResp>
{
    public async ValueTask<TResp> Handle(TReq request, Func<ValueTask<TResp>> next, CancellationToken cancellationToken = default)
    {
        callLog.Add($"{name}:before");
        var result = await next();
        callLog.Add($"{name}:after");
        return result;
    }
}
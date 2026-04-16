using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Tests.Events;

namespace ForEach.Conduit.Tests.Behaviors;

internal sealed class NotificationOrderBehavior(List<string> callLog, string name)
    : INotificationPipelineBehavior<AuditedEvent>
{
    public async ValueTask Handle(AuditedEvent notification, Func<ValueTask> next, CancellationToken cancellationToken = default)
    {
        callLog.Add($"{name}:before");
        await next();
        callLog.Add($"{name}:after");
    }
}
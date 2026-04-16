using System.Collections.Concurrent;
using System.Diagnostics;
using ForEach.Conduit.Commands;
using ForEach.Conduit.Diagnostics;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.Dispatching;

/// <summary>
/// Main dispatcher implementation — the heart of ForEach.Conduit.
///
/// Hot-path dispatch is reflection-free after the first call per request type.
/// A strongly typed wrapper is constructed once via MakeGenericType + Activator.CreateInstance
/// and cached. Every later call uses virtual dispatch — no MethodInfo.Invoke, no boxed
/// object[], no boxed return value.
///
/// OpenTelemetry: when no ActivitySource listener is attached (the default), StartActivity
/// returns null and dispatch takes the zero-overhead fast path. When a listener IS attached,
/// a span is created per dispatch, tagged with the request type and error state.
/// </summary>
internal sealed class ConduitDispatcher : IDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    private static readonly ConcurrentDictionary<Type, VoidCommandWrapper> _voidWrappers = new();
    private static readonly ActivitySource _activity = ConduitTelemetry.ActivitySource;

    internal ConduitDispatcher(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ValueTask<ValueResult> Send(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandType = command.GetType();
        var wrapper = _voidWrappers.GetOrAdd(
            commandType,
            CreateVoidWrapper);

        var span = _activity.StartActivity(commandType.Name);
        if (span is null)
            return wrapper.Execute(
                command,
                _serviceProvider,
                cancellationToken);

        span.SetTag(
            "conduit.operation",
            "send");
        span.SetTag(
            "conduit.request.type",
            commandType.FullName);
        span.SetTag(
            "conduit.request.name",
            commandType.Name);
        return TraceVoidResult(
            wrapper.Execute(
                command,
                _serviceProvider,
                cancellationToken),
            span);
    }

    public ValueTask<ValueResult<TResult>> Send<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        var commandType = command.GetType();
        var wrapper = CommandWrapperCache<TResult>.GetOrCreate(commandType);

        var span = _activity.StartActivity(commandType.Name);
        if (span is null)
            return wrapper.Execute(
                command,
                _serviceProvider,
                cancellationToken);

        span.SetTag(
            "conduit.operation",
            "send");
        span.SetTag(
            "conduit.request.type",
            commandType.FullName);
        span.SetTag(
            "conduit.request.name",
            commandType.Name);
        return TraceResult(
            wrapper.Execute(
                command,
                _serviceProvider,
                cancellationToken),
            span);
    }

    public ValueTask<ValueResult<TResult>> Query<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        var queryType = query.GetType();
        var wrapper = QueryWrapperCache<TResult>.GetOrCreate(queryType);

        var span = _activity.StartActivity(queryType.Name);
        if (span is null)
            return wrapper.Execute(
                query,
                _serviceProvider,
                cancellationToken);

        span.SetTag(
            "conduit.operation",
            "query");
        span.SetTag(
            "conduit.request.type",
            queryType.FullName);
        span.SetTag(
            "conduit.request.name",
            queryType.Name);
        return TraceResult(
            wrapper.Execute(
                query,
                _serviceProvider,
                cancellationToken),
            span);
    }

    public IAsyncEnumerable<T> Stream<T>(
        IStreamQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        // No activity span — IAsyncEnumerable is lazy; the span would close before enumeration
        // starts. Add tracing inside the handler or via a wrapping async iterator instead.
        return StreamQueryWrapperCache<T>.GetOrCreate(query.GetType())
            .Execute(
                query,
                _serviceProvider,
                cancellationToken);
    }

    public ValueTask Publish(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var notifType = notification.GetType();
        var wrapper = NotificationWrapperCache.GetOrCreate(notifType);

        var span = _activity.StartActivity(notifType.Name);
        if (span is null)
            return wrapper.Publish(
                notification,
                _serviceProvider,
                cancellationToken);

        span.SetTag(
            "conduit.operation",
            "publish");
        span.SetTag(
            "conduit.request.type",
            notifType.FullName);
        span.SetTag(
            "conduit.request.name",
            notifType.Name);
        return TracePublish(
            wrapper.Publish(
                notification,
                _serviceProvider,
                cancellationToken),
            span);
    }

    public Task PublishParallel(
        INotification notification,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(notification);
        var notifType = notification.GetType();
        var wrapper = NotificationWrapperCache.GetOrCreate(notifType);

        var span = _activity.StartActivity(notifType.Name);
        if (span is null)
            return wrapper.PublishParallel(
                notification,
                _serviceProvider,
                cancellationToken);

        span.SetTag(
            "conduit.operation",
            "publish.parallel");
        span.SetTag(
            "conduit.request.type",
            notifType.FullName);
        span.SetTag(
            "conduit.request.name",
            notifType.Name);
        return TraceParallelPublish(
            wrapper.PublishParallel(
                notification,
                _serviceProvider,
                cancellationToken),
            span);
    }

    public async Task<Result> SendAsync(
        ICommand command,
        CancellationToken cancellationToken = default)
    {
        var r = await Send(
            command,
            cancellationToken).ConfigureAwait(false);
        return r.IsSuccess ? Result.Success() : Result.Failure(r.Error!.Value);
    }

    public async Task<Result<TResult>> SendAsync<TResult>(
        ICommand<TResult> command,
        CancellationToken cancellationToken = default)
    {
        var r = await Send(
            command,
            cancellationToken).ConfigureAwait(false);
        return r.IsSuccess ? Result<TResult>.Success(r.Value!) : Result<TResult>.Failure(r.Error!.Value);
    }

    public async Task<Result<TResult>> QueryAsync<TResult>(
        IQuery<TResult> query,
        CancellationToken cancellationToken = default)
    {
        var r = await Query(
            query,
            cancellationToken).ConfigureAwait(false);
        return r.IsSuccess ? Result<TResult>.Success(r.Value!) : Result<TResult>.Failure(r.Error!.Value);
    }

    private static async ValueTask<ValueResult> TraceVoidResult(
        ValueTask<ValueResult> task,
        Activity span)
    {
        using (span)
        {
            var result = await task.ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                span.SetStatus(
                    ActivityStatusCode.Error,
                    result.Error?.ToString());
                span.SetTag(
                    "error.type",
                    result.Error?.Code);
            }

            return result;
        }
    }

    private static async ValueTask<ValueResult<T>> TraceResult<T>(
        ValueTask<ValueResult<T>> task,
        Activity span)
    {
        using (span)
        {
            var result = await task.ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                span.SetStatus(
                    ActivityStatusCode.Error,
                    result.Error?.ToString());
                span.SetTag(
                    "error.type",
                    result.Error?.Code);
            }

            return result;
        }
    }

    private static async ValueTask TracePublish(
        ValueTask task,
        Activity span)
    {
        using (span)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                span.SetStatus(
                    ActivityStatusCode.Error,
                    ex.Message);
                span.SetTag(
                    "error.type",
                    ex.GetType().Name);
                throw;
            }
        }
    }

    private static async Task TraceParallelPublish(
        Task task,
        Activity span)
    {
        using (span)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                span.SetStatus(
                    ActivityStatusCode.Error,
                    ex.Message);
                span.SetTag(
                    "error.type",
                    ex.GetType().Name);
                throw;
            }
        }
    }

    private static VoidCommandWrapper CreateVoidWrapper(
        Type commandType)
    {
        var wrapperType = typeof(VoidCommandWrapper<>).MakeGenericType(commandType);
        return (VoidCommandWrapper)Activator.CreateInstance(wrapperType)!;
    }
}
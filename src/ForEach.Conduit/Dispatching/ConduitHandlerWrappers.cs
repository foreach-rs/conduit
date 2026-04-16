using ForEach.Conduit.Commands;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace ForEach.Conduit.Dispatching;

internal sealed class VoidCommandWrapper<TCommand> : VoidCommandWrapper
    where TCommand : ICommand
{
    public override ValueTask<ValueResult> Execute(
        ICommand command,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var handler = services.GetService<ICommandHandler<TCommand>>();
        if (handler is null)
            return ValueTask.FromResult(
                ValueResult.Failure(
                    new Error(
                        "HandlerNotFound",
                        $"No handler registered for {typeof(TCommand).Name}")));

        var behaviors = services.GetServices<IPipelineBehavior<TCommand, ValueResult>>().ToArray();

        if (behaviors.Length == 0)
            return handler.Handle(
                (TCommand)command,
                cancellationToken);

        return RunPipeline(
            handler,
            (TCommand)command,
            behaviors,
            cancellationToken);
    }

    private static async ValueTask<ValueResult> RunPipeline(
        ICommandHandler<TCommand> handler,
        TCommand command,
        IPipelineBehavior<TCommand, ValueResult>[] behaviors,
        CancellationToken cancellationToken = default)
    {
        Func<ValueTask<ValueResult>> pipeline = () => handler.Handle(
            command,
            cancellationToken);
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = pipeline;
            var behavior = behaviors[i];
            pipeline = () => behavior.Handle(
                command,
                next,
                cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }
}

internal sealed class CommandResultWrapper<TCommand, TResult> : CommandResultWrapper<TResult>
    where TCommand : ICommand<TResult>
{
    public override ValueTask<ValueResult<TResult>> Execute(
        ICommand<TResult> command,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var handler = services.GetService<ICommandHandler<TCommand, TResult>>();
        if (handler is null)
            return ValueTask.FromResult(
                ValueResult<TResult>.Failure(
                    new Error(
                        "HandlerNotFound",
                        $"No handler registered for {typeof(TCommand).Name}")));

        var behaviors = services.GetServices<IPipelineBehavior<TCommand, ValueResult<TResult>>>().ToArray();

        if (behaviors.Length == 0)
            return handler.Handle(
                (TCommand)command,
                cancellationToken);

        return RunPipeline(
            handler,
            (TCommand)command,
            behaviors,
            cancellationToken);
    }

    private static async ValueTask<ValueResult<TResult>> RunPipeline(
        ICommandHandler<TCommand, TResult> handler,
        TCommand command,
        IPipelineBehavior<TCommand, ValueResult<TResult>>[] behaviors,
        CancellationToken cancellationToken = default)
    {
        Func<ValueTask<ValueResult<TResult>>> pipeline = () => handler.Handle(
            command,
            cancellationToken);
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = pipeline;
            var behavior = behaviors[i];
            pipeline = () => behavior.Handle(
                command,
                next,
                cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }
}

internal sealed class QueryResultWrapper<TQuery, TResult> : QueryResultWrapper<TResult>
    where TQuery : IQuery<TResult>
{
    public override ValueTask<ValueResult<TResult>> Execute(
        IQuery<TResult> query,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var handler = services.GetService<IQueryHandler<TQuery, TResult>>();
        if (handler is null)
            return ValueTask.FromResult(
                ValueResult<TResult>.Failure(
                    new Error(
                        "HandlerNotFound",
                        $"No handler registered for {typeof(TQuery).Name}")));

        var behaviors = services.GetServices<IPipelineBehavior<TQuery, ValueResult<TResult>>>().ToArray();

        if (behaviors.Length == 0)
            return handler.Handle(
                (TQuery)query,
                cancellationToken);

        return RunPipeline(
            handler,
            (TQuery)query,
            behaviors,
            cancellationToken);
    }

    private static async ValueTask<ValueResult<TResult>> RunPipeline(
        IQueryHandler<TQuery, TResult> handler,
        TQuery query,
        IPipelineBehavior<TQuery, ValueResult<TResult>>[] behaviors,
        CancellationToken cancellationToken = default)
    {
        Func<ValueTask<ValueResult<TResult>>> pipeline = () => handler.Handle(
            query,
            cancellationToken);
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = pipeline;
            var behavior = behaviors[i];
            pipeline = () => behavior.Handle(
                query,
                next,
                cancellationToken);
        }

        return await pipeline().ConfigureAwait(false);
    }
}

internal sealed class NotificationWrapper<TNotification> : NotificationWrapper
    where TNotification : INotification
{
    public override ValueTask Publish(
        INotification notification,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var typed = (TNotification)notification;
        var behaviors = services.GetServices<INotificationPipelineBehavior<TNotification>>().ToArray();

        Func<ValueTask> fanOut = () => SequentialFanOut(
            typed,
            services,
            cancellationToken);

        if (behaviors.Length == 0)
            return fanOut();

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = fanOut;
            var behavior = behaviors[i];
            fanOut = () => behavior.Handle(
                typed,
                next,
                cancellationToken);
        }

        return fanOut();
    }

    public override Task PublishParallel(
        INotification notification,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var typed = (TNotification)notification;
        var behaviors = services.GetServices<INotificationPipelineBehavior<TNotification>>().ToArray();

        Func<ValueTask> fanOut = () => new ValueTask(
            ParallelFanOut(
                typed,
                services,
                cancellationToken));

        if (behaviors.Length == 0)
            return ParallelFanOut(
                typed,
                services,
                cancellationToken);

        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = fanOut;
            var behavior = behaviors[i];
            fanOut = () => behavior.Handle(
                typed,
                next,
                cancellationToken);
        }

        return fanOut().AsTask();
    }

    private static async ValueTask SequentialFanOut(
        TNotification typed,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        foreach (var handler in services.GetServices<INotificationHandler<TNotification>>())
            await handler.Handle(
                typed,
                cancellationToken).ConfigureAwait(false);
    }

    private static Task ParallelFanOut(
        TNotification typed,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var handlers = services.GetServices<INotificationHandler<TNotification>>().ToArray();

        if (handlers.Length == 0) return Task.CompletedTask;
        if (handlers.Length == 1)
            return WrapHandlerTask(
                handlers[0].Handle(
                    typed,
                    cancellationToken).AsTask(),
                handlers[0].GetType().Name);

        var tasks = new Task[handlers.Length];
        for (int i = 0; i < handlers.Length; i++)
            tasks[i] = WrapHandlerTask(
                handlers[i].Handle(
                    typed,
                    cancellationToken).AsTask(),
                handlers[i].GetType().Name);

        return Task.WhenAll(tasks);
    }

    private static async Task WrapHandlerTask(
        Task task,
        string handlerName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Notification handler '{handlerName}' failed: {ex.Message}",
                ex);
        }
    }
}

internal sealed class StreamQueryWrapper<TQuery, T> : StreamQueryWrapper<T>
    where TQuery : IStreamQuery
{
    public override IAsyncEnumerable<T> Execute(
        IStreamQuery query,
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        var handler = services.GetService<IStreamQueryHandler<TQuery, T>>();
        if (handler is null)
            throw new InvalidOperationException(
                $"No IStreamQueryHandler<{typeof(TQuery).Name}, {typeof(T).Name}> registered.");

        var typed = (TQuery)query;
        var behaviors = services.GetServices<IStreamPipelineBehavior<TQuery, T>>().ToArray();

        if (behaviors.Length == 0)
            return handler.Handle(typed, cancellationToken);

        Func<IAsyncEnumerable<T>> pipeline = () => handler.Handle(typed, cancellationToken);
        for (int i = behaviors.Length - 1; i >= 0; i--)
        {
            var next = pipeline;
            var behavior = behaviors[i];
            pipeline = () => behavior.Handle(typed, next, cancellationToken);
        }

        return pipeline();
    }
}
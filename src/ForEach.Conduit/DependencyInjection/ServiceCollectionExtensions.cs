using System.Reflection;
using ForEach.Conduit.Behaviors;
using ForEach.Conduit.Commands;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace ForEach.Conduit.DependencyInjection;

/// <summary>
/// Registration extensions for ForEach.Conduit.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Conduit dispatcher only. Use when registering handlers manually.
    /// Also registers <see cref="ICommandDispatcher"/>, <see cref="IQueryDispatcher"/>, and
    /// <see cref="IEventPublisher"/> as aliases that resolve the same <see cref="IDispatcher"/> instance.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConduit(
        this IServiceCollection services)
    {
        services.TryAddScoped<IDispatcher>(sp => new ConduitDispatcher(sp));
        services.TryAddScoped<ICommandDispatcher>(sp => sp.GetRequiredService<IDispatcher>());
        services.TryAddScoped<IQueryDispatcher>(sp => sp.GetRequiredService<IDispatcher>());
        services.TryAddScoped<IEventPublisher>(sp => sp.GetRequiredService<IDispatcher>());
        return services;
    }

    /// <summary>
    /// Registers the Conduit dispatcher and scans the provided assemblies for all handler
    /// implementations, registering each automatically.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddConduit(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        services.AddConduit();
        HandlerScanner.RegisterFromAssemblies(
            services,
            assemblies);
        return services;
    }

    /// <summary>
    /// Registers the dispatcher and returns a fluent builder for handlers, scanning, and behaviors.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="ConduitBuilder"/> instance.</returns>
    public static ConduitBuilder AddConduitHandlers(
        this IServiceCollection services)
    {
        services.AddConduit();
        return new ConduitBuilder(services);
    }

    /// <summary>
    /// Registers a command handler for a command that does not return a value.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, THandler>(
        this IServiceCollection services)
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        services.TryAddScoped<ICommandHandler<TCommand>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a command handler for a command that returns a response.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddCommandHandler<TCommand, TResult, THandler>(
        this IServiceCollection services)
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        services.TryAddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a query handler.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddQueryHandler<TQuery, TResult, THandler>(
        this IServiceCollection services)
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        services.TryAddScoped<IQueryHandler<TQuery, TResult>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a notification handler. Multiple handlers per notification are valid —
    /// each call appends a new handler to the chain.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNotificationHandler<TNotification, THandler>(
        this IServiceCollection services)
        where TNotification : INotification
        where THandler : class, INotificationHandler<TNotification>
    {
        services.AddScoped<INotificationHandler<TNotification>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers a streaming query handler.
    /// </summary>
    /// <typeparam name="TQuery">The type of the streaming query.</typeparam>
    /// <typeparam name="TResult">The type of the items in the stream.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStreamQueryHandler<TQuery, TResult, THandler>(
        this IServiceCollection services)
        where TQuery : IStreamQuery
        where THandler : class, IStreamQueryHandler<TQuery, TResult>
    {
        services.TryAddScoped<IStreamQueryHandler<TQuery, TResult>, THandler>();
        return services;
    }

    /// <summary>
    /// Registers an open-generic behavior that applies to every request type.
    /// The behavior type must be an open generic implementing IPipelineBehavior&lt;,&gt;.
    /// Example: <c>services.AddPipelineBehavior(typeof(LoggingBehavior&lt;,&gt;))</c>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPipelineBehavior(
        this IServiceCollection services,
        Type openGenericBehavior)
    {
        services.AddScoped(
            typeof(IPipelineBehavior<,>),
            openGenericBehavior);
        return services;
    }

    /// <summary>
    /// Registers a closed behavior that applies only to the specified TRequest/TResponse pair.
    /// Example: <c>services.AddPipelineBehavior&lt;CreateOrderCommand, ValueResult&lt;OrderDto&gt;, ValidationBehavior&gt;()</c>
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddPipelineBehavior<TRequest, TResponse, TBehavior>(
        this IServiceCollection services)
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        services.AddScoped<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
        return services;
    }

    /// <summary>
    /// Registers an open-generic notification behavior that applies to every notification type.
    /// The behavior type must be an open generic implementing INotificationPipelineBehavior&lt;&gt;.
    /// Example: <c>services.AddNotificationPipelineBehavior(typeof(LoggingNotificationBehavior&lt;&gt;))</c>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNotificationPipelineBehavior(
        this IServiceCollection services,
        Type openGenericBehavior)
    {
        services.AddScoped(
            typeof(INotificationPipelineBehavior<>),
            openGenericBehavior);
        return services;
    }

    /// <summary>
    /// Registers a closed notification behavior that applies only to the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddNotificationPipelineBehavior<TNotification, TBehavior>(
        this IServiceCollection services)
        where TNotification : INotification
        where TBehavior : class, INotificationPipelineBehavior<TNotification>
    {
        services.AddScoped<INotificationPipelineBehavior<TNotification>, TBehavior>();
        return services;
    }

    /// <summary>
    /// Registers an open-generic stream pipeline behavior that applies to every streaming query type.
    /// The behavior type must be an open generic implementing
    /// <see cref="IStreamPipelineBehavior{TQuery,T}"/>.
    /// Example: <c>services.AddStreamPipelineBehavior(typeof(LoggingStreamBehavior&lt;,&gt;))</c>
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStreamPipelineBehavior(
        this IServiceCollection services,
        Type openGenericBehavior)
    {
        services.AddScoped(
            typeof(IStreamPipelineBehavior<,>),
            openGenericBehavior);
        return services;
    }

    /// <summary>
    /// Registers a closed stream pipeline behavior that applies only to the specified
    /// streaming query and item type.
    /// Example: <c>services.AddStreamPipelineBehavior&lt;ProductsQuery, Product, AuthStreamBehavior&gt;()</c>
    /// </summary>
    /// <typeparam name="TQuery">The streaming query type.</typeparam>
    /// <typeparam name="T">The item type produced by the stream.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddStreamPipelineBehavior<TQuery, T, TBehavior>(
        this IServiceCollection services)
        where TQuery : IStreamQuery
        where TBehavior : class, IStreamPipelineBehavior<TQuery, T>
    {
        services.AddScoped<IStreamPipelineBehavior<TQuery, T>, TBehavior>();
        return services;
    }

    /// <summary>
    /// Registers a <see cref="TimeoutBehavior{TCommand}"/> for a void command.
    /// When the handler exceeds <paramref name="timeout"/>, the dispatch returns
    /// <see cref="ValueResult.Failure"/> with an <see cref="Error.Timeout"/> error
    /// instead of throwing.
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="timeout">Maximum allowed execution time.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTimeoutBehavior<TCommand>(
        this IServiceCollection services,
        TimeSpan timeout)
        where TCommand : ICommand
    {
        services.AddScoped<IPipelineBehavior<TCommand, ValueResult>>(
            _ => new TimeoutBehavior<TCommand>(timeout));
        return services;
    }

    /// <summary>
    /// Registers a <see cref="TimeoutBehavior{TRequest, TResult}"/> for a typed command or query.
    /// When the handler exceeds <paramref name="timeout"/>, the dispatch returns
    /// <see cref="ValueResult{TResult}.Failure"/> with an <see cref="Error.Timeout"/> error
    /// instead of throwing.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="timeout">Maximum allowed execution time.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddTimeoutBehavior<TRequest, TResult>(
        this IServiceCollection services,
        TimeSpan timeout)
    {
        services.AddScoped<IPipelineBehavior<TRequest, ValueResult<TResult>>>(
            _ => new TimeoutBehavior<TRequest, TResult>(timeout));
        return services;
    }

    // ── Microsoft.Extensions.Resilience integration ──────────────────────────

    /// <summary>
    /// Registers a <see cref="ResiliencePipelineBehavior{TCommand}"/> that wraps void-command
    /// dispatch in the named <see cref="ResiliencePipeline{ValueResult}"/> registered via
    /// <c>services.AddResiliencePipeline&lt;ValueResult&gt;(pipelineName, …)</c>.
    ///
    /// <para>Use this overload for circuit breakers, hedging, and any stateful strategy
    /// where the pipeline must be a singleton managed by
    /// <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>.</para>
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="pipelineName">The key used when registering the pipeline.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddResilienceBehavior<TCommand>(
        this IServiceCollection services,
        string pipelineName)
        where TCommand : ICommand
    {
        services.AddScoped<IPipelineBehavior<TCommand, ValueResult>>(sp =>
        {
            var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return new ResiliencePipelineBehavior<TCommand>(
                provider.GetPipeline<ValueResult>(pipelineName));
        });
        return services;
    }

    /// <summary>
    /// Registers a <see cref="ResiliencePipelineBehavior{TRequest, TResult}"/> that wraps
    /// typed-command/query dispatch in the named
    /// <see cref="ResiliencePipeline{T}"/> where <c>T</c> is <see cref="ValueResult{TResult}"/>.
    ///
    /// <para>Register the pipeline first:
    /// <c>services.AddResiliencePipeline&lt;ValueResult&lt;TResult&gt;&gt;(pipelineName, …)</c></para>
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="pipelineName">The key used when registering the pipeline.</param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddResilienceBehavior<TRequest, TResult>(
        this IServiceCollection services,
        string pipelineName)
    {
        services.AddScoped<IPipelineBehavior<TRequest, ValueResult<TResult>>>(sp =>
        {
            var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return new ResiliencePipelineBehavior<TRequest, TResult>(
                provider.GetPipeline<ValueResult<TResult>>(pipelineName));
        });
        return services;
    }

    /// <summary>
    /// Registers a retry behavior for a void command using exponential back-off with jitter.
    /// Retries on both exceptions and <see cref="ValueResult"/> failures.
    ///
    /// <para>For finer control (circuit breaker, hedging, rate limiter), use
    /// <see cref="AddResilienceBehavior{TCommand}(IServiceCollection,string)"/> with a
    /// named pipeline configured via <c>services.AddResiliencePipeline&lt;ValueResult&gt;(…)</c>.</para>
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="maxAttempts">Total attempts (including the first). Defaults to 3.</param>
    /// <param name="baseDelay">Delay before the first retry. Defaults to 200 ms.</param>
    /// <param name="shouldRetry">
    ///     Optional predicate on <see cref="Error"/> — when supplied, only failures whose
    ///     error satisfies the predicate are retried. When <see langword="null"/>, all
    ///     <see cref="ValueResult"/> failures are retried.
    /// </param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRetryBehavior<TCommand>(
        this IServiceCollection services,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Error, bool>? shouldRetry = null)
        where TCommand : ICommand
    {
        var pipeline = BuildVoidRetryPipeline(maxAttempts, baseDelay ?? TimeSpan.FromMilliseconds(200), shouldRetry);
        services.AddScoped<IPipelineBehavior<TCommand, ValueResult>>(
            _ => new ResiliencePipelineBehavior<TCommand>(pipeline));
        return services;
    }

    /// <summary>
    /// Registers a retry behavior for a typed command or query using exponential back-off with jitter.
    /// Retries on both exceptions and <see cref="ValueResult{TResult}"/> failures.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <param name="maxAttempts">Total attempts (including the first). Defaults to 3.</param>
    /// <param name="baseDelay">Delay before the first retry. Defaults to 200 ms.</param>
    /// <param name="shouldRetry">
    ///     Optional predicate on <see cref="Error"/> — when supplied, only failures whose
    ///     error satisfies the predicate are retried.
    /// </param>
    /// <returns>The service collection.</returns>
    public static IServiceCollection AddRetryBehavior<TRequest, TResult>(
        this IServiceCollection services,
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Error, bool>? shouldRetry = null)
    {
        var pipeline = BuildTypedRetryPipeline<TResult>(maxAttempts, baseDelay ?? TimeSpan.FromMilliseconds(200), shouldRetry);
        services.AddScoped<IPipelineBehavior<TRequest, ValueResult<TResult>>>(
            _ => new ResiliencePipelineBehavior<TRequest, TResult>(pipeline));
        return services;
    }

    private static ResiliencePipeline<ValueResult> BuildVoidRetryPipeline(
        int maxAttempts,
        TimeSpan baseDelay,
        Func<Error, bool>? shouldRetry)
    {
        if (maxAttempts <= 1)
            return ResiliencePipeline<ValueResult>.Empty;

        return new ResiliencePipelineBuilder<ValueResult>()
            .AddRetry(new RetryStrategyOptions<ValueResult>
            {
                MaxRetryAttempts = maxAttempts - 1, // Polly counts retries, not total attempts
                Delay             = baseDelay,
                BackoffType       = DelayBackoffType.Exponential,
                UseJitter         = true,
                ShouldHandle      = args =>
                {
                    if (args.Outcome.Exception is not null)
                        return PredicateResult.True();

                    // ValueResult is a struct — when no exception, the result is always set
                    if (args.Outcome.Result.IsSuccess)
                        return PredicateResult.False();

                    return ValueTask.FromResult(
                        shouldRetry is null || shouldRetry(args.Outcome.Result.Error!.Value));
                }
            })
            .Build();
    }

    private static ResiliencePipeline<ValueResult<TResult>> BuildTypedRetryPipeline<TResult>(
        int maxAttempts,
        TimeSpan baseDelay,
        Func<Error, bool>? shouldRetry)
    {
        if (maxAttempts <= 1)
            return ResiliencePipeline<ValueResult<TResult>>.Empty;

        return new ResiliencePipelineBuilder<ValueResult<TResult>>()
            .AddRetry(new RetryStrategyOptions<ValueResult<TResult>>
            {
                MaxRetryAttempts = maxAttempts - 1,
                Delay             = baseDelay,
                BackoffType       = DelayBackoffType.Exponential,
                UseJitter         = true,
                ShouldHandle      = args =>
                {
                    if (args.Outcome.Exception is not null)
                        return PredicateResult.True();

                    // ValueResult<TResult> is a struct — when no exception, the result is always set
                    if (args.Outcome.Result.IsSuccess)
                        return PredicateResult.False();

                    return ValueTask.FromResult(
                        shouldRetry is null || shouldRetry(args.Outcome.Result.Error!.Value));
                }
            })
            .Build();
    }
}
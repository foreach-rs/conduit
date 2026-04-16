using System.Reflection;
using ForEach.Conduit.Behaviors;
using ForEach.Conduit.Commands;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Resilience;
using Polly;
using Polly.Registry;

namespace ForEach.Conduit.DependencyInjection;

/// <summary>
/// Fluent builder for handler, assembly scanning, and pipeline behavior registration.
/// </summary>
public sealed class ConduitBuilder
{
    private readonly IServiceCollection _services;

    internal ConduitBuilder(
        IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Scans the assembly and registers all handler implementations.
    /// Tip: pass <c>typeof(AnyTypeInYourAssembly).Assembly</c>.
    /// </summary>
    /// <param name="assembly">The assembly to scan.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder ScanAssembly(
        Assembly assembly)
    {
        HandlerScanner.RegisterFromAssembly(
            _services,
            assembly);
        return this;
    }

    /// <summary>Scans multiple assemblies and registers all handler implementations found.</summary>
    /// <param name="assemblies">The assemblies to scan.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder ScanAssemblies(
        params Assembly[] assemblies)
    {
        HandlerScanner.RegisterFromAssemblies(
            _services,
            assemblies);
        return this;
    }

    /// <summary>
    /// Registers a command handler for a command that does not return a value.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddCommandHandler<TCommand, THandler>()
        where TCommand : ICommand
        where THandler : class, ICommandHandler<TCommand>
    {
        _services.TryAddScoped<ICommandHandler<TCommand>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a command handler for a command that returns a response.
    /// </summary>
    /// <typeparam name="TCommand">The type of the command.</typeparam>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddCommandHandler<TCommand, TResult, THandler>()
        where TCommand : ICommand<TResult>
        where THandler : class, ICommandHandler<TCommand, TResult>
    {
        _services.TryAddScoped<ICommandHandler<TCommand, TResult>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a query handler.
    /// </summary>
    /// <typeparam name="TQuery">The type of the query.</typeparam>
    /// <typeparam name="TResult">The type of the response.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddQueryHandler<TQuery, TResult, THandler>()
        where TQuery : IQuery<TResult>
        where THandler : class, IQueryHandler<TQuery, TResult>
    {
        _services.TryAddScoped<IQueryHandler<TQuery, TResult>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a notification handler. Multiple handlers per notification are valid —
    /// each call appends a new handler to the chain.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddNotificationHandler<TNotification, THandler>()
        where TNotification : INotification
        where THandler : class, INotificationHandler<TNotification>
    {
        _services.AddScoped<INotificationHandler<TNotification>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers a streaming query handler.
    /// </summary>
    /// <typeparam name="TQuery">The type of the streaming query.</typeparam>
    /// <typeparam name="TResult">The type of the items in the stream.</typeparam>
    /// <typeparam name="THandler">The type of the handler.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddStreamQueryHandler<TQuery, TResult, THandler>()
        where TQuery : IStreamQuery
        where THandler : class, IStreamQueryHandler<TQuery, TResult>
    {
        _services.TryAddScoped<IStreamQueryHandler<TQuery, TResult>, THandler>();
        return this;
    }

    /// <summary>
    /// Registers an open-generic behavior that applies to every request type.
    /// The behavior type must be an open generic implementing IPipelineBehavior&lt;,&gt;.
    /// Example: <c>AddPipelineBehavior(typeof(LoggingBehavior&lt;,&gt;))</c>
    /// </summary>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddPipelineBehavior(
        Type openGenericBehavior)
    {
        _services.AddScoped(
            typeof(IPipelineBehavior<,>),
            openGenericBehavior);
        return this;
    }

    /// <summary>
    /// Registers a closed behavior that applies only to the specified TRequest/TResponse pair.
    /// Example: <c>AddPipelineBehavior&lt;CreateOrderCommand, ValueResult&lt;OrderDto&gt;, ValidationBehavior&gt;()</c>
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddPipelineBehavior<TRequest, TResponse, TBehavior>()
        where TBehavior : class, IPipelineBehavior<TRequest, TResponse>
    {
        _services.AddScoped<IPipelineBehavior<TRequest, TResponse>, TBehavior>();
        return this;
    }

    /// <summary>
    /// Registers an open-generic notification behavior that applies to every notification type.
    /// The behavior type must be an open generic implementing INotificationPipelineBehavior&lt;&gt;.
    /// Example: <c>AddNotificationPipelineBehavior(typeof(LoggingNotificationBehavior&lt;&gt;))</c>
    /// </summary>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddNotificationPipelineBehavior(
        Type openGenericBehavior)
    {
        _services.AddScoped(
            typeof(INotificationPipelineBehavior<>),
            openGenericBehavior);
        return this;
    }

    /// <summary>
    /// Registers a closed notification behavior that applies only to the specified notification type.
    /// </summary>
    /// <typeparam name="TNotification">The type of the notification.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddNotificationPipelineBehavior<TNotification, TBehavior>()
        where TNotification : INotification
        where TBehavior : class, INotificationPipelineBehavior<TNotification>
    {
        _services.AddScoped<INotificationPipelineBehavior<TNotification>, TBehavior>();
        return this;
    }

    /// <summary>
    /// Registers an open-generic stream pipeline behavior that applies to every streaming query type.
    /// The behavior type must be an open generic implementing
    /// <see cref="IStreamPipelineBehavior{TQuery,T}"/>.
    /// Example: <c>AddStreamPipelineBehavior(typeof(LoggingStreamBehavior&lt;,&gt;))</c>
    /// </summary>
    /// <param name="openGenericBehavior">The type of the open-generic behavior.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddStreamPipelineBehavior(Type openGenericBehavior)
    {
        _services.AddScoped(
            typeof(IStreamPipelineBehavior<,>),
            openGenericBehavior);
        return this;
    }

    /// <summary>
    /// Registers a closed stream pipeline behavior that applies only to the specified
    /// streaming query and item type.
    /// </summary>
    /// <typeparam name="TQuery">The streaming query type.</typeparam>
    /// <typeparam name="T">The item type produced by the stream.</typeparam>
    /// <typeparam name="TBehavior">The type of the behavior.</typeparam>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddStreamPipelineBehavior<TQuery, T, TBehavior>()
        where TQuery : IStreamQuery
        where TBehavior : class, IStreamPipelineBehavior<TQuery, T>
    {
        _services.AddScoped<IStreamPipelineBehavior<TQuery, T>, TBehavior>();
        return this;
    }

    /// <summary>
    /// Registers a <see cref="TimeoutBehavior{TCommand}"/> for a void command.
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="timeout">Maximum allowed execution time.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddTimeoutBehavior<TCommand>(TimeSpan timeout)
        where TCommand : ICommand
    {
        _services.AddScoped<IPipelineBehavior<TCommand, ValueResult>>(
            _ => new TimeoutBehavior<TCommand>(timeout));
        return this;
    }

    /// <summary>
    /// Registers a <see cref="TimeoutBehavior{TRequest, TResult}"/> for a typed command or query.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="timeout">Maximum allowed execution time.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddTimeoutBehavior<TRequest, TResult>(TimeSpan timeout)
    {
        _services.AddScoped<IPipelineBehavior<TRequest, ValueResult<TResult>>>(
            _ => new TimeoutBehavior<TRequest, TResult>(timeout));
        return this;
    }

    // ── Microsoft.Extensions.Resilience integration ──────────────────────────

    /// <summary>
    /// Registers a <see cref="ResiliencePipelineBehavior{TCommand}"/> backed by a named
    /// <see cref="ResiliencePipeline{ValueResult}"/> from <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>.
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="pipelineName">The key used when registering the pipeline.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddResilienceBehavior<TCommand>(string pipelineName)
        where TCommand : ICommand
    {
        _services.AddScoped<IPipelineBehavior<TCommand, ValueResult>>(sp =>
        {
            var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return new ResiliencePipelineBehavior<TCommand>(
                provider.GetPipeline<ValueResult>(pipelineName));
        });
        return this;
    }

    /// <summary>
    /// Registers a <see cref="ResiliencePipelineBehavior{TRequest, TResult}"/> backed by a named
    /// <see cref="ResiliencePipeline{T}"/> from <see cref="Polly.Registry.ResiliencePipelineProvider{TKey}"/>.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="pipelineName">The key used when registering the pipeline.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddResilienceBehavior<TRequest, TResult>(string pipelineName)
    {
        _services.AddScoped<IPipelineBehavior<TRequest, ValueResult<TResult>>>(sp =>
        {
            var provider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
            return new ResiliencePipelineBehavior<TRequest, TResult>(
                provider.GetPipeline<ValueResult<TResult>>(pipelineName));
        });
        return this;
    }

    /// <summary>
    /// Registers a retry behavior for a void command using exponential back-off with jitter.
    /// Retries on both exceptions and <see cref="ValueResult"/> failures.
    /// </summary>
    /// <typeparam name="TCommand">The void command type to protect.</typeparam>
    /// <param name="maxAttempts">Total attempts (including the first). Defaults to 3.</param>
    /// <param name="baseDelay">Delay before the first retry. Defaults to 200 ms.</param>
    /// <param name="shouldRetry">Optional predicate — when supplied, only matching errors are retried.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddRetryBehavior<TCommand>(
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Error, bool>? shouldRetry = null)
        where TCommand : ICommand
    {
        _services.AddRetryBehavior<TCommand>(maxAttempts, baseDelay, shouldRetry);
        return this;
    }

    /// <summary>
    /// Registers a retry behavior for a typed command or query using exponential back-off with jitter.
    /// </summary>
    /// <typeparam name="TRequest">The command or query type to protect.</typeparam>
    /// <typeparam name="TResult">The inner result type.</typeparam>
    /// <param name="maxAttempts">Total attempts (including the first). Defaults to 3.</param>
    /// <param name="baseDelay">Delay before the first retry. Defaults to 200 ms.</param>
    /// <param name="shouldRetry">Optional predicate — when supplied, only matching errors are retried.</param>
    /// <returns>The builder instance for chaining.</returns>
    public ConduitBuilder AddRetryBehavior<TRequest, TResult>(
        int maxAttempts = 3,
        TimeSpan? baseDelay = null,
        Func<Error, bool>? shouldRetry = null)
    {
        _services.AddRetryBehavior<TRequest, TResult>(maxAttempts, baseDelay, shouldRetry);
        return this;
    }
}
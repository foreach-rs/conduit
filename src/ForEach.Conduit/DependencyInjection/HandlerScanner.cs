using System.Reflection;
using ForEach.Conduit.Commands;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ForEach.Conduit.DependencyInjection;

/// <summary>
/// Scans assemblies and registers all concrete handler implementations with the DI container.
///
/// Command/query/stream handlers use <c>TryAddScoped</c> — only one handler per request type
/// is allowed; manually registered handlers take precedence over scanned ones.
///
/// Notification handlers use <c>AddScoped</c> — multiple handlers per notification type
/// are all registered and all will be invoked on publish.
///
/// Open generic types and abstract types are skipped automatically.
/// </summary>
internal static class HandlerScanner
{
    // Command/query/stream: single handler per type — TryAdd wins on first registration.
    private static readonly Type[] SingleHandlerInterfaces =
    [
        typeof(ICommandHandler<>),
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IStreamQueryHandler<,>)
    ];

    // Notifications / notification behaviors: multiple registrations per type are all kept.
    private static readonly Type[] MultiHandlerInterfaces =
    [
        typeof(INotificationHandler<>),
        typeof(INotificationPipelineBehavior<>)
    ];

    internal static void RegisterFromAssembly(
        IServiceCollection services,
        Assembly assembly)
    {
        foreach (var type in GetExportedTypes(assembly))
        {
            if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                continue;

            foreach (var iface in type.GetInterfaces())
            {
                if (!iface.IsGenericType) continue;

                var def = iface.GetGenericTypeDefinition();

                if (Array.IndexOf(
                        SingleHandlerInterfaces,
                        def) >= 0)
                    services.TryAddScoped(
                        iface,
                        type);
                else if (Array.IndexOf(
                             MultiHandlerInterfaces,
                             def) >= 0)
                    services.AddScoped(
                        iface,
                        type);
            }
        }
    }

    internal static void RegisterFromAssemblies(
        IServiceCollection services,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
            RegisterFromAssembly(
                services,
                assembly);
    }

    /// <summary>
    /// Safe wrapper around <see cref="Assembly.GetTypes"/> that handles
    /// <see cref="ReflectionTypeLoadException"/> — thrown when the assembly references
    /// types from missing/unloaded assemblies. Only the successfully loaded types are returned.
    /// </summary>
    internal static IEnumerable<Type> GetExportedTypes(
        Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }
}
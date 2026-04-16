using System.Reflection;
using System.Text;
using ForEach.Conduit.Commands;
using ForEach.Conduit.Queries;

namespace ForEach.Conduit.DependencyInjection;

/// <summary>
/// Startup validation for ForEach.Conduit.
///
/// Usage — call after building the DI container (e.g. after <c>app.Build()</c>):
/// <code>
/// app.Services.ValidateConduitHandlers(typeof(Program).Assembly);
/// </code>
///
/// Scans the provided assemblies for every concrete <see cref="ICommand"/>,
/// <see cref="ICommand{TResponse}"/>, and <see cref="IQuery{TResponse}"/> implementation
/// and verifies that a handler is registered. Throws <see cref="InvalidOperationException"/>
/// with a full list of missing handlers if any are unregistered, so misconfiguration is
/// caught at startup rather than at runtime.
///
/// Note: notifications are excluded from validation — zero handlers for a notification is valid.
/// </summary>
public static class ConduitValidation
{
    /// <summary>
    /// Validates that every command and query type in <paramref name="assemblies"/> has a
    /// registered handler. Throws <see cref="InvalidOperationException"/> listing all gaps.
    /// </summary>
    public static void ValidateConduitHandlers(
        this IServiceProvider services,
        params Assembly[] assemblies)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (assemblies.Length == 0) return;

        var errors = new List<string>();

        foreach (var assembly in assemblies)
        {
            foreach (var type in HandlerScanner.GetExportedTypes(assembly))
            {
                if (type.IsAbstract || type.IsInterface || type.IsGenericTypeDefinition)
                    continue;

                CheckCommandType(
                    type,
                    services,
                    errors);
                CheckQueryType(
                    type,
                    services,
                    errors);
            }
        }

        if (errors.Count == 0) return;

        var sb = new StringBuilder();
        sb.AppendLine($"Conduit handler validation failed — {errors.Count} missing handler(s):");
        foreach (var err in errors)
            sb.AppendLine($"  • {err}");

        throw new InvalidOperationException(sb.ToString());
    }

    private static void CheckCommandType(
        Type type,
        IServiceProvider services,
        List<string> errors)
    {
        // ICommand<TResult> takes priority — the generic handler also satisfies ICommand (base).
        var genericCommandIface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>));

        if (genericCommandIface is not null)
        {
            var tResult = genericCommandIface.GetGenericArguments()[0];
            var handlerType = typeof(ICommandHandler<,>).MakeGenericType(
                type,
                tResult);

            if (services.GetService(handlerType) is null)
                errors.Add($"No ICommandHandler<{type.Name}, {tResult.Name}> registered for command '{type.FullName}'");

            return;
        }

        if (type.GetInterfaces().Contains(typeof(ICommand)))
        {
            var handlerType = typeof(ICommandHandler<>).MakeGenericType(type);

            if (services.GetService(handlerType) is null)
                errors.Add($"No ICommandHandler<{type.Name}> registered for command '{type.FullName}'");
        }
    }

    private static void CheckQueryType(
        Type type,
        IServiceProvider services,
        List<string> errors)
    {
        var queryIface = type.GetInterfaces()
            .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));

        if (queryIface is null) return;

        var tResult = queryIface.GetGenericArguments()[0];
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(
            type,
            tResult);

        if (services.GetService(handlerType) is null)
            errors.Add($"No IQueryHandler<{type.Name}, {tResult.Name}> registered for query '{type.FullName}'");
    }
}
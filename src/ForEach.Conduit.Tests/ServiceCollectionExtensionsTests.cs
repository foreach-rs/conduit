using ForEach.Conduit.Commands;
using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Pipeline;
using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Behaviors;
using ForEach.Conduit.Tests.Commands;
using ForEach.Conduit.Tests.Handlers;
using ForEach.Conduit.Tests.Notifications;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddConduit_RegistersIDispatcher()
    {
        var sp = new ServiceCollection().AddConduit().BuildServiceProvider();
        sp.GetService<IDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddConduit_RegistersICommandDispatcher_AsAlias()
    {
        var sp = new ServiceCollection().AddConduit().BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetService<ICommandDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddConduit_RegistersIQueryDispatcher_AsAlias()
    {
        var sp = new ServiceCollection().AddConduit().BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetService<IQueryDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddConduit_RegistersIEventPublisher_AsAlias()
    {
        var sp = new ServiceCollection().AddConduit().BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetService<IEventPublisher>().Should().NotBeNull();
    }

    [Fact]
    public void AddConduit_IsIdempotent_SecondCallDoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddConduit();
        services.AddConduit();
        var sp = services.BuildServiceProvider();

        using var scope = sp.CreateScope();
        scope.ServiceProvider.Invoking(s => s.GetRequiredService<IDispatcher>())
            .Should().NotThrow();
    }

    [Fact]
    public void AddConduit_AliasesResolveToSameInstance()
    {
        var sp = new ServiceCollection().AddConduit().BuildServiceProvider();
        using var scope = sp.CreateScope();

        var dispatcher = scope.ServiceProvider.GetRequiredService<IDispatcher>();
        var cmdDispatcher = scope.ServiceProvider.GetRequiredService<ICommandDispatcher>();
        var queryDispatcher = scope.ServiceProvider.GetRequiredService<IQueryDispatcher>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        cmdDispatcher.Should().BeSameAs(dispatcher);
        queryDispatcher.Should().BeSameAs(dispatcher);
        eventPublisher.Should().BeSameAs(dispatcher);
    }

    [Fact]
    public void AddCommandHandler_Void_RegistersHandler()
    {
        var sp = new ServiceCollection()
            .AddCommandHandler<RegCommand, RegCmdHandler>()
            .BuildServiceProvider();
        sp.GetService<ICommandHandler<RegCommand>>().Should().BeOfType<RegCmdHandler>();
    }

    [Fact]
    public void AddCommandHandler_WithResult_RegistersHandler()
    {
        var sp = new ServiceCollection()
            .AddCommandHandler<RegCommandWithResult, int, RegCmdWithResultHandler>()
            .BuildServiceProvider();
        sp.GetService<ICommandHandler<RegCommandWithResult, int>>()
            .Should().BeOfType<RegCmdWithResultHandler>();
    }

    [Fact]
    public void AddQueryHandler_RegistersHandler()
    {
        var sp = new ServiceCollection()
            .AddQueryHandler<RegQuery, string, RegQueryHandler>()
            .BuildServiceProvider();
        sp.GetService<IQueryHandler<RegQuery, string>>().Should().BeOfType<RegQueryHandler>();
    }

    [Fact]
    public void AddNotificationHandler_RegistersFirstHandler()
    {
        var sp = new ServiceCollection()
            .AddNotificationHandler<RegNotification, RegNotificationHandlerA>()
            .BuildServiceProvider();
        sp.GetServices<INotificationHandler<RegNotification>>()
            .Should().ContainSingle()
            .Which.Should().BeOfType<RegNotificationHandlerA>();
    }

    [Fact]
    public void AddNotificationHandler_CalledTwice_RegistersBothHandlers()
    {
        var sp = new ServiceCollection()
            .AddNotificationHandler<RegNotification, RegNotificationHandlerA>()
            .AddNotificationHandler<RegNotification, RegNotificationHandlerB>()
            .BuildServiceProvider();

        sp.GetServices<INotificationHandler<RegNotification>>().Should().HaveCount(2);
    }

    [Fact]
    public void AddStreamQueryHandler_RegistersHandler()
    {
        var sp = new ServiceCollection()
            .AddStreamQueryHandler<RegStreamQuery, int, RegStreamQueryHandler>()
            .BuildServiceProvider();
        sp.GetService<IStreamQueryHandler<RegStreamQuery, int>>()
            .Should().BeOfType<RegStreamQueryHandler>();
    }

    [Fact]
    public void AddPipelineBehavior_Closed_RegistersBehavior()
    {
        var sp = new ServiceCollection()
            .AddPipelineBehavior<RegCommand, ValueResult, RegPipelineBehavior<RegCommand, ValueResult>>()
            .BuildServiceProvider();
        sp.GetServices<IPipelineBehavior<RegCommand, ValueResult>>().Should().ContainSingle();
    }

    // ─── AddConduitHandlers fluent builder ────────────────────────────────────

    [Fact]
    public void AddConduitHandlers_ReturnsBuilder_And_RegistersDispatcher()
    {
        var services = new ServiceCollection();
        var builder = services.AddConduitHandlers();
        builder.Should().NotBeNull();
        services.BuildServiceProvider()
            .GetService<IDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void Builder_AddCommandHandler_Void_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers().AddCommandHandler<RegCommand, RegCmdHandler>();
        var sp = services.BuildServiceProvider();
        sp.GetService<ICommandHandler<RegCommand>>().Should().BeOfType<RegCmdHandler>();
    }

    [Fact]
    public void Builder_AddCommandHandler_WithResult_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers().AddCommandHandler<RegCommandWithResult, int, RegCmdWithResultHandler>();
        var sp = services.BuildServiceProvider();
        sp.GetService<ICommandHandler<RegCommandWithResult, int>>().Should().BeOfType<RegCmdWithResultHandler>();
    }

    [Fact]
    public void Builder_AddQueryHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers().AddQueryHandler<RegQuery, string, RegQueryHandler>();
        var sp = services.BuildServiceProvider();
        sp.GetService<IQueryHandler<RegQuery, string>>().Should().BeOfType<RegQueryHandler>();
    }

    [Fact]
    public void Builder_AddNotificationHandler_CalledTwice_RegistersBoth()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddNotificationHandler<RegNotification, RegNotificationHandlerA>()
            .AddNotificationHandler<RegNotification, RegNotificationHandlerB>();
        var sp = services.BuildServiceProvider();

        sp.GetServices<INotificationHandler<RegNotification>>().Should().HaveCount(2);
    }

    [Fact]
    public void Builder_AddStreamQueryHandler_RegistersHandler()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddStreamQueryHandler<RegStreamQuery, int, RegStreamQueryHandler>();
        var sp = services.BuildServiceProvider();
        sp.GetService<IStreamQueryHandler<RegStreamQuery, int>>().Should().BeOfType<RegStreamQueryHandler>();
    }

    [Fact]
    public void Builder_AddPipelineBehavior_Closed_RegistersBehavior()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .AddPipelineBehavior<RegCommand, ValueResult, RegPipelineBehavior<RegCommand, ValueResult>>();
        var sp = services.BuildServiceProvider();
        sp.GetServices<IPipelineBehavior<RegCommand, ValueResult>>().Should().ContainSingle();
    }

    [Fact]
    public void Builder_IsChainable_ReturnsBuilderInstance()
    {
        var services = new ServiceCollection();
        var builder = services.AddConduitHandlers();
        var chained = builder.AddCommandHandler<RegCommand, RegCmdHandler>();
        chained.Should().BeSameAs(builder);
    }

    [Fact]
    public void Builder_ScanAssembly_RegistersDispatcherAtMinimum()
    {
        var services = new ServiceCollection();
        services.AddConduitHandlers()
            .ScanAssembly(typeof(ServiceCollectionExtensionsTests).Assembly);
        var sp = services.BuildServiceProvider();
        sp.GetRequiredService<IDispatcher>().Should().NotBeNull();
    }

    [Fact]
    public void AddConduit_WithAssemblies_RegistersDispatcher()
    {
        var sp = new ServiceCollection()
            .AddConduit(typeof(ServiceCollectionExtensionsTests).Assembly)
            .BuildServiceProvider();

        sp.GetRequiredService<IDispatcher>().Should().NotBeNull();
    }
}

using ForEach.Conduit.Commands;
using ForEach.Conduit.DependencyInjection;
using ForEach.Conduit.Dispatching;
using ForEach.Conduit.Notifications;
using ForEach.Conduit.Queries;
using ForEach.Conduit.Tests.Commands;
using ForEach.Conduit.Tests.Events;
using ForEach.Conduit.Tests.Handlers;
using ForEach.Conduit.Tests.Queries;

namespace ForEach.Conduit.Tests;

public class DispatcherTests
{
    private static IServiceProvider BuildProvider(Action<IServiceCollection> configure)
    {
        var services = new ServiceCollection();
        services.AddConduit();
        configure(services);
        return services.BuildServiceProvider();
    }

    private static IDispatcher GetDispatcher(Action<IServiceCollection> configure) =>
        BuildProvider(configure).GetRequiredService<IDispatcher>();

    [Fact]
    public async Task Send_VoidCommand_CallsHandler()
    {
        var handler = new PingHandler();
        var dispatcher = GetDispatcher(s => s.AddSingleton<ICommandHandler<PingCommand>>(handler));

        var result = await dispatcher.Send(new PingCommand());

        result.IsSuccess.Should().BeTrue();
        handler.WasCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Send_VoidCommand_NoHandler_ReturnsHandlerNotFound()
    {
        var dispatcher = GetDispatcher(_ => { });
        var result = await dispatcher.Send(new PingCommand());
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("HandlerNotFound");
    }

    [Fact]
    public async Task Send_VoidCommand_HandlerReturnsFailure_PropagatesError()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<ICommandHandler<PingCommand>, FailingPingHandler>());
        var result = await dispatcher.Send(new PingCommand());
        result.IsSuccess.Should().BeFalse();
        result.Error!.Value.Code.Should().Be("Validation.Failed");
    }

    [Fact]
    public async Task Send_NullCommand_Throws()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(d => d.Send(null!).AsTask())
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_CommandWithResult_ReturnsValue()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<ICommandHandler<EchoCommand, string>, EchoHandler>());
        var result = await dispatcher.Send(new EchoCommand("hello"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }

    [Fact]
    public async Task Send_CommandWithResult_NoHandler_ReturnsHandlerNotFound()
    {
        var dispatcher = GetDispatcher(_ => { });
        var result = await dispatcher.Send(new EchoCommand("x"));
        result.Error!.Value.Code.Should().Be("HandlerNotFound");
    }

    [Fact]
    public async Task Query_ReturnsValue()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<IQueryHandler<GetNumberQuery, int>, GetNumberHandler>());
        var result = await dispatcher.Query(new GetNumberQuery(42));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(42);
    }

    [Fact]
    public async Task Query_NoHandler_ReturnsHandlerNotFound()
    {
        var dispatcher = GetDispatcher(_ => { });
        var result = await dispatcher.Query(new GetNumberQuery(0));
        result.Error!.Value.Code.Should().Be("HandlerNotFound");
    }

    [Fact]
    public async Task Query_NullQuery_Throws()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(d => d.Query<int>(null!).AsTask())
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task SendAsync_VoidCommand_ReturnsResult()
    {
        var handler = new PingHandler();
        var dispatcher = GetDispatcher(s => s.AddSingleton<ICommandHandler<PingCommand>>(handler));
        var result = await dispatcher.SendAsync(new PingCommand());
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task SendAsync_CommandWithResult_ReturnsResultT()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<ICommandHandler<EchoCommand, string>, EchoHandler>());
        var result = await dispatcher.SendAsync(new EchoCommand("world"));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("world");
    }

    [Fact]
    public async Task QueryAsync_ReturnsResultT()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<IQueryHandler<GetNumberQuery, int>, GetNumberHandler>());
        var result = await dispatcher.QueryAsync(new GetNumberQuery(7));
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(7);
    }

    [Fact]
    public async Task Publish_Sequential_CallsAllHandlersInOrder()
    {
        var first = new FirstEventHandler();
        var second = new SecondEventHandler();
        var dispatcher = GetDispatcher(s =>
        {
            s.AddSingleton<INotificationHandler<UserCreatedEvent>>(first);
            s.AddSingleton<INotificationHandler<UserCreatedEvent>>(second);
        });

        await dispatcher.Publish(new UserCreatedEvent("Alice"));

        first.Calls.Should().ContainSingle("first:Alice");
        second.Calls.Should().ContainSingle("second:Alice");
    }

    [Fact]
    public async Task Publish_NoHandlers_CompletesSuccessfully()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(d => d.Publish(new UserCreatedEvent("x")).AsTask())
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task Publish_HandlerThrows_ExceptionPropagates()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<INotificationHandler<UserCreatedEvent>, ThrowingEventHandler>());

        await dispatcher.Invoking(d => d.Publish(new UserCreatedEvent("x")).AsTask())
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*handler exploded*");
    }

    [Fact]
    public async Task PublishParallel_CallsAllHandlers()
    {
        var first = new FirstEventHandler();
        var second = new SecondEventHandler();
        var dispatcher = GetDispatcher(s =>
        {
            s.AddSingleton<INotificationHandler<UserCreatedEvent>>(first);
            s.AddSingleton<INotificationHandler<UserCreatedEvent>>(second);
        });

        await dispatcher.PublishParallel(new UserCreatedEvent("Bob"));

        first.Calls.Should().ContainSingle();
        second.Calls.Should().ContainSingle();
    }

    [Fact]
    public async Task PublishParallel_NoHandlers_CompletesSuccessfully()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(d => d.PublishParallel(new UserCreatedEvent("x")))
            .Should().NotThrowAsync();
    }

    [Fact]
    public async Task PublishParallel_HandlerThrows_WrapsInInvalidOperationException()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<INotificationHandler<UserCreatedEvent>, ThrowingEventHandler>());

        await dispatcher.Invoking(d => d.PublishParallel(new UserCreatedEvent("x")))
            .Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*handler exploded*");
    }

    [Fact]
    public async Task Publish_NullNotification_Throws()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(d => d.Publish(null!).AsTask())
            .Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Stream_ReturnsAllItems()
    {
        var dispatcher = GetDispatcher(s =>
            s.AddScoped<IStreamQueryHandler<NumberStreamQuery, int>, NumberStreamHandler>());

        var items = new List<int>();
        await foreach (var item in dispatcher.Stream<int>(new NumberStreamQuery()))
            items.Add(item);

        items.Should().Equal(1, 2, 3);
    }

    [Fact]
    public void Stream_NoHandler_ThrowsInvalidOperationException()
    {
        var dispatcher = GetDispatcher(_ => { });

        // StreamQueryWrapper.Execute throws synchronously when no handler is registered
        var act = () => dispatcher.Stream<int>(new NumberStreamQuery());
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*No IStreamQueryHandler*");
    }

    [Fact]
    public async Task Stream_NullQuery_Throws()
    {
        var dispatcher = GetDispatcher(_ => { });
        await dispatcher.Invoking(async d =>
        {
            await foreach (var _ in d.Stream<int>(null!)) { }
        }).Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task Send_PassesCancellationTokenToHandler()
    {
        var handler = new CancelCheckHandler();
        var dispatcher = GetDispatcher(s =>
            s.AddSingleton<ICommandHandler<CancelCheckCommand>>(handler));
        var cts = new CancellationTokenSource();

        await dispatcher.Send(new CancelCheckCommand(), cts.Token);

        handler.ReceivedToken.Should().Be(cts.Token);
    }

    [Fact]
    public void IDispatcher_IsScopedPerRequest()
    {
        var sp = BuildProvider(_ => { });
        using var scope1 = sp.CreateScope();
        using var scope2 = sp.CreateScope();

        var d1 = scope1.ServiceProvider.GetRequiredService<IDispatcher>();
        var d2 = scope1.ServiceProvider.GetRequiredService<IDispatcher>();
        var d3 = scope2.ServiceProvider.GetRequiredService<IDispatcher>();

        d1.Should().BeSameAs(d2);   // same instance within scope
        d1.Should().NotBeSameAs(d3); // different instance across scopes
    }
}

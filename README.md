# ForEach.Conduit

**A high-performance, zero-reflection CQRS mediator for .NET 10+** — with railway-oriented result types,
composable pipelines, built-in resilience, and OpenTelemetry out of the box.

```csharp
// One line to wire up your entire application layer
builder.Services.AddConduit(typeof(Program).Assembly);
```

```csharp
// Dispatch with confidence — no exceptions for domain failures
ValueResult<OrderDto> result = await dispatcher.Send(new PlaceOrderCommand(cart));

return result.Match(
    onSuccess: order => Results.Created($"/orders/{order.Id}", order),
    onFailure: err   => Results.Problem(err.Message));
```

---

## Why ForEach.Conduit?

Most .NET mediators make you choose between **simplicity** and **production readiness**.
Conduit ships both.

| | ForEach.Conduit | MediatR 12 | DispatchR | WolverineFx |
|---|---|---|---|---|
| Zero reflection on hot path | ✅ | ✗ | ✅ | ✗ |
| Railway-oriented result types | ✅ | ✗ | ✗ | ✗ |
| Stream pipeline behaviors | ✅ | ✗ | ✅ | ✗ |
| Notification pipeline behaviors | ✅ | ✗ | ✗ | ✗ |
| Built-in timeout behavior | ✅ | ✗ | ✗ | ✅ |
| Built-in retry / Polly integration | ✅ | ✗ | ✗ | ✅ |
| Built-in OpenTelemetry | ✅ | ✗ | ✗ | ✅ |
| Startup handler validation | ✅ | ✗ | ✗ | ✗ |
| Narrow dispatcher interfaces | ✅ | ✗ | ✗ | ✗ |
| No transitive dependencies\* | ✅ | ✅ | ✅ | ✗ |

\*Base package requires only `Microsoft.Extensions.DependencyInjection.Abstractions`.
Resilience features add `Microsoft.Extensions.Resilience` (Polly v8).

---

## Table of Contents

1. [Installation](#installation)
2. [Registration](#registration)
3. [Commands](#commands)
4. [Queries](#queries)
5. [Notifications](#notifications)
6. [Streaming Queries](#streaming-queries)
7. [Pipeline Behaviors](#pipeline-behaviors)
8. [Stream Pipeline Behaviors](#stream-pipeline-behaviors)
9. [Notification Pipeline Behaviors](#notification-pipeline-behaviors)
10. [Timeout Behavior](#timeout-behavior)
11. [Resilience & Retry](#resilience--retry)
12. [Railway-Oriented Programming](#railway-oriented-programming)
13. [Error Type](#error-type)
14. [Startup Validation](#startup-validation)
15. [OpenTelemetry](#opentelemetry)
16. [Performance Notes](#performance-notes)

---

## Installation

```bash
dotnet add package ForEach.Conduit
```

---

## Registration

### Scan an assembly — zero boilerplate

```csharp
builder.Services.AddConduit(typeof(Program).Assembly);
```

Registers `IDispatcher`, `ICommandDispatcher`, `IQueryDispatcher`, `IEventPublisher`, and every
handler in the assembly automatically. That's it.

### Fluent builder — when you need control

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    .AddPipelineBehavior(typeof(LoggingBehavior<,>))      // open-generic, applies to all
    .AddPipelineBehavior(typeof(ValidationBehavior<,>))
    .AddNotificationPipelineBehavior(typeof(NotificationLoggingBehavior<>))
    .AddRetryBehavior<SendEmailCommand>(maxAttempts: 3)
    .AddTimeoutBehavior<GenerateReportCommand>(TimeSpan.FromSeconds(30));
```

### Manual registration — explicit, zero magic

```csharp
builder.Services
    .AddConduit()
    .AddCommandHandler<CreateOrderCommand, OrderDto, CreateOrderHandler>()
    .AddQueryHandler<GetOrderQuery, OrderDto, GetOrderQueryHandler>()
    .AddNotificationHandler<OrderPlacedEvent, SendConfirmationEmailHandler>()
    .AddNotificationHandler<OrderPlacedEvent, UpdateInventoryHandler>();
```

### Inject what you actually need

Conduit splits dispatch into four focused interfaces. Inject only what your class uses — no
"god object" dependency on a full mediator.

```csharp
// Commands only
public class OrdersController(ICommandDispatcher commands) { ... }

// Queries only
public class ReportsController(IQueryDispatcher queries) { ... }

// Events only
public class DomainEventService(IEventPublisher publisher) { ... }

// Everything (rare, typically only at the composition root)
public class Orchestrator(IDispatcher dispatcher) { ... }
```

---

## Commands

Commands represent intent to change state. Conduit supports both void commands
(side-effects only) and typed commands (return a result).

### Define

```csharp
// Void command — fire and confirm, no data returned
public record DeleteProductCommand(Guid ProductId) : ICommand;

// Typed command — returns a result
public record CreateOrderCommand(Guid CustomerId, List<OrderLineDto> Lines) : ICommand<OrderDto>;
```

### Implement

```csharp
public class CreateOrderHandler(IOrderRepository orders, IUnitOfWork uow)
    : ICommandHandler<CreateOrderCommand, OrderDto>
{
    public async ValueTask<ValueResult<OrderDto>> Handle(
        CreateOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Lines.Count == 0)
            return ValueResult<OrderDto>.Failure(Error.Validation("Order must have at least one line."));

        var order = Order.Create(command.CustomerId, command.Lines);
        await orders.AddAsync(order, cancellationToken);
        await uow.CommitAsync(cancellationToken);

        return ValueResult<OrderDto>.Success(order.ToDto());
    }
}
```

### Dispatch

```csharp
// Minimal API — railway chain, no try/catch
app.MapPost("/orders", async (CreateOrderRequest req, ICommandDispatcher dispatcher) =>
{
    return await dispatcher
        .Send(new CreateOrderCommand(req.CustomerId, req.Lines))
        .MatchAsync(
            onSuccess: order => ValueTask.FromResult(Results.Created($"/orders/{order.Id}", order)),
            onFailure: err   => ValueTask.FromResult(err.Code switch
            {
                "Validation.Failed"   => Results.UnprocessableEntity(err.Message),
                "Validation.Multiple" => Results.UnprocessableEntity(
                    err.InnerErrors!.Select(e => e.Message)),
                "Conflict"            => Results.Conflict(err.Message),
                _                     => Results.Problem(err.Message)
            }));
});
```

---

## Queries

Queries read state without modifying it. The same railway pattern applies — a handler either
succeeds with a value or fails with an `Error`.

### Define

```csharp
public record GetOrderQuery(Guid OrderId) : IQuery<OrderDto>;
public record SearchOrdersQuery(Guid CustomerId, int Page, int PageSize) : IQuery<PagedResult<OrderDto>>;
```

### Implement

```csharp
public class GetOrderHandler(IOrderRepository orders)
    : IQueryHandler<GetOrderQuery, OrderDto>
{
    public async ValueTask<ValueResult<OrderDto>> Handle(
        GetOrderQuery query, CancellationToken cancellationToken = default)
    {
        var order = await orders.FindAsync(query.OrderId, cancellationToken);

        return order is null
            ? ValueResult<OrderDto>.Failure(Error.NotFound($"Order {query.OrderId} not found."))
            : ValueResult<OrderDto>.Success(order.ToDto());
    }
}
```

### Dispatch

```csharp
var result = await dispatcher.Query(new GetOrderQuery(orderId));

return result.Match(
    onSuccess: order => Ok(order),
    onFailure: err   => err.Code == "NotFound" ? NotFound(err.Message) : BadRequest(err.Message));
```

---

## Notifications

Notifications are fan-out events. Any number of handlers can subscribe. Publishing never fails
due to missing handlers — zero handlers for a notification type is valid.

### Define

```csharp
public record OrderPlacedEvent(Guid OrderId, Guid CustomerId, decimal Total) : INotification;
```

### Implement multiple handlers

```csharp
public class SendConfirmationEmailHandler(IEmailService email)
    : INotificationHandler<OrderPlacedEvent>
{
    public async ValueTask Handle(
        OrderPlacedEvent notification, CancellationToken cancellationToken = default)
        => await email.SendOrderConfirmationAsync(
               notification.CustomerId, notification.OrderId, cancellationToken);
}

public class UpdateInventoryHandler(IInventoryService inventory)
    : INotificationHandler<OrderPlacedEvent>
{
    public async ValueTask Handle(
        OrderPlacedEvent notification, CancellationToken cancellationToken = default)
        => await inventory.ReserveItemsForOrderAsync(notification.OrderId, cancellationToken);
}

public class AuditHandler(IAuditLog audit)
    : INotificationHandler<OrderPlacedEvent>
{
    public ValueTask Handle(OrderPlacedEvent notification, CancellationToken cancellationToken = default)
    {
        audit.Record("order.placed", notification.OrderId, notification.Total);
        return ValueTask.CompletedTask;
    }
}
```

### Publish

```csharp
// Sequential — guaranteed order, first failure stops the chain
await publisher.Publish(new OrderPlacedEvent(order.Id, order.CustomerId, order.Total));

// Parallel — all handlers run concurrently
// AggregateException if any fail; each inner exception names the handler type
await publisher.PublishParallel(new OrderPlacedEvent(order.Id, order.CustomerId, order.Total));
```

---

## Streaming Queries

Use streaming queries for large result sets, real-time data feeds, or any scenario where you
want to process items as they arrive without buffering the entire collection in memory.

### Define and implement

```csharp
public record ExportOrdersQuery(Guid TenantId, DateRange Range) : IStreamQuery;

public class ExportOrdersHandler(AppDbContext db)
    : IStreamQueryHandler<ExportOrdersQuery, OrderExportRow>
{
    public IAsyncEnumerable<OrderExportRow> Handle(
        ExportOrdersQuery query, CancellationToken cancellationToken = default)
        => db.Orders
            .Where(o => o.TenantId == query.TenantId
                     && o.CreatedAt >= query.Range.From
                     && o.CreatedAt <= query.Range.To)
            .OrderBy(o => o.CreatedAt)
            .Select(o => new OrderExportRow(o.Id, o.Total, o.CreatedAt))
            .AsAsyncEnumerable();
}
```

### Consume

```csharp
// Stream directly to an HTTP response — no memory pressure from buffering
app.MapGet("/exports/orders", async (IQueryDispatcher dispatcher, HttpResponse response) =>
{
    response.ContentType = "text/csv";
    await response.WriteAsync("Id,Total,Date\n");

    await foreach (var row in dispatcher.Stream(new ExportOrdersQuery(tenantId, range)))
        await response.WriteAsync(row.ToCsv() + "\n");
});
```

---

## Pipeline Behaviors

Behaviors are middleware that wrap every handler execution. They compose around the handler
like an onion — first registered runs outermost (before the handler, returns last).

Use behaviors for: **logging**, **validation**, **authorization**, **caching**, **metrics**,
**exception mapping**, and any other cross-cutting concern.

### Logging behavior (open-generic — applies to all commands and queries)

```csharp
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var requestName = typeof(TRequest).Name;
        logger.LogInformation("→ {Request}", requestName);

        var sw = Stopwatch.StartNew();
        var response = await next();
        sw.Stop();

        logger.LogInformation("← {Request} completed in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
        return response;
    }
}
```

### Validation behavior (FluentValidation integration)

```csharp
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(request, cancellationToken))))
            .Where(r => !r.IsValid)
            .SelectMany(r => r.Errors)
            .Select(e => Error.Validation(e.ErrorMessage))
            .ToArray();

        if (failures.Length > 0)
        {
            var error = failures.Length == 1 ? failures[0] : Error.Aggregate(failures);
            return (TResponse)(object)(typeof(TResponse) == typeof(ValueResult)
                ? ValueResult.Failure(error)
                : Activator.CreateInstance(typeof(TResponse), false, error)!);
        }

        return await next();
    }
}
```

### Authorization behavior

```csharp
public class AuthorizationBehavior<TRequest, TResponse>(ICurrentUser user)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IAuthorizedRequest
{
    public async ValueTask<TResponse> Handle(
        TRequest request,
        Func<ValueTask<TResponse>> next,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(request.RequiredRole))
            return (TResponse)(object)ValueResult.Failure(
                Error.Unauthorized($"Role '{request.RequiredRole}' is required."));

        return await next();
    }
}
```

### Register

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    .AddPipelineBehavior(typeof(LoggingBehavior<,>))      // outermost: logs all requests
    .AddPipelineBehavior(typeof(AuthorizationBehavior<,>)) // next: checks role
    .AddPipelineBehavior(typeof(ValidationBehavior<,>));   // innermost: validates before handler

// Closed behavior — applies only to one specific request type
builder.Services.AddPipelineBehavior<PlaceOrderCommand, ValueResult<OrderDto>, IdempotencyBehavior>();
```

---

## Stream Pipeline Behaviors

Stream behaviors are middleware specifically for streaming queries. They can log, authorize,
filter, or transform the `IAsyncEnumerable<T>` stream before it reaches the consumer — without
modifying the handler.

### Auth check before streaming begins

```csharp
public class StreamAuthBehavior<TQuery, T>(ICurrentUser user)
    : IStreamPipelineBehavior<TQuery, T>
    where TQuery : IStreamQuery, IAuthorizedRequest
{
    public IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        CancellationToken cancellationToken = default)
    {
        if (!user.IsInRole(query.RequiredRole))
            throw new UnauthorizedAccessException($"Role '{query.RequiredRole}' is required.");

        return next();
    }
}
```

### Per-item transformation / enrichment

```csharp
public class AuditStreamBehavior<TQuery, T>(IAuditLog audit)
    : IStreamPipelineBehavior<TQuery, T>
    where TQuery : IStreamQuery
{
    public async IAsyncEnumerable<T> Handle(
        TQuery query,
        Func<IAsyncEnumerable<T>> next,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        audit.Record($"Stream started: {typeof(TQuery).Name}");
        var count = 0;

        await foreach (var item in next().WithCancellation(cancellationToken))
        {
            count++;
            yield return item;
        }

        audit.Record($"Stream completed: {typeof(TQuery).Name}, {count} items");
    }
}
```

### Register

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    .AddStreamPipelineBehavior(typeof(AuditStreamBehavior<,>))  // open-generic
    .AddStreamPipelineBehavior(typeof(StreamAuthBehavior<,>));

// Closed — only for a specific streaming query
builder.Services.AddStreamPipelineBehavior<ExportOrdersQuery, OrderExportRow, TenantFilterBehavior>();
```

---

## Notification Pipeline Behaviors

Notification behaviors wrap the entire fan-out — `next()` runs all registered handlers.
Unique to Conduit; MediatR and DispatchR have no equivalent.

```csharp
public class NotificationLoggingBehavior<TNotification>(
    ILogger<NotificationLoggingBehavior<TNotification>> logger)
    : INotificationPipelineBehavior<TNotification>
    where TNotification : INotification
{
    public async ValueTask Handle(
        TNotification notification,
        Func<ValueTask> next,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("→ Publishing {Notification}", typeof(TNotification).Name);
        var sw = Stopwatch.StartNew();

        await next(); // runs all handlers

        logger.LogInformation("← Published {Notification} in {ElapsedMs}ms",
            typeof(TNotification).Name, sw.ElapsedMilliseconds);
    }
}

// Register
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    .AddNotificationPipelineBehavior(typeof(NotificationLoggingBehavior<>));
```

---

## Timeout Behavior

Protect handlers from hanging indefinitely. When the deadline is exceeded, dispatch returns a
`ValueResult.Failure` with `Error.Timeout` — no exception escapes the behavior.

Caller-initiated cancellations are correctly distinguished from deadline expiry: if *you* cancel
the token, the `OperationCanceledException` propagates as normal; if the *timeout* fires, you
get a clean `Error.Timeout` result.

### Register (DI extension)

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    // Void command — 10 second deadline
    .AddTimeoutBehavior<GenerateReportCommand>(TimeSpan.FromSeconds(10))
    // Typed command — 5 second deadline
    .AddTimeoutBehavior<CreateInvoiceCommand, InvoiceDto>(TimeSpan.FromSeconds(5))
    // Query — 3 second deadline
    .AddTimeoutBehavior<SearchProductsQuery, PagedResult<ProductDto>>(TimeSpan.FromSeconds(3));
```

### Handle the result

```csharp
var result = await dispatcher.Send(new GenerateReportCommand(reportId));

if (!result.IsSuccess && result.Error!.Value.Code == "Timeout")
    return Results.Problem("The report took too long to generate. Try a smaller date range.",
        statusCode: 504);
```

### Direct construction (when you need per-instance configuration)

```csharp
// Registered manually as a closed behavior
services.AddScoped<IPipelineBehavior<SlowCommand, ValueResult>>(
    _ => new TimeoutBehavior<SlowCommand>(TimeSpan.FromSeconds(30)));
```

---

## Resilience & Retry

Conduit integrates natively with **Polly v8** via `Microsoft.Extensions.Resilience`. Choose the
right API for your scenario: the convenience helper for most cases, named pipelines for
stateful strategies (circuit breakers, hedging).

### Quick retry — exponential backoff with jitter

The most common scenario: protect a handler against transient failures (database blips,
flaky HTTP calls, temporary lock contention).

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    // 3 total attempts, 200 ms base delay, exponential backoff + jitter
    .AddRetryBehavior<SendEmailCommand>(maxAttempts: 3)

    // Custom delay and selective retry (only retry "Transient" errors)
    .AddRetryBehavior<SyncInventoryCommand>(
        maxAttempts: 5,
        baseDelay: TimeSpan.FromMilliseconds(500),
        shouldRetry: err => err.Code is "Transient" or "DatabaseTimeout")

    // Typed command / query — same API
    .AddRetryBehavior<GetExternalPriceQuery, decimal>(
        maxAttempts: 3,
        baseDelay: TimeSpan.FromMilliseconds(100));
```

> **How it works:** `shouldRetry` is evaluated on `ValueResult` failures. Exceptions (e.g.
> `HttpRequestException`) are always retried regardless of the predicate. When all attempts are
> exhausted, the last failure result (or re-thrown exception) is returned to the caller.

### Named pipeline — circuit breaker and stateful strategies

For circuit breakers, hedging, or rate limiters, register the pipeline explicitly so Polly can
manage shared state correctly (the circuit must be a singleton, not recreated per-request).

```csharp
// 1. Register the Polly pipeline (typically in Program.cs)
builder.Services.AddResiliencePipeline<string, ValueResult>("payment-circuit", pipeline =>
    pipeline.AddCircuitBreaker(new CircuitBreakerStrategyOptions<ValueResult>
    {
        FailureRatio      = 0.5,
        SamplingDuration  = TimeSpan.FromSeconds(30),
        MinimumThroughput = 10,
        BreakDuration     = TimeSpan.FromSeconds(60),
        ShouldHandle      = args => ValueTask.FromResult(
            args.Outcome.Exception is not null ||
            args.Outcome.Result is { IsSuccess: false })
    }));

// 2. Wire it up to a command
builder.Services.AddResilienceBehavior<ProcessPaymentCommand>("payment-circuit");
```

```csharp
// Handle the circuit-open case in your caller
var result = await dispatcher.Send(new ProcessPaymentCommand(orderId, amount));

return result.Match(
    onSuccess: _ => Results.Ok(),
    onFailure: err => err.Code switch
    {
        "CircuitOpen" => Results.StatusCode(503), // service temporarily unavailable
        _             => Results.Problem(err.Message)
    });
```

### Combine timeout + retry

Stack behaviors to get both timeout protection and retry. Registration order matters — outermost
runs first. Typically you want: **retry wraps timeout**, so each attempt gets its own deadline.

```csharp
builder.Services
    .AddConduitHandlers()
    .ScanAssembly(typeof(Program).Assembly)
    .AddRetryBehavior<CallExternalApiCommand>(maxAttempts: 3, baseDelay: TimeSpan.FromMilliseconds(200))
    .AddTimeoutBehavior<CallExternalApiCommand>(TimeSpan.FromSeconds(5)); // per-attempt timeout
```

---

## Railway-Oriented Programming

Every handler returns `ValueResult<T>` — a lightweight `readonly struct` that is either a success
(with a value) or a failure (with an `Error`). No exceptions for domain failures. No null checks.
No `if (result != null && result.Success)` chains.

The extension API in `ForEach.Conduit.Extensions` lets you compose result-returning calls into
a single expression that short-circuits on the first failure.

```csharp
using ForEach.Conduit.Extensions;
```

### Extension map

| Method | What it does |
|---|---|
| `Map` / `MapAsync` | Transform the success value |
| `Bind` / `BindAsync` | Chain into another result-returning call |
| `Match` / `MatchAsync` | Branch on success or failure, produce a different type |
| `Tap` / `TapAsync` | Side-effect on success (value passes through unchanged) |
| `TapFailure` / `TapFailureAsync` | Side-effect on failure (error passes through unchanged) |
| `Ensure` / `EnsureAsync` | Fail if a predicate is not met |
| `Recover` / `RecoverAsync` | Replace a failure with a success value |
| `RecoverWith` / `RecoverWithAsync` | Replace a failure with another result |
| `Combine` / `CombineAsync` | Merge two independent results; fail if either fails |
| `CombineSequentialAsync` | Chain two dependent results sequentially |
| `Finally` / `FinallyAsync` | Run an action regardless of success or failure |
| `Flatten` | Unwrap `ValueResult<ValueResult<T>>` |

---

### Example 1 — Linear request pipeline

Load an order, check authorization, apply a discount, save, and return a DTO — all in one
expression. The chain short-circuits to the error on the first failure; no exception handling needed.

```csharp
return await dispatcher
    .Query(new GetOrderQuery(orderId))
    .EnsureAsync(
        o => _auth.CanEditAsync(userId, o.Id, cancellationToken),
        Error.Unauthorized("You do not own this order."))
    .BindAsync(o => _discounts.ApplyAsync(o, promoCode, cancellationToken))
    .BindAsync(async discountedOrder =>
    {
        await _orders.UpdateAsync(discountedOrder, cancellationToken);
        await _uow.CommitAsync(cancellationToken);
        return ValueResult<OrderDto>.Success(discountedOrder.ToDto());
    })
    .MatchAsync(
        onSuccess: dto => ValueTask.FromResult(Results.Ok(dto)),
        onFailure: err  => ValueTask.FromResult(err.Code switch
        {
            "NotFound"     => Results.NotFound(err.Message),
            "Unauthorized" => Results.Forbid(),
            _              => Results.BadRequest(err.Message)
        }));
```

---

### Example 2 — Parallel independent lookups

Run two queries concurrently; if either fails, short-circuit without a `Task.WhenAll` try/catch.

```csharp
var customerTask = dispatcher.Query(new GetCustomerQuery(customerId));
var catalogTask  = dispatcher.Query(new GetCatalogQuery(tenantId));

return await customerTask
    .CombineAsync(catalogTask, (customer, catalog) => BuildOrderSummary(customer, catalog))
    .MatchAsync(
        onSuccess: summary => ValueTask.FromResult(Results.Ok(summary)),
        onFailure: err     => ValueTask.FromResult(Results.Problem(err.Message)));
```

---

### Example 3 — Guard chain (multiple business rules)

Validate several domain invariants before executing a command. Each `Ensure` call is skipped if
an earlier one already failed.

```csharp
var result = await dispatcher
    .Query(new GetOrderQuery(orderId))
    .Ensure(o => o.Status == OrderStatus.Confirmed,
            Error.Conflict("Only confirmed orders can be invoiced."))
    .Ensure(o => o.InvoiceId is null,
            Error.Conflict("Invoice already exists for this order."))
    .Ensure(o => o.Total > 0,
            Error.Validation("Cannot invoice a zero-value order."))
    .BindAsync(o => dispatcher.Send(new GenerateInvoiceCommand(o.Id)));
```

---

### Example 4 — Cache fallback with Recover

Try a fast cache lookup; on `NotFound`, fall back to the database and warm the cache.

```csharp
return await dispatcher
    .Query(new GetProductFromCacheQuery(productId))
    .RecoverWithAsync(async err =>
    {
        if (err.Code != "NotFound")
            return ValueResult<ProductDto>.Failure(err); // not a cache miss — re-propagate

        var fromDb = await dispatcher.Query(new GetProductFromDbQuery(productId));
        if (fromDb.IsSuccess)
            await _cache.SetAsync(productId, fromDb.Value!, cancellationToken);
        return fromDb;
    });
```

---

### Example 5 — Side effects with Tap and TapFailure

Emit metrics without interrupting the chain. Both taps pass the value/error through unchanged.

```csharp
var result = await dispatcher
    .Send(new PlaceOrderCommand(cart))
    .TapAsync(order  => _metrics.IncrementAsync("orders.placed", cancellationToken))
    .TapAsync(order  => _outbox.EnqueueAsync(new OrderConfirmationEmail(order.Id), cancellationToken))
    .TapFailureAsync(err => _metrics.IncrementAsync("orders.failed", cancellationToken));
```

---

### Example 6 — Aggregate validation errors

Collect all failures before returning, rather than stopping on the first one.

```csharp
var errors = new List<Error>();

if (string.IsNullOrWhiteSpace(request.Name))
    errors.Add(Error.Validation("Name is required."));
if (request.Price <= 0)
    errors.Add(Error.Validation("Price must be greater than zero."));
if (request.StockQuantity < 0)
    errors.Add(Error.Validation("Stock cannot be negative."));

if (errors.Count > 0)
    return ValueResult<ProductDto>.Failure(Error.Aggregate(errors));

// All clear — continue
```

Map the aggregate to HTTP:

```csharp
if (!result.IsSuccess && result.Error!.Value.InnerErrors is { } inner)
{
    foreach (var e in inner)
        modelState.AddModelError(string.Empty, e.Message);
    return UnprocessableEntity(modelState);
}
```

---

## Error Type

`Error` is a `readonly struct` — zero heap pressure on the success path (the common case).

```csharp
// Factory methods — cover the common HTTP/domain vocabulary
Error.Validation("Price must be positive.");          // code: "Validation.Failed"
Error.NotFound("Order ABC-123 not found.");            // code: "NotFound"
Error.Conflict("Username already taken.");             // code: "Conflict"
Error.Unauthorized("Insufficient permissions.");       // code: "Unauthorized"
Error.Timeout("Handler timed out after 5000ms.");     // code: "Timeout"
Error.CircuitOpen("Payment service unavailable.");    // code: "CircuitOpen"
Error.FromException(ex);                              // code: exception type name
Error.Aggregate(errors);                              // code: "Validation.Multiple"
Error.Aggregate(new[] { "msg1", "msg2" });            // convenience overload
```

```csharp
// Properties
error.Code         // structured code for switch expressions
error.Message      // human-readable description
error.Exception    // original exception (FromException only)
error.InnerErrors  // non-null on Aggregate errors only
error.IsValid      // false for default(Error) — guards against uninitialized structs
error.ToString()   // "[NotFound] Order ABC-123 not found."
```

```csharp
// Equality — based on Code + Message, safe in dictionaries and sets
error1 == error2
```

> **Note:** `default(Error)` has `Code == null`. Always construct via factory methods or the
> public constructor. Check `error.IsValid` if you receive an `Error` from an untrusted source.

---

## Startup Validation

Catch missing handler registrations at application startup, before your first request hits:

```csharp
var app = builder.Build();

// Throws InvalidOperationException listing every unregistered ICommand / IQuery handler
// found in the given assembly. Notifications are excluded — zero handlers is valid.
app.Services.ValidateConduitHandlers(typeof(Program).Assembly);

app.Run();
```

Example error message:

```
InvalidOperationException: The following Conduit handlers are missing registrations:
  - ICommandHandler<CreateOrderCommand, OrderDto>
  - IQueryHandler<GetProductQuery, ProductDto>
```

---

## OpenTelemetry

Conduit emits spans for every dispatch automatically. Zero overhead when no listener is
attached — `ActivitySource.StartActivity` returns `null` on the fast path, no async state
machine is allocated.

```csharp
using OpenTelemetry.Trace;
using ForEach.Conduit.Diagnostics;

builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing
        .AddSource(ConduitTelemetry.SourceName) // "ForEach.Conduit"
        .AddOtlpExporter());
```

Each span is named after the request or notification type and carries:

| Tag | Example value |
|---|---|
| `conduit.request_type` | `MyApp.Features.Orders.CreateOrderCommand` |
| `conduit.success` | `true` / `false` |
| `conduit.error_code` | `"Validation.Failed"` |

---

## Performance Notes

**Zero reflection on the hot path.** The first dispatch for a given type allocates one wrapper
via `MakeGenericType` + `Activator.CreateInstance`, then stores a strongly-typed delegate in a
static `ConcurrentDictionary<Type, Wrapper>`. All subsequent dispatches are a dictionary lookup
plus a virtual method call — equivalent to a direct interface call. No `MethodInfo.Invoke`, no
`object[]` argument arrays, no boxing.

**`ValueResult<T>` is a `readonly struct`.** On the success path the value sits on the stack
with no heap allocation. Only the error path allocates (the two `string` fields of `Error` —
typically compile-time literals that are already interned).

**`Result<T>` is a class.** Use it at system boundaries where `Task<T>` is required (e.g.,
controller action methods that return `Task<IActionResult>`). Prefer `ValueResult<T>` in all
application and domain code.

**Pipeline behaviors are resolved via `GetServices<T>()` per dispatch.** This is a
`ConcurrentDictionary` lookup in the DI container — negligible for most applications. If you
register many open-generic behaviors and measure it as a bottleneck, cache the resolved list
in a singleton wrapper service.

**Resilience pipelines are singletons.** When you use `AddResilienceBehavior(pipelineName)`,
the `ResiliencePipeline<T>` is fetched from `ResiliencePipelineProvider<string>` which keeps
all pipelines as singletons. Circuit breaker state is shared correctly across all requests.
When you use `AddRetryBehavior`, the pipeline is built once at registration time and captured
by the factory lambda — same singleton semantics.

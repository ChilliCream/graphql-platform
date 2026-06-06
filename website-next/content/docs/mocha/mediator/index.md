---
title: "Overview"
description: "Use the Mocha Mediator to dispatch commands, queries, and notifications within a single process. Source-generated at compile time for zero-reflection dispatch with pre-compiled middleware pipelines."
---

```csharp
builder.Services
    .AddMediator()
    .AddCatalog(); // source-generated from your assembly name
```

That registers the mediator infrastructure, discovers your handlers at compile time, and wires up the dispatch pipeline. `.AddCatalog()` is a source-generated extension method - it knows your handlers and message types at compile time and produces direct dispatch code with no reflection.

# What the mediator is

The mediator sits between your application code and your handlers. Instead of injecting handler interfaces directly, you inject `IMediator` (or `ISender` / `IPublisher`) and dispatch messages through it. The mediator routes each message to the correct handler based on its type.

```csharp
// Without mediator - tight coupling
app.MapPost("/orders", async (PlaceOrderCommandHandler handler) =>
    await handler.HandleAsync(new PlaceOrderCommand(...)));

// With mediator - decoupled dispatch
app.MapPost("/orders", async (ISender sender) =>
    await sender.SendAsync(new PlaceOrderCommand(...)));
```

The mediator provides three things your handlers cannot do alone: a middleware pipeline that wraps every handler invocation with cross-cutting concerns (logging, transactions, validation), polymorphic dispatch that routes messages by type at runtime, and a seam between your application layer and your domain logic. This is the [Mediator pattern](https://refactoring.guru/design-patterns/mediator) -- objects communicate through a central hub instead of referencing each other directly.

If you have used [MediatR](https://github.com/jbogard/MediatR), the concepts are familiar. Mocha Mediator takes a different approach to performance: a [Roslyn source generator](https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/source-generators-overview) analyzes your handler registrations at compile time and produces pre-compiled pipeline delegates. No `MakeGenericType`, no service provider lookups to resolve the pipeline, no reflection at runtime.

# When to use the mediator vs. the message bus

Mocha has two dispatch mechanisms. Use the right one for the situation:

| Use the **mediator** when...                                                                                           | Use the **message bus** when...                     |
| ---------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------- |
| Dispatch stays in-process                                                                                              | Messages cross process or service boundaries        |
| You want [CQRS](https://learn.microsoft.com/en-us/azure/architecture/patterns/cqrs) separation of commands and queries | You want pub/sub events across services             |
| You need a request/response pipeline with middleware                                                                   | You need transport-level features (retries, outbox) |
| Handlers live in the same assembly or solution                                                                         | Handlers live in different services                 |

The mediator and the message bus complement each other. A common pattern is to use the mediator for in-process CQRS dispatch within a service, and the message bus for inter-service event-driven communication.

# Messages

Messages are plain C# types that implement a marker interface. The marker interface tells the mediator how to route the message and what return type to expect.

## Commands

Commands represent actions that change state. Use imperative verb-noun naming: `PlaceOrder`, `ProcessPayment`.

```csharp
// A command that returns no response
public record DeleteOrderCommand(Guid OrderId) : ICommand;

// A command that returns a response
public record PlaceOrderCommand(
    Guid ProductId,
    int Quantity,
    string CustomerId) : ICommand<PlaceOrderResult>;

public record PlaceOrderResult(bool Success, Guid? OrderId = null, string? Error = null);
```

## Queries

Queries represent read operations that return data without side effects. Use noun-based naming: `GetProducts`, `GetOrderById`.

```csharp
public record GetProductsQuery : IQuery<List<Product>>;

public record GetProductByIdQuery(Guid Id) : IQuery<Product?>;
```

## Notifications

Notifications represent events that multiple handlers can observe. Use past-tense naming: `OrderPlaced`, `PaymentCompleted`.

```csharp
public record OrderPlacedNotification(
    Guid OrderId,
    decimal Amount) : INotification;
```

## Message type reference

| Interface             | Purpose              | Dispatch method | Return type            |
| --------------------- | -------------------- | --------------- | ---------------------- |
| `ICommand`            | Action, no response  | `SendAsync`     | `ValueTask`            |
| `ICommand<TResponse>` | Action with response | `SendAsync`     | `ValueTask<TResponse>` |
| `IQuery<TResponse>`   | Read operation       | `QueryAsync`    | `ValueTask<TResponse>` |
| `INotification`       | Multi-handler event  | `PublishAsync`  | `ValueTask`            |

# Handlers

Each message type has a corresponding handler interface. The mediator routes each message to exactly one handler - except notifications, which fan out to all registered handlers.

## Command handlers

```csharp
// Handles a void command
public sealed class DeleteOrderCommandHandler(AppDbContext db)
    : ICommandHandler<DeleteOrderCommand>
{
    public async ValueTask HandleAsync(
        DeleteOrderCommand command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync(command.OrderId);
        if (order is not null)
        {
            db.Orders.Remove(order);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}

// Handles a command with a response
public sealed class PlaceOrderCommandHandler(AppDbContext db)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            ProductId = command.ProductId,
            Quantity = command.Quantity,
            CustomerId = command.CustomerId
        };

        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);

        return new PlaceOrderResult(true, order.Id);
    }
}
```

## Query handlers

```csharp
public sealed class GetProductsQueryHandler(AppDbContext db)
    : IQueryHandler<GetProductsQuery, List<Product>>
{
    public async ValueTask<List<Product>> HandleAsync(
        GetProductsQuery query, CancellationToken cancellationToken)
        => await db.Products.ToListAsync(cancellationToken);
}
```

## Notification handlers

Multiple handlers can subscribe to the same notification type. The mediator invokes all of them.

```csharp
public sealed class SendOrderConfirmationEmail(IEmailService email)
    : INotificationHandler<OrderPlacedNotification>
{
    public async ValueTask HandleAsync(
        OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await email.SendAsync(
            $"Order {notification.OrderId} confirmed", cancellationToken);
    }
}

public sealed class UpdateAnalyticsDashboard(IAnalytics analytics)
    : INotificationHandler<OrderPlacedNotification>
{
    public async ValueTask HandleAsync(
        OrderPlacedNotification notification, CancellationToken cancellationToken)
    {
        await analytics.RecordOrderAsync(
            notification.OrderId, notification.Amount);
    }
}
```

## Handler interface reference

| Interface                              | Message type          | Response    |
| -------------------------------------- | --------------------- | ----------- |
| `ICommandHandler<TCommand>`            | `ICommand`            | void        |
| `ICommandHandler<TCommand, TResponse>` | `ICommand<TResponse>` | `TResponse` |
| `IQueryHandler<TQuery, TResponse>`     | `IQuery<TResponse>`   | `TResponse` |
| `INotificationHandler<TNotification>`  | `INotification`       | void        |

# Dispatching messages

Inject `IMediator`, `ISender`, or `IPublisher` from DI and call the appropriate method.

```csharp
// Send a command with a response
app.MapPost("/orders", async (PlaceOrderRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new PlaceOrderCommand(request.ProductId, request.Quantity, request.CustomerId));

    return result.Success
        ? Results.Created($"/api/orders/{result.OrderId}", result)
        : Results.BadRequest(result.Error);
});

// Send a query
app.MapGet("/products", async (ISender sender) =>
    await sender.QueryAsync(new GetProductsQuery()));

// Publish a notification
app.MapPost("/orders/{id}/ship", async (Guid id, IPublisher publisher) =>
{
    await publisher.PublishAsync(new OrderShippedNotification(id));
    return Results.Ok();
});
```

`ISender` handles commands and queries. `IPublisher` handles notifications. `IMediator` combines both interfaces - inject it when you need both in the same class.

## Untyped dispatch

When the message type is not known at compile time, use the `object`-based overloads:

```csharp
// Dispatch a command or query by runtime type
object message = GetMessageFromSomewhere();
object? result = await sender.SendAsync(message);

// Dispatch a notification by runtime type
object notification = GetNotificationFromSomewhere();
await publisher.PublishAsync(notification);
```

The runtime type of the message must implement one of the marker interfaces (`ICommand`, `ICommand<TResponse>`, `IQuery<TResponse>`, or `INotification`). An exception is thrown if it does not.

# Registration and source generation

## Register the mediator

```csharp
builder.Services
    .AddMediator()
    .AddCatalog(); // source-generated from assembly name "Demo.Catalog"
```

`AddMediator()` registers the core mediator infrastructure and the default notification publish mode. The source-generated `Add{ModuleName}()` method registers:

- All command, query, and notification handlers found in your assembly
- Pre-compiled terminal delegates for each message type (no reflection at runtime)

You do not register handlers manually unless you need to. The source generator discovers them by scanning for classes that implement handler interfaces.

## Module naming

The source generator names the registration method based on your assembly:

1. If you apply `[assembly: MediatorModule("Billing")]`, the method is `AddBilling()`
2. Otherwise, it uses the last segment of the assembly name: `Demo.Catalog` produces `AddCatalog()`

To set an explicit module name, add the attribute to any file in your project:

```csharp
using Mocha.Mediator;

[assembly: MediatorModule("Billing")]
```

## Manual handler registration with AddHandler

When you need to register handlers outside the source generator's reach - from a plugin assembly, a dynamically loaded module, or in integration tests - use `AddHandler<T>()`:

```csharp
builder.Services
    .AddMediator()
    .AddHandler<PlaceOrderCommandHandler>()
    .AddHandler<GetProductsQueryHandler>()
    .AddHandler<OrderShippedEmailHandler>();
```

`AddHandler<T>()` inspects the type for handler interfaces (`ICommandHandler`, `IQueryHandler`, `INotificationHandler`), builds the pipeline configuration, and registers the handler in DI. It throws `InvalidOperationException` if `T` does not implement a handler interface.

You can mix source-generated and manual registration. If both register the same handler type, the configurations are composed:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()                              // source-generated handlers
    .AddHandler<ExternalPaymentHandler>();      // additional handler from another assembly
```

> **Prefer the source generator.** The source-generated registration path uses pre-compiled terminal delegates with no reflection. The source-generated output is designed for long-term stability across versions. Manual registration is available for edge cases but its internal behavior may change between releases.

## Configure service lifetime

By default, handlers are registered as `Scoped`. To change the default:

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(options =>
    {
        options.ServiceLifetime = ServiceLifetime.Transient;
    })
    .AddCatalog();
```

Call `ConfigureOptions` before `Add{ModuleName}()` so the source-generated method reads the updated lifetime.

## Configuration options reference

| Option                    | Type                      | Default      | Description                                   |
| ------------------------- | ------------------------- | ------------ | --------------------------------------------- |
| `ServiceLifetime`         | `ServiceLifetime`         | `Scoped`     | Default DI lifetime for handler registrations |
| `NotificationPublishMode` | `NotificationPublishMode` | `Sequential` | How notification handlers are dispatched      |

# Named mediators

To run multiple independent mediator instances (each with its own handlers and middleware), use named mediators. Named mediators use .NET's [keyed dependency injection](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection#keyed-services).

```csharp
// Register a named mediator
builder.Services
    .AddMediator("billing")
    .AddBilling();

// Register the default (unnamed) mediator
builder.Services
    .AddMediator()
    .AddCatalog();
```

Resolve a named mediator from DI:

```csharp
app.MapPost("/payments", async (
    [FromKeyedServices("billing")] ISender sender,
    ProcessPaymentRequest request) =>
{
    var result = await sender.SendAsync(
        new ProcessPaymentCommand(request.Amount));
    return Results.Ok(result);
});

// Or resolve from IServiceProvider directly
var billingMediator = serviceProvider
    .GetRequiredKeyedService<IMediator>("billing");
```

Each named mediator has its own handler registrations, middleware pipeline, and runtime. The default mediator (registered with `AddMediator()` without a name) is resolved normally without keyed services.

# Putting it together

Here is a complete minimal API application with commands, queries, and notifications:

```csharp
using Mocha.Mediator;

var builder = WebApplication.CreateBuilder(args);

// Register mediator with source-generated handlers
builder.Services
    .AddMediator()
    .AddMyApp();

var app = builder.Build();

// Command - place an order
app.MapPost("/orders", async (PlaceOrderRequest request, ISender sender) =>
{
    var result = await sender.SendAsync(
        new PlaceOrderCommand(request.ProductId, request.Quantity));
    return result.Success
        ? Results.Created($"/orders/{result.OrderId}", result)
        : Results.BadRequest(result.Error);
});

// Query - list products
app.MapGet("/products", async (ISender sender) =>
    await sender.QueryAsync(new GetProductsQuery()));

// Notification - broadcast that an order shipped
app.MapPost("/orders/{id}/ship", async (Guid id, IPublisher publisher) =>
{
    await publisher.PublishAsync(new OrderShippedNotification(id));
    return Results.Ok();
});

app.Run();

// ── Messages ────────────────────────────────────────

public record PlaceOrderCommand(Guid ProductId, int Quantity)
    : ICommand<PlaceOrderResult>;

public record PlaceOrderResult(
    bool Success, Guid? OrderId = null, string? Error = null);

public record GetProductsQuery : IQuery<IReadOnlyList<ProductDto>>;

public record ProductDto(Guid Id, string Name, decimal Price);

public record OrderShippedNotification(Guid OrderId) : INotification;

// ── Handlers ────────────────────────────────────────

public sealed class PlaceOrderCommandHandler(ILogger<PlaceOrderCommandHandler> logger)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = Guid.NewGuid();
        logger.LogInformation("Order {OrderId} placed", orderId);
        return new ValueTask<PlaceOrderResult>(
            new PlaceOrderResult(true, orderId));
    }
}

public sealed class GetProductsQueryHandler
    : IQueryHandler<GetProductsQuery, IReadOnlyList<ProductDto>>
{
    private static readonly IReadOnlyList<ProductDto> Products =
    [
        new(Guid.NewGuid(), "Keyboard", 149.99m),
        new(Guid.NewGuid(), "Mouse", 79.99m),
    ];

    public ValueTask<IReadOnlyList<ProductDto>> HandleAsync(
        GetProductsQuery query, CancellationToken cancellationToken)
        => new(Products);
}

public sealed class OrderShippedEmailHandler(ILogger<OrderShippedEmailHandler> logger)
    : INotificationHandler<OrderShippedNotification>
{
    public ValueTask HandleAsync(
        OrderShippedNotification notification,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order {OrderId} shipped - email sent", notification.OrderId);
        return ValueTask.CompletedTask;
    }
}

public record PlaceOrderRequest(Guid ProductId, int Quantity);
```

If everything worked, `dotnet run` starts the server and you can:

- `POST /orders` with a JSON body to place an order
- `GET /products` to list products
- `POST /orders/{id}/ship` to publish a shipped notification

# Troubleshooting

## `InvalidOperationException: No pipeline registered for message type`

The source generator did not find a handler for your message type. Verify:

- Your handler class implements the correct interface (e.g., `ICommandHandler<YourCommand, YourResponse>`)
- Your message type implements the correct marker interface (e.g., `ICommand<YourResponse>`)
- You called the source-generated `.Add{ModuleName}()` method on the mediator builder
- The handler is in the same project that the source generator can see
- The project references `Mocha.Analyzers` as an analyzer (not a regular project reference)

## Handlers are not being called

If dispatch succeeds but your handler code does not execute, check that:

- Your middleware calls the `next` delegate - a middleware that forgets to call `next` silently short-circuits the pipeline
- You are not accidentally registering handlers manually in addition to the source-generated method, which could result in duplicate registrations

## The source-generated method does not appear

If IntelliSense does not show `Add{ModuleName}()`:

- Confirm the `Mocha.Analyzers` package is referenced with `OutputItemType="Analyzer"` in your `.csproj`
- Rebuild the project - source generators run during compilation
- Check the build output for analyzer warnings prefixed with `MO`

## `InvalidOperationException` when calling `AddHandler<T>()`

The type you passed does not implement any handler interface. Make sure `T` implements one of: `ICommandHandler<TCommand>`, `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>`, or `INotificationHandler<TNotification>`.

## Named mediator returns wrong handlers

Each named mediator resolves handlers from the same DI container. Make sure you register each module's handlers on the correct `IMediatorHostBuilder` instance - the one returned by the `AddMediator("name")` call for that name.

# Next steps

You have a working mediator with CQRS dispatch. Here is where to go next:

- **Customize the pipeline:** [Pipeline & Middleware](/docs/mocha/v16/mediator/pipeline-and-middleware) - add validation, logging, transactions, and other cross-cutting concerns. Configure notification publish modes and OpenTelemetry instrumentation.

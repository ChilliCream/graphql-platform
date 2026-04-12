---
title: "Handlers and Consumers"
description: "Learn how to implement message handlers in Mocha - event handlers, request handlers, send handlers, batch handlers, and the low-level consumer interface. Understand DI scoping, exception behavior, and how to publish from within a handler."
---

# Handlers and consumers

You implement a handler interface, and the source generator discovers it at compile time. Mocha routes matching messages to your handler automatically. This page covers every handler type, when to use each one, and the patterns that apply to all of them: DI scoping, exception behavior, and publishing from within a handler.

## When to use which handler

Choose a handler interface based on the messaging pattern you are implementing:

| Interface                          | Use when                                                                                        |
| ---------------------------------- | ----------------------------------------------------------------------------------------------- |
| `IEventHandler<T>`                 | Reacting to a published event. No reply expected. Multiple handlers can receive the same event. |
| `IEventRequestHandler<TReq, TRes>` | Handling a request and returning a typed response to the caller.                                |
| `IEventRequestHandler<TReq>`       | Handling a send (fire-and-forget) with no typed response.                                       |
| `IBatchEventHandler<T>`            | Processing multiple events at once for throughput efficiency.                                   |
| `IConsumer<T>`                     | Accessing raw envelope metadata - headers, correlation IDs, or the full consume context.        |

If you have read [Messaging Patterns](/docs/mocha/v1/messaging-patterns), these map directly: `IEventHandler<T>` is for `PublishAsync`, `IEventRequestHandler<TReq>` is for `SendAsync`, and `IEventRequestHandler<TReq, TRes>` is for `RequestAsync`.

# Event handler

An event handler reacts to published events. The publisher does not know or care who handles the event. Multiple handlers can receive the same event type independently.

By the end of this section, you will have a working event handler that logs published orders.

## Define the message

```csharp
// OrderPlaced.cs
namespace MyApp.Messages;

public sealed record OrderPlaced
{
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
}
```

Messages are plain C# records. No base class or marker interface required for pub/sub events.

## Implement the handler

```csharp
// OrderPlacedHandler.cs
using Mocha;
using MyApp.Messages;

namespace MyApp.Handlers;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order placed: {OrderId} for customer {CustomerId}, total {TotalAmount}",
            message.OrderId,
            message.CustomerId,
            message.TotalAmount);

        await Task.CompletedTask;
    }
}
```

`IEventHandler<T>` has a single method: `HandleAsync(T message, CancellationToken cancellationToken)`. The bus deserializes the message and calls your handler. Constructor dependencies are resolved from a scoped DI container - more on this in [DI scoping](#di-scoping).

## Register and run

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddMyApp() // source-generated - registers OrderPlacedHandler automatically
    .AddInMemory(); // or .AddRabbitMQ()

var app = builder.Build();
app.Run();
```

`.AddMyApp()` is a source-generated extension method that discovers all handlers in the assembly and registers them. The source generator found `OrderPlacedHandler`, saw that it implements `IEventHandler<OrderPlaced>`, and emitted a registration call for it. For details on how the source generator works and how to customize the module name, see [Handler Registration](/docs/mocha/v1/handler-registration).

## Verify the handler runs

Publish an event from an API endpoint. `IMessageBus` is a scoped service - resolve it through endpoint injection:

```csharp
app.MapPost("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlaced
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-42",
        TotalAmount = 149.99m
    }, CancellationToken.None);

    return Results.Ok("Published");
});
```

If everything worked, you see this in the console:

```text
info: MyApp.Handlers.OrderPlacedHandler[0]
      Order placed: 3f2504e0-4f89-11d3-9a0c-0305e82c3301 for customer customer-42, total 149.99
```

# Request handler

A request handler processes a request and returns a typed response. The caller awaits the result. Use this when the sender needs data back.

## Define the request and response

The request record must implement `IEventRequest<TResponse>`. This marker interface tells Mocha the expected response type and enables type-safe `RequestAsync` calls.

```csharp
// GetProductRequest.cs
using Mocha;

namespace MyApp.Messages;

public sealed record GetProductRequest : IEventRequest<GetProductResponse>
{
    public required Guid ProductId { get; init; }
}

public sealed record GetProductResponse
{
    public required Guid ProductId { get; init; }
    public required string Name { get; init; }
    public required decimal Price { get; init; }
    public required bool IsAvailable { get; init; }
}
```

## Implement the handler

```csharp
// GetProductRequestHandler.cs
using Mocha;
using MyApp.Messages;

namespace MyApp.Handlers;

public class GetProductRequestHandler(AppDbContext db)
    : IEventRequestHandler<GetProductRequest, GetProductResponse>
{
    public async ValueTask<GetProductResponse> HandleAsync(
        GetProductRequest request,
        CancellationToken cancellationToken)
    {
        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        return new GetProductResponse
        {
            ProductId = request.ProductId,
            Name = product?.Name ?? string.Empty,
            Price = product?.Price ?? 0,
            IsAvailable = product?.StockQuantity > 0
        };
    }
}
```

The return value is sent back to the caller automatically. The return value must not be `null` - if you return `null`, the bus throws an `InvalidOperationException`.

## Register and call

```csharp
builder.Services
    .AddMessageBus()
    .AddMyApp() // source-generated - registers GetProductRequestHandler automatically
    .AddRabbitMQ();
```

From the caller side:

```csharp
var response = await bus.RequestAsync(
    new GetProductRequest { ProductId = productId },
    cancellationToken);

// response.Name, response.Price, response.IsAvailable
```

If the handler throws, the exception propagates back to the caller. See [Exception behavior](#exception-behavior) for details.

# Send handler

A send handler processes a one-way instruction. There is no typed response. The sender dispatches the message and moves on.

## Define the message

Send messages do not implement `IEventRequest<T>` because there is no typed response.

```csharp
// ReserveInventoryCommand.cs
namespace MyApp.Messages;

public sealed record ReserveInventoryCommand
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}
```

## Implement the handler

Use `IEventRequestHandler<TRequest>` - the single type parameter variant, with no response type:

```csharp
// ReserveInventoryCommandHandler.cs
using Mocha;
using MyApp.Messages;

namespace MyApp.Handlers;

public class ReserveInventoryCommandHandler(
    AppDbContext db,
    ILogger<ReserveInventoryCommandHandler> logger)
    : IEventRequestHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(
        ReserveInventoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Reserving {Quantity} units of product {ProductId}",
            request.Quantity,
            request.ProductId);

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product {request.ProductId} not found");
        }

        product.StockQuantity -= request.Quantity;
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

## Register and send

```csharp
builder.Services
    .AddMessageBus()
    .AddMyApp() // source-generated - registers ReserveInventoryCommandHandler automatically
    .AddRabbitMQ();
```

Send the message:

```csharp
await bus.SendAsync(new ReserveInventoryCommand
{
    OrderId = orderId,
    ProductId = productId,
    Quantity = 3
}, cancellationToken);
```

Expected output:

```text
info: MyApp.Handlers.ReserveInventoryCommandHandler[0]
      Reserving 3 units of product a1b2c3d4-...
```

`SendAsync` completes after the message is dispatched to the transport. It does not wait for the handler to finish. To wait for completion, use `RequestAsync` instead - Mocha sends an automatic acknowledgment when the handler finishes.

# Batch handler

A batch handler receives groups of messages at once instead of one at a time. Use batch handlers for high-throughput scenarios where processing messages in bulk is more efficient - bulk database writes, aggregations, or analytics pipelines.

## Implement the handler

```csharp
// OrderPlacedBatchHandler.cs
using Mocha;
using MyApp.Messages;

namespace MyApp.Handlers;

public class OrderPlacedBatchHandler(
    AppDbContext db,
    ILogger<OrderPlacedBatchHandler> logger)
    : IBatchEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        IMessageBatch<OrderPlaced> batch,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing batch of {Count} orders",
            batch.Count);

        var totalRevenue = 0m;
        foreach (var order in batch)
        {
            totalRevenue += order.TotalAmount;
        }

        db.RevenueSummaries.Add(new RevenueSummary
        {
            OrderCount = batch.Count,
            TotalRevenue = totalRevenue,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Revenue summary created: {Count} orders, {Total:C} total",
            batch.Count,
            totalRevenue);
    }
}
```

`IMessageBatch<T>` implements `IReadOnlyList<T>`, so you can iterate, index, and check `.Count`. The `CompletionMode` property tells you why the batch was dispatched: `Size` (reached the configured maximum), `Time` (timeout expired), or `Forced` (endpoint shutting down).

To access envelope metadata for a specific message in the batch, call `batch.GetContext(index)` to get an `IConsumeContext<T>` with headers, correlation IDs, and timestamps.

## Register and configure

```csharp
builder.Services
    .AddMessageBus()
    .AddMyApp() // source-generated - registers OrderPlacedBatchHandler automatically
    .AddBatchHandler<OrderPlacedBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 50;
        opts.BatchTimeout = TimeSpan.FromSeconds(10);
    })
    .AddRabbitMQ();
```

The source generator registers the batch handler, but you can chain `.AddBatchHandler<T>(opts => ...)` after `AddMyApp()` to override batch configuration options. Without explicit configuration, Mocha uses the defaults: 100 messages per batch, 1-second timeout.

## Publish events as normal

```csharp
for (var i = 0; i < 100; i++)
{
    await bus.PublishAsync(new OrderPlaced
    {
        OrderId = Guid.NewGuid(),
        CustomerId = $"customer-{i}",
        TotalAmount = 99.99m
    }, cancellationToken);
}
```

With `MaxBatchSize = 50`, the 100 messages arrive as two batches of 50. Expected output:

```text
info: MyApp.Handlers.OrderPlacedBatchHandler[0]
      Processing batch of 50 orders
info: MyApp.Handlers.OrderPlacedBatchHandler[0]
      Revenue summary created: 50 orders, $4,999.50 total
info: MyApp.Handlers.OrderPlacedBatchHandler[0]
      Processing batch of 50 orders
info: MyApp.Handlers.OrderPlacedBatchHandler[0]
      Revenue summary created: 50 orders, $4,999.50 total
```

# Advanced: accessing the envelope

For cases where you need the full consume context - message headers, correlation IDs, source addresses - implement `IConsumer<T>` instead of a handler interface.

```csharp
// OrderAuditConsumer.cs
using Mocha;
using MyApp.Messages;

namespace MyApp.Consumers;

public class OrderAuditConsumer(ILogger<OrderAuditConsumer> logger)
    : IConsumer<OrderPlaced>
{
    public async ValueTask ConsumeAsync(
        IConsumeContext<OrderPlaced> context,
        CancellationToken cancellationToken)
    {
        var order = context.Message;

        logger.LogInformation(
            "Audit: OrderId={OrderId} MessageId={MessageId} " +
            "CorrelationId={CorrelationId} Source={Source}",
            order.OrderId,
            context.MessageId,
            context.CorrelationId,
            context.SourceAddress);

        // Read custom headers attached at publish time
        if (context.Headers.TryGetValue("x-tenant", out var tenant))
        {
            logger.LogInformation("Tenant: {Tenant}", tenant);
        }

        await Task.CompletedTask;
    }
}
```

`IConsumeContext<T>` gives you the deserialized message plus envelope fields: `MessageId`, `CorrelationId`, `ConversationId`, `CausationId`, `SourceAddress`, `DestinationAddress`, `SentAt`, `Headers`, `DeliveryCount`, and more. See [Messages](/docs/mocha/v1/messages) for how correlation identifiers relate to each other.

Register with `.AddConsumer<T>()`:

```csharp
builder.Services
    .AddMessageBus()
    .AddMyApp() // source-generated - registers OrderAuditConsumer automatically
    .AddRabbitMQ();
```

Use `IConsumer<T>` when you need envelope metadata or custom header inspection. For business logic that operates on the message payload alone, handler interfaces are simpler.

# DI scoping

Mocha creates a new DI scope for each message. Your handler is instantiated from that scope, its constructor dependencies are resolved from it, and the scope is disposed when the handler completes.

This means `DbContext` and other scoped services are safe to inject directly into handler constructors:

```csharp
// AppDbContext is a scoped service - safe to inject
public class OrderPlacedHandler(AppDbContext db, ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        db.ProcessedOrders.Add(new ProcessedOrder { OrderId = message.OrderId });
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

Each message gets its own scope and its own `DbContext` instance. Two messages processing concurrently do not share a `DbContext`.

Singleton services are resolved from the root container as usual. If you inject a singleton that holds scoped state, you will get unexpected behavior - the same problem as in any ASP.NET Core application.

# Exception behavior

When `HandleAsync` throws, the behavior depends on the handler type and the middleware pipeline:

- **Event handlers and send handlers:** The exception is caught by the pipeline. By default, Mocha retries the message according to the configured retry policy, then moves it to the dead-letter queue if retries are exhausted. See [Reliability](/docs/mocha/v1/reliability) for retry and fault configuration.
- **Request handlers:** The exception propagates back to the caller as a fault. If you use `RequestAsync`, it throws on the caller side. The caller receives the error, not a timeout.
- **Batch handlers:** If the handler throws, all messages in the batch fault together. The pipeline treats the entire batch as a failed unit.

When a message arrives, it passes through middleware before reaching your handler. The pipeline handles fault routing, dead-letter delivery, observability, and concurrency limits - without any code in your handler. See [Middleware and Pipelines](/docs/mocha/v1/middleware-and-pipelines) for details on writing custom pipeline middleware.

# Publishing from a handler

To publish a message from within a handler, inject `IMessageBus` via the constructor:

```csharp
public class OrderPlacedHandler(
    AppDbContext db,
    IMessageBus messageBus,
    ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        // Handle the inbound event
        var invoice = new Invoice { OrderId = message.OrderId };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);

        // Publish a downstream event
        await messageBus.PublishAsync(
            new InvoiceCreated
            {
                InvoiceId = invoice.Id,
                OrderId = message.OrderId,
                Amount = message.TotalAmount
            },
            cancellationToken);

        logger.LogInformation(
            "Invoice created and InvoiceCreated published for order {OrderId}",
            message.OrderId);
    }
}
```

Messages published from within a handler automatically inherit the `ConversationId` and `CorrelationId` from the inbound message. The bus sets `CausationId` on the outgoing message to the `MessageId` of the inbound message. This creates a traceable parent-child chain across services without any extra code. See [Messages](/docs/mocha/v1/messages) for how correlation identifiers work.

# Further reading

- [Handler Registration](/docs/mocha/v1/handler-registration) - How the source generator discovers handlers and how to customize registration.
- [Event-Driven Consumer](https://www.enterpriseintegrationpatterns.com/patterns/messaging/EventDrivenConsumer.html) - The EIP pattern that defines push-based message consumption, which is what Mocha's handlers implement.
- [Competing Consumers](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CompetingConsumers.html) - When multiple instances of your service run, they compete for messages on the same queue. This is the concurrency model for Mocha handlers under load.

# Next steps

Your handlers are registered. Learn how the source generator discovers and registers them in [Handler Registration](/docs/mocha/v1/handler-registration), or how Mocha routes messages to them in [Routing and Endpoints](/docs/mocha/v1/routing-and-endpoints).

> **Runnable examples:** [BatchHandler](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/HandlersAndConsumers/BatchHandler), [LowLevelConsumer](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/HandlersAndConsumers/LowLevelConsumer), [CustomConsumer](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/HandlersAndConsumers/CustomConsumer)
>
> **Full demo:** [Demo.Billing](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo/Demo.Billing) shows event handlers (`OrderPlacedEventHandler`), batch handlers (`OrderPlacedBatchHandler` with revenue aggregation, `BulkOrderBatchHandler` for high-volume processing), and request handlers (`ProcessRefundCommandHandler`).

---
title: "Introduction"
description: "Mocha is a messaging framework for .NET that provides a message bus for inter-service communication, a source-generated mediator for in-process CQRS, middleware pipelines, saga orchestration, and deep observability through Nitro integration."
---

```csharp
// Inter-service messaging via the message bus
builder.Services
    .AddMessageBus()
    .AddOrderService() // source-generated handler registration
    .AddRabbitMQ();

// In-process CQRS via the mediator
builder.Services
    .AddMediator()
    .AddHandlers();
```

Mocha gives you two dispatch mechanisms. The **message bus** sends messages across service boundaries through a transport like RabbitMQ. The **mediator** dispatches commands, queries, and notifications within a single process using source-generated code - no reflection, no dictionary lookups. Use them independently or together.

# What Mocha is

Mocha is a messaging framework for .NET with two complementary dispatch systems:

- **Message bus** - sends messages across service boundaries through transports like RabbitMQ. Supports pub/sub events, request/reply, saga orchestration, inbox/outbox reliability, and pluggable transports. Follows the patterns described in [Enterprise Integration Patterns](https://www.enterpriseintegrationpatterns.com/patterns/messaging/Introduction.html).
- **Mediator** - dispatches commands, queries, and notifications within a single process. A Roslyn source generator produces a concrete mediator class at compile time with specialized dispatch and pre-compiled pipeline delegates. No reflection, no runtime code generation.

Both integrate directly into ASP.NET Core's dependency injection and are designed for [event-driven architectures](https://learn.microsoft.com/en-us/azure/architecture/guide/architecture-styles/event-driven). Use the message bus when messages cross process boundaries. Use the mediator when you want in-process CQRS with pipeline behaviors for cross-cutting concerns like validation, logging, and transactions. Most real-world services use both: the mediator handles internal command/query dispatch, and the message bus handles inter-service events.

The framework is handler-first in both cases. You implement handler interfaces, and Mocha builds the infrastructure around those declarations - whether that means wiring up transport endpoints and middleware pipelines for the bus, or generating a type-safe mediator class with pre-compiled dispatch for in-process handlers.

# Terminology

These terms appear throughout the documentation. They are defined once here and used consistently everywhere.

| Term                 | Definition                                                                                                                                                                                                                              |
| -------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| **Message**          | A plain C# record representing business data. The unit of communication between handlers and services.                                                                                                                                  |
| **Event**            | A message published via `PublishAsync`. Represents something that happened. Multiple handlers can receive an event.                                                                                                                     |
| **Request**          | A message sent via `SendAsync` or `RequestAsync`. When sent with `RequestAsync`, the sender awaits a typed response. When sent with `SendAsync`, it is fire-and-forget.                                                                 |
| **Handler**          | A class implementing a Mocha handler interface that processes a specific message type.                                                                                                                                                  |
| **Consumer**         | The processing unit that wraps a handler or custom logic. Mocha builds consumers automatically from handlers, or you can implement `IConsumer<T>` or `Consumer<T>` directly.                                                            |
| **Endpoint**         | A named, addressable unit in the messaging topology. Each endpoint has a transport address, a middleware pipeline, and a kind (default, error, skipped, or reply). Receive endpoints consume messages; dispatch endpoints produce them. |
| **Transport**        | The infrastructure layer connecting Mocha to a message broker, such as RabbitMQ or an in-process channel.                                                                                                                               |
| **Pipeline**         | The chain of middleware that processes a message from the transport through to the handler.                                                                                                                                             |
| **Saga**             | A long-running stateful workflow that coordinates multiple messages and transitions across services.                                                                                                                                    |
| **Mediator**         | An in-process dispatcher that routes commands, queries, and notifications to their handlers without a transport layer. Source-generated at compile time.                                                                                |
| **Source generator** | A Roslyn analyzer that discovers handlers and sagas at compile time and generates typed registration code. Used by both the [mediator](./mediator/index.md) and the [message bus](./handler-registration.md).           |
| **Command**          | A mediator message representing an action. Implements `ICommand` (void) or `ICommand<TResponse>` (with response). Dispatched via `SendAsync`.                                                                                           |
| **Query**            | A mediator message representing a read operation. Implements `IQuery<TResponse>`. Dispatched via `QueryAsync`.                                                                                                                          |

# Architecture

When you call `PublishAsync`, here is what happens:

{/* TopologyVisualization placeholder */}

Your code in OrderService calls `PublishAsync` with an `OrderPlaced` message **(1)**. Mocha serializes it and hands it to the dispatch endpoint **(2)**, which sends it into RabbitMQ. The broker routes the message through two exchanges into the queue (the unnumbered transport layer between the services). On the BillingService side, the receive endpoint **(3)** picks the message off the queue, runs it through the middleware pipeline, and calls `HandleAsync` on your `OrderPlacedHandler` **(4)**. Middleware in the pipeline handles cross-cutting concerns - tracing, retries, concurrency limits - without touching your handler code.

# Core capabilities

## Handler-first design

You write handlers. That is the primary abstraction. Implement an interface, register it with the builder, and your handler runs when the matching message arrives.

```csharp
public class OrderPlacedHandler(AppDbContext db)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        var invoice = new Invoice { OrderId = message.OrderId };
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

Mocha provides handler interfaces for each messaging pattern:

| Interface                          | Pattern                | Bus method               |
| ---------------------------------- | ---------------------- | ------------------------ |
| `IEventHandler<T>`                 | Pub/sub events         | `PublishAsync`           |
| `IEventRequestHandler<TReq, TRes>` | Request/reply          | `RequestAsync`           |
| `IEventRequestHandler<TReq>`       | Send (fire-and-forget) | `SendAsync`              |
| `IBatchEventHandler<T>`            | Batch processing       | `PublishAsync` (batched) |

## Messaging patterns and consumers

Mocha supports three core patterns for message-driven systems. Each pattern answers a different question:

- **Events (pub/sub):** Who needs to know? Publish once, all subscribers receive it. Use `PublishAsync` and `IEventHandler<T>`.
- **Send (fire-and-forget):** Who should act? Deliver to one endpoint without waiting for a result. Use `SendAsync` and `IEventRequestHandler<TRequest>`.
- **Request/reply:** What is the result? Send and await a typed response. Use `RequestAsync` and `IEventRequestHandler<TRequest, TResponse>`.

Handlers are the fastest way to get started, but they are not the only option. If you need full control over message processing, implement `IConsumer<T>` for a lightweight consumer or extend `Consumer<T>` for complete customization.

## Pluggable transports

Switch transports without changing your handler code, and use multiple transports at the same time. Register a default transport, then route specific messages through a different transport when you need different throughput or delivery characteristics:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddEventHandler<ClickStreamHandler>()
    // Default transport for most messages
    .RabbitMQ()
    // High-throughput transport for specific messages
    .AddEventHub(t => t.AddEventHandler<ClickStreamHandler>());
```

## Middleware pipelines

Underneath, everything in Mocha is a middleware pipeline - dispatch, receive, and consumer processing are each a chain you can customize. Mocha compiles these pipelines into an optimized chain with no per-message dictionary lookups or dynamic dispatch at runtime. You can insert your own middleware at any point in the pipeline for cross-cutting concerns like logging, validation, or custom error handling.

## OpenTelemetry-native observability

Every message dispatch, receive, and handler execution produces structured traces and metrics through OpenTelemetry. Correlation IDs propagate across service boundaries automatically.

```csharp
builder.Services
    .AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedHandler>()
    .AddRabbitMQ();
```

Connect your services to Nitro to introspect your messaging configuration visually. Here is a real-world example: OrderApi publishes `OrderPlaced`, and the broker fans it out to both BillingService and InventoryService through separate exchanges and queues. The trace shows every hop:

{/* TopologyVisualization placeholder */}

Expand the visualization to see the full trace sidebar. Each numbered step - publish, dispatch, receive, consume - is a real OpenTelemetry span. The unnumbered nodes between dispatch and receive are the RabbitMQ exchanges and queues the message passed through. When something goes wrong, this is how you answer "where did the message go?" without digging through logs.

## Resiliency

Mocha provides an inbox and outbox to guarantee reliable message processing. The **outbox** ensures that database writes and message dispatches succeed or fail together - no lost messages during failures. The **inbox** ensures that messages are processed exactly once, even when the transport delivers duplicates. Both are optimized for your specific database system.

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddEntityFramework<AppDbContext>(p =>
    {
        p.UsePostgresOutbox();
        p.UseTransaction();
        p.UsePostgresInbox();
    })
    .AddRabbitMQ();
```

## Saga orchestration

Sagas coordinate multi-step workflows that span multiple services and messages. You define a state machine with states and transitions, and Mocha validates the entire state machine - every state must be reachable and every path must lead to a final state. Reducing the possibility of deploying a saga that gets stuck in an intermediate state.

{/* TopologyVisualization placeholder */}

Mocha persists saga state, manages transitions, and supports compensation when steps fail. See [Sagas](./sagas.md) for a full walkthrough.

## In-process mediator

For commands and queries that stay within a single service, the mediator provides CQRS dispatch with middleware - without a transport layer. Define your messages with marker interfaces, implement handlers, and the source generator wires everything at compile time:

```csharp
// Define a command and its handler
public record PlaceOrderCommand(Guid ProductId, int Quantity)
    : ICommand<PlaceOrderResult>;

public class PlaceOrderCommandHandler(AppDbContext db)
    : ICommandHandler<PlaceOrderCommand, PlaceOrderResult>
{
    public async ValueTask<PlaceOrderResult> HandleAsync(
        PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        // business logic
        return new PlaceOrderResult(true, Guid.NewGuid());
    }
}
```

```csharp
// Register and use
builder.Services
    .AddMediator()
    .AddCatalog()
    .UseEntityFrameworkTransactions<AppDbContext>();

app.MapPost("/orders", async (ISender sender) =>
    await sender.SendAsync(new PlaceOrderCommand(productId, 2)));
```

The mediator supports commands (with and without responses), queries, notifications, middleware, and EF Core transaction wrapping (commands only by default, configurable via delegate). The source generator produces a typed registration method per assembly (e.g. `AddCatalog()`) that wires up all handlers and pre-compiled dispatch pipelines automatically. See [Mediator](./mediator/index.md) for the full guide.

# Learning paths

Choose an entry point based on how you learn best:

- **Get something running first:** [Quick Start](./quick-start.md) -zero to a working message bus in under five minutes with the InMemory transport.
- **Understand the concepts first:** [Messages](./messages.md) then [Messaging Patterns](./messaging-patterns.md) - learn what flows through the system and what patterns govern how it flows.
- **Evaluating Mocha for a specific broker:** [Transports](./transports/index.md) - understand the transport abstraction and what is available.
- **In-process CQRS:** [Mediator](./mediator/index.md) - dispatch commands, queries, and notifications within a single service using the source-generated mediator.

- **See a real-world system:** The [Demo application](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo) is a complete e-commerce system with three services (Catalog, Billing, Shipping) that demonstrates event-driven communication, sagas, batch processing, the transactional outbox, and .NET Aspire orchestration.

Ready to build? Start with the [Quick Start](./quick-start.md).

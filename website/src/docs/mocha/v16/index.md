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
| **Source generator** | A Roslyn analyzer that discovers handlers and sagas at compile time and generates typed registration code. Used by both the [mediator](/docs/mocha/v16/mediator) and the [message bus](/docs/mocha/v16/handler-registration).             |
| **Command**          | A mediator message representing an action. Implements `ICommand` (void) or `ICommand<TResponse>` (with response). Dispatched via `SendAsync`.                                                                                           |
| **Query**            | A mediator message representing a read operation. Implements `IQuery<TResponse>`. Dispatched via `QueryAsync`.                                                                                                                          |

# Architecture

When you call `PublishAsync`, here is what happens:

<TopologyVisualization data='{"services":[{"host":{"serviceName":"OrderService","assemblyName":"OrderService.dll","instanceId":"order-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[],"routes":{"inbound":[],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlaced","endpoint":{"name":"e/order-placed","address":"rabbitmq://localhost/e/order-placed","transportName":"RabbitMQ"}}]},"sagas":[]},{"host":{"serviceName":"BillingService","assemblyName":"BillingService.dll","instanceId":"billing-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"OrderPlacedHandler","identityType":"OrderPlacedHandler","identityTypeFullName":"MyApp.Handlers.OrderPlacedHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler","endpoint":{"name":"billing.order-placed","address":"rabbitmq://localhost/billing.order-placed","transportName":"RabbitMQ"}}],"outbound":[]},"sagas":[]}],"transports":[{"identifier":"rabbitmq://localhost:5672/","name":"RabbitMQ","schema":"rabbitmq","transportType":"RabbitMQMessagingTransport","receiveEndpoints":[{"name":"billing.order-placed","kind":"default","address":"rabbitmq://localhost/billing.order-placed","source":{"address":"rabbitmq://localhost:5672/q/billing.order-placed"}}],"dispatchEndpoints":[{"name":"e/order-placed","kind":"default","address":"rabbitmq://localhost/e/order-placed","destination":{"address":"rabbitmq://localhost:5672/e/order-placed"}}],"topology":{"address":"rabbitmq://localhost:5672/","entities":[{"kind":"exchange","name":"order-placed","address":"rabbitmq://localhost:5672/e/order-placed","flow":"inbound","properties":{"type":"fanout","durable":true,"autoDelete":false,"autoProvision":true}},{"kind":"exchange","name":"billing.order-placed","address":"rabbitmq://localhost:5672/e/billing.order-placed","flow":"inbound","properties":{"type":"fanout","durable":true,"autoDelete":false,"autoProvision":true}},{"kind":"queue","name":"billing.order-placed","address":"rabbitmq://localhost:5672/q/billing.order-placed","flow":"outbound","properties":{"durable":true,"exclusive":false,"autoDelete":false,"autoProvision":true}}],"links":[{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/order-placed/e/billing.order-placed","source":"rabbitmq://localhost:5672/e/order-placed","target":"rabbitmq://localhost:5672/e/billing.order-placed","direction":"forward","properties":{"routingKey":null,"autoProvision":true}},{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/billing.order-placed/q/billing.order-placed","source":"rabbitmq://localhost:5672/e/billing.order-placed","target":"rabbitmq://localhost:5672/q/billing.order-placed","direction":"forward","properties":{"routingKey":null,"autoProvision":true}}]}}]}' trace='{"traceId":"intro-event-flow-trace-001","activities":[{"id":"ie-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":45,"status":"ok","operation":"publish","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","transport":"RabbitMQ"},{"id":"ie-2","parentId":"ie-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"e/order-placed","endpointAddress":"rabbitmq://localhost/e/order-placed","transport":"RabbitMQ"},{"id":"ie-3","parentId":"ie-2","startTime":"2024-06-15T10:30:00.012Z","durationMs":3,"status":"ok","operation":"receive","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"billing.order-placed","endpointAddress":"rabbitmq://localhost/billing.order-placed","transport":"RabbitMQ"},{"id":"ie-4","parentId":"ie-3","startTime":"2024-06-15T10:30:00.015Z","durationMs":12,"status":"ok","operation":"consume","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler"}]}' />

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

<TopologyVisualization data='{"services":[{"host":{"serviceName":"OrderApi","assemblyName":"OrderApi.dll","instanceId":"order-api-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[],"routes":{"inbound":[],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlaced","endpoint":{"name":"e/order-placed","address":"rabbitmq://localhost/e/order-placed","transportName":"RabbitMQ"}}]},"sagas":[]},{"host":{"serviceName":"BillingService","assemblyName":"BillingService.dll","instanceId":"billing-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"CreateInvoiceHandler","identityType":"CreateInvoiceHandler","identityTypeFullName":"MyApp.Billing.CreateInvoiceHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"CreateInvoiceHandler","endpoint":{"name":"billing.create-invoice","address":"rabbitmq://localhost/billing.create-invoice","transportName":"RabbitMQ"}}],"outbound":[]},"sagas":[]},{"host":{"serviceName":"InventoryService","assemblyName":"InventoryService.dll","instanceId":"inventory-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"ReserveStockHandler","identityType":"ReserveStockHandler","identityTypeFullName":"MyApp.Inventory.ReserveStockHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"ReserveStockHandler","endpoint":{"name":"inventory.reserve-stock","address":"rabbitmq://localhost/inventory.reserve-stock","transportName":"RabbitMQ"}}],"outbound":[]},"sagas":[]}],"transports":[{"identifier":"rabbitmq://localhost:5672/","name":"RabbitMQ","schema":"rabbitmq","transportType":"RabbitMQMessagingTransport","receiveEndpoints":[{"name":"billing.create-invoice","kind":"default","address":"rabbitmq://localhost/billing.create-invoice","source":{"address":"rabbitmq://localhost:5672/q/billing.create-invoice"}},{"name":"inventory.reserve-stock","kind":"default","address":"rabbitmq://localhost/inventory.reserve-stock","source":{"address":"rabbitmq://localhost:5672/q/inventory.reserve-stock"}}],"dispatchEndpoints":[{"name":"e/order-placed","kind":"default","address":"rabbitmq://localhost/e/order-placed","destination":{"address":"rabbitmq://localhost:5672/e/order-placed"}}],"topology":{"address":"rabbitmq://localhost:5672/","entities":[{"kind":"exchange","name":"order-placed","address":"rabbitmq://localhost:5672/e/order-placed","flow":"inbound","properties":{"type":"fanout","durable":true,"autoDelete":false,"autoProvision":true}},{"kind":"exchange","name":"billing.create-invoice","address":"rabbitmq://localhost:5672/e/billing.create-invoice","flow":"inbound","properties":{"type":"fanout","durable":true,"autoDelete":false,"autoProvision":true}},{"kind":"exchange","name":"inventory.reserve-stock","address":"rabbitmq://localhost:5672/e/inventory.reserve-stock","flow":"inbound","properties":{"type":"fanout","durable":true,"autoDelete":false,"autoProvision":true}},{"kind":"queue","name":"billing.create-invoice","address":"rabbitmq://localhost:5672/q/billing.create-invoice","flow":"outbound","properties":{"durable":true,"exclusive":false,"autoDelete":false,"autoProvision":true}},{"kind":"queue","name":"inventory.reserve-stock","address":"rabbitmq://localhost:5672/q/inventory.reserve-stock","flow":"outbound","properties":{"durable":true,"exclusive":false,"autoDelete":false,"autoProvision":true}}],"links":[{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/order-placed/e/billing.create-invoice","source":"rabbitmq://localhost:5672/e/order-placed","target":"rabbitmq://localhost:5672/e/billing.create-invoice","direction":"forward","properties":{"routingKey":null,"autoProvision":true}},{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/order-placed/e/inventory.reserve-stock","source":"rabbitmq://localhost:5672/e/order-placed","target":"rabbitmq://localhost:5672/e/inventory.reserve-stock","direction":"forward","properties":{"routingKey":null,"autoProvision":true}},{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/billing.create-invoice/q/billing.create-invoice","source":"rabbitmq://localhost:5672/e/billing.create-invoice","target":"rabbitmq://localhost:5672/q/billing.create-invoice","direction":"forward","properties":{"routingKey":null,"autoProvision":true}},{"kind":"bind","address":"rabbitmq://localhost:5672/b/e/inventory.reserve-stock/q/inventory.reserve-stock","source":"rabbitmq://localhost:5672/e/inventory.reserve-stock","target":"rabbitmq://localhost:5672/q/inventory.reserve-stock","direction":"forward","properties":{"routingKey":null,"autoProvision":true}}]}}]}' trace='{"traceId":"otel-real-world-trace-001","activities":[{"id":"ow-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":85,"status":"ok","operation":"publish","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","transport":"RabbitMQ"},{"id":"ow-2","parentId":"ow-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":3,"status":"ok","operation":"dispatch","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"e/order-placed","endpointAddress":"rabbitmq://localhost/e/order-placed","transport":"RabbitMQ"},{"id":"ow-3","parentId":"ow-2","startTime":"2024-06-15T10:30:00.014Z","durationMs":4,"status":"ok","operation":"receive","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"billing.create-invoice","endpointAddress":"rabbitmq://localhost/billing.create-invoice","transport":"RabbitMQ"},{"id":"ow-4","parentId":"ow-3","startTime":"2024-06-15T10:30:00.018Z","durationMs":32,"status":"ok","operation":"consume","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","consumerName":"CreateInvoiceHandler"},{"id":"ow-5","parentId":"ow-2","startTime":"2024-06-15T10:30:00.016Z","durationMs":3,"status":"ok","operation":"receive","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"inventory.reserve-stock","endpointAddress":"rabbitmq://localhost/inventory.reserve-stock","transport":"RabbitMQ"},{"id":"ow-6","parentId":"ow-5","startTime":"2024-06-15T10:30:00.019Z","durationMs":45,"status":"ok","operation":"consume","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","consumerName":"ReserveStockHandler"}]}' />

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

<TopologyVisualization data='{"services":[{"host":{"serviceName":"OrderService","assemblyName":"OrderService.dll","instanceId":"order-svc-1"},"messageTypes":[{"identity":"msg:RequestQuickRefundRequest","runtimeType":"RequestQuickRefundRequest","runtimeTypeFullName":"MyApp.Messages.RequestQuickRefundRequest","isInterface":false,"isInternal":false},{"identity":"msg:ProcessRefundCommand","runtimeType":"ProcessRefundCommand","runtimeTypeFullName":"MyApp.Messages.ProcessRefundCommand","isInterface":false,"isInternal":false},{"identity":"msg:ProcessRefundResponse","runtimeType":"ProcessRefundResponse","runtimeTypeFullName":"MyApp.Messages.ProcessRefundResponse","isInterface":false,"isInternal":false},{"identity":"msg:QuickRefundResponse","runtimeType":"QuickRefundResponse","runtimeTypeFullName":"MyApp.Messages.QuickRefundResponse","isInterface":false,"isInternal":false}],"consumers":[],"routes":{"inbound":[{"kind":"request","messageTypeIdentity":"msg:RequestQuickRefundRequest","consumerName":"QuickRefundSagaConsumer","endpoint":{"name":"saga-endpoint","address":"loopback://localhost/q/saga-endpoint","transportName":"InMemory"}},{"kind":"reply","messageTypeIdentity":"msg:ProcessRefundResponse","consumerName":"QuickRefundSagaConsumer","endpoint":{"name":"saga-reply","address":"loopback://localhost/q/saga-reply","transportName":"InMemory"}}],"outbound":[{"kind":"send","messageTypeIdentity":"msg:ProcessRefundCommand","endpoint":{"name":"billing-handler","address":"loopback://localhost/q/billing-handler","transportName":"InMemory"}}]},"sagas":[{"name":"QuickRefundSaga","stateType":"RefundSagaState","stateTypeFullName":"MyApp.Sagas.RefundSagaState","consumerName":"QuickRefundSagaConsumer","states":[{"name":"Initial","isInitial":true,"isFinal":false,"onEntry":{},"transitions":[{"eventType":"RequestQuickRefundRequest","eventTypeFullName":"MyApp.Messages.RequestQuickRefundRequest","transitionTo":"AwaitingRefund","transitionKind":"request","autoProvision":false,"send":[{"messageType":"ProcessRefundCommand","messageTypeFullName":"MyApp.Messages.ProcessRefundCommand"}]}]},{"name":"AwaitingRefund","isInitial":false,"isFinal":false,"onEntry":{},"transitions":[{"eventType":"ProcessRefundResponse","eventTypeFullName":"MyApp.Messages.ProcessRefundResponse","transitionTo":"Completed","transitionKind":"reply","autoProvision":false}]},{"name":"Completed","isInitial":false,"isFinal":true,"onEntry":{},"response":{"eventType":"QuickRefundResponse","eventTypeFullName":"MyApp.Messages.QuickRefundResponse"},"transitions":[]}]}]},{"host":{"serviceName":"BillingService","assemblyName":"BillingService.dll","instanceId":"billing-svc-1"},"messageTypes":[{"identity":"msg:ProcessRefundCommand","runtimeType":"ProcessRefundCommand","runtimeTypeFullName":"MyApp.Messages.ProcessRefundCommand","isInterface":false,"isInternal":false},{"identity":"msg:ProcessRefundResponse","runtimeType":"ProcessRefundResponse","runtimeTypeFullName":"MyApp.Messages.ProcessRefundResponse","isInterface":false,"isInternal":false}],"consumers":[{"name":"ProcessRefundCommandHandler","identityType":"ProcessRefundCommandHandler","identityTypeFullName":"MyApp.Handlers.ProcessRefundCommandHandler"}],"routes":{"inbound":[{"kind":"request","messageTypeIdentity":"msg:ProcessRefundCommand","consumerName":"ProcessRefundCommandHandler","endpoint":{"name":"billing-handler","address":"loopback://localhost/q/billing-handler","transportName":"InMemory"}}],"outbound":[{"kind":"send","messageTypeIdentity":"msg:ProcessRefundResponse","endpoint":{"name":"saga-reply","address":"loopback://localhost/q/saga-reply","transportName":"InMemory"}}]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"saga-endpoint","kind":"default","address":"loopback://localhost/q/saga-endpoint","source":{"address":"loopback://localhost/q/saga-endpoint"}},{"name":"billing-handler","kind":"default","address":"loopback://localhost/q/billing-handler","source":{"address":"loopback://localhost/q/billing-handler"}},{"name":"saga-reply","kind":"reply","address":"loopback://localhost/q/saga-reply","source":{"address":"loopback://localhost/q/saga-reply"}}],"dispatchEndpoints":[{"name":"saga-endpoint","kind":"default","address":"loopback://localhost/q/saga-endpoint","destination":{"address":"loopback://localhost/q/saga-endpoint"}},{"name":"billing-handler","kind":"default","address":"loopback://localhost/q/billing-handler","destination":{"address":"loopback://localhost/q/billing-handler"}},{"name":"saga-reply","kind":"reply","address":"loopback://localhost/q/saga-reply","destination":{"address":"loopback://localhost/q/saga-reply"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"queue","name":"saga-endpoint","address":"loopback://localhost/q/saga-endpoint","flow":"inbound","properties":{"type":"request"}},{"kind":"queue","name":"billing-handler","address":"loopback://localhost/q/billing-handler","flow":"inbound","properties":{"type":"request"}},{"kind":"queue","name":"saga-reply","address":"loopback://localhost/q/saga-reply","flow":"outbound","properties":{"type":"reply"}}],"links":[]}}]}' trace='{"traceId":"saga-orchestration-trace-001","activities":[{"id":"so-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":120,"status":"ok","operation":"send","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"msg:RequestQuickRefundRequest","transport":"InMemory"},{"id":"so-2","parentId":"so-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"msg:RequestQuickRefundRequest","endpointName":"saga-endpoint","endpointAddress":"loopback://localhost/q/saga-endpoint","transport":"InMemory"},{"id":"so-3","parentId":"so-2","startTime":"2024-06-15T10:30:00.008Z","durationMs":3,"status":"ok","operation":"receive","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"msg:RequestQuickRefundRequest","endpointName":"saga-endpoint","endpointAddress":"loopback://localhost/q/saga-endpoint","transport":"InMemory"},{"id":"so-4","parentId":"so-3","startTime":"2024-06-15T10:30:00.011Z","durationMs":25,"status":"ok","operation":"consume","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"msg:RequestQuickRefundRequest","consumerName":"QuickRefundSagaConsumer"},{"id":"so-5","parentId":"so-4","startTime":"2024-06-15T10:30:00.012Z","durationMs":22,"status":"ok","operation":"saga-transition","sagaName":"QuickRefundSaga","sagaInstanceId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","fromState":"Initial","toState":"AwaitingRefund","eventType":"RequestQuickRefundRequest"},{"id":"so-6","parentId":"so-5","startTime":"2024-06-15T10:30:00.013Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundCommand","messageTypeIdentity":"msg:ProcessRefundCommand","endpointName":"billing-handler","endpointAddress":"loopback://localhost/q/billing-handler","transport":"InMemory"},{"id":"so-7","parentId":"so-6","startTime":"2024-06-15T10:30:00.020Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundCommand","messageTypeIdentity":"msg:ProcessRefundCommand","endpointName":"billing-handler","endpointAddress":"loopback://localhost/q/billing-handler","transport":"InMemory"},{"id":"so-8","parentId":"so-7","startTime":"2024-06-15T10:30:00.023Z","durationMs":20,"status":"ok","operation":"consume","messageType":"ProcessRefundCommand","messageTypeIdentity":"msg:ProcessRefundCommand","consumerName":"ProcessRefundCommandHandler"},{"id":"so-9","parentId":"so-8","startTime":"2024-06-15T10:30:00.043Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundResponse","messageTypeIdentity":"msg:ProcessRefundResponse","endpointName":"saga-reply","endpointAddress":"loopback://localhost/q/saga-reply","transport":"InMemory"},{"id":"so-10","parentId":"so-9","startTime":"2024-06-15T10:30:00.050Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundResponse","messageTypeIdentity":"msg:ProcessRefundResponse","endpointName":"saga-reply","endpointAddress":"loopback://localhost/q/saga-reply","transport":"InMemory"},{"id":"so-11","parentId":"so-10","startTime":"2024-06-15T10:30:00.053Z","durationMs":15,"status":"ok","operation":"consume","messageType":"ProcessRefundResponse","messageTypeIdentity":"msg:ProcessRefundResponse","consumerName":"QuickRefundSagaConsumer"},{"id":"so-12","parentId":"so-11","startTime":"2024-06-15T10:30:00.054Z","durationMs":12,"status":"ok","operation":"saga-transition","sagaName":"QuickRefundSaga","sagaInstanceId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","fromState":"AwaitingRefund","toState":"Completed","eventType":"ProcessRefundResponse"}]}' />

Mocha persists saga state, manages transitions, and supports compensation when steps fail. See [Sagas](/docs/mocha/v16/sagas) for a full walkthrough.

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

The mediator supports commands (with and without responses), queries, notifications, middleware, and EF Core transaction wrapping (commands only by default, configurable via delegate). The source generator produces a typed registration method per assembly (e.g. `AddCatalog()`) that wires up all handlers and pre-compiled dispatch pipelines automatically. See [Mediator](/docs/mocha/v16/mediator) for the full guide.

# Learning paths

Choose an entry point based on how you learn best:

- **Get something running first:** [Quick Start](/docs/mocha/v16/quick-start) -zero to a working message bus in under five minutes with the InMemory transport.
- **Understand the concepts first:** [Messages](/docs/mocha/v16/messages) then [Messaging Patterns](/docs/mocha/v16/messaging-patterns) - learn what flows through the system and what patterns govern how it flows.
- **Evaluating Mocha for a specific broker:** [Transports](/docs/mocha/v16/transports) - understand the transport abstraction and what is available.
- **In-process CQRS:** [Mediator](/docs/mocha/v16/mediator) - dispatch commands, queries, and notifications within a single service using the source-generated mediator.

- **See a real-world system:** The [Demo application](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo) is a complete e-commerce system with three services (Catalog, Billing, Shipping) that demonstrates event-driven communication, sagas, batch processing, the transactional outbox, and .NET Aspire orchestration.

Ready to build? Start with the [Quick Start](/docs/mocha/v16/quick-start).

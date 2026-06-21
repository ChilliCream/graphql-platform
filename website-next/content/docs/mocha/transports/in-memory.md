---
title: "InMemory Transport"
description: "Set up the InMemory transport for development, testing, and single-process messaging scenarios in Mocha."
---

The InMemory transport routes messages through in-process topics and queues without any external broker. Messages never leave the application process and are never persisted to disk.

# Install and register

**1.** Install the package:

```bash
dotnet add package Mocha.Transport.InMemory
```

**2.** Register the transport:

```csharp
using Mocha;
using Mocha.Transport.InMemory;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddInMemory(); // One line - no configuration needed

var app = builder.Build();
app.Run();
```

`.AddInMemory()` registers the transport with default conventions. Topics, queues, and bindings are created automatically based on your registered handlers and message types.

# Verify it works

Add an endpoint that publishes through the bus and check that your handler receives it:

```csharp
app.MapPost("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlacedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-1",
        TotalAmount = 99.99m
    }, CancellationToken.None);

    return Results.Ok();
});
```

Send a POST request to `/orders`. Because the InMemory transport dispatches within the same process, delivery is near-instantaneous and your handler's logic executes before the HTTP response returns.

# How the in-process topology works

The InMemory transport replicates the same topic/queue/binding model that RabbitMQ uses - except everything lives inside your process memory. There is no broker, no network, and no serialization to disk. When you call `PublishAsync`, Mocha routes the message through an in-process topic to every queue bound to that topic, then invokes the handler bound to each queue.

This model is why swapping `.AddInMemory()` for `.AddRabbitMQ()` requires no application code changes. The same topic/queue/binding topology that runs in-process with InMemory is provisioned on the broker when you switch to RabbitMQ.

By default, the InMemory transport processes messages sequentially in the order they are published. Each message is delivered to its handler before the next one is dispatched.

> [!NOTE]
> InMemory tests exercise handler logic and message routing, but not RabbitMQ-specific behavior such as topology conflicts, acknowledgement semantics, connection recovery, or quorum queue characteristics. For testing broker-specific behavior, use a real broker in a test container.

The following shows the default topology Mocha creates when you register an event handler with the InMemory transport:

<MochaTopologyVisualization data='{"services":[{"host":{"serviceName":"MyService","assemblyName":"MyService.dll","instanceId":"my-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"OrderPlacedHandler","identityType":"OrderPlacedHandler","identityTypeFullName":"MyApp.Handlers.OrderPlacedHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler","endpoint":{"name":"my-service.order-placed","address":"loopback://localhost/q/my-service.order-placed","transportName":"InMemory"}}],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlaced","endpoint":{"name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","transportName":"InMemory"}}]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"my-service.order-placed","kind":"default","address":"loopback://localhost/q/my-service.order-placed","source":{"address":"loopback://localhost/q/my-service.order-placed"}}],"dispatchEndpoints":[{"name":"OrderPlaced","kind":"default","address":"loopback://localhost/c/OrderPlaced","destination":{"address":"loopback://localhost/c/OrderPlaced"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"channel","name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","flow":"inbound","properties":{"type":"publish"}},{"kind":"queue","name":"my-service.order-placed","address":"loopback://localhost/q/my-service.order-placed","flow":"outbound","properties":{}}],"links":[{"kind":"subscription","address":"loopback://localhost/sub/OrderPlaced-my-service.order-placed","source":"loopback://localhost/c/OrderPlaced","target":"loopback://localhost/q/my-service.order-placed","direction":"forward","properties":{}}]}}]}' />

# Configure queues

Use `transport.Queue("name")` when you want to choose the queue name, bind multiple handlers to one queue, or configure receive settings. The queue builder is the easiest way to customize in-memory topology because it combines queue declaration, handler binding, convention binding, and endpoint settings in one place.

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddInMemory(transport =>
    {
        transport.BindExplicitly();

        transport.Queue("order-processing")
            .BindImplicitly()
            .MaxConcurrency(5)
            .FaultEndpoint("order-errors")
            .Handler<OrderPlacedEventHandler>();
    });
```

`BindExplicitly()` at the transport scope means only queues you configure are used for receiving. `BindImplicitly()` on the queue tells Mocha to keep the convention-derived topic binding for the messages handled by that queue.

Calling `Queue("name")` without `Handler<T>()`, `Consumer<T>()`, or `Receives<T>()` declares only the in-memory queue. Add a handler, consumer, or received message type when the queue should also consume messages.

```csharp
transport.Queue("audit")
    .Receives<OrderPlacedEvent>();
```

# Declare topology resources

The InMemory transport auto-generates topology from your handler registrations and queue builders.

> [!CAUTION]
> Use `DeclareTopic()`, `DeclareQueue()`, and `DeclareBinding()` only when you need topology resources that are not represented by a receiving queue builder. For handler queues, prefer `transport.Queue("name")`.

To declare infrastructure-only topology:

```csharp
builder.Services
    .AddMessageBus()
    .AddInMemory(transport =>
    {
        // Declare a topic
        transport.DeclareTopic("order-events");

        // Declare a queue
        transport.DeclareQueue("billing-orders");

        // Bind the topic to the queue
        transport.DeclareBinding("order-events", "billing-orders");
    });
```

# Configure convention endpoints

Use `transport.Handler<T>()` at the end of the transport configuration when you want to keep the convention-derived queue name and only tune one handler endpoint:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddInMemory(transport =>
    {
        transport.Handler<OrderPlacedEventHandler>()
            .ConfigureEndpoint(e => e.MaxConcurrency(5));
    });
```

The handler keeps its convention-derived endpoint name. `ConfigureEndpoint()` can be called multiple times - actions compose in declaration order:

```csharp
transport.Handler<OrderPlacedEventHandler>()
    .ConfigureEndpoint(e => e.MaxConcurrency(5))
    .ConfigureEndpoint(e => e.FaultEndpoint("order-errors"));
```

For raw `IConsumer` types, use `transport.Consumer<T>()`:

```csharp
transport.Consumer<OrderAuditConsumer>()
    .ConfigureEndpoint(e => e.MaxConcurrency(3));
```

In a multi-transport setup, `Handler<T>()` also claims the handler for this transport, overriding the default:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddEventHandler<AuditHandler>()
    .AddRabbitMQ(r => r.IsDefaultTransport())
    .AddInMemory(m => m.Handler<AuditHandler>());
// OrderPlacedEventHandler → RabbitMQ (default)
// AuditHandler → InMemory (claimed)
```

# Next steps

- [RabbitMQ Transport](./rabbitmq.md) - Configure the RabbitMQ transport for production deployments.
- [Handlers and Consumers](../handlers-and-consumers.md) - Learn about every handler type and how to register them.

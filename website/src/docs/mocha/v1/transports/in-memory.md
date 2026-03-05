---
title: "InMemory Transport"
description: "Set up the InMemory transport for development, testing, and single-process messaging scenarios in Mocha."
---

# InMemory transport

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

:::note
InMemory tests exercise handler logic and message routing, but not RabbitMQ-specific behavior such as topology conflicts, acknowledgement semantics, connection recovery, or quorum queue characteristics. For testing broker-specific behavior, use a real broker in a test container.
:::

The following shows the default topology Mocha creates when you register an event handler with the InMemory transport:

<TopologyVisualization data='{"services":[{"host":{"serviceName":"MyService","assemblyName":"MyService.dll","instanceId":"my-svc-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MyApp.Messages.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"OrderPlacedHandler","identityType":"OrderPlacedHandler","identityTypeFullName":"MyApp.Handlers.OrderPlacedHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler","endpoint":{"name":"my-service.order-placed","address":"loopback://localhost/q/my-service.order-placed","transportName":"InMemory"}}],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlaced","endpoint":{"name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","transportName":"InMemory"}}]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"my-service.order-placed","kind":"default","address":"loopback://localhost/q/my-service.order-placed","source":{"address":"loopback://localhost/q/my-service.order-placed"}}],"dispatchEndpoints":[{"name":"OrderPlaced","kind":"default","address":"loopback://localhost/c/OrderPlaced","destination":{"address":"loopback://localhost/c/OrderPlaced"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"channel","name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","flow":"inbound","properties":{"type":"publish"}},{"kind":"queue","name":"my-service.order-placed","address":"loopback://localhost/q/my-service.order-placed","flow":"outbound","properties":{}}],"links":[{"kind":"subscription","address":"loopback://localhost/sub/OrderPlaced-my-service.order-placed","source":"loopback://localhost/c/OrderPlaced","target":"loopback://localhost/q/my-service.order-placed","direction":"forward","properties":{}}]}}]}' />

# Declare custom topology

The InMemory transport auto-generates topology from your handler registrations. To declare custom topology explicitly:

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

To control which handlers consume from which queues:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddInMemory(transport =>
    {
        transport.BindHandlersExplicitly();

        transport.Endpoint("order-processing")
            .Handler<OrderPlacedEventHandler>();
    });
```

# Next steps

- [RabbitMQ Transport](/docs/mocha/v1/transports/rabbitmq) - Configure the RabbitMQ transport for production deployments.
- [Handlers and Consumers](/docs/mocha/v1/handlers-and-consumers) - Learn about every handler type and how to register them.

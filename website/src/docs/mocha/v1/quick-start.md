---
title: "Quick Start"
description: "Get started with Mocha in under five minutes. Install packages, register the message bus, define an event handler, publish your first event, and verify it works."
---

By the end of this guide, you will have an ASP.NET Core app that publishes an `OrderPlaced` event and handles it - all running in-process with the InMemory transport.

Here is what you are building:

<TopologyVisualization data='{"services":[{"host":{"serviceName":"MochaQuickStart","assemblyName":"MochaQuickStart.dll","instanceId":"quickstart-1"},"messageTypes":[{"identity":"msg:OrderPlaced","runtimeType":"OrderPlaced","runtimeTypeFullName":"MochaQuickStart.OrderPlaced","isInterface":false,"isInternal":false}],"consumers":[{"name":"OrderPlacedHandler","identityType":"OrderPlacedHandler","identityTypeFullName":"MochaQuickStart.OrderPlacedHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler","endpoint":{"name":"order-placed-handler","address":"loopback://localhost/q/order-placed-handler","transportName":"InMemory"}}],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlaced","endpoint":{"name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","transportName":"InMemory"}}]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"order-placed-handler","kind":"default","address":"loopback://localhost/q/order-placed-handler","source":{"address":"loopback://localhost/q/order-placed-handler"}}],"dispatchEndpoints":[{"name":"OrderPlaced","kind":"default","address":"loopback://localhost/c/OrderPlaced","destination":{"address":"loopback://localhost/c/OrderPlaced"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"channel","name":"OrderPlaced","address":"loopback://localhost/c/OrderPlaced","flow":"inbound","properties":{"type":"publish"}},{"kind":"queue","name":"order-placed-handler","address":"loopback://localhost/q/order-placed-handler","flow":"outbound","properties":{}}],"links":[{"kind":"subscription","address":"loopback://localhost/sub/OrderPlaced-order-placed-handler","source":"loopback://localhost/c/OrderPlaced","target":"loopback://localhost/q/order-placed-handler","direction":"forward","properties":{}}]}}]}' trace='{"traceId":"quickstart-trace-001","activities":[{"id":"qs-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":35,"status":"ok","operation":"publish","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","transport":"InMemory"},{"id":"qs-2","parentId":"qs-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"OrderPlaced","endpointAddress":"loopback://localhost/c/OrderPlaced","transport":"InMemory"},{"id":"qs-3","parentId":"qs-2","startTime":"2024-06-15T10:30:00.008Z","durationMs":3,"status":"ok","operation":"receive","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","endpointName":"order-placed-handler","endpointAddress":"loopback://localhost/q/order-placed-handler","transport":"InMemory"},{"id":"qs-4","parentId":"qs-3","startTime":"2024-06-15T10:30:00.011Z","durationMs":10,"status":"ok","operation":"consume","messageType":"OrderPlaced","messageTypeIdentity":"msg:OrderPlaced","consumerName":"OrderPlacedHandler"}]}' />

# Create the project

```bash
dotnet new web -n MochaQuickStart
cd MochaQuickStart
```

# Install the packages

You need two packages: the core bus and the InMemory transport.

```bash
dotnet add package Mocha
dotnet add package Mocha.Transport.InMemory
```

The InMemory transport keeps everything in-process - no broker to install, no infrastructure to configure. It is the fastest way to get started.

# Define a message

A message is a plain C# record. Create a file called `OrderPlaced.cs`:

```csharp
// OrderPlaced.cs
namespace MochaQuickStart;

public sealed record OrderPlaced(
    Guid OrderId,
    string ProductName,
    decimal Amount);
```

No base class, no marker interface. Any record or class works as a message.

# Create a handler

A handler is a class that implements `IEventHandler<T>`. Create a file called `OrderPlacedHandler.cs`:

```csharp
using Mocha;

namespace MochaQuickStart;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order received: {OrderId} - {ProductName} for {Amount:C}",
            message.OrderId,
            message.ProductName,
            message.Amount);

        return ValueTask.CompletedTask;
    }
}
```

The bus calls `HandleAsync` every time an `OrderPlaced` event is published. The handler receives the deserialized message and a cancellation token.

# Register the bus

Open `Program.cs` and replace its contents:

```csharp
// Program.cs
using Mocha;
using Mocha.Transport.InMemory;
using MochaQuickStart;

var builder = WebApplication.CreateBuilder(args);

// Register the message bus, handlers, and transport
builder.Services
    .AddMessageBus()
    .AddMochaQuickStart() // source-generated - discovers all handlers in this assembly
    .AddInMemory();

var app = builder.Build();

// Endpoint that publishes an event
app.MapPost("/orders", async (IMessageBus bus) =>
{
    var orderPlaced = new OrderPlaced(
        OrderId: Guid.NewGuid(),
        ProductName: "Mechanical Keyboard",
        Amount: 149.99m);

    await bus.PublishAsync(orderPlaced, CancellationToken.None);

    return Results.Ok(new { orderPlaced.OrderId, Status = "Published" });
});

app.Run();
```

Each registration line has a single responsibility:

- `AddMessageBus()` - registers the bus runtime and core services into DI.
- `AddMochaQuickStart()` - source-generated method that discovers and registers all handlers in this assembly. Named after the project - to customize the name, see [Handler Registration](/docs/mocha/v1/handler-registration).
- `AddInMemory()` - adds the InMemory transport; messages stay in-process.

# Publish and verify

Run the app:

```bash
dotnet run
```

Check your console output for the actual URL. ASP.NET Core's default port may differ depending on your SDK version and launch settings. Then, in another terminal, send a POST request using that URL:

```bash
curl -X POST http://localhost:5000/orders
```

You should see JSON from curl:

```json
{ "orderId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890", "status": "Published" }
```

And in the application console, the handler's log message appears:

```text
info: MochaQuickStart.OrderPlacedHandler[0]
      Order received: a1b2c3d4-e5f6-7890-abcd-ef1234567890 - Mechanical Keyboard for $149.99
```

If you see that log line, it worked.

## What happened?

Your POST request hit the `/orders` endpoint, which called `PublishAsync` on `IMessageBus`. The bus serialized the `OrderPlaced` record and handed it to the InMemory transport. The transport delivered it to the registered receive endpoint, which ran the message through the pipeline and invoked `HandleAsync` on your `OrderPlacedHandler`. The log line you see is proof the full path executed: publisher to bus to transport to handler.

# Next steps

You have a working message bus. Here is where to go next:

- **Understand messages:** [Messages](/docs/mocha/v1/messages) - learn what a message is, how the envelope wraps it, and naming conventions for events and commands.
- **Learn the three patterns:** [Messaging Patterns](/docs/mocha/v1/messaging-patterns) - understand when to use pub/sub events, commands, and request/reply.
- **Move to production:** [Transports](/docs/mocha/v1/transports) - switch from InMemory to RabbitMQ for real workloads.

Now that you have a working app, learn how messages work in [Messages](/docs/mocha/v1/messages).

> **Runnable example:** [Examples/QuickStart](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/QuickStart)
>
> **Full demo:** The [Demo application](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo) shows a complete e-commerce system with Catalog, Billing, and Shipping services communicating through Mocha and orchestrated with .NET Aspire.

---
title: "Quick Start"
description: "Get started with Mocha in under five minutes. Install packages, register the message bus, define an event handler, publish your first event, and verify it works."
---

By the end of this guide, you will have an ASP.NET Core app that publishes an `OrderPlaced` event and handles it -- all running in-process with the InMemory transport.

Here is what you are building:

```
POST /orders  →  PublishAsync(OrderPlaced)  →  InMemory Bus  →  OrderPlacedHandler
```

# Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download) or later

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

The InMemory transport keeps everything in-process -- no broker to install, no infrastructure to configure. It is the fastest way to get started and also useful for [testing](/docs/mocha/v1/testing).

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
// OrderPlacedHandler.cs
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
            "Order received: {OrderId} — {ProductName} for {Amount:C}",
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

// Register the message bus, handler, and transport
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();

var app = builder.Build();

// Start the messaging runtime before the app begins serving requests.
// This initializes all transports and receive endpoints so the bus is
// ready to route messages when PublishAsync is called.
await app.StartMessagingAsync();

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

- `AddMessageBus()` -- registers the bus runtime and core services into DI.
- `AddEventHandler<OrderPlacedHandler>()` -- registers your handler so the bus routes `OrderPlaced` events to it.
- `AddInMemory()` -- adds the InMemory transport; messages stay in-process.
- `StartMessagingAsync()` -- starts all transports and receive endpoints. Call this before the app begins serving requests.

<Warning>

Call `StartMessagingAsync()` before `app.Run()`. Without it, the bus has no active transport and published events are not delivered.

</Warning>

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
      Order received: a1b2c3d4-e5f6-7890-abcd-ef1234567890 — Mechanical Keyboard for $149.99
```

If you see that log line, it worked.

## What just happened?

Your POST request hit the `/orders` endpoint, which called `PublishAsync` on `IMessageBus`. The bus serialized the `OrderPlaced` record and handed it to the InMemory transport. The transport delivered it to the registered receive endpoint, which ran the message through the pipeline and invoked `HandleAsync` on your `OrderPlacedHandler`. The log line you see is proof the full path executed: publisher to bus to transport to handler.

# Next steps

You have a working message bus. Here is where to go next:

- **Understand messages:** [Messages](/docs/mocha/v1/messages) -- learn what a message is, how the envelope wraps it, and naming conventions for events and commands.
- **Learn the three patterns:** [Messaging Patterns](/docs/mocha/v1/messaging-patterns) -- understand when to use pub/sub events, commands, and request/reply.
- **Move to production:** [Transports](/docs/mocha/v1/transports) -- switch from InMemory to RabbitMQ for real workloads.

Now that you have a working app, learn how messages work in [Messages](/docs/mocha/v1/messages).

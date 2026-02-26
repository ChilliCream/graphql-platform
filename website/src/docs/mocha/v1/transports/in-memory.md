---
title: "InMemory Transport"
description: "Set up the InMemory transport for development, testing, and single-process messaging scenarios in Mocha."
---

# InMemory transport

The InMemory transport routes messages through in-process topics and queues without any external broker. Messages never leave the application process and are never persisted to disk.

:::warning
**Not for production.** The InMemory transport is for development and testing only. All queued messages are lost when the process exits. It cannot communicate across process boundaries, so it cannot model multi-service scenarios. For production, use the [RabbitMQ transport](/docs/mocha/v1/transports/rabbitmq).
:::

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
    .AddInMemory(); // One line — no configuration needed

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

The InMemory transport replicates the same topic/queue/binding model that RabbitMQ uses — except everything lives inside your process memory. There is no broker, no network, and no serialization to disk. When you call `PublishAsync`, Mocha routes the message through an in-process topic to every queue bound to that topic, then invokes the handler bound to each queue.

This model is why swapping `.AddInMemory()` for `.AddRabbitMQ()` requires no application code changes. The same topic/queue/binding topology that runs in-process with InMemory is provisioned on the broker when you switch to RabbitMQ.

By default, the InMemory transport processes messages sequentially in the order they are published. Each message is delivered to its handler before the next one is dispatched.

:::note
InMemory tests exercise handler logic and message routing, but not RabbitMQ-specific behavior such as topology conflicts, acknowledgement semantics, connection recovery, or quorum queue characteristics. For testing broker-specific behavior, use a real broker in a test container.
:::

# Integration testing

The InMemory transport is the recommended transport for integration tests. It eliminates external dependencies, runs in-process, and provides deterministic behavior.

## Basic test setup

Create a new host per test to avoid shared state between test runs:

```csharp
var host = new HostBuilder()
    .ConfigureServices(services =>
    {
        services
            .AddMessageBus()
            .AddEventHandler<OrderPlacedEventHandler>()
            .AddInMemory();
    })
    .Build();

await host.StartAsync();
```

Publish a message and assert the handler ran:

```csharp
using var scope = host.Services.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

await bus.PublishAsync(new OrderPlacedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerId = "test-customer",
    TotalAmount = 50.00m
}, CancellationToken.None);

// Assert your handler's side effects (database writes, state changes, etc.)
```

Because there is no network I/O, tests run fast and reliably without flaky timing issues.

## Async handler completion

The InMemory transport dispatches messages synchronously by default, which means `PublishAsync` returns only after the handler has completed. You can assert side effects immediately after awaiting the publish call — no polling or manual delays required.

For handlers that trigger further message publishing internally, all downstream handlers also complete before `PublishAsync` returns.

## Test isolation

Create a new `HostBuilder` and call `StartAsync` for each test. Reusing a host across tests introduces shared state in the in-process topology and can cause test interference.

With xUnit, use `IAsyncLifetime` to manage the host lifecycle:

```csharp
public class OrderTests : IAsyncLifetime
{
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureServices(services =>
            {
                services
                    .AddMessageBus()
                    .AddEventHandler<OrderPlacedEventHandler>()
                    .AddInMemory();
            })
            .Build();

        await _host.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    [Fact]
    public async Task OrderPlaced_HandlerReceivesEvent()
    {
        using var scope = _host.Services.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        await bus.PublishAsync(new OrderPlacedEvent
        {
            OrderId = Guid.NewGuid(),
            CustomerId = "test-customer",
            TotalAmount = 50.00m
        }, CancellationToken.None);

        // Assert side effects
    }
}
```

Each test gets a fresh bus with no residual state from previous tests.

## Concurrency behavior

By default, the InMemory transport processes messages sequentially. If you publish multiple messages in a loop, each is delivered and handled before the next is dispatched. This makes test assertions straightforward — there are no race conditions to account for.

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

- [RabbitMQ Transport](/docs/mocha/v1/transports/rabbitmq) — Configure the RabbitMQ transport for production deployments.
- [Handlers and Consumers](/docs/mocha/v1/handlers-and-consumers) — Learn about every handler type and how to register them.
- [Testing](/docs/mocha/v1/testing) — Learn patterns for testing handler logic and message flows.

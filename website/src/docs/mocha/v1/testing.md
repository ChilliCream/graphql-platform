---
title: "Testing"
description: "Test message-driven code in Mocha with confidence. Learn when to call handlers directly, when to use the InMemory transport, and when to bring in a real broker. Includes the TestBus fixture, saga multi-event sequences, error handling assertions, and xUnit IClassFixture guidance."
---

# Testing

Message-driven code is straightforward to test once you know which layer to test at. The key is choosing the right tool for the question you're asking.

## Testing philosophy

Martin Fowler's [Practical Test Pyramid](https://martinfowler.com/articles/practical-test-pyramid.html) and [Testing Strategies in Microservices](https://martinfowler.com/articles/microservice-testing/) establish a clear principle: use many cheap, fast unit tests to verify business logic, and a smaller number of integration tests to verify that the system wires together correctly.

This applies directly to message-driven code:

| What you're testing                                                                    | Approach                                                | Speed        |
| -------------------------------------------------------------------------------------- | ------------------------------------------------------- | ------------ |
| Handler decision logic — what it sends, publishes, or modifies                         | Call the handler directly. No bus, no DI, no transport. | Microseconds |
| Routing, middleware, pipeline behavior, message flow                                   | Use InMemory transport. Full pipeline, no broker.       | Milliseconds |
| Transport-specific configuration — exchange bindings, queue types, connection recovery | Use the real broker in a targeted test.                 | Seconds      |

Most tests should live in the first two rows. A healthy test suite for a message-driven service has many handler unit tests, a moderate number of InMemory integration tests, and a small number of broker-level tests for infrastructure verification.

## The MessageRecorder utility

Many integration tests need to verify that a handler received a message. Because handler execution is asynchronous, you need a synchronization primitive that signals when a message arrives. Copy this utility class into your test project once:

```csharp
using System.Collections.Concurrent;

public sealed class MessageRecorder
{
    private readonly SemaphoreSlim _semaphore = new(0);

    // All messages recorded by this instance, in arrival order.
    public ConcurrentBag<object> Messages { get; } = [];

    public void Record(object message)
    {
        Messages.Add(message);
        _semaphore.Release();
    }

    // Blocks until the expected number of messages arrive or the timeout expires.
    // Returns true if all messages arrived; false if the timeout was reached.
    public async Task<bool> WaitAsync(TimeSpan timeout, int expectedCount = 1)
    {
        for (var i = 0; i < expectedCount; i++)
        {
            if (!await _semaphore.WaitAsync(timeout))
                return false;
        }
        return true;
    }
}
```

`WaitAsync` uses a `SemaphoreSlim` rather than `Task.Delay`. This means tests complete as soon as the handler fires — not after a fixed delay — making the suite fast and deterministic.

## The TestBus fixture

Every integration test needs the same three steps: configure the bus, build the DI container, and start the messaging runtime. The `TestBus` fixture extracts these into one reusable method so your tests stay focused on what they're asserting.

```csharp
public static class TestBus
{
    public static async Task<ServiceProvider> CreateAsync(
        Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        configure(builder);

        builder.AddInMemory(); // Use InMemory transport for all tests

        var provider = services.BuildServiceProvider();

        // Start the messaging runtime. The cast is an internal detail
        // hidden here so individual tests never need to know about it.
        var runtime = (MessagingRuntime)provider
            .GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync(CancellationToken.None);

        return provider;
    }
}
```

Each test calls `TestBus.CreateAsync`, configures its handlers, and gets back a running `ServiceProvider`. When the provider is disposed at the end of the test, the bus shuts down cleanly.

> Each test creates its own `ServiceProvider` with its own InMemory transport instance. Tests are isolated by default — no shared state, no cross-test interference. All InMemory bus tests can run in parallel without collection isolation.

## Unit testing handler logic

When you want to verify what a handler _decides_ — which messages it produces, which fields it sets — call it directly. No transport, no DI container, no `TestBus`.

Define the handler with its dependency injected via the constructor:

```csharp
public record OrderPlaced(string OrderId, decimal Total);

public sealed class OrderPlacedHandler(MessageRecorder recorder)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}
```

Call it directly in the test:

```csharp
[Fact]
public async Task Handler_records_the_order_it_receives()
{
    var recorder = new MessageRecorder();
    var handler = new OrderPlacedHandler(recorder);

    await handler.HandleAsync(
        new OrderPlaced("ORD-1", 99.99m),
        CancellationToken.None);

    var message = Assert.IsType<OrderPlaced>(Assert.Single(recorder.Messages));
    Assert.Equal("ORD-1", message.OrderId);
}
```

Five lines of setup. No bus, no async timing, no waiting. Use this pattern for the majority of your handler tests.

Use the InMemory transport when you need to verify that the pipeline delivers the message correctly — middleware, routing, serialization, header propagation. Use the real broker only when testing broker-specific configuration.

## Integration: publish and assert

When you need to verify that a published event reaches its handler through the real pipeline, use `TestBus` with `MessageRecorder`.

Define the message and handler:

```csharp
public record OrderPlaced(string OrderId, decimal Total);

public sealed class OrderPlacedHandler(MessageRecorder recorder)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}
```

Write the test using the fixture:

```csharp
[Fact]
public async Task PublishAsync_delivers_event_to_handler()
{
    // Arrange
    var recorder = new MessageRecorder();
    await using var provider = await TestBus.CreateAsync(builder =>
    {
        builder.Services.AddSingleton(recorder);
        builder.AddEventHandler<OrderPlacedHandler>();
    });

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act
    await bus.PublishAsync(
        new OrderPlaced("ORD-1", 99.99m),
        CancellationToken.None);

    // Assert
    Assert.True(
        await recorder.WaitAsync(TimeSpan.FromSeconds(10)),
        "Handler did not receive the event within timeout");

    var message = Assert.IsType<OrderPlaced>(Assert.Single(recorder.Messages));
    Assert.Equal("ORD-1", message.OrderId);
    Assert.Equal(99.99m, message.Total);
}
```

If the assertion passes, `recorder.Messages` contains the `OrderPlaced` instance exactly as published.

## Integration: request/reply

Request/reply tests do not need a `MessageRecorder`. `RequestAsync` awaits the handler's response directly, so you assert on the return value.

```csharp
public record GetOrderStatus(string OrderId) : IEventRequest<OrderStatusResponse>;

public record OrderStatusResponse(string OrderId, string Status);

public sealed class GetOrderStatusHandler
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(
        GetOrderStatus request,
        CancellationToken cancellationToken)
        => new(new OrderStatusResponse(request.OrderId, "Shipped"));
}
```

```csharp
[Fact]
public async Task RequestAsync_returns_typed_response()
{
    // Arrange
    await using var provider = await TestBus.CreateAsync(builder =>
        builder.AddRequestHandler<GetOrderStatusHandler>());

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act
    var response = await bus.RequestAsync(
        new GetOrderStatus("ORD-1"),
        CancellationToken.None);

    // Assert
    Assert.Equal("ORD-1", response.OrderId);
    Assert.Equal("Shipped", response.Status);
}
```

## Integration: send (fire-and-forget)

`SendAsync` dispatches a command to a single handler without waiting for a response. Use `MessageRecorder` to verify delivery.

```csharp
public record ProcessRefund(string OrderId, decimal Amount);

public sealed class ProcessRefundHandler(MessageRecorder recorder)
    : IEventRequestHandler<ProcessRefund>
{
    public ValueTask HandleAsync(
        ProcessRefund request,
        CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return default;
    }
}
```

```csharp
[Fact]
public async Task SendAsync_delivers_command_to_handler()
{
    // Arrange
    var recorder = new MessageRecorder();
    await using var provider = await TestBus.CreateAsync(builder =>
    {
        builder.Services.AddSingleton(recorder);
        builder.AddRequestHandler<ProcessRefundHandler>();
    });

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act
    await bus.SendAsync(
        new ProcessRefund("ORD-1", 49.99m),
        CancellationToken.None);

    // Assert
    Assert.True(
        await recorder.WaitAsync(TimeSpan.FromSeconds(10)),
        "Handler did not receive the command within timeout");

    var refund = Assert.IsType<ProcessRefund>(Assert.Single(recorder.Messages));
    Assert.Equal("ORD-1", refund.OrderId);
    Assert.Equal(49.99m, refund.Amount);
}
```

## Integration: fan-out to multiple handlers

When multiple handlers subscribe to the same event, the bus delivers to all of them. Use keyed services to give each handler its own recorder.

```csharp
public sealed class AuditHandler(
    [FromKeyedServices("audit")] MessageRecorder recorder)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}

public sealed class NotificationHandler(
    [FromKeyedServices("notify")] MessageRecorder recorder)
    : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken cancellationToken)
    {
        recorder.Record(message);
        return default;
    }
}
```

```csharp
[Fact]
public async Task PublishAsync_fans_out_to_all_handlers()
{
    // Arrange
    var auditRecorder = new MessageRecorder();
    var notifyRecorder = new MessageRecorder();

    await using var provider = await TestBus.CreateAsync(builder =>
    {
        builder.Services.AddKeyedSingleton("audit", auditRecorder);
        builder.Services.AddKeyedSingleton("notify", notifyRecorder);
        builder.AddEventHandler<AuditHandler>();
        builder.AddEventHandler<NotificationHandler>();
    });

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act
    await bus.PublishAsync(new OrderPlaced("ORD-1", 99.99m), CancellationToken.None);

    // Assert — both handlers must receive the event
    Assert.True(await auditRecorder.WaitAsync(TimeSpan.FromSeconds(10)));
    Assert.True(await notifyRecorder.WaitAsync(TimeSpan.FromSeconds(10)));
    Assert.Single(auditRecorder.Messages);
    Assert.Single(notifyRecorder.Messages);
}
```

## Integration: custom headers

To verify that custom headers flow through the pipeline, use `IConsumer<T>` to access `IConsumeContext<T>.Headers` in the handler.

```csharp
public sealed class HeaderCapture
{
    private readonly SemaphoreSlim _semaphore = new(0);
    public ConcurrentBag<Dictionary<string, object?>> CapturedHeaders { get; } = [];

    public void RecordHeaders(IReadOnlyHeaders headers)
    {
        var dict = new Dictionary<string, object?>();
        foreach (var h in headers)
            dict[h.Key] = h.Value;
        CapturedHeaders.Add(dict);
        _semaphore.Release();
    }

    public async Task<bool> WaitAsync(TimeSpan timeout)
        => await _semaphore.WaitAsync(timeout);
}

public sealed class HeaderSpyConsumer(HeaderCapture capture)
    : IConsumer<OrderPlaced>
{
    public ValueTask ConsumeAsync(IConsumeContext<OrderPlaced> context)
    {
        capture.RecordHeaders(context.Headers);
        return default;
    }
}
```

```csharp
[Fact]
public async Task PublishAsync_propagates_custom_headers()
{
    // Arrange
    var capture = new HeaderCapture();
    await using var provider = await TestBus.CreateAsync(builder =>
    {
        builder.Services.AddSingleton(capture);
        builder.AddConsumer<HeaderSpyConsumer>();
    });

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act
    await bus.PublishAsync(
        new OrderPlaced("ORD-1", 99.99m),
        new PublishOptions { Headers = new() { ["x-tenant"] = "acme" } },
        CancellationToken.None);

    // Assert
    Assert.True(await capture.WaitAsync(TimeSpan.FromSeconds(10)));
    var headers = Assert.Single(capture.CapturedHeaders);
    Assert.True(headers.TryGetValue("x-tenant", out var tenant));
    Assert.Equal("acme", tenant);
}
```

## Saga testing

`SagaTester<T>` runs saga state machines in isolation — no bus, no DI container, no transport. Use it to verify saga logic by feeding events in sequence and asserting on state and outbound messages.

The example below covers a three-event order saga: placed, payment received, then shipped.

```csharp
using Mocha.Sagas.Tests;

[Fact]
public async Task Saga_transitions_through_states_and_sends_confirmation()
{
    // Arrange — define the saga inline or reference your saga class
    var saga = Saga.Create<OrderSagaState>(x =>
    {
        x.Initially()
            .OnEvent<OrderPlaced>()
            .StateFactory(_ => new OrderSagaState())
            .TransitionTo("Placed");

        x.During("Placed")
            .OnEvent<PaymentReceived>()
            .TransitionTo("Paid")
            .Send(ctx => new SendConfirmationEmail(ctx.State.OrderId));

        x.During("Paid")
            .OnEvent<OrderShipped>()
            .TransitionTo("Shipped");

        x.Finally("Shipped");
    });

    var tester = SagaTester.Create(saga);

    // Act — first event creates the saga instance
    await tester.ExecuteAsync(new OrderPlaced("ORD-1", 99.99m));
    tester.ExpectState("Placed");

    // Act — second event transitions and triggers an outbound send
    await tester.ExecuteAsync(new PaymentReceived("ORD-1"));
    tester.ExpectState("Paid");
    tester.ExpectSentMessage<SendConfirmationEmail>();

    // Act — third event completes the saga
    await tester.ExecuteAsync(new OrderShipped("ORD-1"));
    tester.ExpectCompleted();
}
```

To test a saga mid-flow without replaying all preceding events, use `SetState` to seed the starting state:

```csharp
[Fact]
public async Task Saga_in_Paid_state_completes_on_shipment()
{
    var tester = SagaTester.Create(saga);

    // Skip to the "Paid" state directly
    tester.SetState(new OrderSagaState { CurrentState = "Paid", OrderId = "ORD-2" });

    await tester.ExecuteAsync(new OrderShipped("ORD-2"));
    tester.ExpectCompleted();
}
```

`SagaTester<T>` assertion methods:

| Method                        | Purpose                                                  |
| ----------------------------- | -------------------------------------------------------- |
| `ExpectState(string)`         | Assert the saga is in a specific named state.            |
| `ExpectCompleted()`           | Assert the saga reached its final state and was removed. |
| `ExpectSentMessage<T>()`      | Assert a message was sent via `SendAsync`.               |
| `ExpectPublishedMessage<T>()` | Assert a message was published via `PublishAsync`.       |
| `ExpectReplyMessage<T>()`     | Assert a reply was sent via `ReplyAsync`.                |
| `SetState(T)`                 | Pre-seed saga state for testing mid-flow transitions.    |

## Testing error handling

When a handler throws, the bus wraps the exception and surfaces it to the caller. For `RequestAsync`, the exception type is `RemoteErrorException`.

Define a handler that throws:

```csharp
public record GetOrderStatus(string OrderId) : IEventRequest<OrderStatusResponse>;

public sealed class FailingOrderStatusHandler
    : IEventRequestHandler<GetOrderStatus, OrderStatusResponse>
{
    public ValueTask<OrderStatusResponse> HandleAsync(
        GetOrderStatus request,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("Order not found.");
}
```

Assert that `RequestAsync` throws the wrapped exception:

```csharp
[Fact]
public async Task RequestAsync_throws_RemoteErrorException_when_handler_fails()
{
    // Arrange
    await using var provider = await TestBus.CreateAsync(builder =>
        builder.AddRequestHandler<FailingOrderStatusHandler>());

    using var scope = provider.CreateScope();
    var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

    // Act & Assert
    var ex = await Assert.ThrowsAsync<RemoteErrorException>(() =>
        bus.RequestAsync(
            new GetOrderStatus("ORD-1"),
            CancellationToken.None).AsTask());

    Assert.Contains("Order not found.", ex.Message);
}
```

For event handlers (publish/send), a thrown exception does not propagate to the publisher. The bus handles the failure according to the configured retry and dead-letter policy. To assert that error handling middleware ran, assert on side effects — for example, a `MessageRecorder` attached to an error handler, or an entry written to a log sink.

See [Reliability](/docs/mocha/v1/reliability) for details on retry policies and dead-letter configuration.

## xUnit IClassFixture guidance

By default, xUnit creates a new instance of the test class for every test. This means every test already gets its own `TestBus`. No special configuration is needed for isolation.

If you want to share a single bus across all tests in a class — for example, to avoid repeated startup cost — use `IClassFixture<T>` with `IAsyncLifetime`.

Create the fixture:

```csharp
public sealed class SharedBusFixture : IAsyncLifetime
{
    public ServiceProvider Provider { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Provider = await TestBus.CreateAsync(builder =>
        {
            // Register handlers shared across all tests in the class.
            builder.AddRequestHandler<GetOrderStatusHandler>();
        });
    }

    public async Task DisposeAsync()
    {
        await Provider.DisposeAsync();
    }
}
```

Use it in a test class:

```csharp
public sealed class OrderStatusTests(SharedBusFixture fixture)
    : IClassFixture<SharedBusFixture>
{
    [Fact]
    public async Task Returns_shipped_status()
    {
        using var scope = fixture.Provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();

        var response = await bus.RequestAsync(
            new GetOrderStatus("ORD-1"),
            CancellationToken.None);

        Assert.Equal("Shipped", response.Status);
    }
}
```

> Use `IClassFixture` only when the shared bus does not carry mutable state between tests. Request/reply handlers are safe to share because they produce no side effects in the bus. Tests that use `MessageRecorder` should each create their own `TestBus` to avoid recorder state leaking between tests.

## Next steps

You now have the full testing toolkit for Mocha: direct handler invocation for fast unit tests, the `TestBus` fixture for InMemory integration tests, `SagaTester<T>` for saga sequences, and `RemoteErrorException` for error path assertions.

To understand how the InMemory transport compares to production transports, see [Transports](/docs/mocha/v1/transports).

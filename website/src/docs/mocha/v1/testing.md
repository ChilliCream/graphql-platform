---
title: "Testing"
description: "Test Mocha messaging with the Mocha.Testing package. Message tracking, completion detection, typed assertions, snapshot testing, and diagnostic output for integration tests."
---

# Testing

`Mocha.Testing` gives you one thing: an observer that watches every message flowing through the bus, detects when all work is done, and lets you assert on what happened. You use the real bus, the real transport, and the real handlers. The only test-specific addition is `IMessageTracker`.

```csharp
services.AddMessageTracking();
```

One call. No test harness, no fake bus, no wrapper. The tracker registers as a diagnostic event listener and records everything.

# Install

```bash
dotnet add package Mocha.Testing
```

The package has no dependency on any test framework. Assertion failures throw `MessageTrackingException` with built-in diagnostic output, so it works with xUnit, NUnit, MSTest, or anything else.

# Your first test

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Testing;
using Mocha.Transport.InMemory;

public class OrderTests
{
    [Fact]
    public async Task PlaceOrder_Should_PublishOrderConfirmed()
    {
        // Arrange
        await using var provider = new ServiceCollection()
            .AddMessageBus()
            .AddInMemory()
            .AddEventHandler<PlaceOrderHandler>()
            .AddEventHandler<OrderConfirmationHandler>()
            .AddMessageTracking()
            .BuildServiceProvider(true);

        var runtime = provider.GetRequiredService<IMessagingRuntime>();
        await runtime.StartAsync();

        var tracker = provider.GetRequiredService<IMessageTracker>();

        // Act
        using var scope = provider.CreateScope();
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new PlaceOrder { OrderId = "123" }, CancellationToken.None);

        // Assert
        var result = await tracker.WaitForCompletionAsync();

        result.ShouldHaveConsumed<PlaceOrder>();
        var confirmed = result.ShouldHavePublished<OrderConfirmed>();
        Assert.Equal("123", confirmed.OrderId);
    }
}
```

That is the entire pattern:

1. Build a service provider with `AddMessageTracking()`
2. Start the runtime
3. Resolve `IMessageTracker`
4. Publish or send messages through the real `IMessageBus`
5. Call `WaitForCompletionAsync()` — it returns when all cascading work finishes
6. Assert on the result

# Core concepts

## IMessageTracker

The primary test interface. Resolve it from your service provider after calling `AddMessageTracking()`.

```csharp
var tracker = provider.GetRequiredService<IMessageTracker>();
```

`IMessageTracker` is an observer, not a wrapper. It implements `ITrackedMessages` (cumulative view of all messages across all waits) and provides:

- `WaitForCompletionAsync()` — blocks until every dispatched message has been consumed or has failed
- `WaitForConsumed<T>()` — blocks until a specific message type is consumed
- `Timeline` — ordered list of every lifecycle event (dispatch, receive, consume)
- `ToDiagnosticString()` — human-readable summary of all tracked activity
- `WhenSent<T>()` — configures a stub response for sent messages

## MessageTrackingResult

`WaitForCompletionAsync()` returns a `MessageTrackingResult`. Each call returns a **delta** — only the messages since the previous call. The tracker maintains a high-water mark internally.

```csharp
var result = await tracker.WaitForCompletionAsync();
result.Dispatched  // messages dispatched in this step
result.Consumed    // messages consumed in this step
result.Failed      // messages that failed in this step
result.Completed   // true if all messages reached a terminal state
result.Elapsed     // how long the wait took
```

## Cumulative vs delta

The tracker itself (`IMessageTracker`) holds the **cumulative** view — all messages across all `WaitForCompletionAsync()` calls. Each `MessageTrackingResult` holds the **delta** — only the messages from that specific wait.

```csharp
// Step 1
await bus.PublishAsync(new OrderCreated { OrderId = "1" }, ct);
var step1 = await tracker.WaitForCompletionAsync();
// step1.Dispatched.Count == 1

// Step 2
await bus.PublishAsync(new OrderCreated { OrderId = "2" }, ct);
var step2 = await tracker.WaitForCompletionAsync();
// step2.Dispatched.Count == 1 (delta — only step 2)

// Cumulative
// tracker.Dispatched.Count == 2 (both steps)
```

No `Reset()` method. No session objects. The delta pattern handles multi-step tests naturally.

# Usage patterns

## Multi-step saga test

When testing a saga or any multi-step workflow, call `WaitForCompletionAsync()` after each step. Each result contains only the messages from that step.

```csharp
[Fact]
public async Task Saga_Should_TransitionThroughStates()
{
    await using var provider = new ServiceCollection()
        .AddMessageBus()
        .AddInMemory()
        .AddSaga<OrderSaga>()
        .AddEventHandler<PaymentHandler>()
        .AddMessageTracking()
        .BuildServiceProvider(true);

    var runtime = provider.GetRequiredService<IMessagingRuntime>();
    await runtime.StartAsync();

    var tracker = provider.GetRequiredService<IMessageTracker>();

    // Step 1: Create order
    using (var scope = provider.CreateScope())
    {
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new OrderCreated { OrderId = "order-1" }, CancellationToken.None);
    }

    var step1 = await tracker.WaitForCompletionAsync();
    step1.ShouldHaveConsumed<OrderCreated>();
    step1.ShouldHaveSent<ProcessPayment>();

    // Step 2: Complete payment — step2 only has messages since step1
    using (var scope = provider.CreateScope())
    {
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await bus.PublishAsync(new PaymentCompleted { OrderId = "order-1" }, CancellationToken.None);
    }

    var step2 = await tracker.WaitForCompletionAsync();
    step2.ShouldHaveConsumed<PaymentCompleted>();
    step2.ShouldHavePublished<OrderShipped>();
}
```

## API test with WebApplicationFactory

For testing HTTP endpoints that trigger messages, use `ConfigureTestServices` to add tracking to your existing bus configuration.

```csharp
public class OrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public OrderApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddMessageTracking();
            });
        });
    }

    [Fact]
    public async Task PostOrder_Should_PublishOrderCreated()
    {
        var client = _factory.CreateClient();
        var tracker = _factory.Services.GetRequiredService<IMessageTracker>();

        var response = await client.PostAsJsonAsync("/api/orders",
            new { OrderId = "123", Amount = 99.99m });
        response.EnsureSuccessStatusCode();

        var result = await tracker.WaitForCompletionAsync();
        var created = result.ShouldHavePublished<OrderCreated>();
        Assert.Equal("123", created.OrderId);
    }
}
```

`AddMessageTracking()` does not force a transport. Your production `AddRabbitMQ()` registration is preserved. The tracker works with any transport — it observes at the pipeline level, not the transport level.

## WaitForConsumed\<T\>

When you need to wait for a specific message type to be consumed rather than waiting for all work to complete:

```csharp
await bus.PublishAsync(new PlaceOrder { OrderId = "123" }, CancellationToken.None);

var confirmed = await tracker.WaitForConsumed<OrderConfirmed>();
Assert.Equal("123", confirmed.OrderId);
```

This is useful when your test only cares about one specific message in a chain, or when you want to inspect the message before all cascading work finishes.

## Stubbing external services

When a handler sends a message to an external service that is not running in your test, use `WhenSent<T>().RespondWith(...)` to stub the response.

```csharp
var tracker = provider.GetRequiredService<IMessageTracker>();

// Stub: when ProcessPayment is sent, respond with PaymentApproved
tracker.WhenSent<ProcessPayment>()
    .RespondWith(cmd => new PaymentApproved { OrderId = cmd.OrderId });

using var scope = provider.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
await bus.PublishAsync(new PlaceOrder { OrderId = "123" }, CancellationToken.None);

var result = await tracker.WaitForCompletionAsync();
result.ShouldHaveConsumed<PaymentApproved>();
```

Without the stub, a sent message with no consumer would cause `WaitForCompletionAsync()` to time out — the per-envelope state machine never reaches a terminal state. The stub acts as the consumer and marks the envelope as complete.

# Assertions

All assertion methods are extension methods on `ITrackedMessages`, so they work on both `IMessageTracker` (cumulative) and `MessageTrackingResult` (delta). Every assertion throws `MessageTrackingException` with diagnostic output on failure.

## ShouldHavePublished\<T\>

Asserts that a message of type `T` was published (via `PublishAsync`). Returns the message for further inspection.

```csharp
var order = result.ShouldHavePublished<OrderConfirmed>();
Assert.Equal("123", order.OrderId);
```

With a predicate:

```csharp
var order = result.ShouldHavePublished<OrderConfirmed>(m => m.OrderId == "123");
```

## ShouldHaveSent\<T\>

Asserts that a message of type `T` was sent (via `SendAsync`). Returns the message.

```csharp
var payment = result.ShouldHaveSent<ProcessPayment>();
Assert.Equal(49.99m, payment.Amount);
```

## ShouldHaveConsumed\<T\>

Asserts that a message of type `T` was consumed by a handler. Returns the message.

```csharp
var order = result.ShouldHaveConsumed<OrderCreated>();
Assert.Equal("ORD-1", order.OrderId);
```

With a predicate:

```csharp
var order = result.ShouldHaveConsumed<OrderCreated>(m => m.OrderId == "ORD-2");
```

## ShouldNotHaveDispatched\<T\>

Asserts that no message of type `T` was dispatched (published or sent).

```csharp
result.ShouldNotHaveDispatched<RefundIssued>();
```

## ShouldNotHaveConsumed\<T\>

Asserts that no message of type `T` was consumed.

```csharp
result.ShouldNotHaveConsumed<ErrorEvent>();
```

## ShouldHaveNoOtherMessages

Asserts that no messages were tracked at all. Useful for negative tests.

```csharp
result.ShouldHaveNoOtherMessages();
```

# Snapshot testing

`Mocha.Testing` integrates with CookieCrumble for snapshot testing. Snapshot the full message flow instead of writing individual assertions.

## Markdown snapshots

```csharp
[Fact]
public async Task PlaceOrder_Should_MatchSnapshot()
{
    // ... arrange and act ...

    await tracker.WaitForCompletionAsync();
    tracker.MatchMarkdownSnapshot();
}
```

The snapshot captures all dispatched, consumed, and failed messages in a deterministic format. On the first run, the snapshot file is created. On subsequent runs, the output is compared against the saved snapshot.

## Inline snapshots

For smaller tests, use inline snapshots to keep the expected output in the test file:

```csharp
await tracker.WaitForCompletionAsync();
tracker.MatchInlineSnapshot(@"expected snapshot content here");
```

Both methods work on `ITrackedMessages`, so you can snapshot either the cumulative tracker or a specific `MessageTrackingResult` delta.

# Diagnostic output

When a test fails, you get diagnostic output automatically. Every assertion failure includes a dump of what was actually tracked.

## Timeout diagnostics

When `WaitForCompletionAsync()` times out, the `MessageTrackingException` includes a timeline showing exactly where things got stuck:

```
Mocha message tracking timed out after 10.0s.

Timeline:
  0.0ms  Dispatched   PlaceOrder          (msg-001)
  0.2ms  Received     PlaceOrder          (msg-001)
  0.3ms  ConsumeStart PlaceOrder          (msg-001)
  1.1ms  Dispatched   ProcessPayment      (msg-002)
  1.2ms  ConsumeDone  PlaceOrder          (msg-001) [0.9ms]

Pending:
  ProcessPayment (msg-002) — Dispatched, no consumer or stub registered
```

## ToDiagnosticString

Call `tracker.ToDiagnosticString()` at any point to get a human-readable summary of all tracked activity. Useful for debugging:

```csharp
var diagnostic = tracker.ToDiagnosticString();
Console.WriteLine(diagnostic);
```

## Assertion failure diagnostics

When an assertion like `ShouldHavePublished<T>()` fails, the exception message includes the full list of dispatched, consumed, and failed messages so you can see what actually happened:

```
Expected a OrderConfirmed message to have been published, but none was found.

Dispatched:
  OrderCreated (msg-001)
Consumed:
  OrderCreated (msg-001)
Failed:
  (none)
```

# FakeTimeProvider

`AddMessageTracking()` also registers a `FakeTimeProvider` (from `Microsoft.Extensions.Time.Testing`) as the `TimeProvider` in the container. This lets you control time in tests that use time-dependent logic:

```csharp
var timeProvider = provider.GetRequiredService<FakeTimeProvider>();
timeProvider.Advance(TimeSpan.FromMinutes(5));
```

# Migration from MessageRecorder

If you have existing tests using `MessageRecorder` and `WaitAsync(timeout, expectedCount)`, here is how to migrate.

## Before

```csharp
var provider = await CreateBusAsync(builder =>
{
    builder.AddEventHandler<MyHandler>();
});

var recorder = provider.GetRequiredService<MessageRecorder>();
await bus.PublishAsync(new OrderCreated { OrderId = "1" }, ct);
await recorder.WaitAsync(TimeSpan.FromSeconds(5), expectedCount: 1);

Assert.Single(recorder.Messages.OfType<OrderCreated>());
```

## After

```csharp
var provider = new ServiceCollection()
    .AddMessageBus()
    .AddInMemory()
    .AddEventHandler<MyHandler>()
    .AddMessageTracking()
    .BuildServiceProvider(true);

var runtime = provider.GetRequiredService<IMessagingRuntime>();
await runtime.StartAsync();

var tracker = provider.GetRequiredService<IMessageTracker>();

using var scope = provider.CreateScope();
var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
await bus.PublishAsync(new OrderCreated { OrderId = "1" }, CancellationToken.None);

var result = await tracker.WaitForCompletionAsync();
result.ShouldHaveConsumed<OrderCreated>();
```

Key differences:

| MessageRecorder                            | IMessageTracker                                         |
| ------------------------------------------ | ------------------------------------------------------- |
| Must know expected message count upfront   | Detects completion automatically                        |
| Manual `Assert.Single` / `Assert.Contains` | Typed `ShouldHavePublished<T>()` returns `T`            |
| No failure diagnostics                     | Full diagnostic output on timeout and assertion failure |
| Single flat list of messages               | Separated into Dispatched, Consumed, Failed             |
| Single snapshot across all waits           | Delta results per `WaitForCompletionAsync()` call       |

Both patterns coexist. Existing `MessageRecorder` tests continue to work. New tests should use `IMessageTracker`.

# Design notes

**Transport-agnostic.** The tracker is backed by a diagnostic event listener that observes the messaging pipeline. It works with InMemory, RabbitMQ, or any transport. `AddMessageTracking()` does not force any specific transport.

**Completion detection.** Uses per-envelope state machines, not activity counters. Each message envelope is tracked through its lifecycle (dispatched, received, consume-started, consume-completed/failed). Completion is reached when every tracked envelope has reached a terminal state. This correctly handles cascading messages, fan-out to multiple subscribers, and handler failures.

**No test framework dependency.** All assertions throw `MessageTrackingException`, not `Xunit.Sdk.XunitException` or similar. The diagnostic output is baked into the exception message so it appears in any test runner's output.

**Parallel-safe.** No singleton state. Each test creates its own service provider with its own tracker instance.

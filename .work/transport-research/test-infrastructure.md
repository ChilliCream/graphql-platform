# Mocha MessageBus Test Infrastructure Study

## Overview
The Mocha test infrastructure is transport-agnostic. Tests focus on **behavior verification** rather than transport-specific implementation details. The pattern is: fixture → isolate → register handlers → build test bus → execute → assert.

---

## Fixture Patterns (Test Container Management)

### RabbitMQ Fixture (Squadron-based Docker)
**Location**: `Mocha.Transport.RabbitMQ.Tests/Helpers/RabbitMQFixture.cs`

- Uses **Squadron** library for Docker container lifecycle (`RabbitMQResource`)
- Creates isolated **virtual hosts** per test to prevent interference
- Virtual hosts are cleaned up after test completes
- Collection fixture pattern: `[Collection("RabbitMQ")]` + `ICollectionFixture<RabbitMQFixture>`
- VHost naming: `{TestName}_{FilePathHash}` for uniqueness
- Returns `VhostContext` with `IConnectionFactory` bound to isolated vhost

**Key methods**:
```csharp
public async Task<VhostContext> CreateVhostAsync()  // Per-test isolation
await _fixture.DeleteVhostAsync(vhostName)          // Cleanup
await _fixture.CloseAllConnectionsAsync()           // Force-close if needed
```

### Postgres Fixture (Database-based Isolation)
**Location**: `Mocha.Transport.Postgres.Tests/Helpers/PostgresFixture.cs`

- Uses Squadron's `PostgreSqlResource` for Docker lifecycle
- Creates isolated **database per test** (not schemas, full databases)
- Database naming: `mocha_{TestName}_{FilePathHash}` (lowercase, max 63 chars)
- Handles connection termination before dropping database
- Collection fixture pattern: `[Collection("Postgres")]` + `ICollectionFixture<PostgresFixture>`
- Returns `DatabaseContext` with connection string for isolated database

**Key methods**:
```csharp
public async Task<DatabaseContext> CreateDatabaseAsync()  // Per-test isolation
await fixture.DropDatabaseAsync(dbName)                   // Cleanup
```

### InMemory Fixture (No Container)
**Location**: `Mocha.Transport.InMemory.Tests/Helpers/InMemoryBusFixture.cs`

- No container/fixture needed—in-process transport
- Uses static helper methods: `CreateBusAsync()`, `CreateRuntimeAsync()`
- No collection fixture required
- Primarily used for topology/configuration unit tests

---

## Shared Test Helpers

### MessageRecorder
**Location**: `Mocha.TestHelpers/MessageRecorder.cs`

Core pattern for capturing async message delivery:
```csharp
public sealed class MessageRecorder
{
    public ConcurrentBag<object> Messages { get; }  // Thread-safe collection
    public void Record(object message)              // Called by handlers
    public async Task<bool> WaitAsync(
        TimeSpan timeout,
        int expectedCount = 1)                      // Blocks until N messages or timeout
}
```

Used in **all** handler tests to verify messages were delivered. Timeout pattern:
```csharp
var recorder = new MessageRecorder();
// ... register handler that calls recorder.Record() ...
// ... send message ...
Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
            "Handler did not receive the request");
```

### Test Message Types
**Location**: `Mocha.TestHelpers/TestMessages.cs`

Standard test message types used across all transport tests:
- `OrderCreated` — simple event
- `ProcessPayment` — request message (request-reply pattern)
- `GetOrderStatus` — event request (request-reply with response)
- `OrderStatusResponse` — response type
- `OrderCreatedHandler` — implements `IEventHandler<OrderCreated>`

Handlers are defined inline in each test file, not shared.

### BatchMessageRecorder
**Location**: `Mocha.TestHelpers/BatchMessageRecorder.cs`

For batching tests—records message batches instead of individual messages.

### Concurrency/Instrumentation Helpers
- `ConcurrencyTracker` — tracks concurrent execution
- `InvocationCounter` — counts invocations

---

## TestBus Pattern

### Standard Flow
All transports follow this pattern:

```csharp
[Collection("RabbitMQ")]  // or "Postgres", or nothing for InMemory
public class SendTests
{
    private readonly RabbitMQFixture _fixture;

    public SendTests(RabbitMQFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered()
    {
        // ARRANGE: Isolate
        var recorder = new MessageRecorder();
        await using var vhost = await _fixture.CreateVhostAsync();  // RabbitMQ
        // OR
        await using var db = await _fixture.CreateDatabaseAsync();   // Postgres
        // OR
        // (no isolation for InMemory)

        // ARRANGE: Register bus + handlers + transport
        await using var bus = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddRabbitMQ()  // or .AddPostgres() or .AddInMemory()
            .BuildTestBusAsync();

        // ACT: Get bus from scope
        using var scope = bus.Provider.CreateScope();
        var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        await messageBus.SendAsync(new ProcessPayment { ... }, CancellationToken.None);

        // ASSERT: Verify via MessageRecorder
        Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)),
                    "Handler did not receive the request");
        var msg = Assert.Single(recorder.Messages);
        Assert.IsType<ProcessPayment>(msg);
    }
}
```

### BuildTestBusAsync Extension
**Location**: `Mocha.Transport.RabbitMQ.Tests/Helpers/TestBus.cs` (similar for Postgres)

```csharp
public static async Task<TestBus> BuildTestBusAsync(this IMessageBusHostBuilder builder)
{
    var provider = builder.Services.BuildServiceProvider();
    var runtime = (MessagingRuntime)provider.GetRequiredService<IMessagingRuntime>();
    await runtime.StartAsync(CancellationToken.None);  // START transports
    return new TestBus(provider, runtime);
}

// TestBus cleanup
public async ValueTask DisposeAsync()
{
    foreach (var transport in runtime.Transports)
    {
        if (transport.IsStarted)
        {
            await transport.StopAsync(runtime, CancellationToken.None);  // STOP cleanly
        }
    }
    await provider.DisposeAsync();
}
```

**Key**: `BuildTestBusAsync()` **starts the runtime**, `using` ensures cleanup.

---

## Behavior Test Files Required for New Transport

All transports must implement these behavior test files (in `Behaviors/` folder):

### Core Messaging Patterns
- **SendTests** — request-reply (send → handler)
- **PublishSubscribeTests** — pub-sub (publish → multiple subscribers)
- **RequestReplyTests** — request with response
- **BatchingTests** — message batching
- **InboxTests** — inbox pattern for crash recovery (RabbitMQ, Postgres, InMemory have this)

### Reliability & Resilience
- **FaultHandlingTests** — error handling + dead-letter queues
- **ConcurrencyLimiterTests** — concurrency constraints
- **ConcurrencyTests** — concurrent message processing
- **ErrorQueueTests** — failed message routing
- **CustomHeaderTests** — custom headers propagation

### Configuration & Initialization
- **BusDefaultsIntegrationTests** — default behavior verification
- **AutoProvisionIntegrationTests** — automatic queue/exchange creation
- **ExplicitTopologyTests** — explicit queue/topic configuration (RabbitMQ, Postgres)
- **TransportMiddlewareTests** — middleware pipeline (sends, publishes, requests)
- **EndpointMiddlewareTests** — endpoint configuration middleware

### Transport-Specific Optional Tests
- **ConnectionRecoveryTests** — connection loss handling (RabbitMQ)
- **ReconnectionResilienceTests** — reconnection behavior (Postgres)
- **ConnectionHealthTests** — connection health monitoring (Postgres)
- **ConsumerLifecycleTests** — consumer startup/shutdown (Postgres)
- **MessageStoreTests** — persistent message store (Postgres only)
- **CorrelationTests** — correlation ID tracking (InMemory)
- **VolumeTests** — high-volume stress test (InMemory)

### Topology Tests (per transport)
Located in `Topology/` subfolder:
- **{Transport}MessagingTopologyTests** — topology building
- **{Transport}QueueTests** — queue topology elements
- **{Transport}TopicTests** — topic topology elements
- **{Transport}BindingTests** — exchange/queue bindings (RabbitMQ)
- **{Transport}SubscriptionTests** — topic subscriptions (Postgres)
- **{Transport}MessageTypeExtensionTests** — message type → queue/exchange mapping

---

## Key Patterns & Conventions

### Test Naming Convention
```
{Method}_Should_{Outcome}_When_{Condition}
```

Examples:
- `SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered`
- `SendAsync_Should_DeliverToCorrectHandler_When_MultipleQueuesExist`
- `PublishAsync_Should_DeliverToAllSubscribers_When_MultipleSubscribersRegistered`

### TimeOut Constants
```csharp
private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);  // RabbitMQ, Postgres
private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(10);  // InMemory
```

### Handler Definition (Test-Local)
Handlers are **defined inline** in the test class, not shared. Example:
```csharp
public sealed class ProcessPaymentHandler(MessageRecorder recorder)
    : IEventRequestHandler<ProcessPayment>
{
    public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
    {
        recorder.Record(request);
        return default;
    }
}
```

### Keyed Services for Multiple Handlers
Used when testing routing to different handlers for different message types:
```csharp
.AddKeyedSingleton("payment", paymentRecorder)
.AddKeyedSingleton("refund", refundRecorder)
.AddRequestHandler<ProcessPaymentKeyedHandler>()    // Uses [FromKeyedServices("payment")]
.AddRequestHandler<ProcessRefundKeyedHandler>()     // Uses [FromKeyedServices("refund")]
```

### Isolation Strategy
- **RabbitMQ**: Virtual hosts per test (complete separation)
- **Postgres**: Databases per test (complete separation)
- **InMemory**: Process-level (no isolation, must run sequentially if shared state)

---

## Changes Between Transports (Minimal Variation Points)

### Fixture Creation
```csharp
// RabbitMQ
await using var vhost = await _fixture.CreateVhostAsync();
await using var bus = await new ServiceCollection()
    .AddSingleton(vhost.ConnectionFactory)  // ← transport-specific dependency

// Postgres
await using var db = await _fixture.CreateDatabaseAsync();
await using var bus = await new ServiceCollection()
    .AddPostgres(t => t.ConnectionString(db.ConnectionString))  // ← transport-specific config

// InMemory
await using var bus = await new ServiceCollection()
    .AddInMemory()  // ← no fixture, no config needed
```

### Transport Registration
```csharp
.AddRabbitMQ()              // RabbitMQ
.AddPostgres(t => ...)      // Postgres with connection string
.AddInMemory()              // InMemory
```

### That's it!
The **rest of the test is identical** across transports. The MessageBus abstraction ensures behavior is the same.

---

## File Structure for New Transport (e.g., AzureEventHub)

```
src/Mocha/test/Mocha.Transport.AzureEventHub.Tests/
├── Helpers/
│   ├── AzureEventHubFixture.cs         (Squadron-based or Azure emulator)
│   ├── TestBus.cs                      (BuildTestBusAsync extension)
│   └── AzureEventHubBusFixture.cs      (Optional: topology helpers)
├── Behaviors/
│   ├── SendTests.cs
│   ├── PublishSubscribeTests.cs
│   ├── RequestReplyTests.cs
│   ├── BatchingTests.cs
│   ├── InboxTests.cs
│   ├── FaultHandlingTests.cs
│   ├── ConcurrencyLimiterTests.cs
│   ├── ConcurrencyTests.cs
│   ├── ErrorQueueTests.cs
│   ├── CustomHeaderTests.cs
│   ├── BusDefaultsIntegrationTests.cs
│   ├── AutoProvisionIntegrationTests.cs
│   ├── ExplicitTopologyTests.cs
│   ├── TransportMiddlewareTests.cs
│   ├── EndpointMiddlewareTests.cs
│   └── (optional: connection/resilience tests)
├── Topology/
│   ├── AzureEventHubMessagingTopologyTests.cs
│   ├── AzureEventHubPartitionTests.cs
│   ├── AzureEventHubSubscriptionTests.cs
│   └── AzureEventHubMessageTypeExtensionTests.cs
├── Connection/ (optional)
│   └── AzureEventHubConnectionManagerTests.cs
└── Mocha.Transport.AzureEventHub.Tests.csproj
```

---

## Summary: What Minimal Test Infrastructure Looks Like

To add a new transport, you need:

1. **One fixture** (RabbitMQFixture-like or PostgresFixture-like) that:
   - Manages container/service lifecycle (Squadron-based or direct Azure SDK)
   - Provides isolation (virtual hosts, databases, or namespaces)
   - Has a collection definition: `[CollectionDefinition("AzureEventHub")]`

2. **One TestBus extension** that:
   - Builds ServiceProvider from IMessageBusHostBuilder
   - Starts the MessagingRuntime
   - Returns TestBus for cleanup

3. **Copy all behavior test files** from another transport and change:
   - `using` namespace
   - Collection fixture name: `[Collection("AzureEventHub")]`
   - Transport registration: `.AddAzureEventHub(...)`
   - Fixture parameter injection
   - Connection setup (connection string, credentials, etc.)

4. **Copy topology test files** and adapt for Azure Event Hub naming/concepts:
   - Partitions instead of queues?
   - Consumer groups instead of exchanges?
   - Specific Azure semantics

The test **logic itself does not change**—only the setup/teardown and transport registration.

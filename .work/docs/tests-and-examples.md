# Mocha Azure Event Hub Transport: Tests and Usage Patterns

## Overview

The Mocha Azure Event Hub transport enables distributed messaging with Azure Event Hubs. This document explores unit tests, integration patterns, and the order fulfillment example to document configuration recipes and edge cases.

---

## Unit Tests

Located in `/workspaces/hc2/src/Mocha/test/Mocha.Transport.AzureEventHub.Tests/`

### Test Organization

The test suite is organized into three categories:

#### **Behaviors** (core messaging patterns)
- `SendTests.cs` — Send/Request-Reply patterns
- `PublishSubscribeTests.cs` — Publish/Subscribe patterns
- `RequestReplyTests.cs` — Request/Reply correlation
- `FaultHandlingTests.cs` — Exception handling and error propagation
- `ErrorQueueTests.cs` — Dead letter/error hub routing
- `ConsumerGroupIsolationTests.cs` — Multi-consumer group isolation
- `PartitionRoutingTests.cs` — Partition key routing
- `ConcurrencyTests.cs` — Concurrency and threading behavior
- `ConcurrencyLimiterTests.cs` — Rate limiting and backpressure
- `BatchDispatchTests.cs` — Batch sending modes
- `CustomHeaderTests.cs` — Message headers and metadata

#### **Topology**
- `EventHubTopologyTests.cs` — Hub name resolution and endpoint configuration
- `EventHubMessageEnvelopeParserTests.cs` — Message serialization/deserialization

#### **Connection**
- `BlobStorageCheckpointStoreTests.cs` — Checkpoint persistence with Azure Blob Storage

#### **Transport Lifecycle**
- `EventHubTransportTests.cs` — Transport configuration and endpoint creation
- `EventHubHealthCheckTests.cs` — Health checks and diagnostics

### Test Infrastructure

**EventHubFixture** (`Helpers/EventHubFixture.cs`):
- Uses testcontainers to spin up Azure Event Hubs emulator + Azurite (blob storage)
- Supports both local emulator (default) and production connection strings via `EVENTHUB_CONNECTION_STRING` env var
- Emulator connection string pattern:
  ```
  Endpoint=sb://<host>:<port>;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true
  ```
- Provides isolated hubs per test category (`send`, `pubsub`, `reqreply`, `batch`, `fault`, `concurrency`, `headers`, `partition`)
- Uses `$Default` consumer group for test isolation (emulator limitation)

**TestBus Helper** (`Helpers/TestBus.cs`):
- Extension: `.BuildTestBusAsync()` on DI service collection
- Automatically starts the message bus and waits for consumers
- Returns a scoped instance for message publishing

**MessageRecorder** (`Helpers/`):
- Records received messages in thread-safe collection
- `WaitAsync(timeout)` method for assertion synchronization
- Typical usage:
  ```csharp
  var recorder = new MessageRecorder();
  // ... publish message
  Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)));
  var msg = Assert.Single(recorder.Messages);
  ```

### Key Test Scenarios Covered

#### **Send/Request-Reply**
- Single request handler receives the request
- Multiple handlers with different message types route correctly
- Send timeouts and cancellation

#### **Publish/Subscribe**
- Single handler receives published event
- Multiple handlers all receive the same event (fanout)
- Sequential messages all delivered

#### **Routing & Addressing**
Three URI schemes supported:
```csharp
// Short-hand hub scheme
new Uri("hub://my-hub")

// Scheme-relative path (preferred for dynamic endpoints)
new Uri("eventhub:///h/my-hub")

// Full topology address
new Uri(topology.Address, "h/my-hub")
```

Special endpoints:
- `eventhub:///replies` — Request/reply hub (managed by transport)
- `eventhub:///error` — Dead-letter hub for failed messages
- `eventhub:///skipped` — Dead-letter hub for unhandled messages

#### **Fault Handling**
- Handler exceptions propagate as `RemoteErrorException` to requesters
- One throwing handler doesn't affect other handlers (isolation)
- Failed messages route to error hub with fault headers:
  - `fault-exception-type`
  - `fault-message`
  - `fault-stack-trace`
  - `fault-timestamp`

#### **Batch Modes**
Default batch mode: `EventHubBatchMode.Single`

Configuration:
```csharp
t.AddEventHub(cfg =>
    cfg.DefaultBatchMode = EventHubBatchMode.Batch
)

// Or per-endpoint
t.Endpoint("my-hub")
    .BatchMode(EventHubBatchMode.Batch)
```

#### **Configuration via Code**

Runtime configuration patterns tested:
```csharp
// Connection string
t.ConnectionString("Endpoint=sb://...")

// Custom connection provider
t.ConnectionProvider(provider => new CustomConnectionProvider())

// Endpoint with consumer group
t.Endpoint("my-hub")
    .ConsumerGroup("my-group")
    .Handler<MyHandler>()
    .FaultEndpoint("eventhub:///h/error")

// Kind classification
t.Endpoint("error-ep")
    .Kind(ReceiveEndpointKind.Error)
    .Consumer<ErrorConsumer>()
```

---

## Example Application: Order Fulfillment

Located in `/workspaces/hc2/src/Mocha/examples/AzureEventHubTransport/`

### Architecture Overview

Three services + Aspire orchestration:

```
┌─────────────────────────────────────────────────────────────────┐
│                      Aspire AppHost                             │
│  - Declares hub topology                                        │
│  - Sets up docker emulator                                      │
│  - Manages service startup ordering                             │
└─────────────────────────────────────────────────────────────────┘
          ↓
┌──────────────────┬─────────────────────────┬─────────────────────┐
│ Order Service    │ Shipping Service        │ Notification Service│
├──────────────────┼─────────────────────────┼─────────────────────┤
│ - Mediator       │ - Bus handlers          │ - Bus handlers      │
│   (local CQRS)   │ - Listens for ship cmds │ - Listens for all   │
│ - Bus            │ - Publishes shipped evt │   events            │
│ - Saga           │                         │                     │
│ - HTTP API       │                         │                     │
│ - Simulator      │                         │                     │
└──────────────────┴─────────────────────────┴─────────────────────┘
          ↓
┌─────────────────────────────────────────────────────────────────┐
│              Azure Event Hubs (emulator)                        │
│  - order-placed-hub      (published by OrderService)            │
│  - payment-processed-hub (published by OrderService)            │
│  - order-shipped-hub     (published by ShippingService)         │
│  - order-fulfilled-hub   (published by OrderService saga)       │
│  - process-payment-hub   (sent by saga)                         │
│  - ship-order-hub        (sent by saga)                         │
│  - replies-hub           (request/reply correlation)            │
│  - error-hub             (fault messages)                       │
│  - skipped-hub           (unhandled messages)                   │
└─────────────────────────────────────────────────────────────────┘
```

### Workflow: Order Fulfillment Saga

**Flow:**
1. HTTP POST `/api/orders` → Customer places order
2. OrderService.CreateOrderCommandHandler (Mediator) → Create order locally
3. OrderService publishes `OrderPlacedEvent`
4. OrderFulfillmentSaga (in-memory) receives event → Creates saga instance
5. Saga sends `ProcessPaymentCommand` to payment handler
6. OrderService.ProcessPaymentCommandHandler handles command → Publishes `PaymentProcessedEvent`
7. Saga correlates event by CorrelationId → Sends `ShipOrderCommand`
8. ShippingService.ShipOrderCommandHandler handles command → Publishes `OrderShippedEvent`
9. Saga correlates event → Publishes `OrderFulfilledEvent` and completes
10. NotificationService.OrderFulfilledNotificationHandler receives completion

**Key Pattern: Correlation**
Commands and events carry `CorrelationId` to associate saga state:
```csharp
new ProcessPaymentCommand 
{ 
    CorrelationId = state.Id  // state.Id = saga instance id
}
```

Saga correlates reply events by the same CorrelationId.

### Hub Declaration (Aspire AppHost)

File: `AzureEventHubTransport.AppHost/AppHost.cs`

```csharp
var eventHubs = builder
    .AddAzureEventHubs("eventhubs")
    .RunAsEmulator();

// Shared event hubs (for events published by multiple services)
var orderPlacedHub = eventHubs.AddHub("order-placed-hub",
    hubName: "azure-event-hub-transport.contracts.events.order-placed");
orderPlacedHub.AddConsumerGroup("order-placed-orders", groupName: "order-service");
orderPlacedHub.AddConsumerGroup("order-placed-notifications", groupName: "notification-service");

// Command hubs (for saga commands)
var processPaymentHub = eventHubs.AddHub("process-payment-hub",
    hubName: "azure-event-hub-transport.contracts.commands.process-payment");
processPaymentHub.AddConsumerGroup("process-payment-orders", groupName: "order-service");

// Reply hub (request/reply)
eventHubs.AddHub("replies");

// Error/skipped hubs
eventHubs.AddHub("error");
eventHubs.AddHub("skipped");

// Service references
builder
    .AddProject<Projects.AzureEventHubTransport_OrderService>("order-service")
    .WithReference(eventHubs)
    .WaitFor(eventHubs);
```

**Naming Convention:**
Hub names match the event type's namespace + class name:
- Event: `OrderPlacedEvent` in namespace `AzureEventHubTransport.Contracts.Events`
- Hub name: `azure-event-hub-transport.contracts.events.order-placed` (lowercased, hyphens for word boundaries)

This naming is automatic via Mocha's publish endpoint convention.

### Service: OrderService

File: `AzureEventHubTransport.OrderService/Program.cs`

```csharp
var eventHubConnectionString =
    builder.Configuration.GetConnectionString("eventhubs")
    ?? "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

// Mediator for local CQRS
builder
    .Services.AddMediator()
    .AddInstrumentation()
    .AddHandler<CreateOrderCommandHandler>()
    .AddHandler<GetOrderStatusQueryHandler>();

// Message Bus for distributed messaging
builder.Services.AddInMemorySagas();
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<ProcessPaymentCommandHandler>()
    .AddEventHandler<OrderFulfilledEventHandler>()
    .AddSaga<OrderFulfillmentSaga>()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString));

// Background worker
builder.Services.AddHostedService<OrderSimulatorWorker>();
```

**Key Points:**
- Mediator and Message Bus are separate DI registrations
- Both use `AddInstrumentation()` for tracing/metrics
- Sagas are registered with `.AddSaga<T>()`
- `.AddInMemorySagas()` stores saga state in-memory (dev/test only; use durable store in production)

#### HTTP API Endpoints

**Create Order:**
```csharp
app.MapPost("/api/orders", async (PlaceOrderRequest request, ISender sender, IMessageBus messageBus, ct) =>
{
    // 1. Create locally via mediator
    var result = await sender.SendAsync(
        new CreateOrderCommand { ProductName = ..., Quantity = ... }, ct);

    // 2. Publish event to bus (triggers saga)
    await messageBus.PublishAsync(
        new OrderPlacedEvent { OrderId = result.OrderId, ... }, ct);

    return Results.Created(...);
});
```

**Get Order Status:**
```csharp
app.MapGet("/api/orders/{orderId:guid}/status", async (Guid orderId, ISender sender) =>
{
    var status = await sender.QueryAsync(new GetOrderStatusQuery { OrderId = orderId });
    return Results.Ok(status);
});
```

#### Order Simulator Worker

File: `OrderSimulatorWorker.cs`

Simulates customer orders every 5 seconds:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    while (!stoppingToken.IsCancellationRequested)
    {
        // 1. Create order locally via mediator
        var result = await sender.SendAsync(new CreateOrderCommand { ... });

        // 2. Publish event (triggers saga)
        await messageBus.PublishAsync(new OrderPlacedEvent { ... });

        await Task.Delay(5000, stoppingToken);
    }
}
```

**Use Case:** Allows the example to run without manual HTTP calls; automatic event generation for demos.

### Service: ShippingService

File: `AzureEventHubTransport.ShippingService/Program.cs`

Minimal configuration:
```csharp
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<ShipOrderCommandHandler>()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString));
```

**Handler:**
```csharp
public sealed class ShipOrderCommandHandler : IEventHandler<ShipOrderCommand>
{
    public async ValueTask HandleAsync(ShipOrderCommand command, CancellationToken ct)
    {
        // Simulate shipping processing
        await Task.Delay(500, ct);

        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8]}";
        var carrier = Carriers[Random.Shared.Next(Carriers.Length)];

        // Publish event for saga correlation
        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = command.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                CorrelationId = command.CorrelationId  // Correlate back to saga
            },
            ct);
    }
}
```

### Service: NotificationService

File: `AzureEventHubTransport.NotificationService/Program.cs`

Subscribes to all domain events:
```csharp
builder
    .Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<PaymentProcessedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddEventHandler<OrderFulfilledNotificationHandler>()
    .AddEventHub(t => t.ConnectionString(eventHubConnectionString));
```

**Handler Example:**
```csharp
public sealed class OrderPlacedNotificationHandler : IEventHandler<OrderPlacedEvent>
{
    public ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken ct)
    {
        logger.LogInformation(
            "[EMAIL] Order confirmation sent to {CustomerEmail} — order {OrderId}",
            message.CustomerEmail, message.OrderId);

        return ValueTask.CompletedTask;
    }
}
```

**Pattern:** Event handlers are fire-and-forget; no return values expected.

### Contracts

Location: `AzureEventHubTransport.Contracts/`

#### Events (for Publish/Subscribe)

Example: `OrderPlacedEvent.cs`
```csharp
public sealed class OrderPlacedEvent
{
    public required Guid OrderId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string CustomerEmail { get; init; }
    public required DateTimeOffset PlacedAt { get; init; }
}
```

**Contract Ownership:**
- Events shared across services (in Contracts project)
- Services subscribe via `AddEventHandler<OrderPlacedEvent>()`
- Each service gets its own consumer group for independent read position

#### Commands (for Send/Request-Reply)

Example: `ProcessPaymentCommand.cs`
```csharp
public sealed class ProcessPaymentCommand : ICorrelatable
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerEmail { get; init; }
    public Guid? CorrelationId { get; init; }  // For saga correlation
}
```

**ICorrelatable:**
- Optional but recommended for saga workflows
- Allows saga to correlate request/response by CorrelationId

#### Saga Configuration

File: `OrderFulfillmentSaga.cs`

```csharp
public sealed class OrderFulfillmentSaga : Saga<OrderFulfillmentState>
{
    protected override void Configure(ISagaDescriptor<OrderFulfillmentState> saga)
    {
        // Initial state: receive OrderPlacedEvent
        saga.Initially()
            .OnEvent<OrderPlacedEvent>()
            .StateFactory(evt => new OrderFulfillmentState { OrderId = evt.OrderId, ... })
            .Send<ProcessPaymentCommand>(/* create command */)
            .TransitionTo(AwaitingPayment);

        // Middle state: waiting for payment
        saga.During(AwaitingPayment)
            .OnEvent<PaymentProcessedEvent>()
            .Send<ShipOrderCommand>(/* create command */)
            .TransitionTo(AwaitingShipment);

        // Final state: waiting for shipment
        saga.During(AwaitingShipment)
            .OnEvent<OrderShippedEvent>()
            .Publish<OrderFulfilledEvent>(/* create event */)
            .TransitionTo(Completed);

        saga.Finally(Completed);
    }
}
```

**Saga Lifecycle:**
- `.Initially()` — state created when first event arrives
- `.StateFactory(evt => ...)` — instantiate saga state from event data
- `.During(state)` — configuration for a specific state
- `.OnEvent<TEvent>()` — declare which event triggers this transition
- `.Then()` — lambda to mutate saga state
- `.Send<TCommand>()` — dispatch a command to a handler
- `.Publish<TEvent>()` — publish an event
- `.TransitionTo(state)` — move saga to next state
- `.Finally(state)` — final state (saga completes)

---

## Configuration Patterns

### Connection String vs. DefaultAzureCredential

**Development (Local Emulator):**
```csharp
var connectionString = 
    "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true";

builder.Services.AddMessageBus()
    .AddEventHub(t => t.ConnectionString(connectionString));
```

**Production (with Managed Identity):**
```csharp
var credentials = new DefaultAzureCredential();
var namespace = "my-eventhub-namespace";

builder.Services.AddMessageBus()
    .AddEventHub(t => t.Credentials(credentials, namespace));
```

**Configuration via appsettings.json:**
```csharp
var connectionString = 
    builder.Configuration.GetConnectionString("eventhubs")
    ?? "Endpoint=sb://localhost;...;UseDevelopmentEmulator=true";

builder.Services.AddMessageBus()
    .AddEventHub(t => t.ConnectionString(connectionString));
```

### Handler Registration Patterns

**Event Handlers (Pub/Sub):**
```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedHandler>()
    .AddEventHandler<OrderFulfilledHandler>();
```

**Request Handlers (Send):**
```csharp
builder.Services
    .AddMessageBus()
    .AddRequestHandler<ProcessPaymentHandler>();
```

**Consumers (Direct consumption, no handler interface):**
```csharp
builder.Services
    .AddMessageBus()
    .AddConsumer<ErrorMessageConsumer>();
```

**Sagas (Stateful orchestration):**
```csharp
builder.Services
    .AddInMemorySagas()  // Storage backend
    .AddMessageBus()
    .AddSaga<OrderFulfillmentSaga>();
```

### Endpoint Configuration

```csharp
builder.Services.AddMessageBus()
    .AddEventHub(t =>
    {
        // Global connection
        t.ConnectionString("...");

        // Default batch mode for all endpoints
        t.DefaultBatchMode = EventHubBatchMode.Batch;

        // Per-endpoint configuration
        t.Endpoint("order-endpoint")
            .Hub("my-hub")
            .ConsumerGroup("my-group")
            .Handler<OrderHandler>()
            .FaultEndpoint("eventhub:///h/error")
            .Kind(ReceiveEndpointKind.Default);

        // Error endpoint
        t.Endpoint("error-handler")
            .Hub("error")
            .ConsumerGroup("error-group")
            .Consumer<ErrorConsumer>()
            .Kind(ReceiveEndpointKind.Error);
    });
```

---

## Edge Cases & Behaviors

### Consumer Group Isolation

Multiple services can subscribe to the same hub via **different consumer groups**:

```
[AppHost]
orderPlacedHub.AddConsumerGroup("order-service-group", groupName: "order-service");
orderPlacedHub.AddConsumerGroup("notification-group", groupName: "notification-service");

[OrderService]
AddEventHandler<OrderPlacedHandler>()  // Uses "order-service-group"

[NotificationService]
AddEventHandler<OrderPlacedHandler>()  // Uses "notification-service-group"
```

Each consumer group maintains independent read position → both services can reprocess events independently.

### Partition Routing

Commands can be routed to specific partitions:
```csharp
await messageBus.SendAsync(
    message: command,
    partitionKey: $"customer-{customerId}"  // All customer orders → same partition
);
```

Ensures order-preserving processing for a given customer.

### Batch vs. Single Dispatch

**Single Mode (default):**
- Each message sent immediately
- Lower latency
- Higher API calls

**Batch Mode:**
- Accumulate messages, send together
- Higher throughput
- Configuration:
  ```csharp
  t.DefaultBatchMode = EventHubBatchMode.Batch;
  ```

### Error Handling & Dead Letters

Failed messages automatically route to error hub:
1. Handler throws exception
2. Message + exception details → error hub (via `FaultEndpoint`)
3. Error headers captured:
   - `fault-exception-type` — exception class name
   - `fault-message` — exception message
   - `fault-stack-trace` — stack trace
   - `fault-timestamp` — when fault occurred
4. Error consumer can retry, log, alert, etc.

### Request/Reply Correlation

Automatic via **reply hub** (default name: "Replies"):
```
[Client]
await messageBus.RequestAsync(command)  // Waits for reply
         ↓
[Handler receives request]
         ↓
[Handler processes, publishes reply]
         ↓
[Client receives reply via replies hub]
```

Mocha matches requests/replies by correlation ID. If handler throws, client receives `RemoteErrorException`.

---

## Testing Checklist

When adding new handlers or events:

1. **Unit Test Pattern:**
   ```csharp
   var recorder = new MessageRecorder();
   var bus = await new ServiceCollection()
       .AddSingleton(recorder)
       .AddMessageBus()
       .AddEventHandler<MyHandler>()
       .AddEventHub(t => t
           .ConnectionString(_fixture.ConnectionString)
           .Endpoint("test-hub").ConsumerGroup("test-group"))
       .BuildTestBusAsync();

   await bus.PublishAsync(new MyEvent { ... });
   Assert.True(await recorder.WaitAsync(TimeSpan.FromSeconds(30)));
   ```

2. **Fixture Setup:**
   - Use `EventHubFixture` for testcontainers
   - Decorate test class with `[Collection("EventHub")]`
   - Request fixture in constructor

3. **Assertions:**
   - Avoid vacuous assertions (e.g., `Assert.NotNull` alone)
   - Assert message content, not just receipt
   - Use `Assert.Single()`, `Assert.Multiple()` for clarity

4. **Edge Cases:**
   - Multiple handlers for same event
   - Handler exceptions
   - Concurrent message processing
   - Timeout behavior

---

## Key Configuration Recipes

### Development Setup (Local Emulator)
```csharp
// appsettings.Development.json
{
  "ConnectionStrings": {
    "eventhubs": "Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true"
  }
}

// Program.cs
var conn = builder.Configuration.GetConnectionString("eventhubs")
    ?? throw new InvalidOperationException("Missing eventhubs connection");

builder.Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<MyHandler>()
    .AddEventHub(t => t.ConnectionString(conn));
```

### Production Setup (Managed Identity)
```csharp
// Uses Azure.Identity.DefaultAzureCredential
// Supports: Environment variables, Managed Identity, CLI login, etc.

var credentials = new DefaultAzureCredential();
var fullyQualifiedNamespace = "myapp.servicebus.windows.net";

builder.Services.AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<MyHandler>()
    .AddEventHub(t => t.Credentials(credentials, fullyQualifiedNamespace));
```

### Saga Orchestration with In-Memory Storage (Dev/Test)
```csharp
builder.Services.AddInMemorySagas();
builder.Services.AddMessageBus()
    .AddEventHandler<PaymentHandler>()
    .AddEventHandler<ShippingHandler>()
    .AddSaga<OrderFulfillmentSaga>()
    .AddEventHub(t => t.ConnectionString(connString));
```

### Health Check Integration
```csharp
builder.Services
    .AddHealthChecks()
    .AddEventHub("eventhub-health");
```

---

## Summary

The Mocha Azure Event Hub transport provides:

1. **Pub/Sub** — Events broadcast to multiple handlers via consumer groups
2. **Send/Reply** — Request-reply with automatic correlation
3. **Sagas** — Stateful orchestration across multiple services
4. **Error Handling** — Automatic fault routing with detailed headers
5. **Batch Control** — Single vs. batch message dispatch
6. **Consumer Groups** — Independent read positions for competing consumers
7. **Partition Routing** — Order preservation per partition key

Common patterns:
- **Event handlers** for async event processing
- **Request handlers** for synchronous request-reply
- **Sagas** for multi-step workflows
- **Error consumers** for dead-letter handling

Connection strategies:
- Local emulator (development)
- DefaultAzureCredential (production with Managed Identity)
- Connection string (any environment)

The example demonstrates a complete order fulfillment workflow with mediator (local CQRS), message bus (distributed events), saga orchestration, and event notifications—a real-world messaging architecture pattern.

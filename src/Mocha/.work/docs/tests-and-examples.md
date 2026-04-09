# Mocha Kafka Transport Tests and Examples - Comprehensive Documentation

## Overview

This document provides a thorough analysis of the Mocha Kafka transport tests (in `/workspaces/hc3/src/Mocha/test/Mocha.Transport.Kafka.Tests/`) and the example application (in `/workspaces/hc3/src/Mocha/examples/KafkaTransport/`).

---

## Part 1: Test Infrastructure and Fixtures

### 1.1 Test Fixture Setup

#### KafkaFixture (Primary Infrastructure)
Location: `Mocha.Transport.Kafka.Tests/Helpers/KafkaFixture.cs`

**Purpose**: Manages a containerized Kafka instance for all tests using Testcontainers.

**Key Components**:
- **KafkaContainer**: Uses `confluentinc/cp-kafka:7.6.0` Docker image
- **Lifecycle**: Implements `IAsyncLifetime` for proper initialization/disposal in xUnit
- **Bootstrap Servers**: Retrieved from the running container at `InitializeAsync()`
- **Test Context Generation**: Creates isolated topic prefixes per test using SHA256 hashing of test name and file path

**KafkaTestContext**:
- Provides isolated topic naming: `{baseName}-{testName}_{filehash}`
- Prevents topic name collisions across concurrent tests
- Accessed via `CreateTestContext()` in each test

**Collection Management**:
- Marked with `[CollectionDefinition("Kafka")]` for test isolation
- Tests using `[Collection("Kafka")]` share a single KafkaFixture instance

#### KafkaBusFixture (Builder/Runtime Helper)
Location: `Mocha.Transport.Kafka.Tests/Helpers/KafkaBusFixture.cs`

**Purpose**: Simplifies creation of messaging runtime and topology for unit tests.

**Static Methods**:
- `CreateTopology()`: Returns tuple of (bus config, transport, topology) without running the bus
- `CreateRuntime()`: Returns a configured `MessagingRuntime` with handlers
- `CreateRuntimeWithHandlers()`: Creates runtime with custom handler registration

#### TestBus Extension
Location: `Mocha.Transport.Kafka.Tests/Helpers/TestBus.cs`

**Purpose**: Provides `BuildTestBusAsync()` extension method for integration tests.

**Features**:
- Creates a fully initialized message bus instance
- Starts consuming and processing messages immediately
- Returns an `AsyncDisposableTestBus` for cleanup
- Allows integration tests to run real Kafka-to-handler flows

### 1.2 Test Recorder Utilities

**MessageRecorder**: Thread-safe collection tracking messages received by handlers
- `Record(message)`: Adds a message to the collection
- `WaitAsync(timeout, expectedCount)`: Blocks until specified message count received or timeout
- Used in all behavior tests to verify message delivery

**BatchMessageRecorder**: Extends MessageRecorder for batch handler testing
- Tracks `IMessageBatch<T>` objects instead of individual messages
- Verifies batch completion mode (Size vs. Time)

**ConcurrencyTracker**: Tracks concurrent handler execution
- `Enter()` / `Exit()`: Increment/decrement active handler count
- `PeakConcurrency`: Maximum concurrent executions observed
- Used to verify MaxConcurrency constraints

**ErrorCapture**: Captures faulted messages and headers
- `Record(IConsumeContext)`: Records message with full header map
- `RecordHeaders(IConsumeContext)`: Records headers only
- `CapturedHeaders`: Stores fault metadata (exception type, message, stack trace, timestamp)

---

## Part 2: Core Messaging Patterns Tests

### 2.1 Publish/Subscribe Pattern Tests

**File**: `Behaviors/PublishSubscribeTests.cs`

**Test Scenarios**:

1. **Single Handler Delivery** (`PublishAsync_Should_DeliverToHandler_When_SingleHandlerRegistered`)
   - Publishes `OrderCreated` event
   - Verifies single handler receives it
   - Validates message content (OrderId)

2. **Fan-Out to Multiple Handlers** (`PublishAsync_Should_FanOutToAllHandlers_When_MultipleHandlersRegistered`)
   - Registers two handlers for same event type
   - Uses `[FromKeyedServices]` for handler differentiation
   - Verifies both handlers receive the event independently
   - Demonstrates parallel handler execution

3. **Sequential Multiple Events** (`PublishAsync_Should_DeliverAll_When_MultipleEventsSequential`)
   - Publishes 3 events sequentially
   - Waits for all 3 to be received
   - Verifies message ordering and content

4. **Rapid-Fire Event Storm** (`PublishAsync_Should_DeliverAll_When_RapidFire`)
   - Publishes 50 events in tight loop
   - Verifies all messages delivered without loss
   - Confirms uniqueness (no duplicates)
   - Validates message ordering preserved

**Key Test Patterns**:
- Uses `IEventHandler<T>` interface for event subscribers
- Demonstrates handler injection via DI container
- Shows async message processing with `ValueTask`
- Validates end-to-end delivery guarantees

---

### 2.2 Request-Reply Pattern Tests

**File**: `Behaviors/RequestReplyTests.cs`

**Test Scenarios**:

1. **Typed Response** (`RequestAsync_Should_ReturnTypedResponse_When_HandlerRegistered`)
   - Sends `GetOrderStatus` request
   - Receives `OrderStatusResponse` with status data
   - Verifies response correlation and data accuracy

2. **Concurrent Requests** (`RequestAsync_Should_CorrelateResponses_When_ConcurrentRequests`)
   - Makes 10 concurrent request-reply calls with different OrderIds
   - Each receives correct response for its request
   - Tests correlation IDs and reply topic routing

3. **Void Request** (`RequestAsync_Should_Complete_When_VoidRequestAcknowledged`)
   - Sends `ProcessPayment` request with void response (`IEventRequestHandler<T>`)
   - RequestAsync completes after handler acknowledges
   - Validates command pattern over RPC

4. **Multiple Request Types** (`RequestAsync_Should_ReturnCorrectResponse_When_MultipleRequestTypesRegistered`)
   - Registers handlers for `GetOrderStatus` and `GetShipmentStatus`
   - Each request routes to correct handler
   - Verifies response type-safety and routing logic

**Key Interfaces Used**:
- `IEventRequest<TResponse>`: Marker for request messages
- `IEventRequestHandler<TRequest, TResponse>`: Typed request handler
- `IEventRequestHandler<TRequest>`: Void response request handler

---

### 2.3 Send Pattern Tests

**File**: `Behaviors/SendTests.cs`

**Test Scenarios**:

1. **Handler Reception** (`SendAsync_Should_DeliverToHandler_When_RequestHandlerRegistered`)
   - Sends `ProcessPayment` command to handler
   - Verifies handler receives it (fire-and-forget, no response)
   - Message is delivered once to configured endpoint

2. **Multiple Topic Routing** (`SendAsync_Should_DeliverToCorrectHandler_When_MultipleTopicsExist`)
   - Two handlers for different message types (ProcessPayment, ProcessRefund)
   - Send message to ProcessPayment handler
   - Verifies other handler does NOT receive it (exclusive routing)
   - Confirms send uses topic-based routing, not broadcast

3. **Different Topics** (`SendAsync_Should_DeliverToEachHandler_When_SendingToDifferentTopics`)
   - Sends ProcessPayment to payment topic
   - Sends ProcessRefund to refund topic
   - Both handlers receive their respective messages
   - Validates multi-topic deployment patterns

**Key Differences from Publish**:
- Send is point-to-point (single handler per message)
- No fan-out; message goes to exactly one endpoint
- Requires explicit topic configuration

---

### 2.4 Batching Pattern Tests

**File**: `Behaviors/BatchingTests.cs`

**Test Scenarios**:

1. **Size-Triggered Batch** (`Handler_Should_ReceiveBatch_When_SingleMessageSizeTrigger`)
   - Configuration: `MaxBatchSize = 1`
   - One message immediately triggers batch delivery
   - Handler receives `IMessageBatch<OrderCreated>`
   - `batch.CompletionMode == BatchCompletionMode.Size`
   - Useful for testing batch processing logic

2. **Timeout-Triggered Batch** (`Handler_Should_ReceiveBatch_When_TimeoutExpires`)
   - Configuration: `MaxBatchSize = 100`, `BatchTimeout = 200ms`
   - One message waits for timeout (since batch won't fill)
   - Batch delivered via timer, not size trigger
   - `batch.CompletionMode == BatchCompletionMode.Time`
   - Demonstrates accumulation with timeout fallback

**Batch Handler Interface**:
```csharp
public interface IBatchEventHandler<in TMessage>
{
    ValueTask HandleAsync(IMessageBatch<TMessage> batch, CancellationToken cancellationToken);
}
```

**IMessageBatch<T>**:
- Implements `IEnumerable<T>` for LINQ iteration
- Properties: `Count`, `CompletionMode`
- Useful for analytics, bulk inserts, or deduplication

---

### 2.5 Fault Handling Tests

**File**: `Behaviors/FaultHandlingTests.cs`

**Test Scenarios**:

1. **Request Handler Throws** (`RequestAsync_Should_ThrowRemoteError_When_HandlerThrows`)
   - Handler throws `InvalidOperationException`
   - Caller receives `RemoteErrorException` wrapping original exception
   - Exception type name preserved in message
   - Demonstrates error propagation in RPC

2. **Publish One Handler Fails** (`PublishAsync_Should_NotAffectOtherHandlers_When_OneHandlerThrows`)
   - Register two event handlers for same event
   - One handler throws, other handles successfully
   - Second handler still receives the event despite first handler's failure
   - Validates isolation: one handler's error doesn't break others

**Error Handling Patterns**:
- Handlers are invoked independently
- Exception doesn't stop message delivery to other handlers
- Exceptions are logged but don't block processing
- In request-reply: exception converted to RemoteErrorException for caller

---

### 2.6 Error Queue / Fault Endpoint Tests

**File**: `Behaviors/ErrorQueueTests.cs`

**Test Scenarios**:

1. **Publish to Error Topic** (`PublishAsync_Should_RouteToErrorTopic_When_HandlerThrows`)
   - Configure fault endpoint: `.FaultEndpoint($"kafka:///t/{errorTopic}")`
   - Handler throws exception
   - Message routed to error topic with fault metadata
   - Headers contain: `fault-exception-type`, `fault-message`, `fault-stack-trace`, `fault-timestamp`
   - Error consumer receives the faulted message

2. **Send to Error Topic** (`SendAsync_Should_RouteToErrorTopic_When_HandlerThrows`)
   - Same pattern for Send messages
   - Message preserved, headers enriched with fault info

3. **Original Body Preservation** (`ErrorTopic_Should_PreserveOriginalBody_When_HandlerFaults`)
   - Error topic consumer can deserialize original message
   - No payload loss during fault routing
   - Allows dead-letter queue processing

**Configuration**:
```csharp
t.Endpoint("handler-ep")
    .Handler<ThrowingOrderHandler>()
    .FaultEndpoint($"kafka:///t/{errorTopic}");

t.Endpoint("error-ep")
    .Topic(errorTopic)
    .Kind(ReceiveEndpointKind.Error)
    .Consumer<ErrorSpyConsumer>();
```

---

### 2.7 Inbox (Idempotency) Tests

**File**: `Behaviors/InboxTests.cs`

**Test Scenarios**:

1. **Duplicate Deduplication** (`Inbox_Should_DeduplicateMessage_When_SameMessageIdPublishedTwice`)
   - Same logical message published twice (forced same MessageId)
   - Only first message processed by handler
   - Second message dropped as duplicate
   - Validates inbox prevents duplicate processing

2. **Different Message IDs** (`Inbox_Should_ProcessBothMessages_When_DifferentMessageIds`)
   - Two distinct messages (auto-generated MessageIds)
   - Both processed by handler
   - Demonstrates that different messages are not deduplicated

3. **Skip Inbox Flag** (`Inbox_Should_ProcessMessage_When_SkipInboxIsSet`)
   - Middleware sets `InboxMiddlewareFeature.SkipInbox = true`
   - Same MessageId published twice
   - Both messages processed (inbox bypass)
   - Useful for non-idempotent scenarios or trusted sources

4. **Null MessageId** (`Inbox_Should_ProcessMessage_When_MessageIdIsNull`)
   - Messages with `null` MessageId bypass deduplication
   - Both messages processed
   - Inbox records nothing (null MessageId not tracked)

**Integration Points**:
- Requires `IMessageInbox` service registration
- Middleware: `UseInboxCore()`
- Feature: `InboxMiddlewareFeature`
- Headers: MessageId propagated via envelope

**In-Memory Inbox Implementation**:
- `ExistsAsync()`: Checks if message already processed
- `TryClaimAsync()`: Claims message for processing; returns false if duplicate
- `RecordAsync()`: Records message in inbox after processing
- `CleanupAsync()`: Removes old entries based on age

---

### 2.8 Concurrency Control Tests

**File**: `Behaviors/ConcurrencyTests.cs`

**Test Scenarios**:

1. **MaxConcurrency = 1** (`Handler_Should_LimitConcurrency_When_MaxConcurrencySetToOne`)
   - Configuration: `.MaxConcurrency(1)`
   - 20 messages published
   - Handler tracks concurrent execution: `PeakConcurrency == 1`
   - Messages processed sequentially

2. **MaxConcurrency > 1** (`Handler_Should_AllowParallelism_When_MaxConcurrencyGreaterThanOne`)
   - Configuration: `.MaxConcurrency(5)`
   - 20 messages published
   - Handler tracks concurrent execution: `1 < PeakConcurrency <= 5`
   - Messages processed in parallel, respecting limit

**Concurrency Tracking**:
```csharp
public sealed class SlowOrderHandler(ConcurrencyTracker tracker) : IEventHandler<OrderCreated>
{
    public async ValueTask HandleAsync(OrderCreated message, CancellationToken cancellationToken)
    {
        tracker.Enter();
        try
        {
            await Task.Delay(500, cancellationToken);  // Simulate slow processing
        }
        finally
        {
            tracker.Exit();
        }
    }
}
```

**Endpoint Configuration**:
```csharp
t.Endpoint("slow-ep")
    .Handler<SlowOrderHandler>()
    .MaxConcurrency(5);  // Limits parallel handlers to 5
```

---

### 2.9 Auto-Provisioning Tests

**File**: `Behaviors/AutoProvisionIntegrationTests.cs`

**Test Scenarios**:

1. **Default Auto-Provision** (`PublishAsync_Should_Deliver_When_AutoProvisionEnabledByDefault`)
   - No explicit configuration (auto-provision defaults to `true`)
   - Handler registered without explicit topic declaration
   - Topics created automatically
   - Event delivered successfully

2. **Explicit Auto-Provision True** (`PublishAsync_Should_Deliver_When_AutoProvisionExplicitlyEnabled`)
   - Explicit: `.AutoProvision(true)`
   - Topics created on-demand
   - Event delivered successfully

**Default Behavior**:
```csharp
// Default - topics auto-created
.AddKafka(t => t.BootstrapServers(ctx.BootstrapServers))

// Explicit - same effect
.AddKafka(t =>
{
    t.BootstrapServers(ctx.BootstrapServers);
    t.AutoProvision(true);
})
```

---

### 2.10 Topology and Bus Configuration Tests

**Files**: 
- `Topology/KafkaTopologyDescriptorTests.cs`
- `Topology/KafkaMessagingTopologyTests.cs`
- `Topology/KafkaBusDefaultsTests.cs`

**Test Coverage**:

1. **Topic Management**
   - `AddTopic()`: Adds topic to topology
   - Duplicate topic detection (throws InvalidOperationException)
   - Topic enumeration via `Topics` collection

2. **Bus Defaults**
   - Default partition count (defaults to 1)
   - Default replication factor (defaults to 1)
   - Override defaults: `.ConfigureDefaults(d => d.Topic.Partitions = 6)`

3. **Topology Validation**
   - Transport schema validation (must be "kafka")
   - Topology address scheme verification
   - Transport type verification (KafkaMessagingTransport)

---

### 2.11 Message Envelope and Serialization Tests

**File**: `KafkaMessageEnvelopeParserTests.cs`

**Tests**:
- Message deserialization from Kafka records
- Header extraction and mapping
- Correlation ID propagation
- Custom header parsing

---

### 2.12 Middleware and Behavior Integration Tests

**Files**:
- `Behaviors/TransportMiddlewareTests.cs`
- `Behaviors/EndpointMiddlewareTests.cs`
- `Behaviors/CustomHeaderTests.cs`
- `Behaviors/BusDefaultsIntegrationTests.cs`
- `Behaviors/ConcurrencyLimiterTests.cs`

**Coverage Areas**:
- Custom header propagation
- Transport-level middleware
- Endpoint-level configuration
- Concurrency limiting at transport level

---

## Part 3: Example Application Architecture

### 3.1 High-Level Overview

**Location**: `/workspaces/hc3/src/Mocha/examples/KafkaTransport/`

**Services**:
1. **KafkaTransport.OrderService** - Central order processor + saga coordinator
2. **KafkaTransport.ShippingService** - Shipment handler
3. **KafkaTransport.NotificationService** - Event notifications
4. **KafkaTransport.Contracts** - Shared message types
5. **KafkaTransport.AppHost** - Aspire orchestration
6. **KafkaTransport.ServiceDefaults** - Service configuration

**Messaging Topology**:
```
OrderService
  ├─ Publishes: OrderPlacedEvent (choreography trigger)
  ├─ Handles: OrderShippedEvent (shipment confirmation)
  ├─ Handles: GetOrderStatusRequest (request-reply)
  ├─ Handles: ProcessOrderCommand (send pattern)
  └─ Saga: OrderFulfillmentSaga (state coordination)

ShippingService
  ├─ Subscribes: OrderPlacedEvent
  └─ Publishes: OrderShippedEvent

NotificationService
  ├─ Subscribes: OrderPlacedEvent
  ├─ Subscribes: OrderShippedEvent
  └─ Subscribes: OrderFulfilledEvent
```

---

### 3.2 Message Types (Contracts)

**File**: `KafkaTransport.Contracts/`

#### Events

**OrderPlacedEvent** (Domain event)
```csharp
public sealed class OrderPlacedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }
    public required string ProductName { get; init; }
    public required int Quantity { get; init; }
    public required decimal TotalAmount { get; init; }
    public required string CustomerEmail { get; init; }
    public required DateTimeOffset PlacedAt { get; init; }
    
    Guid? ICorrelatable.CorrelationId => OrderId;  // Saga correlation
}
```
- **Pattern**: Choreography trigger
- **Handlers**: ShippingService (OrderPlacedEventHandler), NotificationService, Saga (OrderFulfillmentSaga)
- **Purpose**: Broadcasts new order to all interested services

**OrderShippedEvent** (Domain event)
```csharp
public sealed class OrderShippedEvent : ICorrelatable
{
    public required Guid OrderId { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required DateTimeOffset ShippedAt { get; init; }
    
    Guid? ICorrelatable.CorrelationId => OrderId;
}
```
- **Pattern**: Choreography confirmation
- **Handlers**: OrderService (OrderShippedEventHandler), NotificationService, Saga (OrderFulfillmentSaga)
- **Purpose**: Confirms shipment; triggers saga state transition

**OrderFulfilledEvent** (Aggregate event)
```csharp
public sealed class OrderFulfilledEvent
{
    public required Guid OrderId { get; init; }
    public required string ProductName { get; init; }
    public required string TrackingNumber { get; init; }
    public required string Carrier { get; init; }
    public required DateTimeOffset PlacedAt { get; init; }
    public required DateTimeOffset ShippedAt { get; init; }
    public required DateTimeOffset FulfilledAt { get; init; }
}
```
- **Pattern**: Saga completion summary
- **Handlers**: NotificationService (final confirmation)
- **Purpose**: Published when saga completes Fulfilled state

#### Commands

**ProcessOrderCommand** (Async command)
```csharp
public sealed class ProcessOrderCommand
{
    public required Guid OrderId { get; init; }
    public required string Action { get; init; }
    public required DateTimeOffset RequestedAt { get; init; }
}
```
- **Pattern**: Send pattern (fire-and-forget)
- **Handler**: ProcessOrderCommandHandler in OrderService
- **Purpose**: Demonstrates send pattern; log command execution

#### Requests (Request-Reply)

**GetOrderStatusRequest**
```csharp
public sealed class GetOrderStatusRequest : IEventRequest<GetOrderStatusResponse>
{
    public required Guid OrderId { get; init; }
}
```

**GetOrderStatusResponse**
```csharp
public sealed class GetOrderStatusResponse
{
    public required Guid OrderId { get; init; }
    public required string Status { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
```
- **Pattern**: Synchronous request-reply
- **Handler**: GetOrderStatusRequestHandler in OrderService
- **Purpose**: Query current order status from any service

---

### 3.3 Saga: OrderFulfillmentSaga

**File**: `KafkaTransport.OrderService/Sagas/OrderFulfillmentSaga.cs`

**State Class**: `OrderFulfillmentState`
```csharp
public sealed class OrderFulfillmentState : SagaStateBase
{
    public Guid OrderId { get; set; }
    public string ProductName { get; set; }
    public int Quantity { get; set; }
    public decimal TotalAmount { get; set; }
    public string CustomerEmail { get; set; }
    public DateTimeOffset PlacedAt { get; set; }
    
    public string? TrackingNumber { get; set; }      // Set by OrderShipped
    public string? Carrier { get; set; }            // Set by OrderShipped
    public DateTimeOffset? ShippedAt { get; set; }  // Set by OrderShipped
}
```

**Saga Configuration**:
```csharp
public sealed class OrderFulfillmentSaga : Saga<OrderFulfillmentState>
{
    protected override void Configure(ISagaDescriptor<OrderFulfillmentState> descriptor)
    {
        // 1. Initially state
        //    When OrderPlacedEvent arrives, create new state
        descriptor
            .Initially()
            .OnEvent<OrderPlacedEvent>()
            .StateFactory(OrderFulfillmentState.FromOrderPlaced)  // Create state from event
            .TransitionTo(AwaitingShipment);                      // Move to waiting state
        
        // 2. AwaitingShipment state
        //    When OrderShippedEvent arrives, update state with tracking
        descriptor
            .During(AwaitingShipment)
            .OnEvent<OrderShippedEvent>()
            .Then((state, e) =>
            {
                state.TrackingNumber = e.TrackingNumber;
                state.Carrier = e.Carrier;
                state.ShippedAt = e.ShippedAt;
            })
            .TransitionTo(Fulfilled);                             // Move to final state
        
        // 3. Fulfilled state (final)
        //    On entry, publish summary event
        descriptor
            .Finally(Fulfilled)
            .OnEntry()
            .Publish<OrderFulfilledEvent>(
                (_, state) => state.ToFulfilledEvent(),          // Convert state to event
                null);
    }
}
```

**State Transitions**:
```
[New Order] → Initially + OrderPlacedEvent
    ↓
[AwaitingShipment] ← Waiting for shipping
    ↓
[OrderShippedEvent received] → Update state
    ↓
[Fulfilled] → (finally state)
    ↓
[Publish OrderFulfilledEvent] → Saga completes
```

**Key Features**:
- **Correlation**: OrderId used as CorrelationId to associate events with saga instance
- **Choreography**: Waits for events from other services (ShippingService)
- **State Persistence**: Saga state stored (in-memory in example; PostgreSQL in production)
- **Completion**: Publishes final event when fulfilled
- **Idempotency**: Saga pattern handles retries and duplicate events

---

### 3.4 OrderService

**File**: `KafkaTransport.OrderService/Program.cs`

**Configuration Structure**:
```csharp
// 1. Saga store (in-memory for demo)
builder.Services.AddInMemorySagas();

// 2. In-process mediator for API layer (CQRS)
builder.Services
    .AddMediator()
    .AddInstrumentation()
    .AddHandler<PlaceOrderCommandHandler>()
    .AddHandler<GetOrderStatusQueryHandler>();

// 3. Distributed message bus (Kafka)
builder.Services
    .AddMessageBus()
    .AddInstrumentation()
    .AddSaga<OrderFulfillmentSaga>()
    .AddEventHandler<OrderShippedEventHandler>()          // Receives OrderShippedEvent
    .AddRequestHandler<GetOrderStatusRequestHandler>()    // Handles request-reply
    .AddBatchHandler<OrderAnalyticsBatchHandler>(opts => opts.MaxBatchSize = 100)
    .AddEventHandler<ProcessOrderCommandHandler>()        // Handles commands
    .AddKafka(t =>
    {
        t.BootstrapServers(bootstrapServers);
        t.AutoProvision(true);
        
        // Explicit send pattern configuration
        t.DeclareTopic("process-order");
        t.Endpoint("process-order-ep")
            .Topic("process-order")
            .Handler<ProcessOrderCommandHandler>();
        t.DispatchEndpoint("send-demo")
            .ToTopic("process-order")
            .Send<ProcessOrderCommand>();
    });

// 4. Background worker (simulates order placement)
builder.Services.AddHostedService<OrderSimulatorWorker>();
```

**API Endpoints**:

1. **POST /api/orders** - Create Order (via Mediator)
```csharp
app.MapPost("/api/orders", async (PlaceOrderRequest request, ISender mediator) =>
{
    var result = await mediator.SendAsync(
        new PlaceOrderCommand { ... },
        CancellationToken.None);
    return Results.Created($"/api/orders/{result.OrderId}", result);
});
```
- Uses in-process mediator for command handling
- Handler publishes OrderPlacedEvent on message bus
- Returns created order with OrderId

2. **GET /api/orders/{orderId:guid}/status** - Query Order Status (via Mediator)
```csharp
app.MapGet("/api/orders/{orderId:guid}/status", async (Guid orderId, ISender mediator) =>
{
    var result = await mediator.QueryAsync(new GetOrderStatusQuery { OrderId = orderId }, CancellationToken.None);
    return Results.Ok(result);
});
```
- Queries order status via mediator
- In real app would query database; demo returns static status

3. **POST /api/demo/publish** - Publish Event (direct bus usage)
```csharp
app.MapPost("/api/demo/publish", async (IMessageBus messageBus, CancellationToken ct) =>
{
    await messageBus.PublishAsync(
        new OrderPlacedEvent { OrderId = Guid.NewGuid(), ... },
        ct);
    return Results.Ok(new { orderId, elapsedMs = sw.ElapsedMilliseconds });
});
```
- Demonstrates pub-sub pattern
- Publishes to multiple subscribers

4. **POST /api/demo/send** - Send Command
```csharp
app.MapPost("/api/demo/send", async (IMessageBus messageBus, CancellationToken ct) =>
{
    await messageBus.SendAsync(
        new ProcessOrderCommand { OrderId = Guid.NewGuid(), Action = "validate", ... },
        ct);
    return Results.Ok(...);
});
```
- Demonstrates send pattern (fire-and-forget)
- Message routed to specific endpoint

5. **POST /api/demo/request-reply** - Synchronous Request
```csharp
app.MapPost("/api/demo/request-reply", async (IMessageBus messageBus, CancellationToken ct) =>
{
    var response = await messageBus.RequestAsync(
        new GetOrderStatusRequest { OrderId = Guid.NewGuid() },
        ct);
    return Results.Ok(new { request = ..., response = ..., elapsedMs = ... });
});
```
- Demonstrates request-reply pattern
- Blocks waiting for response from GetOrderStatusRequestHandler

6. **POST /api/demo/batch** - Batch Processing Demo
```csharp
app.MapPost("/api/demo/batch", async (IServiceScopeFactory scopeFactory, CancellationToken ct) =>
{
    const int count = 500;
    const int workers = 10;
    // Launch 10 workers, each publishing 50 messages
    await Task.WhenAll(tasks);
    
    return Results.Ok(new {
        count,
        elapsedMs,
        messagesPerSecond = count * 1000.0 / Math.Max(1, elapsedMs)
    });
});
```
- Demonstrates batch handler processing
- OrderAnalyticsBatchHandler collects batches of OrderPlacedEvents
- Logs analytics (total revenue, unique customers per batch)

7. **POST /api/demo/bulk-publish** - High-Volume Publishing
```csharp
app.MapPost("/api/demo/bulk-publish", async (HttpContext context, IServiceScopeFactory scopeFactory, CancellationToken ct) =>
{
    var total = int.TryParse(countStr, out var c) ? Math.Clamp(c, 1, 100_000) : 50_000;
    // Distribute publishing across 10 workers
    return Results.Ok(new { count = total, elapsedMs, messagesPerSecond = ... });
});
```
- Stress test endpoint
- Can publish up to 100k messages
- Measures throughput (messages/second)

8. **GET /api/messageBus/topology** - Topology Inspection
```csharp
app.MapMessageBusDeveloperTopology();
```
- Developer endpoint to inspect configured topics, handlers, endpoints
- Useful for debugging topology configuration

**Handler Implementations**:

1. **OrderShippedEventHandler**
   - Subscribes to `OrderShippedEvent`
   - Updates order state (in real app: database update)
   - Marks order as shipped

2. **GetOrderStatusRequestHandler**
   - Handles `GetOrderStatusRequest`
   - Returns `GetOrderStatusResponse`
   - Queries order status (static "Processing" in demo)

3. **ProcessOrderCommandHandler**
   - Handles `ProcessOrderCommand`
   - Logs command execution
   - Demonstrates send pattern

4. **OrderAnalyticsBatchHandler**
   - Handles batch of `OrderPlacedEvent`
   - Aggregates: total revenue, unique customers
   - Logs analytics

---

### 3.5 OrderSimulatorWorker

**File**: `KafkaTransport.OrderService/OrderSimulatorWorker.cs`

**Purpose**: Background service that simulates continuous order placement

**Configuration**:
```csharp
public sealed class OrderSimulatorWorker : BackgroundService
{
    private static readonly string[] Products = [
        "Mechanical Keyboard", "Wireless Mouse", "USB-C Hub",
        "Monitor Stand", "Webcam HD", "Noise-Cancelling Headphones",
        "Laptop Sleeve", "Desk Lamp", "Ergonomic Chair", "Standing Desk"
    ];
    
    private static readonly string[] Customers = [
        "alice@example.com", "bob@example.com", "carol@example.com",
        "dave@example.com", "eve@example.com"
    ];
}
```

**Execution Loop**:
```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    await Task.Delay(3000, stoppingToken);  // Wait for bus startup
    
    while (!stoppingToken.IsCancellationRequested)
    {
        try
        {
            // Create new scope for each order (DI isolation)
            await using var scope = scopeFactory.CreateAsyncScope();
            var messageBus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
            
            // Generate random order
            var orderId = Guid.NewGuid();
            var product = Products[Random.Shared.Next(Products.Length)];
            var quantity = Random.Shared.Next(1, 6);
            var unitPrice = Math.Round(Random.Shared.Next(1999, 49999) / 100m, 2);
            var customer = Customers[Random.Shared.Next(Customers.Length)];
            
            var orderEvent = new OrderPlacedEvent { ... };
            
            // Publish to message bus
            await messageBus.PublishAsync(orderEvent, stoppingToken);
            
            logger.LogInformation("Simulated order {OrderId}: {Quantity}x {Product} for {Customer}", ...);
        }
        catch (OperationCanceledException) { break; }
        catch (Exception ex) { logger.LogError(ex, "Error placing simulated order"); }
        
        await Task.Delay(5000, stoppingToken);  // Delay 5 seconds between orders
    }
}
```

**Key Features**:
- Publishes every 5 seconds (configurable)
- Random product and customer selection
- Random quantity (1-5) and price
- Logs each simulated order
- Triggers real order processing chain (shipping, notifications, saga)

**Use Cases**:
- Local development (continuous order flow)
- Demo/testing (shows message flow end-to-end)
- Load testing (can adjust delay/frequency)

---

### 3.6 ShippingService

**File**: `KafkaTransport.ShippingService/Program.cs`

**Architecture**: Simple event-driven worker service (no API endpoints beyond health checks)

**Configuration**:
```csharp
builder.Services
    .AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddKafka(t =>
    {
        t.BootstrapServers(bootstrapServers);
        t.AutoProvision(true);
    });
```

**Handler Implementation**: `OrderPlacedEventHandler`

```csharp
public sealed class OrderPlacedEventHandler(
    IMessageBus messageBus,
    ILogger<OrderPlacedEventHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    private static readonly string[] Carriers = ["FedEx", "UPS", "DHL", "USPS"];
    
    public async ValueTask HandleAsync(OrderPlacedEvent message, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Preparing shipment for order {OrderId}: {Quantity}x {ProductName} (${TotalAmount}) → {CustomerEmail}",
            message.OrderId, message.Quantity, message.ProductName, message.TotalAmount, message.CustomerEmail);
        
        // Simulate shipping processing
        await Task.Delay(500, cancellationToken);
        
        // Generate tracking info
        var trackingNumber = $"TRK-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
        var carrier = Carriers[Random.Shared.Next(Carriers.Length)];
        
        // Publish shipment confirmation (choreography: triggers saga transition)
        await messageBus.PublishAsync(
            new OrderShippedEvent
            {
                OrderId = message.OrderId,
                TrackingNumber = trackingNumber,
                Carrier = carrier,
                ShippedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
        
        logger.LogInformation("Order {OrderId} shipped via {Carrier}, tracking: {TrackingNumber}", ...);
    }
}
```

**Message Flow**:
1. OrderService publishes `OrderPlacedEvent`
2. ShippingService receives event via handler
3. Simulates 500ms processing (shipping prep)
4. Publishes `OrderShippedEvent` to message bus
5. OrderService saga receives confirmation
6. NotificationService receives confirmation

**Deployment Model**:
- Standalone service (can be scaled independently)
- Subscribes only to `OrderPlacedEvent`
- No API endpoints (pure event processor)
- Auto-subscribes when handler registered

---

### 3.7 NotificationService

**File**: `KafkaTransport.NotificationService/Program.cs`

**Architecture**: Simple event-driven worker (receives events, logs/notifies)

**Configuration**:
```csharp
builder.Services
    .AddMessageBus()
    .AddInstrumentation()
    .AddEventHandler<OrderPlacedNotificationHandler>()
    .AddEventHandler<OrderShippedNotificationHandler>()
    .AddEventHandler<OrderFulfilledNotificationHandler>()
    .AddKafka(t =>
    {
        t.BootstrapServers(bootstrapServers);
        t.AutoProvision(true);
    });
```

**Handlers**:

1. **OrderPlacedNotificationHandler**
   - Subscribes to `OrderPlacedEvent`
   - "Sends email" to customer confirming order received
   - Logs: "Order {OrderId} placed for {CustomerEmail}"

2. **OrderShippedNotificationHandler**
   - Subscribes to `OrderShippedEvent`
   - "Sends email" with tracking info
   - Logs: "Order {OrderId} shipped via {Carrier}, tracking: {TrackingNumber}"

3. **OrderFulfilledNotificationHandler**
   - Subscribes to `OrderFulfilledEvent`
   - "Sends final confirmation email"
   - Logs: "Order {OrderId} fulfilled and delivered"

**Deployment Model**:
- Decoupled from order processing
- Can be deployed, upgraded, or scaled independently
- If NotificationService is down, orders still process
- When it comes back online, it processes queued events

---

### 3.8 AppHost (Aspire Orchestration)

**File**: `KafkaTransport.AppHost/AppHost.cs`

**Purpose**: Defines distributed application topology using .NET Aspire

**Configuration**:
```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Infrastructure: Kafka instance with UI
var kafka = builder.AddKafka("kafka").WithKafkaUI();

// OrderService
builder
    .AddProject<Projects.KafkaTransport_OrderService>("order-service")
    .WithReference(kafka)
    .WaitFor(kafka);

// ShippingService
builder
    .AddProject<Projects.KafkaTransport_ShippingService>("shipping-service")
    .WithReference(kafka)
    .WaitFor(kafka);

// NotificationService
builder
    .AddProject<Projects.KafkaTransport_NotificationService>("notification-service")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();
```

**Infrastructure Management**:
- **Kafka Container**: Automatically started with Kafka UI
- **Service Startup Order**: Each service waits for Kafka before starting
- **Environment Injection**: Kafka connection string injected as "kafka" connection
- **Service Communication**: All services on shared Kafka instance

**Aspire Features**:
- Health checks automatically configured
- Distributed tracing (OpenTelemetry)
- Metrics collection
- Dashboard for monitoring services

---

### 3.9 ServiceDefaults

**File**: `KafkaTransport.ServiceDefaults/Extensions.cs`

**Purpose**: Centralized configuration for all services

**Configuration Areas**:
1. **Health Checks**: /health, /health/ready endpoints
2. **Service Discovery**: (Aspire-based)
3. **Instrumentation**: OpenTelemetry tracing, metrics
4. **Logging**: Structured logging configuration

**Usage in Each Service**:
```csharp
builder.AddServiceDefaults();
```

---

## Part 4: End-to-End Message Flow Examples

### 4.1 Order Placement Flow (Pub-Sub + Saga)

**Initiator**: OrderSimulatorWorker

```
1. OrderSimulatorWorker
   └─ PublishAsync(OrderPlacedEvent { orderId, product, quantity, amount, email })

2. Message Bus (Kafka topic: "order-placed-event" or auto-generated)
   ├─ OrderService.OrderFulfillmentSaga
   │  └─ Initially.OnEvent<OrderPlacedEvent>
   │     ├─ Creates OrderFulfillmentState (From: orderId, product, quantity, amount, email)
   │     └─ TransitionTo(AwaitingShipment)
   │
   ├─ ShippingService.OrderPlacedEventHandler
   │  ├─ Logs: "Preparing shipment..."
   │  ├─ Delay(500ms)
   │  └─ PublishAsync(OrderShippedEvent { orderId, trackingNumber, carrier, shippedAt })
   │     └─ Kafka topic: "order-shipped-event"
   │
   └─ NotificationService.OrderPlacedNotificationHandler
      └─ Logs: "Order placed for {email}"

3. OrderShippedEvent (Kafka)
   ├─ OrderService.OrderFulfillmentSaga
   │  ├─ During(AwaitingShipment).OnEvent<OrderShippedEvent>
   │  ├─ Updates state: tracking, carrier, shippedAt
   │  ├─ TransitionTo(Fulfilled)
   │  └─ Finally(Fulfilled).OnEntry
   │     └─ PublishAsync(OrderFulfilledEvent) [with aggregated data]
   │        └─ Kafka topic: "order-fulfilled-event"
   │
   ├─ OrderService.OrderShippedEventHandler
   │  └─ Logs: "Order shipped"
   │
   └─ NotificationService.OrderShippedNotificationHandler
      └─ Logs: "Order shipped via {carrier}"

4. OrderFulfilledEvent (Kafka)
   └─ NotificationService.OrderFulfilledNotificationHandler
      └─ Logs: "Order fulfilled"
```

**Timeline**:
- T+0: OrderPlacedEvent published
- T+0-100ms: All handlers receive event (parallel fan-out)
- T+500ms: ShippingService publishes OrderShippedEvent
- T+500-600ms: Saga receives and transitions; publishes OrderFulfilledEvent
- T+500-600ms: OrderService and NotificationService receive OrderShippedEvent
- T+500-600ms: NotificationService receives OrderFulfilledEvent

**Total Latency**: ~600ms (500ms shipping simulation + 100ms processing)

---

### 4.2 Request-Reply Flow

**Initiator**: Client calls `/api/demo/request-reply`

```
1. Client HTTP Request
   └─ OrderService.RequestAsync(GetOrderStatusRequest { orderId })

2. Message Bus
   ├─ Kafka topic: "get-order-status-request" (or similar)
   │
   └─ OrderService.GetOrderStatusRequestHandler
      ├─ Handles: GetOrderStatusRequest { orderId }
      ├─ Creates: GetOrderStatusResponse { orderId, status, updatedAt }
      └─ Replies to reply topic (Kafka internal topic)
         └─ Contains: Response message + CorrelationId

3. Reply Topic (Kafka)
   └─ Client correlation engine
      └─ Matches response to original request
         └─ Returns GetOrderStatusResponse to awaiting RequestAsync call

4. HTTP Response
   └─ Client receives:
      {
        "request": { "orderId": "..." },
        "response": { "orderId": "...", "status": "Processing", "updatedAt": "..." },
        "elapsedMs": 45
      }
```

**Correlation Mechanism**:
- Each RequestAsync call generates unique correlation ID
- Request sent to handler topic with correlationId header
- Handler response sent to reply topic with same correlationId
- Client listener matches responses by correlationId

**Timeout Protection**:
- RequestAsync blocks indefinitely or until timeout
- If no response received, request fails with timeout exception

---

### 4.3 Send (Fire-and-Forget) Flow

**Initiator**: Client calls `/api/demo/send`

```
1. Client HTTP Request
   └─ OrderService.SendAsync(ProcessOrderCommand { orderId, action })

2. Message Bus
   ├─ Kafka topic: "process-order" (explicitly declared)
   │  (Configured via: t.DeclareTopic("process-order"))
   │
   └─ OrderService.ProcessOrderCommandHandler
      ├─ Handles: ProcessOrderCommand { orderId, action }
      ├─ Logs: "Processing command for order {orderId}: {action}"
      └─ Returns ValueTask.CompletedTask (no response)

3. SendAsync completes
   └─ HTTP Response (no wait for handler completion)
      {
        "orderId": "...",
        "action": "validate",
        "elapsedMs": 5
      }
```

**Key Difference from Request-Reply**:
- No correlation ID needed
- No response expected
- HTTP returns immediately (before handler processes)
- Handler processes asynchronously

**Concurrency**: Multiple SendAsync calls can be in-flight simultaneously

---

## Part 5: Test Execution and Infrastructure

### 5.1 Test Framework

**Framework**: xUnit with Testcontainers

**Collection Pattern**:
- All tests marked with `[Collection("Kafka")]` share single KafkaFixture
- KafkaFixture starts single containerized Kafka instance
- Tests run in parallel within collection (IPC safe)
- Each test gets isolated topic namespace

### 5.2 Test Container Management

**Container Image**: `confluentinc/cp-kafka:7.6.0`

**Lifecycle**:
1. **Fixture Initialize**: StartAsync() → Container starts
2. **Test Execution**: Tests access BootstrapServers
3. **Fixture Dispose**: DisposeAsync() → Container stopped

**Topic Isolation**:
- Each test gets unique topic prefix: `{testName}_{filehash}`
- Prevents cross-test pollution
- Allows tests to run in parallel

### 5.3 Test Utilities Reused Across Tests

**MessageRecorder**:
```csharp
var recorder = new MessageRecorder();

// In handler: recorder.Record(message)
// In test:  Assert.True(await recorder.WaitAsync(timeout, expectedCount))
```

**Pattern**:
- Register recorder in DI container
- Inject into handler
- Handler calls Record() when message received
- Test waits for expected count with timeout

**Advantages**:
- Type-safe message capture
- Async-friendly (no busy-wait polling)
- Supports count expectations

---

## Part 6: Key Testing Patterns and Best Practices

### 6.1 Arranging Tests

**Pattern 1: Fixture + Runtime**
```csharp
var ctx = _fixture.CreateTestContext();  // Get isolated context
var runtime = KafkaBusFixture.CreateRuntime(t =>
{
    // Configure bus topology
    t.DeclareTopic("my-topic");
    t.DispatchEndpoint("ep").ToTopic("my-topic").Send<MyMessage>();
});
```

**Pattern 2: Service Collection + Build Bus**
```csharp
var ctx = _fixture.CreateTestContext();
await using var bus = await new ServiceCollection()
    .AddSingleton(recorder)
    .AddMessageBus()
    .AddEventHandler<MyHandler>()
    .AddKafka(t => t.BootstrapServers(ctx.BootstrapServers))
    .BuildTestBusAsync();
```

### 6.2 Asserting Message Reception

**Single Message**:
```csharp
Assert.True(await recorder.WaitAsync(timeout), "Message not received");
var msg = Assert.Single(recorder.Messages);
Assert.Equal("expected", msg.Property);
```

**Multiple Messages**:
```csharp
Assert.True(await recorder.WaitAsync(timeout, expectedCount: 3), "Not all messages received");
Assert.Equal(3, recorder.Messages.Count);
var ids = recorder.Messages.Cast<MyMessage>().Select(m => m.Id).OrderBy(x => x).ToList();
Assert.Equal(expected, ids);
```

**Batch Messages**:
```csharp
Assert.True(await recorder.WaitAsync(timeout), "Batch not received");
var batch = Assert.IsAssignableFrom<IMessageBatch<MyMessage>>(Assert.Single(recorder.Batches));
Assert.Equal(BatchCompletionMode.Size, batch.CompletionMode);
```

### 6.3 Verifying Fault Handling

**Error Queue**:
```csharp
var capture = new ErrorCapture();
// Configure fault endpoint
t.Endpoint("handler-ep")
    .Handler<ThrowingHandler>()
    .FaultEndpoint($"kafka:///t/{errorTopic}");

t.Endpoint("error-ep")
    .Topic(errorTopic)
    .Kind(ReceiveEndpointKind.Error)
    .Consumer<ErrorSpyConsumer>();

// Assert
Assert.True(headers.ContainsKey("fault-exception-type"));
Assert.True(headers.ContainsKey("fault-message"));
Assert.True(headers.ContainsKey("fault-stack-trace"));
```

### 6.4 Measuring Concurrency

**ConcurrencyTracker Pattern**:
```csharp
var tracker = new ConcurrencyTracker();

// In handler:
tracker.Enter();
try { ... } finally { tracker.Exit(); }

// Assert:
Assert.Equal(1, tracker.PeakConcurrency);  // Sequential
Assert.True(tracker.PeakConcurrency > 1);  // Parallel
```

---

## Part 7: Message Serialization and Headers

### 7.1 Header Propagation

**Standard Headers** (auto-added by framework):
- `content-type`: Message type (JSON)
- `message-type`: Full type name
- `message-id`: Unique message identifier
- `correlation-id`: For request-reply pairing
- `timestamp`: When message was published

**Custom Headers** (in IConsumeContext):
- Accessible via `context.Headers`
- Preserved through fault routing
- Used for feature metadata

### 7.2 Fault Headers

**Auto-Added on Error Routing**:
- `fault-exception-type`: Exception type name
- `fault-message`: Exception message
- `fault-stack-trace`: Full stack trace
- `fault-timestamp`: When error occurred

---

## Part 8: Configuration Patterns

### 8.1 Bus Configuration

**Minimal**:
```csharp
.AddKafka(t => t.BootstrapServers("localhost:9092"))
```

**With Defaults**:
```csharp
.AddKafka(t =>
{
    t.BootstrapServers("localhost:9092");
    t.AutoProvision(true);  // Create topics automatically
    t.ConfigureDefaults(d =>
    {
        d.Topic.Partitions = 3;
        d.Topic.ReplicationFactor = 1;
    });
})
```

**With Explicit Topology**:
```csharp
.AddKafka(t =>
{
    t.BootstrapServers("localhost:9092");
    t.DeclareTopic("orders");
    t.DeclareTopic("shipments");
    
    t.Endpoint("orders-ep")
        .Topic("orders")
        .Handler<OrderHandler>();
    
    t.DispatchEndpoint("send-orders")
        .ToTopic("orders")
        .Send<OrderCommand>();
})
```

### 8.2 Endpoint Configuration

**Handler Endpoint**:
```csharp
t.Endpoint("my-ep")
    .Handler<MyHandler>()
    .MaxConcurrency(5)
    .FaultEndpoint("kafka:///t/error-topic");
```

**Send Endpoint** (DispatchEndpoint):
```csharp
t.DispatchEndpoint("send-ep")
    .ToTopic("commands")
    .Send<MyCommand>();
```

### 8.3 Handler Registration

**Event Handler**:
```csharp
.AddEventHandler<MyEventHandler>()  // Implements IEventHandler<T>
```

**Request Handler**:
```csharp
.AddRequestHandler<MyRequestHandler>()  // Implements IEventRequestHandler<TReq, TResp>
```

**Batch Handler**:
```csharp
.AddBatchHandler<MyBatchHandler>(opts =>
{
    opts.MaxBatchSize = 100;
    opts.BatchTimeout = TimeSpan.FromSeconds(5);
})
```

**Saga**:
```csharp
.AddSaga<MySaga>()  // Implements Saga<TState>
```

---

## Summary: Test Scenarios Covered

| Pattern | Test File | Scenarios |
|---------|-----------|-----------|
| Pub-Sub | PublishSubscribeTests | Single, multiple handlers, sequential, rapid-fire |
| Request-Reply | RequestReplyTests | Typed, concurrent, multiple types, void |
| Send | SendTests | Delivery, routing, multiple topics |
| Batching | BatchingTests | Size trigger, timeout trigger |
| Fault Handling | FaultHandlingTests | Request throws, publish partial failure |
| Error Queue | ErrorQueueTests | Routing to error topic, header preservation |
| Inbox | InboxTests | Deduplication, different IDs, skip inbox, null ID |
| Concurrency | ConcurrencyTests | Sequential (max=1), parallel (max>1) |
| Auto-Provision | AutoProvisionIntegrationTests | Default, explicit true/false |
| Topology | KafkaMessagingTopologyTests | Topic creation, defaults, duplicates |
| Serialization | KafkaMessageEnvelopeParserTests | Envelope parsing, headers |
| Middleware | Various *MiddlewareTests | Custom headers, transport/endpoint-level |

---

## Summary: Example Application Components

| Component | Purpose | Pattern |
|-----------|---------|---------|
| OrderService | Order creation, saga coordination | Pub-Sub + Saga + API |
| ShippingService | Shipment processing | Event handler |
| NotificationService | Customer notifications | Event fan-out |
| OrderFulfillmentSaga | Order state machine | Choreography-based saga |
| OrderSimulatorWorker | Continuous order generation | Background worker |
| AppHost | Service orchestration | Aspire |

---

## Key Learnings

1. **Kafka Transport Supports Full Messaging Patterns**: Pub-Sub, Send, Request-Reply, Batching
2. **Saga Pattern Implemented via Event Choreography**: Waits for events from multiple services
3. **Idempotency via Inbox**: Deduplicates messages by MessageId
4. **Concurrency Control**: Per-endpoint MaxConcurrency limits
5. **Fault Handling**: Automatic routing to error topics with metadata headers
6. **Auto-Provisioning**: Topics created automatically unless disabled
7. **Aspire Integration**: Service discovery, health checks, distributed tracing
8. **Testability**: Container-based integration tests with real Kafka
9. **Isolation**: Test topics prefixed per test to prevent pollution
10. **Scalability Demo**: Bulk publish endpoint shows 50k+ messages/sec potential

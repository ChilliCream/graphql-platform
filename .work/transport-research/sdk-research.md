# Azure Event Hubs .NET SDK Research

## 1. Official .NET Client Libraries

There are **two NuGet packages** that make up the modern Event Hubs SDK:

| Package | Latest Stable | Purpose |
|---------|---------------|---------|
| `Azure.Messaging.EventHubs` | **5.12.2** | Core client library: producer, consumer, primitives |
| `Azure.Messaging.EventHubs.Processor` | **5.12.2** | `EventProcessorClient` — distributed consumer with checkpoint store |

Both follow the unified Azure SDK design guidelines and target .NET Standard 2.0+.

> **Legacy package**: `Microsoft.Azure.EventHubs` is the older library and should **not** be used for new work.

---

## 2. Connection Model

### Transport Protocol
- Default: **AMQP 1.0 over TCP** (port 5671/5672)
- Alternative: **AMQP over WebSockets** (port 443) — useful when TCP ports are blocked; slightly higher latency due to WebSocket handshake overhead
- Configured via `EventHubConnectionOptions.TransportType` (`AmqpTcp` or `AmqpWebSockets`)
- Proxy support is only available with `AmqpWebSockets`

### Connection Lifecycle
- Connections are managed internally by the client — no explicit connection pooling API exposed to the user
- Each client (producer, consumer, processor) manages its own AMQP connection and creates AMQP **links** (unidirectional virtual transfer paths) on top of **sessions**
- Clients are designed to be **long-lived singletons** — create once and reuse for the lifetime of the application
- The `EventHubConnection` class can optionally be shared across multiple clients to share the underlying AMQP connection

### Connection Options
```csharp
var options = new EventHubProducerClientOptions
{
    ConnectionOptions = new EventHubConnectionOptions
    {
        TransportType = EventHubsTransportType.AmqpWebSockets,
        Proxy = new WebProxy("http://proxy:8080")
    }
};
```

### Authentication
- **Connection string**: `Endpoint=sb://{namespace}.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...;EntityPath={eventHubName}`
- **Azure.Identity credentials** (e.g., `DefaultAzureCredential`, `ManagedIdentityCredential`) with fully qualified namespace + event hub name
- **Shared Access Signature (SAS)** tokens

---

## 3. Producer API

There are **two producer clients** with different trade-offs:

### 3a. `EventHubProducerClient` (explicit batching)

The standard producer. All operations are **async only**. The workflow is:

1. **Create a batch**: `CreateBatchAsync()` returns an `EventDataBatch` with a size limit matching the service maximum
2. **Add events**: `batch.TryAdd(eventData)` — returns `false` if the event would exceed the batch size limit
3. **Send**: `SendAsync(batch)` — publishes the entire batch atomically

```csharp
await using var producer = new EventHubProducerClient(connectionString, eventHubName);

using EventDataBatch batch = await producer.CreateBatchAsync();
batch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Event 1")));
batch.TryAdd(new EventData(Encoding.UTF8.GetBytes("Event 2")));

await producer.SendAsync(batch);
```

**Key characteristics:**
- Thread-safe and designed to be a singleton
- Sending is explicit — you control exactly when batches are published
- `CreateBatchAsync` can accept `CreateBatchOptions` to target a specific partition or set a partition key
- `SendAsync` resolves when the Event Hubs service has **acknowledged receipt** and assumed responsibility for delivery
- `DisposeAsync`/`CloseAsync` must be called to release resources

### 3b. `EventHubBufferedProducerClient` (automatic batching)

A higher-level producer that manages batching automatically:

1. **Enqueue events**: `EnqueueEventAsync(eventData)` or `EnqueueEventsAsync(events)` — adds to an internal buffer
2. **Automatic sending**: Events are batched and published automatically when the batch is full **or** `MaximumWaitTime` has elapsed

```csharp
var bufferedProducer = new EventHubBufferedProducerClient(connectionString, eventHubName);

bufferedProducer.SendEventBatchSucceededAsync += args => { /* ... */ return Task.CompletedTask; };
bufferedProducer.SendEventBatchFailedAsync += args => { /* ... */ return Task.CompletedTask; };

await bufferedProducer.EnqueueEventAsync(new EventData("Hello"));

// Must flush/close to ensure all buffered events are sent
await bufferedProducer.CloseAsync();
```

**Key characteristics:**
- Deferred publishing — events sit in a buffer before being sent
- `MaximumEventBufferLengthPerPartition` defaults to 1500 events
- `MaximumConcurrentSendsPerPartition` defaults to 1 (increase for higher throughput when ordering is not needed)
- Success/failure notifications via `SendEventBatchSucceededAsync` and `SendEventBatchFailedAsync` events
- Supports idempotent retries (`EnableIdempotentRetries`) to avoid duplication at a minor throughput cost
- Must be closed/flushed explicitly to ensure all buffered events are published

### Partition Targeting

Both producers support:
- **Automatic partition assignment** (default): Event Hubs service round-robins across partitions
- **Partition key** (`CreateBatchOptions.PartitionKey`): Events with the same key go to the same partition (consistent hashing)
- **Explicit partition ID** (`CreateBatchOptions.PartitionId`): Direct targeting of a specific partition

---

## 4. Consumer API

There are **three consumer approaches**, from highest-level to lowest:

### 4a. `EventProcessorClient` (recommended for production)

Lives in the `Azure.Messaging.EventHubs.Processor` package. This is the **recommended approach** for production workloads.

- **Push-based**: Registers event handlers that are invoked as events arrive
- **Distributed**: Multiple instances coordinate via a checkpoint store (Azure Blob Storage) to balance partitions
- **Resilient**: Automatically recovers from transient failures and rebalances partitions

```csharp
var storageClient = new BlobContainerClient(storageConnectionString, containerName);
var processor = new EventProcessorClient(storageClient, consumerGroup, ehConnectionString, eventHubName);

processor.ProcessEventAsync += async (args) =>
{
    // Process the event
    var data = args.Data;

    // Checkpoint to record progress
    await args.UpdateCheckpointAsync();
};

processor.ProcessErrorAsync += (args) =>
{
    Console.WriteLine($"Error: {args.Exception}");
    return Task.CompletedTask;
};

await processor.StartProcessingAsync();
// ... runs continuously ...
await processor.StopProcessingAsync();
```

### 4b. `EventHubConsumerClient` (simple scenarios)

- **Pull-based via async enumerable**: `ReadEventsAsync()` or `ReadEventsFromPartitionAsync()`
- Returns `IAsyncEnumerable<PartitionEvent>` — events are iterated as they become available
- **No checkpoint management** — the application must track its own position
- Good for dev/test, simple scenarios, or single-partition reading

```csharp
await using var consumer = new EventHubConsumerClient(consumerGroup, connectionString, eventHubName);

await foreach (PartitionEvent partitionEvent in consumer.ReadEventsAsync(cancellationToken))
{
    var data = partitionEvent.Data;
    var partition = partitionEvent.Partition;
}
```

### 4c. `PartitionReceiver` (low-level, high-performance)

- Lives in `Azure.Messaging.EventHubs.Primitives` namespace
- **Batch polling** via `ReceiveBatchAsync(maxCount)` — not an async enumerable
- Thin wrapper over the AMQP transport — maximum control and throughput
- Timeout-based: does **not** honor cancellation when a service operation is active
- Used when maximum throughput is needed and the application accepts additional complexity

```csharp
var receiver = new PartitionReceiver(consumerGroup, partitionId, EventPosition.Earliest, connectionString, eventHubName);

IEnumerable<EventData> events = await receiver.ReceiveBatchAsync(100, TimeSpan.FromSeconds(1));
```

---

## 5. Message Model (`EventData`)

### Construction
```csharp
// From byte array
var event1 = new EventData(new byte[] { 0x01, 0x02 });

// From BinaryData (preferred)
var event2 = new EventData(BinaryData.FromString("Hello"));
var event3 = new EventData(BinaryData.FromObjectAsJson(new { Name = "test" }));
```

### Properties

| Property | Type | Description |
|----------|------|-------------|
| `EventBody` | `BinaryData` | The raw event payload (preferred over `Body`) |
| `Body` | `ReadOnlyMemory<byte>` | Legacy accessor for the raw bytes |
| `Properties` | `IDictionary<string, object>` | Application-defined key/value metadata (user properties) |
| `SystemProperties` | `IReadOnlyDictionary<string, object>` | Read-only, service-set properties (offset, sequence number, enqueued time, partition key) |
| `ContentType` | `string?` | MIME type hint for the body (e.g., `"application/json"`) |
| `CorrelationId` | `string?` | Application-defined correlation identifier for tracing |
| `MessageId` | `string?` | Application-defined unique identifier |
| `PartitionKey` | `string?` | (Read-only on received events) The partition key used when publishing |
| `SequenceNumber` | `long` | (Read-only) Service-assigned sequence number within the partition |
| `Offset` | `long` | (Read-only) The offset of the event within the partition stream |
| `EnqueuedTime` | `DateTimeOffset` | (Read-only) When the service accepted the event |

### Key Notes
- `Properties` values must be primitive types (string, int, long, double, bool, byte[], DateTimeOffset, etc.) — no complex objects
- `EventData` and `EventDataBatch` are **not thread-safe**
- Maximum event size is 1 MB (256 KB on Basic tier) including all properties and overhead

---

## 6. Addressing Model: Event Hubs, Partitions, Consumer Groups

### Hierarchy
```
Namespace (e.g., mynamespace.servicebus.windows.net)
  └── Event Hub (topic-like entity)
        ├── Partition 0
        ├── Partition 1
        ├── Partition 2
        └── ...
```

### Event Hubs (the topic equivalent)
- An Event Hub is the primary addressable entity — analogous to a "topic"
- Events are published to an Event Hub (not directly to partitions, unless explicitly targeted)
- Event Hubs retain events for a configurable period (1–90 days, or unlimited with capture)

### Partitions
- Ordered, immutable sequences of events (append-only commit log)
- Each partition is an independent log — ordering is guaranteed **only within** a partition
- The number of partitions is set at Event Hub creation time (cannot be changed after creation in Standard tier; can be increased but not decreased in Premium/Dedicated)
- Typical range: 2–32 partitions (up to 2000 on Dedicated)
- Events are assigned to partitions by:
  1. Round-robin (default, when no partition key specified)
  2. Partition key hash (consistent assignment)
  3. Explicit partition ID

### Consumer Groups
- A consumer group is a **logical view** of the entire Event Hub
- Each consumer group maintains its own independent read position (offset/checkpoint) per partition
- Default consumer group: `$Default`
- Multiple consumer groups allow different applications/services to independently consume the same events
- Maximum of 5 concurrent readers per partition per consumer group (important limit!)

### Key Differences from Traditional Queues
- **No competing consumers within a partition** — each partition is read by at most one consumer instance within a consumer group at a time
- **No message deletion on read** — events are retained based on a time-based retention policy, not consumption
- **Replay is possible** — consumers can seek to any position (offset, sequence number, enqueued time) and re-read events

---

## 7. Acknowledgment Model

Event Hubs uses a **checkpoint-based** acknowledgment model, which is fundamentally different from message queues.

### Publishing Side
- `SendAsync` returns when the **service acknowledges receipt** — this is a durable acknowledgment (at-least-once delivery to the Event Hub)
- With `EnableIdempotentRetries`, the producer can achieve **exactly-once publishing** per partition

### Consumer Side — Checkpointing (NOT per-message ack)
- There is **no per-message acknowledgment** — Event Hubs does not track individual message consumption
- Instead, consumers record their position via **checkpoints**
- A checkpoint says: "I have processed all events up to sequence number X in partition Y"
- Checkpoints are stored externally (typically Azure Blob Storage for `EventProcessorClient`)
- Checkpointing is **optional and explicit** — the consumer decides when to checkpoint

### Delivery Guarantees
- **At-least-once** is the standard guarantee: if a consumer crashes between processing an event and checkpointing, it will re-process events from the last checkpoint on restart
- **At-most-once** is achievable by checkpointing *before* processing (risk of data loss)
- **Exactly-once** is not natively supported on the consumer side — applications must implement idempotent processing
- On the producer side, idempotent retries provide exactly-once publishing semantics

### No Dead-Letter Queue
- Event Hubs does **not** have a built-in dead-letter queue (unlike Service Bus)
- "Poison" message handling must be implemented by the application (e.g., logging, forwarding to a separate Event Hub or storage)
- Events are always retained for the configured retention period regardless of processing success/failure

---

## 8. Error Handling & Retries

### Retry Options (`EventHubsRetryOptions`)

All clients accept retry configuration:

```csharp
var options = new EventHubProducerClientOptions
{
    RetryOptions = new EventHubsRetryOptions
    {
        Mode = EventHubsRetryMode.Exponential,
        MaximumRetries = 5,
        Delay = TimeSpan.FromMilliseconds(800),
        MaximumDelay = TimeSpan.FromSeconds(10),
        TryTimeout = TimeSpan.FromSeconds(60)
    }
};
```

| Property | Default | Description |
|----------|---------|-------------|
| `Mode` | `Exponential` | `Fixed` or `Exponential` backoff |
| `MaximumRetries` | 3 | Max retry attempts |
| `Delay` | 0.8s | Base delay (or fixed delay) |
| `MaximumDelay` | 60s | Cap on exponential backoff |
| `TryTimeout` | 60s | Timeout for a single attempt |
| `CustomRetryPolicy` | null | Override all above with a custom `EventHubsRetryPolicy` implementation |

### EventProcessorClient Error Handling
- **`ProcessErrorAsync`** handler is mandatory — called when internal processor errors occur
- **Critical**: Exceptions thrown from `ProcessEventAsync` are **NOT** caught by the processor and are **NOT** redirected to `ProcessErrorAsync` — you must wrap your handler code in try/catch
- The processor makes every effort to recover from infrastructure errors and continue
- If an unrecoverable error occurs, the processor forfeits ownership of all partitions so work can be redistributed

### No Built-in Circuit Breaking
- The SDK does not include circuit breaker patterns — retries are the primary resilience mechanism
- Applications should implement their own circuit breaking if needed
- The `EventProcessorClient` internally handles transient failures and reconnection

---

## 9. Configuration Patterns

### Connection String Construction
```csharp
// Basic
var producer = new EventHubProducerClient(connectionString, eventHubName);

// With options
var producer = new EventHubProducerClient(connectionString, eventHubName, new EventHubProducerClientOptions
{
    RetryOptions = new EventHubsRetryOptions { MaximumRetries = 5 },
    ConnectionOptions = new EventHubConnectionOptions
    {
        TransportType = EventHubsTransportType.AmqpTcp
    }
});
```

### Azure Identity (recommended for production)
```csharp
var credential = new DefaultAzureCredential();
var producer = new EventHubProducerClient(
    "mynamespace.servicebus.windows.net",
    "myeventhub",
    credential);
```

### Shared Connection
```csharp
var connection = new EventHubConnection(connectionString, eventHubName);
var producer = new EventHubProducerClient(connection);
var consumer = new EventHubConsumerClient("$Default", connection);
```

### DI Registration Pattern (ASP.NET Core)
```csharp
// Typically registered as singletons since clients are long-lived
services.AddSingleton(sp => new EventHubProducerClient(connectionString, eventHubName));
services.AddSingleton(sp => new EventHubConsumerClient("$Default", connectionString, eventHubName));
```

---

## 10. EventProcessorClient vs EventHubConsumerClient

| Aspect | `EventProcessorClient` | `EventHubConsumerClient` |
|--------|------------------------|--------------------------|
| **Package** | `Azure.Messaging.EventHubs.Processor` | `Azure.Messaging.EventHubs` |
| **Model** | Push (event handler callbacks) | Pull (async enumerable) |
| **Scaling** | Distributed — multiple instances balance partitions | Single instance — no coordination |
| **Checkpointing** | Built-in via checkpoint store | Manual (application manages offsets) |
| **Partition management** | Automatic partition ownership and load balancing | Manual — you choose which partitions to read |
| **Recovery** | Automatic reconnection, rebalancing | Manual reconnection logic needed |
| **Use case** | Production workloads at scale | Dev/test, simple scenarios, single-partition |
| **Dependencies** | Requires Azure Blob Storage | None beyond core Event Hubs package |
| **Concurrency** | One handler invocation per partition at a time (ordered) | Application controls concurrency |

**When to use `EventProcessorClient`:**
- Production workloads that need to read from all partitions
- Multiple consumer instances sharing the load
- Need automatic failover and partition rebalancing
- Want built-in checkpoint management

**When to use `EventHubConsumerClient`:**
- Development and testing
- Simple single-instance consumers
- Reading from a specific partition
- Scenarios where you don't need distributed coordination

---

## 11. Checkpoint Store (Azure Blob Storage)

### Setup
```csharp
// Create blob container client for checkpoint storage
var storageClient = new BlobContainerClient(storageConnectionString, "checkpoints");

// Create processor with blob checkpoint store
var processor = new EventProcessorClient(
    storageClient,
    "$Default",                    // consumer group
    eventHubConnectionString,
    eventHubName);
```

### How It Works
- The `EventProcessorClient` stores checkpoint data in Azure Blob Storage
- Each checkpoint is a small blob containing the sequence number and offset for a specific **partition + consumer group** combination
- Blob naming pattern: `{fullyQualifiedNamespace}/{eventHubName}/{consumerGroup}/checkpoint/{partitionId}`

### Checkpoint Storage Architecture
- **One blob per partition per consumer group** — very lightweight
- Ownership claims are also stored as blobs for partition load balancing
- The `BlobContainerClient` can use any authentication method supported by Azure Storage

### Best Practices
- Use a **separate container for each consumer group** (same storage account is fine)
- Checkpoint frequency is a trade-off:
  - **More frequent** = less reprocessing on failure, more storage I/O
  - **Less frequent** = less storage I/O, more reprocessing on failure
  - Common pattern: checkpoint every N events or every N seconds
- The storage account should be in the **same region** as the Event Hub for latency

### Checkpointing in Code
```csharp
processor.ProcessEventAsync += async (args) =>
{
    // Process the event
    await ProcessEvent(args.Data);

    // Checkpoint every 100 events
    if (args.Data.SequenceNumber % 100 == 0)
    {
        await args.UpdateCheckpointAsync();
    }
};
```

### Custom Checkpoint Stores
- The `EventProcessorClient` specifically requires `BlobContainerClient`
- For custom storage backends, you can subclass `EventProcessor<TPartition>` (the abstract base in `Azure.Messaging.EventHubs.Primitives`) and implement your own checkpoint/ownership store

---

## Key Takeaways for Transport Implementation

1. **Producer**: Use `EventHubProducerClient` with explicit batching (`CreateBatchAsync` + `SendAsync`) for maximum control. The `EventHubBufferedProducerClient` is an option for fire-and-forget scenarios.

2. **Consumer**: Use `EventProcessorClient` for production-grade distributed consumption. For simpler use cases or testing, `EventHubConsumerClient` with async enumerables works well.

3. **No per-message ack**: Unlike Service Bus, there is no message lock/complete/abandon pattern. Progress is tracked via checkpoints.

4. **No dead-letter queue**: Poison message handling must be implemented by the application.

5. **Partition key is the routing mechanism**: Events with the same partition key always land in the same partition, ensuring ordering for related events.

6. **Clients are singletons**: All clients are designed to be created once and reused. They manage their own AMQP connections internally.

7. **Max 5 readers per partition per consumer group**: This is a hard service limit to be aware of.

8. **AMQP-based**: The SDK uses AMQP 1.0 over TCP by default, with WebSocket fallback available.

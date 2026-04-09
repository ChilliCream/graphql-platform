# Mocha Azure Event Hubs Transport - Internals

## Overview

The Mocha Azure Event Hubs transport provides bidirectional messaging integration with Azure Event Hubs. It manages connections, batches outbound messages, processes inbound messages with checkpointing, and handles distributed partition coordination.

---

## 1. Message Publish Flow

### PublishAsync Entry Point → Dispatch

When `IMessageBus.PublishAsync()` is called for a message destined for Event Hubs:

1. The message envelope is routed to an `EventHubDispatchEndpoint` based on the configured destination
2. The endpoint's `DispatchAsync()` method is invoked with a dispatch context containing the normalized `MessageEnvelope`

### Envelope → EventData Conversion

The dispatch endpoint transforms the Mocha `MessageEnvelope` into an Azure `EventData`:

- **Body**: Zero-copy from `envelope.Body` → `new EventData(envelope.Body)`
- **AMQP Structured Properties** (no dictionary allocation):
  - `MessageId` → AMQP `Properties.MessageId`
  - `CorrelationId` → AMQP `Properties.CorrelationId`
  - `ContentType` → AMQP `Properties.ContentType`
  - `MessageType` → AMQP `Properties.Subject`
  - `ResponseAddress` → AMQP `Properties.ReplyTo`

- **Application Properties** (for overflow headers):
  - `ConversationId` → `x-conversation-id`
  - `CausationId` → `x-causation-id`
  - `SourceAddress` → `x-source-address`
  - `DestinationAddress` → `x-destination-address`
  - `FaultAddress` → `x-fault-address`
  - `EnclosedMessageTypes` → `x-enclosed-message-types` (semicolon-delimited)
  - `SentAt` → `x-sent-at` (Unix milliseconds)
  - Custom headers → application properties (with DateTime → Unix ms conversion)

### Partition Routing

Partition targeting follows a precedence model:

1. **x-partition-id header** (per-message override): `SendEventOptions.PartitionId`
2. **Endpoint configuration PartitionId** (static per-endpoint): Set during endpoint initialization
3. **x-partition-key header** (round-robin within range): `SendEventOptions.PartitionKey`
4. **No targeting** (default, round-robin): No `SendEventOptions` provided

All messages targeting the same partition are grouped together for batching efficiency.

### Batching Strategy: EventHubBatchDispatcher

Batching is controlled by `EventHubBatchMode`:

#### Single Mode (Default)
Each message is sent immediately via `producer.SendAsync([eventData])`. No accumulation overhead, but lower throughput.

#### Batch Mode
Messages are accumulated asynchronously into `EventDataBatch` instances:

1. **Enqueue Phase**:
   - `EnqueueAsync(eventData, sendOptions)` is called from `DispatchAsync()`
   - A `PendingEvent` record wrapping the event and options is pushed to an unbounded channel
   - The caller awaits a `TaskCompletionSource` that completes when the batch is sent

2. **Process Loop** (background `Task`):
   - Runs continuously in a loop, blocked on `WaitToReadAsync()`
   - Upon unblocking, calls `DrainAndSendAsync()` to accumulate and send messages

3. **Drain Logic**:
   - **Partition Coherency**: Messages with different partition targets trigger a flush of the current batch
   - **Batch Fullness**: When `batch.TryAdd(event)` returns `false`, the batch is sent and a new one is created
   - **Timeout**: A timer (`_maxWaitTime`, default 100ms) fires to flush partially full batches

4. **Send and Complete**:
   - `SendAsync(batch)` is called on the `EventHubProducerClient`
   - All pending events in the batch have their `TaskCompletionSource` set to completed or exception
   - Batch is disposed; loop continues

5. **Shutdown**:
   - Channel writer is completed, signaling no more enqueues
   - Outstanding events are drained and sent one final time
   - Process loop exits

**Key Properties**:
- Thread-safe for concurrent `EnqueueAsync` calls (channel is `SingleReader=true`)
- Partition keys and IDs are never mixed in a batch
- Max batch size is enforced by the Azure SDK; oversized single events fail fast
- Messages cannot exceed 1MB (checked before dispatch)

---

## 2. Message Receive Flow

### Processor Initialization

When a receive endpoint starts:

1. A `MochaEventProcessor` is instantiated with:
   - Connection credentials (connection string or token credential)
   - A message handler delegate
   - Checkpoint and ownership stores
   - Checkpoint interval (default 100 messages)

2. The processor inherits from Azure's `EventProcessor<EventProcessorPartition>`:
   - Handles automatic partition ownership and load balancing
   - Manages partition assignment via distributed coordination (if an ownership store is provided)
   - Recovers from consumer group lag via checkpointing

### Event Processing Loop

`MochaEventProcessor.OnProcessingEventBatchAsync()` is called by the Azure SDK for each batch of events:

1. **Iterate Over Events**:
   - For each event in the batch, invoke the message handler with: `(eventData, partitionId, cancellationToken)`
   - Track the last successful sequence number

2. **Error Handling**:
   - If an exception occurs (excluding `OperationCanceledException`), log the error and skip to the next event
   - Skipped events are NOT checkpointed; they will be reprocessed on next consumer group recovery
   - This provides **at-least-once** delivery semantics within a consumer group

3. **Checkpointing**:
   - A counter per partition increments on successful processing
   - Checkpoint is saved when:
     - Counter reaches `checkpointInterval` (default 100), OR
     - The last event in the batch is processed
   - Only the `lastSuccessfulSequence` is checkpointed (not including skipped events)

4. **Checkpoint Recovery**:
   - On processor restart, `GetCheckpointAsync()` retrieves the last checkpoint
   - If a checkpoint exists, the processor starts from `EventPosition.FromSequenceNumber(seq, isInclusive: false)`
   - If no checkpoint exists, starts from `EventPosition.Latest`

### Message Handler Integration

The message handler invokes `ExecuteAsync()` on the receive endpoint, which:

1. Sets features in the receive context:
   - `EventHubReceiveFeature.EventData` (raw Azure event)
   - `EventHubReceiveFeature.PartitionId` (string partition ID)
   - `EventHubReceiveFeature.SequenceNumber` (long)
   - `EventHubReceiveFeature.EnqueuedTime` (DateTimeOffset)

2. Runs the middleware pipeline, which includes:
   - Message envelope parsing
   - Deserialization
   - Error handling and retry logic

### Envelope Parsing

`EventHubMessageEnvelopeParser.Parse()` reconstructs the `MessageEnvelope`:

- **Body**: Zero-copy from `eventData.EventBody.ToMemory()`
- **AMQP Properties**:
  - `Properties.MessageId` → `envelope.MessageId`
  - `Properties.CorrelationId` → `envelope.CorrelationId`
  - `Properties.ContentType` → `envelope.ContentType`
  - `Properties.Subject` → `envelope.MessageType`
  - `Properties.ReplyTo` → `envelope.ResponseAddress`
- **Application Properties**:
  - Well-known headers mapped to envelope fields
  - Custom headers collected into `envelope.Headers` (avoiding allocation if none exist)
  - Dates stored as Unix ms; converted back to `DateTimeOffset`

---

## 3. Connection Management

### EventHubConnectionManager

Manages singleton `EventHubProducerClient` instances per hub name:

```
EventHubConnectionManager
  ↓
  ConcurrentDictionary<string, EventHubProducerClient>
    ├─ "hub1" → EventHubProducerClient
    ├─ "hub2" → EventHubProducerClient
    └─ ...
```

**Thread Safety**:
- `GetOrCreateProducer(hubName)` uses `ConcurrentDictionary.GetOrAdd()` for lock-free creation
- Producer clients are long-lived and thread-safe (per Azure SDK design)
- All dispatching must complete before `DisposeAsync()` to avoid `ObjectDisposedException`

**Connection Provider**:
- Either a connection string or token credential is supplied
- Used by the provisioner during startup
- Supports custom providers via `EventHubTransportConfiguration.ConnectionProvider`

---

## 4. Message Envelope

### Header Mapping Strategy

Headers are split between two AMQP locations to minimize allocations:

| Envelope Field | AMQP Location | Transport Header |
|---|---|---|
| MessageId | Properties.MessageId | (structured) |
| CorrelationId | Properties.CorrelationId | (structured) |
| ContentType | Properties.ContentType | (structured) |
| MessageType | Properties.Subject | (structured) |
| ResponseAddress | Properties.ReplyTo | (structured) |
| ConversationId | ApplicationProperties | x-conversation-id |
| CausationId | ApplicationProperties | x-causation-id |
| SourceAddress | ApplicationProperties | x-source-address |
| DestinationAddress | ApplicationProperties | x-destination-address |
| FaultAddress | ApplicationProperties | x-fault-address |
| EnclosedMessageTypes | ApplicationProperties | x-enclosed-message-types (;-delimited) |
| SentAt | ApplicationProperties | x-sent-at (Unix ms) |
| Custom Headers | ApplicationProperties | (as-is) |

### Custom Headers

- Serialized to application properties as-is
- `DateTime` and `DateTimeOffset` are converted to Unix milliseconds to work with AMQP
- On receive, all application properties not in the well-known set are added to `envelope.Headers`
- If no custom headers exist, `envelope.Headers` is `Headers.Empty()` (zero allocation)

---

## 5. Topology Model

### Topics and Subscriptions

The transport maintains two collections:

- **Topics**: `EventHubTopic` entities representing Event Hubs (queues/topics in other transports)
- **Subscriptions**: `EventHubSubscription` entities representing consumer groups listening to hubs

### Discovery and Configuration

Topics and subscriptions are configured during transport setup:

1. Via fluent API: `transport.Topic("hub-name")`, `transport.ReceiveEndpoint("hub-name")`
2. Stored in `EventHubMessagingTopology` (thread-safe via lock)
3. Retrieved by receive/dispatch endpoints during initialization

### Consumer Groups

- Default: `$Default` (always exists on every Event Hub)
- Custom: Created on demand during provisioning
- Scoped to an Event Hub and a receiving endpoint
- Multiple endpoints can listen on the same hub via different consumer groups

### Naming Convention

- Topic URI: `eventhub://{namespace}/{hub-name}`
- Subscription (consumer group): Associated with topic + consumer group name

---

## 6. Error Handling

### Dispatch Errors

**Pre-Dispatch Validation**:
- Message size > 1MB → `InvalidOperationException` (fast-fail)
- Missing hub name → `InvalidOperationException`

**Batch Send Errors**:
- All pending events in the batch are failed with the exception
- Batch is disposed

**Single Send Errors**:
- Event is failed immediately

### Receive Errors

**Per-Event Errors** (in `OnProcessingEventBatchAsync`):
- Exceptions are logged but do NOT break the batch loop
- Skipped events are NOT checkpointed
- Batch processing continues with next event
- Result: **at-least-once** delivery; failed messages will be retried

**Partition-Level Errors** (in `OnProcessingErrorAsync`):
- Logged with partition and operation context
- Does not stop processor; processor continues attempting to process

**Consumer Group Recovery**:
- Powered by checkpointing: when a processor restarts, it resumes from the last checkpoint
- Failed/skipped events are reprocessed

### No Error Endpoints

Currently, the transport does not automatically route failed messages to configured error endpoints. Failed event processing is logged, and recovery relies on the at-least-once guarantees of consumer group checkpointing.

---

## 7. Startup Sequence

### Transport Initialization (`OnAfterInitialized`)

1. **Connection Provider Resolution**:
   - Uses `configuration.ConnectionProvider` if supplied
   - Falls back to connection string if provided
   - Falls back to `FullyQualifiedNamespace` + `DefaultAzureCredential()`
   - Throws if none are configured

2. **Topology URI Construction**:
   - Builds URI: `eventhub://{fullyQualifiedNamespace}`
   - Used by topology and endpoints for lookups

3. **Topology Creation**:
   - Instantiates `EventHubMessagingTopology` with auto-provision flag
   - Adds topics and subscriptions from configuration
   - Stores reference in transport

4. **Connection Manager Creation**:
   - Singleton instance managing producer clients
   - Ready for dispatch endpoint startup

### Pre-Start Provisioning (`OnBeforeStartAsync`)

1. **Check Auto-Provision Flag**:
   - If `false`, skip provisioning (topics/subscriptions assumed to pre-exist)

2. **Check ARM Credentials**:
   - If `SubscriptionId`, `ResourceGroupName`, or `NamespaceName` are missing, skip (e.g., emulator scenarios)
   - These are needed for ARM-based provisioning

3. **Provision Topics**:
   - For each topic with `AutoProvision = true`, call `provisioner.ProvisionTopicAsync(hubName, partitionCount)`
   - Idempotent (create or update)

4. **Provision Subscriptions**:
   - For each subscription with `AutoProvision = true`, call `provisioner.ProvisionSubscriptionAsync(hubName, consumerGroupName)`
   - Skips `$Default` consumer group (always exists)
   - Idempotent

### Endpoint Startup

**Dispatch Endpoint**:
1. Resolves topic from topology
2. If batch mode is enabled, creates `EventHubBatchDispatcher` with producer
3. Ready to dispatch messages

**Receive Endpoint**:
1. Resolves checkpoint and ownership stores (or uses defaults)
2. Creates `MochaEventProcessor` with credentials
3. Calls `StartProcessingAsync()` to begin partition assignment and event consumption
4. Processor is running and processing events

---

## 8. Checkpoint Strategy

### In-Memory Checkpoint Store (Default)

- Stores last-processed sequence number per (namespace, hub, consumer group, partition) tuple
- Checkpoints are lost on process restart
- Provides **at-least-once** delivery within a process lifetime
- Useful for dev/test and single-instance scenarios

### Blob Storage Checkpoint Store

- Backed by Azure Blob Storage
- Supports distributed multi-instance scenarios
- Survives process restarts
- Enables graceful rebalancing across instances

### Partition Ownership Store

- Coordinates partition ownership across multiple processor instances
- Allows horizontal scaling: when a new instance starts, it claims partitions and rebalances
- Backed by Blob Storage or database (similar to checkpoint store)

### Checkpoint Interval

- Default: 100 messages per partition
- Configurable per receive endpoint
- Trade-off: smaller interval = more frequent disk I/O but faster recovery; larger = fewer writes but longer replay on restart

---

## Key Design Principles

### Zero-Copy Messaging

- Message bodies are passed as `ReadOnlyMemory<byte>` throughout
- No buffering or data duplication

### Allocation Minimization

- AMQP structured properties used directly (no dictionary for standard headers)
- Custom headers collected into a `Headers` instance only if present
- Batch dispatcher uses channel-based queue with minimal GC pressure

### Partition Coherency

- Batches never mix messages targeting different partitions
- Enables ordered delivery within partitions when using partition keys

### Graceful Degradation

- Single-instance mode works without ownership stores (no distributed coordination)
- Connection string mode works without ARM credentials (emulator-friendly)
- Batch mode is opt-in; defaults to single mode for lower latency

### At-Least-Once Delivery

- Checkpoint-based recovery ensures no message is lost if a processor restarts
- Skipped messages on error are reprocessed
- Applications must be idempotent or use deduplication

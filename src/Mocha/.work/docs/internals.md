# Mocha Kafka Transport: Internals and Design

This document provides a detailed exploration of the Mocha Kafka transport implementation, covering connection management, topology provisioning, message flow, error handling, and performance considerations.

## Overview

The Mocha Kafka transport (`Mocha.Transport.Kafka`) is a complete implementation of the Mocha messaging transport abstraction for Apache Kafka. It manages:

- A **shared producer** for all outbound messages (dispatch endpoints)
- **Per-endpoint consumers** for inbound messages (receive endpoints)
- **Automatic topology provisioning** for topics and consumer groups
- **Message envelope serialization** across Kafka headers and message body
- **At-least-once delivery semantics** with manual offset commits
- **Request-reply patterns** with instance-specific reply topics

---

## 1. Connection Management

### KafkaConnectionManager

**File:** `Connection/KafkaConnectionManager.cs`

The `KafkaConnectionManager` owns the lifecycle of all Kafka client instances and implements double-checked locking for thread-safe initialization.

#### Key Responsibilities

1. **Producer Lifecycle**
   - Single shared `IProducer<byte[], byte[]>` for all dispatch endpoints
   - Initialized lazily on first outbound message
   - Double-checked locking in `EnsureProducerCreated()`
   - Flushed and disposed on transport shutdown

2. **Consumer Creation**
   - Each receive endpoint creates its own consumer via `CreateConsumer()`
   - Consumers are not pooled; one per endpoint per instance
   - Each consumer has its own consumer group ID

3. **Admin Client**
   - Single shared `IAdminClient` for topology provisioning
   - Created lazily on first topology provisioning

4. **In-flight Tracking**
   - `ConcurrentDictionary<TaskCompletionSource, byte>` tracks pending dispatches
   - Enables graceful shutdown: pending messages are flushed before disposal
   - TCS instances untracked after delivery report or error

#### Producer Configuration

```csharp
BootstrapServers = configured value
LingerMs = 5                    // 5ms batching window
BatchNumMessages = 10000        // Batch by count OR time
Acks = Acks.All                // All in-sync replicas must ack
EnableIdempotence = true        // Prevent duplicate delivery
EnableDeliveryReports = true    // Receive delivery callbacks
```

**Configuration Customization:**
Users can override these defaults via `Action<ProducerConfig>?` on transport setup:

```csharp
t.ProducerConfig(config => 
{
    config.LingerMs = 10;           // Override linger time
    config.BatchNumMessages = 5000; // Override batch size
});
```

#### Consumer Configuration

```csharp
BootstrapServers = configured value
GroupId = receive endpoint specific
EnableAutoCommit = false           // Manual commits required
AutoOffsetReset = Earliest         // Start from beginning if no offset
EnablePartitionEof = false         // Don't emit EOF markers
MaxPollIntervalMs = 600_000        // 10 minute timeout for processing
```

Similar customization is available for consumers via `Action<ConsumerConfig>?`.

#### Graceful Shutdown

On `DisposeAsync()`:
1. Producer is flushed with 10-second timeout to ensure all pending messages are sent
2. Remaining in-flight TCS instances are canceled (these represent messages where delivery report never arrived)
3. Admin client is disposed
4. In-flight tracking dictionary is cleared

This ensures no message loss during graceful shutdown.

---

## 2. Topic Provisioning and Naming

### Automatic Topic Provisioning

The transport supports optional automatic topic creation (enabled by default). Topics are created:
- Before transport startup if `AutoProvision` is true
- Lazily during dispatch for dynamically-created endpoints

### Topic Configuration Hierarchy

**Order of precedence (lowest to highest):**

1. **Transport-level defaults** (`KafkaBusDefaults.Topic`)
   ```csharp
   builder.Services.AddMessageBus()
       .AddKafka(t =>
       {
           t.Defaults().Partitions(3);
           t.Defaults().ReplicationFactor((short)2);
           t.Defaults().TopicConfigs(new Dictionary<string, string>
           {
               ["retention.ms"] = "604800000",  // 7 days
               ["cleanup.policy"] = "delete"
           });
       });
   ```

2. **Explicitly declared topics** (`KafkaTopicConfiguration`)
   ```csharp
   t.DeclareTopic(new KafkaTopicConfiguration 
   { 
       Name = "my-topic",
       Partitions = 5,
       ReplicationFactor = 3
   });
   ```

3. **Convention-discovered topics** (auto-created from handlers and routes)

### Naming Conventions

The `DefaultNamingConventions` class converts .NET types to kebab-case topic/endpoint names:

#### Send Endpoints (Point-to-Point Commands)

**Pattern:** `{message-type-name}`

Examples:
- `CreateOrderCommand` → `create-order`
- `ProcessPaymentMessage` → `process-payment`

**Usage:** Used when calling `bus.SendAsync<T>()` or configuring explicit send routes.

#### Publish Endpoints (Pub/Sub Events)

**Pattern:** `{namespace}.{message-type-name}`

Examples:
- `Messages.OrderCreatedEvent` → `messages.order-created`
- `Events.PaymentProcessedEvent` → `events.payment-processed`

**Usage:** Used when calling `bus.PublishAsync<T>()` or configuring subscriptions.

#### Receive Endpoints

**Pattern:** `{service-name}.{handler-type-name}` (for subscriptions) or direct name

Examples:
- Handler `OrderShippedEventHandler` → `order-shipped` (base name)
- With service name "notification-service" → `notification-service.order-shipped`

**Kind-based naming:**
- Default: `order-shipped`
- Error: `order-shipped_error`
- Skipped: `order-shipped_skipped`
- Reply: `response-{guid-without-hyphens}`

#### Topology Convention Flow

1. **Configuration phase** (`IKafkaReceiveEndpointConfigurationConvention`)
   - `KafkaDefaultReceiveEndpointConvention` assigns topic name and consumer group ID
   - Topic name defaults to endpoint name if not explicitly set
   - Consumer group ID defaults to topic name if not explicitly set

2. **Topology discovery phase** (`IKafkaReceiveEndpointTopologyConvention`)
   - `KafkaReceiveEndpointTopologyConvention` discovers and creates missing topics
   - Creates main topic (with short retention for reply topics: 1 hour)
   - Creates error topic for default endpoints (`topic_error`)
   - Creates topics for all inbound routes' message types

3. **Dispatch endpoint topology discovery** (`IKafkaDispatchEndpointTopologyConvention`)
   - `KafkaDispatchEndpointTopologyConvention` creates topics for dispatch endpoints
   - Creates main topic if not found
   - Creates error topic if main topic isn't already an error/skipped topic

---

## 3. Consumer Groups and Partition Assignment

### Consumer Group Strategy

Each receive endpoint has its own consumer group (identified by `ConsumerGroupId`):
- **Subscribe (Pub/Sub):** Consumer group name allows multiple consumers to each receive all messages
- **Handler routes:** Handler instances within the same service share a consumer group, enabling competing consumers for load balancing
- **Reply endpoints:** Instance-specific consumer groups ensure replies route to correct instance

### Partition Assignment

The Kafka client-side consumer uses the **Range** (or **RoundRobin**) partition assignment strategy by default:
- Partitions are assigned sequentially to available consumers in the group
- When a consumer joins/leaves, rebalancing occurs
- The `SetPartitionsAssignedHandler` and `SetPartitionsRevokedHandler` track rebalancing

#### Rebalancing Behavior

```csharp
.SetPartitionsAssignedHandler((consumer, partitions) =>
    logger.KafkaPartitionsAssigned(groupId, partitions))
.SetPartitionsRevokedHandler((consumer, partitions) =>
{
    // No special action: processing is sequential, so there are no
    // in-flight messages from revoked partitions when this handler fires.
    logger.KafkaPartitionsRevoked(groupId, partitions);
})
```

**Important:** The consume loop is sequential (not concurrent), so there are no in-flight messages when partitions are revoked. This simplifies shutdown logic.

### Consumer Loop Lifecycle

```csharp
// Sequential, single-threaded consume loop
while (!cancellationToken.IsCancellationRequested)
{
    ConsumeResult<byte[], byte[]>? result;
    try
    {
        result = consumer.Consume(cancellationToken);  // Blocking call
    }
    catch (ConsumeException ex)
    {
        // Log and continue transient errors
        logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
        continue;
    }

    // Process message
    await ExecuteAsync(context =>
    {
        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
        feature.ConsumeResult = result;
        feature.Consumer = consumer;
    });
}
```

Each message is processed one at a time in the consume loop thread.

---

## 4. Message Serialization and Envelope Format

### Message Structure

Messages are split into:
- **Key:** UTF-8 encoded string (typically message ID or correlation ID)
- **Headers:** Kafka headers carrying envelope metadata
- **Body:** Raw message bytes (JSON or other format, transparent to transport)

### Well-Known Headers

The transport reserves these Kafka header keys (with `mocha-` prefix):

```
mocha-message-id            → MessageEnvelope.MessageId
mocha-correlation-id        → MessageEnvelope.CorrelationId
mocha-conversation-id       → MessageEnvelope.ConversationId
mocha-causation-id          → MessageEnvelope.CausationId
mocha-source-address        → MessageEnvelope.SourceAddress
mocha-destination-address   → MessageEnvelope.DestinationAddress
mocha-response-address      → MessageEnvelope.ResponseAddress
mocha-fault-address         → MessageEnvelope.FaultAddress
mocha-content-type          → MessageEnvelope.ContentType
mocha-message-type          → MessageEnvelope.MessageType
mocha-sent-at               → MessageEnvelope.SentAt (ISO 8601 string)
mocha-enclosed-message-types → MessageEnvelope.EnclosedMessageTypes (comma-separated)
```

All header values are UTF-8 encoded strings.

### Serialization on Dispatch

**File:** `KafkaDispatchEndpoint.cs`

When dispatching a message:

1. **Key Selection**
   ```csharp
   var keySource = envelope.CorrelationId ?? envelope.MessageId;
   byte[]? key = keySource is not null ? Encoding.UTF8.GetBytes(keySource) : null;
   ```
   - Falls back to message ID if no correlation ID
   - Ensures ordering by related messages (same correlation = same partition)

2. **Header Mapping**
   - Each well-known envelope field mapped to a Kafka header
   - Custom headers from `envelope.Headers` passed through as-is
   - Null values are skipped

3. **Body Optimization**
   ```csharp
   byte[] body;
   if (MemoryMarshal.TryGetArray(envelope.Body, out var segment)
       && segment.Offset == 0
       && segment.Count == segment.Array!.Length)
   {
       body = segment.Array;  // Avoid ToArray() copy if already backed by byte[]
   }
   else
   {
       body = envelope.Body.ToArray();
   }
   ```
   - Avoids unnecessary memory copy if body is already a full byte array
   - Zero-copy when possible

### Deserialization on Receive

**File:** `KafkaMessageEnvelopeParser.cs`

The `KafkaMessageEnvelopeParser.Instance.Parse()` singleton converts Kafka consume results to envelopes:

1. **Header Extraction**
   - Reads each well-known header and maps to envelope field
   - Parses `SentAt` as ISO 8601 DateTimeOffset
   - Splits `EnclosedMessageTypes` on comma into ImmutableArray

2. **Custom Headers**
   - Two-pass scanning: first pass counts non-well-known headers to size collection
   - Second pass populates custom headers
   - Avoids allocation if no custom headers exist
   - Returns empty `Headers` object for zero custom headers

3. **Envelope Construction**
   - All fields populated from headers and message body
   - Ready for middleware pipeline

---

## 5. Receive Pipeline and Middleware

### Middleware Execution Order

The receive pipeline is composed of per-transport and per-endpoint middlewares:

```
1. KafkaParsingMiddleware       (transport-level, auto-added)
2. ApplicationMiddlewares        (user-provided, ordered)
3. ErrorRoutingMiddleware        (base transport)
4. HandlerDispatchMiddleware     (base transport)
5. KafkaCommitMiddleware         (transport-level, auto-added)
```

### KafkaParsingMiddleware

**File:** `Middlewares/Receive/KafkaParsingMiddleware.cs`

Runs first, converts Kafka consume result to MessageEnvelope:

```csharp
public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
{
    var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
    var consumeResult = feature.ConsumeResult;
    
    var envelope = KafkaMessageEnvelopeParser.Instance.Parse(consumeResult);
    context.SetEnvelope(envelope);  // Set for downstream processing
    
    await next(context);
}
```

### KafkaCommitMiddleware

**File:** `Middlewares/Receive/KafkaCommitMiddleware.cs`

Runs last, commits offset after successful processing:

```csharp
public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
{
    var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
    
    try
    {
        await next(context);
        
        // Commit offset AFTER successful processing
        // Safe because pipeline runs on consume loop thread (sequential)
        feature.Consumer.Commit(feature.ConsumeResult);
    }
    catch
    {
        // Do NOT commit on error
        // Message will be redelivered on next poll
        throw;
    }
}
```

**At-Least-Once Semantics:**
- Offset is committed only after the message has been successfully processed or routed to error/skipped queues
- If processing fails, the offset is not committed, ensuring redelivery
- The error/skipped routing middleware catches most exceptions before commit, so the catch block handles only catastrophic failures in the error routing itself

### KafkaReceiveFeature

**File:** `Features/KafkaReceiveFeature.cs`

Pooled feature carrying Kafka context through the pipeline:

```csharp
public sealed class KafkaReceiveFeature : IPooledFeature
{
    public ConsumeResult<byte[], byte[]> ConsumeResult { get; set; }
    public IConsumer<byte[], byte[]> Consumer { get; set; }
    
    public string Topic => ConsumeResult.Topic;
    public int Partition => ConsumeResult.Partition.Value;
    public long Offset => ConsumeResult.Offset.Value;
}
```

Enables middleware to access:
- Raw message body and headers
- Consumer instance for manual offset commits
- Partition and offset metadata

---

## 6. Dispatch (Outbound) Pipeline

### Async Produce with Callback

**File:** `KafkaDispatchEndpoint.cs`

The dispatch uses `producer.Produce()` with a delivery report callback for optimal performance:

```csharp
var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
connectionManager.TrackInflight(tcs);

// Link cancellation token to TCS
await using var ctr = cancellationToken.Register(static state =>
{
    var t = (TaskCompletionSource)state!;
    t.TrySetCanceled();
}, tcs);

try
{
    producer.Produce(topicName, message, report =>
    {
        // Callback runs on librdkafka's delivery-report thread
        // Do NOT access context or features here
        if (report.Error.IsError)
        {
            tcs.TrySetException(new KafkaException(report.Error));
        }
        else
        {
            tcs.TrySetResult();
        }
        
        connectionManager.UntrackInflight(tcs);
    });
}
catch (ProduceException<byte[], byte[]> ex)
{
    tcs.TrySetException(ex);
    connectionManager.UntrackInflight(tcs);
}

await tcs.Task;
```

**Performance Optimization:**
- Uses `Produce()` callback instead of `ProduceAsync()` to avoid Task allocation per message
- TaskCompletionSource allocated per dispatch, reused if pooling implemented
- Delivery report runs on librdkafka's internal thread, not the caller's context

**Thread Safety:**
- Callback runs on librdkafka's delivery-report thread
- Cannot access `context` or features (they may be pooled/recycled)
- Only TCS is updated in the callback
- Caller awaits TCS.Task on their context

### Dynamic Topic Resolution

Reply endpoints dynamically resolve destination topic from message envelope:

```csharp
if (Kind == DispatchEndpointKind.Reply)
{
    if (!Uri.TryCreate(envelope.DestinationAddress, UriKind.Absolute, out var destinationAddress))
    {
        throw new InvalidOperationException("Destination address is not a valid URI");
    }
    
    // Extract topic name from kafka:///t/topic_name format
    var path = destinationAddress.AbsolutePath.AsSpan();
    Span<Range> ranges = stackalloc Range[2];
    var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);
    
    if (segmentCount == 2 && path[ranges[0]] is "t")
    {
        return new string(path[ranges[1]]);
    }
    
    throw new InvalidOperationException(...);
}

return Topic!.Name;
```

Regular endpoints use the bound topic directly.

### Lazy Topic Provisioning

Dispatch endpoints optionally provision topics lazily on first dispatch:

```csharp
private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
{
    if (_isProvisioned)
    {
        return;
    }
    
    var autoProvision = ((KafkaMessagingTopology)transport.Topology).AutoProvision;
    if (Topic is not null && (Topic.AutoProvision ?? autoProvision))
    {
        await transport.ConnectionManager.ProvisionTopologyAsync([Topic], cancellationToken);
    }
    
    _isProvisioned = true;
}
```

Allows dynamically-created endpoints (created at runtime for unknown message types) to provision their topics on first use.

---

## 7. Error Handling and Fault Patterns

### Error Topic Routing

The base transport's error routing middleware automatically routes failed messages to error topics:

**Pattern:** `{topic-name}_error`

Examples:
- `order-created` → `order-created_error` (for failures during handler execution)
- `create-order` → `create-order_error` (for failures during command processing)

The transport automatically creates error topics as part of topology provisioning.

### Transient Consumer Errors

Consumer errors are logged but do not break the consume loop:

```csharp
try
{
    result = consumer.Consume(cancellationToken);
}
catch (ConsumeException ex)
{
    // Log and continue -- transient errors should not kill the loop
    _logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
    continue;
}
```

Common transient errors (network blips, temporary broker unavailability) are retried automatically.

### Redelivery on Processing Failure

If a message fails processing and error routing fails, the offset is not committed:

```csharp
try
{
    await next(context);  // Error routing middleware inside
    feature.Consumer.Commit(feature.ConsumeResult);
}
catch
{
    // Do NOT commit
    throw;
}
```

The message remains unconsumed and will be redelivered on next poll, ensuring no message loss.

### Poison Message Prevention

Repeated redelivery can occur if a message always fails. Poison messages should be:
- Logged with full context (offset, partition, payload)
- Manually skipped or deleted (topic management tool)
- Or routed to dead-letter via application-specific logic

The framework does not implement automatic poison message handling; this is left to operational procedures.

---

## 8. Request-Reply Pattern

### Instance-Specific Reply Topics

Request-reply uses instance-specific reply topics derived from the host instance ID:

```csharp
public string GetInstanceEndpoint(Guid consumerId)
{
    // Format: response-{guid-without-hyphens}
    return $"response-{consumerId:N}";
}
```

Example: `response-a1b2c3d4e5f64a1b2c3d4e5f64a1b2c3d4`

### Reply Topic Configuration

Reply topics are marked as temporary with short retention:

```csharp
if (endpoint.Kind == ReceiveEndpointKind.Reply)
{
    topicConfig.TopicConfigs = new Dictionary<string, string>
    {
        ["retention.ms"] = "3600000",  // 1 hour
        ["cleanup.policy"] = "delete"
    };
}
```

Ensures old reply topics auto-delete after 1 hour, reducing broker storage.

### Request-Reply Flow

1. **Request sent:** Envelope includes `ResponseAddress` → `kafka:///t/response-{guid}`
2. **Handler processes:** Creates reply message with `DestinationAddress` from `ResponseAddress`
3. **Reply dispatch:** Endpoint resolves topic from destination address, sends to reply topic
4. **Reply receive:** Instance's reply endpoint listens on instance-specific topic
5. **Correlation:** Correlation ID matches request-reply pair

This pattern enables true request-reply semantics on Kafka without correlation IDs stored in external systems.

---

## 9. Performance Considerations

### Allocation Patterns

The transport is designed for zero or minimal allocations on hot paths:

1. **Producer batching:**
   - `LingerMs = 5` and `BatchNumMessages = 10000` batch messages efficiently
   - Reduces context switches and system calls

2. **Body copying optimization:**
   - Uses `MemoryMarshal.TryGetArray()` to detect existing byte arrays
   - Avoids `ToArray()` copy when body is already fully-backed by byte[]

3. **Header counting:**
   - Two-pass scanning in parsing middleware
   - Avoids allocation if all headers are well-known
   - Sizes custom headers collection exactly

4. **Feature pooling:**
   - `KafkaReceiveFeature` is pooled via `IPooledFeature`
   - Reset and reused across messages in the same instance
   - Reduces GC pressure

5. **TaskCompletionSource allocation:**
   - One TCS per dispatch operation
   - TODO comment suggests future IValueTaskSource pooling for high-throughput

### Configuration Knobs Affecting Performance

| Setting | Default | Impact |
|---------|---------|--------|
| `LingerMs` | 5 | Batching delay; higher = more batching, higher latency |
| `BatchNumMessages` | 10000 | Batch by count; higher = more efficient, memory-heavy |
| `MaxPollIntervalMs` | 600000 (10 min) | Max time to process single message; timeout = rebalance |
| `retention.ms` (default topics) | None (infinite) | Storage requirement; set for event topics |
| `cleanup.policy` | Default (`delete`) | Deletion vs. compaction; leave as `delete` for events |

**High-throughput tuning:**
```csharp
t.ConsumerConfig(config =>
{
    config.MaxPollIntervalMs = 1_200_000;  // 20 minutes if processing slow batches
    config.SessionTimeoutMs = 10_000;       // Rebalance sensitivity
});

t.ProducerConfig(config =>
{
    config.LingerMs = 10;       // Longer batching
    config.BatchNumMessages = 20000;
    config.BatchSize = 1024 * 1024;  // 1 MB batches
});
```

### Known Constraints

1. **Sequential Message Processing:**
   - Messages are processed one at a time in the consume loop
   - Enables simple offset management but limits concurrency
   - Partition assignment strategy determines parallelism across multiple consumers

2. **Manual Offset Commits:**
   - No automatic offset commits (safer but requires careful ordering)
   - Offset only committed after full middleware pipeline
   - If processing pipeline fails, message is redelivered

3. **No Built-in Poison Message Handling:**
   - Repeated failures on same message will cause repeated redelivery
   - Application must implement circuit breakers or manual skipping
   - Monitor error topics for poison messages

4. **Instance-Specific Reply Topics:**
   - Each instance gets its own reply topic
   - Scaling reply topics with instance count may impact broker storage
   - Short retention (1 hour) mitigates this

5. **Shared Producer Thread Safety:**
   - All dispatch endpoints share one producer
   - Produce method is thread-safe but internally queues
   - Producer.Flush() on shutdown blocks; ensure timeout is reasonable

---

## 10. Topology Resource Initialization

### Initialization Flow

```
1. Transport Creation
   ↓
2. OnAfterInitialized()
   - Resolve bootstrap servers
   - Create KafkaMessagingTopology
   - Load explicitly declared topics
   ↓
3. Receive/Dispatch Endpoint Creation (per endpoint configuration)
   - Create endpoint instance
   - OnInitialize() called
   ↓
4. Convention Application
   - IKafkaReceiveEndpointConfigurationConvention
   - IKafkaDispatchEndpointTopologyConvention
   - IKafkaReceiveEndpointTopologyConvention
   ↓
5. OnComplete() called
   - Resolve source/destination topics from topology
   - Validate topics exist
   ↓
6. OnBeforeStartAsync()
   - Ensure producer created
   - Provision topology (if AutoProvision enabled)
   ↓
7. OnStartAsync() (per endpoint)
   - Create consumer
   - Subscribe to topic
   - Start consume loop
```

### Explicit Topic Declaration

Users can declare topics upfront:

```csharp
builder.Services.AddMessageBus()
    .AddKafka(t =>
    {
        // Explicit declaration with custom settings
        t.DeclareTopic(new KafkaTopicConfiguration
        {
            Name = "orders",
            Partitions = 3,
            ReplicationFactor = 2,
            TopicConfigs = new Dictionary<string, string>
            {
                ["retention.ms"] = "604800000",  // 7 days
                ["min.insync.replicas"] = "2"
            }
        });
        
        // Convention-based topic for handler
        t.Endpoint("order-processor").Topic("orders").Handler<OrderHandler>();
    });
```

Declared topics override defaults and convention-discovered topics.

---

## 11. Configuration Examples

### Minimal Configuration

```csharp
builder.Services.AddMessageBus()
    .AddKafka(t => t.BootstrapServers("localhost:9092"));
    // AutoProvision defaults to true
    // Topics created automatically as endpoints are configured
```

### Full Configuration

```csharp
builder.Services.AddMessageBus()
    .AddKafka(t =>
    {
        // Connection
        t.BootstrapServers("kafka1:9092,kafka2:9092,kafka3:9092");
        
        // Topology
        t.AutoProvision(true);
        t.Defaults()
            .Partitions(3)
            .ReplicationFactor((short)2)
            .TopicConfigs(new Dictionary<string, string>
            {
                ["retention.ms"] = "604800000",
                ["compression.type"] = "snappy"
            });
        
        // Explicit topics
        t.DeclareTopic(new KafkaTopicConfiguration
        {
            Name = "events",
            Partitions = 5,
            ReplicationFactor = 3
        });
        
        // Producer tuning
        t.ProducerConfig(config =>
        {
            config.LingerMs = 10;
            config.BatchNumMessages = 20000;
            config.CompressionType = CompressionType.Snappy;
        });
        
        // Consumer tuning
        t.ConsumerConfig(config =>
        {
            config.MaxPollIntervalMs = 1_200_000;
            config.SessionTimeoutMs = 30_000;
        });
        
        // Endpoints
        t.Endpoint("events-processor")
            .Topic("events")
            .Handler<OrderPlacedEventHandler>();
        
        t.Endpoint("commands-processor")
            .Topic("commands")
            .Handler<ProcessOrderCommandHandler>();
        
        t.DispatchEndpoint("publish-events")
            .ToTopic("events")
            .Publish<OrderCreatedEvent>();
    });
```

### Custom Consumer Group IDs

For competing consumers pattern (load balancing), each handler instance shares a consumer group:

```csharp
// All instances of OrderProcessorHandler use same consumer group
// Messages are distributed across instances
t.Endpoint("order-processor")
    .Topic("orders")
    .ConsumerGroupId("order-processors")  // Explicit group ID
    .Handler<OrderProcessorHandler>();
```

---

## 12. Summary: Key Design Decisions

| Decision | Rationale | Tradeoff |
|----------|-----------|----------|
| **Shared Producer** | Single producer reduces connection overhead and enables efficient batching | All dispatch endpoints share resources; producer config is global |
| **Per-Endpoint Consumer** | Isolation and independent lifecycle management | More connections; not pooled |
| **Manual Offset Commits** | Precise control; commits only after successful processing | More complex; requires careful ordering in middleware |
| **Sequential Message Processing** | Simple offset management; predictable ordering | Limited concurrency; bottleneck on single partition |
| **Instance-Specific Reply Topics** | Enables true request-reply without external correlation store | Reply topic per instance; short TTL needed |
| **Automatic Topic Provisioning** | Developer convenience; works out-of-the-box | Less control; requires topology knowledge at runtime |
| **Async Produce with Callback** | Zero-copy batching; avoids Task allocation per dispatch | Callback on librdkafka thread; cannot access context |
| **Two-Pass Header Scanning** | Zero allocation for all-well-known headers | Slight overhead for first pass |

---

## 13. Testing and Debugging

### Observability

The transport logs at appropriate levels:
- **Error:** Consumer/producer errors, delivery failures
- **Debug:** Log messages from librdkafka
- **Information:** Partition assignments, group membership changes

Enable debug logging to see message flow:

```csharp
builder.Logging.AddFilter("Mocha.Transport.Kafka", LogLevel.Debug);
```

### Feature Access in Tests

Tests can access Kafka-specific context via `KafkaReceiveFeature`:

```csharp
var context = // ... receive context
var feature = context.Features.Get<KafkaReceiveFeature>();
var topic = feature.Topic;
var partition = feature.Partition;
var offset = feature.Offset;
```

### Simulating Failures

To test error handling:

1. Stop a consumer: Simulates group rebalancing
2. Pause topic: Simulates broker unavailability
3. Manually commit wrong offset: Simulates offset corruption
4. Kill producer: Simulates delivery failure

---


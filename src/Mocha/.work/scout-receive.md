# Scout: Kafka Receive Pipeline Analysis

## Summary

This document thoroughly documents the exact code involved in the Kafka receive pipeline, including the five key findings identified:

1. **Finding #1 (SetEnvelope bug)** — Headers.AddRange(Headers) copies into itself
2. **Finding #6 (MessageEnvelope intermediate)** — How the intermediate envelope is created
3. **Finding #7 (header allocations on receive)** — Where header allocations occur
4. **Finding #8 (per-message commit)** — How offsets are committed
5. **Finding #9 (partition assignment)** — How partitions are assigned

---

## Finding #1: SetEnvelope Bug — Headers.AddRange(Headers)

### Location
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs`  
**Method:** `ReceiveContext.SetEnvelope(MessageEnvelope envelope)`  
**Lines:** 192-218

### Exact Code
```csharp
public void SetEnvelope(MessageEnvelope envelope)
{
    Envelope = envelope;
    MessageId = envelope.MessageId;
    CorrelationId = envelope.CorrelationId;
    ConversationId = envelope.ConversationId;
    CausationId = envelope.CausationId;
    SourceAddress = envelope.SourceAddress.ToUri();
    DestinationAddress = envelope.DestinationAddress.ToUri();
    ResponseAddress = envelope.ResponseAddress.ToUri();
    FaultAddress = envelope.FaultAddress.ToUri();
    ContentType = MessageContentType.Parse(envelope.ContentType);
    SentAt = envelope.SentAt;
    DeliverBy = envelope.DeliverBy;
    DeliveryCount = envelope.DeliveryCount;
    Body = envelope.Body;

    if (envelope.Headers is not null)
    {
        Headers.AddRange(Headers);  // ← BUG: Copies _headers into itself!

        foreach (var header in envelope.Headers)
        {
            Headers.Set(header.Key, header.Value);
        }
    }
}
```

### The Bug
**Line 211:** `Headers.AddRange(Headers);`

This is a copy-into-self bug. The intention appears to be:
- Either: preserve existing headers before replacing them
- Or: clear headers before adding new ones

Instead, this line adds all current headers to itself, effectively duplicating them. Then it iterates over `envelope.Headers` and sets each one, which overwrites duplicates but leaves the self-copied entries.

### Impact
- Headers from a previous message (if the context was reused from the pool) are **duplicated** instead of replaced
- The overwrite via `.Set()` only overwrites keys that exist in `envelope.Headers`, leaving orphaned duplicates from previous messages
- Since `ReceiveContext` is pooled (initialized in `ReceiveEndpoint.ExecuteAsync()` at line 127 with `pools.ReceiveContext.Get()`), this is a **cross-message contamination bug**

### Root Cause
The code should either:
1. Clear headers first: `Headers.Clear();` then add envelope headers
2. Or remove the buggy line entirely since `.Set()` overwrites by key

---

## Finding #6: MessageEnvelope Intermediate Structure

### Location
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs`  
**Class:** `KafkaMessageEnvelopeParser`  
**Lines:** 1-165

### Overview
The `MessageEnvelope` is an intermediate representation created from each Kafka `ConsumeResult<byte[], byte[]>`. It carries:
- All message metadata (IDs, addresses, timestamps)
- Custom headers (non-well-known Kafka headers)
- Message body

### Message Flow
1. **Kafka ConsumeResult** (from librdkafka)
   - Contains: message key, value, headers, partition, offset, timestamp
   - Extracted in `KafkaReceiveEndpoint.ConsumeLoopAsync()` (line 90)

2. **MessageEnvelope Created** (in `KafkaParsingMiddleware`)
   - Location: `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaParsingMiddleware.cs` (line 23)
   - Created by: `KafkaMessageEnvelopeParser.Instance.Parse(consumeResult)`

3. **Envelope Set on Context** (line 25)
   - Called: `context.SetEnvelope(envelope)`
   - The bug in Finding #1 occurs here

### The Parse Method
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs`  
**Method:** `Parse()` (lines 21-44)

```csharp
public MessageEnvelope Parse(ConsumeResult<byte[], byte[]> consumeResult)
{
    var kafkaHeaders = consumeResult.Message.Headers;

    var envelope = new MessageEnvelope
    {
        MessageId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.MessageId),
        CorrelationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CorrelationId),
        ConversationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ConversationId),
        CausationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CausationId),
        SourceAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.SourceAddress),
        DestinationAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.DestinationAddress),
        ResponseAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ResponseAddress),
        FaultAddress = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.FaultAddress),
        ContentType = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ContentType),
        MessageType = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.MessageType),
        SentAt = ParseSentAt(kafkaHeaders),
        EnclosedMessageTypes = ParseEnclosedMessageTypes(kafkaHeaders),
        Headers = BuildHeaders(kafkaHeaders),
        Body = consumeResult.Message.Value ?? Array.Empty<byte>()
    };

    return envelope;
}
```

### Well-Known Headers
The parser distinguishes between:
- **Well-known headers** (extracted to envelope fields): MessageId, CorrelationId, ConversationId, CausationId, SourceAddress, DestinationAddress, ResponseAddress, FaultAddress, ContentType, MessageType, SentAt, EnclosedMessageTypes
- **Custom headers** (placed in `Envelope.Headers`)

See `_wellKnownHeaders` FrozenSet at lines 142-157.

### Header Building Strategy
**Method:** `BuildHeaders()` (lines 102-140)

Uses a two-pass approach for allocation efficiency:
1. **First pass:** Count non-well-known headers
2. **Second pass:** Only allocate `Mocha.Headers` if custom headers exist

This avoids allocation when all headers are well-known.

---

## Finding #7: Header Allocations on Receive

### Allocation Points

#### 1. ConsumeResult Headers (Confluent.Kafka)
**Source:** Kafka consumer (librdkafka)
- Confluent.Kafka library manages this
- Headers arrive as `Confluent.Kafka.Headers` collection

#### 2. MessageEnvelope.Headers Created
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs`  
**Method:** `BuildHeaders()` (lines 102-140)

```csharp
private static Mocha.Headers BuildHeaders(Confluent.Kafka.Headers? kafkaHeaders)
{
    if (kafkaHeaders is null || kafkaHeaders.Count == 0)
    {
        return Mocha.Headers.Empty();
    }

    // First pass: count non-well-known headers
    var customCount = 0;
    foreach (var header in kafkaHeaders)
    {
        if (!IsWellKnownHeader(header.Key))
        {
            customCount++;
        }
    }

    if (customCount == 0)
    {
        return Mocha.Headers.Empty();
    }

    // Second pass: populate custom headers
    var result = new Mocha.Headers(customCount);  // ← Allocation #1
    foreach (var header in kafkaHeaders)
    {
        if (IsWellKnownHeader(header.Key))
        {
            continue;
        }

        if (header.GetValueBytes() is { } bytes)
        {
            result.Set(header.Key, Encoding.UTF8.GetString(bytes));  // ← UTF-8 decode allocation #2
        }
    }

    return result;
}
```

**Allocations:**
1. **`new Mocha.Headers(customCount)`** — Only if custom headers exist
2. **`Encoding.UTF8.GetString(bytes)`** — For each custom header value

#### 3. ReceiveContext Headers Pool
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs`  
**Lines:** 19

```csharp
private readonly Headers _headers = new();
```

Each `ReceiveContext` has its own pooled `_headers` collection.

#### 4. Context.SetEnvelope Reuses Pooled Headers
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs`  
**Lines:** 209-217

After the bug (line 211), envelope headers are merged into the pooled headers collection via `.Set()` method.

### Summary of Allocation Sites
| Site | Frequency | Size | Avoidable? |
|------|-----------|------|-----------|
| `new Mocha.Headers(customCount)` | Per message with custom headers | O(custom_header_count) | Yes, if headers could be stored as a view |
| `Encoding.UTF8.GetString()` | Per custom header | O(header_value_length) | Yes, if we could use Span<byte> throughout |
| Pooled `ReceiveContext._headers` | Per message | O(headers_after_reset) | No, but could be zero-allocation with proper clearing |

---

## Finding #8: Per-Message Offset Commit

### Commit Timing
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaCommitMiddleware.cs`  
**Method:** `InvokeAsync()` (lines 19-40)

```csharp
public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
{
    var feature = context.Features.GetOrSet<KafkaReceiveFeature>();

    try
    {
        await next(context);

        // Commit offset after successful processing (or successful error routing).
        // This is a synchronous call into librdkafka. Safe because the pipeline
        // runs on the consume loop thread (sequential processing).
        feature.Consumer.Commit(feature.ConsumeResult);
    }
    catch
    {
        // Do NOT commit -- message will be redelivered.
        // In practice, the fault handling middleware upstream catches most exceptions
        // and routes to the error topic, so this catch handles only catastrophic
        // failures in the error routing itself.
        throw;
    }
}
```

### Commit Semantics
- **At-least-once delivery:** Offset is committed **after** successful message processing
- **Failure handling:** On exception, offset is **not** committed, message is redelivered
- **Synchronous commit:** `feature.Consumer.Commit()` is a blocking call to librdkafka
- **Thread-safe:** Safe to call synchronously because the consume loop (where this runs) processes messages sequentially

### Consume Loop Context
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs`  
**Method:** `ConsumeLoopAsync()` (lines 79-126)

```csharp
private async Task ConsumeLoopAsync(
    IConsumer<byte[], byte[]> consumer,
    CancellationToken cancellationToken)
{
    try
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            ConsumeResult<byte[], byte[]>? result;
            try
            {
                result = consumer.Consume(cancellationToken);  // ← Blocks until message available
            }
            catch (ConsumeException ex)
            {
                _logger.KafkaConsumeError(ConsumerGroupId, ex.Error.Reason);
                continue;
            }

            if (result is null)
            {
                continue;
            }

            try
            {
                await ExecuteAsync(
                    static (context, state) =>
                    {
                        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
                        feature.ConsumeResult = state.result;
                        feature.Consumer = state.consumer;
                    },
                    (result, consumer),
                    cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.KafkaConsumeLoopError(ConsumerGroupId, ex);
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected on shutdown
    }
}
```

### Commit Frequency
- **One commit per message** — After the entire middleware pipeline and consumer handlers complete
- **Pipeline order** (from outermost to innermost):
  1. Transport Circuit Breaker
  2. Concurrency Limiter
  3. Instrumentation
  4. Circuit Breaker
  5. Dead Letter
  6. Fault Handler
  7. Expiry
  8. Message Type Selection
  9. Routing → Consumer Handlers
  10. **KafkaCommitMiddleware** (commits after all above complete)
  11. **KafkaParsingMiddleware** (parses ConsumeResult into MessageEnvelope)

Wait, this order seems reversed. Let me check how middleware is registered and compiled...

### Middleware Order Analysis

From `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Topology/Extensions/KafkaTransportDescriptorExtensions.cs` (lines 10-23):

```csharp
internal static IKafkaMessagingTransportDescriptor AddDefaults(
    this IKafkaMessagingTransportDescriptor descriptor)
{
    descriptor.AddConvention(new KafkaDefaultReceiveEndpointConvention());
    descriptor.AddConvention(new KafkaReceiveEndpointTopologyConvention());
    descriptor.AddConvention(new KafkaDispatchEndpointTopologyConvention());

    descriptor
        .UseReceive(KafkaReceiveMiddlewares.Commit, after: ReceiveMiddlewares.ConcurrencyLimiter.Key);
    descriptor
        .UseReceive(KafkaReceiveMiddlewares.Parsing, after: KafkaReceiveMiddlewares.Commit.Key);

    return descriptor;
}
```

**Registration order (execution order is REVERSED by MiddlewareCompiler):**
1. `Commit` is registered **after** `ConcurrencyLimiter`
2. `Parsing` is registered **after** `Commit`

From `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/MiddlewareCompiler.cs` (lines 63-101):
- Middlewares are **reversed** before folding (line 86: `middlewares.Reverse();`)
- Then folded right-to-left (lines 90-94)

**Actual execution order (reversed):**
1. **Parsing** (innermost) — parses Kafka ConsumeResult into MessageEnvelope
2. **Commit** — commits offset after next() returns
3. **ConcurrencyLimiter** and all others (outermost)

This means:
- **Parsing happens first** (extracts envelope from ConsumeResult)
- **Then all other middleware runs** (consumer handlers, fault routing, etc.)
- **Finally Commit** (commits offset after entire pipeline succeeds)

### Error Routing and Commit
From the comments in `KafkaCommitMiddleware.cs`:
> "Commit offset after successful processing (or successful error routing)."

This implies:
- If a message handler throws, the fault middleware catches it and routes to error topic
- This is considered "successful error routing" — the offset is committed
- Only if fault routing itself throws is the offset not committed

---

## Finding #9: Partition Assignment

### Consumer Configuration
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Connection/KafkaConnectionManager.cs`  
**Method:** `CreateConsumer()` (lines 94-124)

```csharp
public IConsumer<byte[], byte[]> CreateConsumer(
    string groupId,
    ILogger logger)
{
    var config = new ConsumerConfig
    {
        BootstrapServers = _bootstrapServers,
        GroupId = groupId,
        EnableAutoCommit = false,
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnablePartitionEof = false,
        MaxPollIntervalMs = 600_000 // 10 minutes
    };

    _consumerConfigOverrides?.Invoke(config);

    return new ConsumerBuilder<byte[], byte[]>(config)
        .SetKeyDeserializer(Deserializers.ByteArray)
        .SetValueDeserializer(Deserializers.ByteArray)
        .SetErrorHandler((_, e) => logger.KafkaConsumerError(groupId, e.Reason))
        .SetLogHandler((_, msg) => logger.KafkaConsumerLog(groupId, msg.Message))
        .SetPartitionsAssignedHandler((consumer, partitions) =>
            logger.KafkaPartitionsAssigned(groupId, partitions))
        .SetPartitionsRevokedHandler((consumer, partitions) =>
        {
            // No special action needed: processing is sequential, so there are no
            // in-flight messages from revoked partitions when this handler fires.
            logger.KafkaPartitionsRevoked(groupId, partitions);
        })
        .Build();
}
```

### Key Configuration Points

#### PartitionAssignmentStrategy (Not Explicitly Set)
- **Default:** Confluent.Kafka library uses Kafka broker's default strategy (typically `RoundRobin` or `Range` depending on broker version)
- **No explicit override:** The code does not set `PartitionAssignmentStrategy` in `ConsumerConfig`
- **User can override:** `_consumerConfigOverrides` callback (passed to constructor) allows configuration override

#### Other Consumer Settings
| Setting | Value | Reason |
|---------|-------|--------|
| `EnableAutoCommit` | `false` | Manual offset commit via `KafkaCommitMiddleware` |
| `AutoOffsetReset` | `Earliest` | Start from beginning if no committed offset |
| `EnablePartitionEof` | `false` | Don't emit EOF signals between polls |
| `MaxPollIntervalMs` | `600_000` (10 min) | Allow long message processing without timeout |

#### Partition Assignment Handlers
**SetPartitionsAssignedHandler** (line 115):
- Logs partition assignment for observability
- No special initialization needed

**SetPartitionsRevokedHandler** (line 117):
- Logs partition revocation for observability
- **No explicit offset commit** (Confluent.Kafka handles revocation automatically)
- Comment notes: Sequential processing means no in-flight messages from revoked partitions

### How Partitions Are Consumed
**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs`  
**Method:** `OnStartAsync()` (lines 56-77)

```csharp
protected override ValueTask OnStartAsync(
    IMessagingRuntimeContext context,
    CancellationToken cancellationToken)
{
    if (Transport is not KafkaMessagingTransport kafkaTransport)
    {
        throw new InvalidOperationException("Transport is not a KafkaMessagingTransport");
    }

    _logger = context.Services.GetRequiredService<ILogger<KafkaReceiveEndpoint>>();
    _cts = new CancellationTokenSource();
    _consumer = kafkaTransport.ConnectionManager.CreateConsumer(ConsumerGroupId, _logger);
    _consumer.Subscribe(Topic.Name);  // ← Subscribe to topic by name

    _consumeLoopTask = Task.Factory.StartNew(
        () => ConsumeLoopAsync(_consumer, _cts.Token),
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default).Unwrap();

    return ValueTask.CompletedTask;
}
```

**Key points:**
1. One consumer per receive endpoint
2. One consumer group per endpoint (GroupId = endpoint name)
3. Consumer subscribes to **one topic**
4. Kafka broker automatically assigns partitions to the consumer
5. All partitions for that topic are delivered to this consumer (since only one consumer in the group)

### Partition Assignment Flow
1. Consumer calls `Subscribe(topicName)` (line 68)
2. Kafka broker detects new consumer in group
3. Broker triggers rebalancing
4. `SetPartitionsAssignedHandler` is called with assigned partitions
5. Consumer begins polling with `Consume(cancellationToken)` (line 90 in ConsumeLoopAsync)
6. librdkafka internally manages round-robin fetch across assigned partitions

---

## Pipeline Infrastructure

### Receive Pipeline Order
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/MiddlewareCompiler.cs`

The pipeline is compiled by:
1. Collecting all middleware configurations from transport and endpoint
2. Applying any pipeline modifiers
3. **Reversing the list** (so first-configured becomes outermost)
4. Folding right-to-left to create nested delegates

**Result:** Execution order is opposite of registration order.

### Default Pipeline (Endpoint Terminator)
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs` (lines 351-369)

```csharp
private static async ValueTask DefaultPipeline(IReceiveContext context)
{
    var feature = context.Features.GetOrSet<ReceiveConsumerFeature>();
    var consumers = feature.Consumers;

    foreach (var consumer in consumers)
    {
        try
        {
            feature.CurrentConsumer = consumer;
            await consumer.ProcessAsync(context);
            feature.MessageConsumed = true;
        }
        finally
        {
            feature.CurrentConsumer = null;
        }
    }
}
```

This is the innermost pipeline terminator that:
1. Gets all consumers registered for this endpoint
2. Invokes each consumer's `ProcessAsync(context)`
3. Sets `MessageConsumed = true` flag

### Message Context Lifecycle
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs` (lines 115-150)

```csharp
public async ValueTask ExecuteAsync<TState>(
    Action<ReceiveContext, TState> configure,
    TState state,
    CancellationToken cancellationToken)
{
    var logger = _runtimeState!.Logger;
    var services = _runtimeState!.ServiceProvider;
    var pools = _runtimeState.Pools;
    var lazyRuntime = _runtimeState.LazyRuntime;

    await using var scope = services.CreateAsyncScope();

    var context = pools.ReceiveContext.Get();  // ← Get from pool
    try
    {
        context.Initialize(scope.ServiceProvider, this, lazyRuntime.Runtime, cancellationToken);

        configure(context, state);  // ← Called from ConsumeLoopAsync to set ConsumeResult

        var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
        accessor.Context = context;

        await _pipeline(context);  // ← Execute the compiled pipeline
    }
    catch (Exception ex)
    {
        // exceptions should technically never bubble up here.
        logger.LogCritical(ex, "Error processing message");
    }
    finally
    {
        var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
        accessor.Context = null;
        pools.ReceiveContext.Return(context);  // ← Return to pool
    }
}
```

**Key observations:**
1. `ReceiveContext` is pooled for reuse
2. Each message gets a **scoped service provider** (`services.CreateAsyncScope()`)
3. Context is initialized with the scope before pipeline execution
4. Pipeline executes (middleware chain)
5. Context is reset and returned to pool

### Context Reset
**File:** `/workspaces/hc3/src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs` (lines 155-180)

```csharp
public void Reset()
{
    Services = null!;
    Runtime = null!;
    Transport = null!;
    Endpoint = null!;
    Envelope = null!;
    ContentType = null!;
    MessageType = null!;
    MessageId = null!;
    CorrelationId = null!;
    ConversationId = null!;
    CausationId = null!;
    SourceAddress = null!;
    DestinationAddress = null!;
    ResponseAddress = null!;
    FaultAddress = null!;
    SentAt = DateTimeOffset.UtcNow;
    DeliverBy = null;
    DeliveryCount = null;
    Body = Array.Empty<byte>();
    Host = null!;
    CancellationToken = CancellationToken.None;
    _headers.Clear();  // ← Clears headers
    _features.Reset();
}
```

After message processing, the context is **reset** before being returned to the pool.

---

## KafkaReceiveFeature — Pooled Feature

**File:** `/workspaces/hc3/src/Mocha/src/Mocha.Transport.Kafka/Features/KafkaReceiveFeature.cs`

```csharp
public sealed class KafkaReceiveFeature : IPooledFeature
{
    public ConsumeResult<byte[], byte[]> ConsumeResult { get; set; } = null!;
    public IConsumer<byte[], byte[]> Consumer { get; set; } = null!;

    public string Topic => ConsumeResult.Topic;
    public int Partition => ConsumeResult.Partition.Value;
    public long Offset => ConsumeResult.Offset.Value;

    public void Initialize(object state)
    {
        ConsumeResult = null!;
        Consumer = null!;
    }

    public void Reset()
    {
        ConsumeResult = null!;
        Consumer = null!;
    }
}
```

**Purpose:**
- Carries the raw Kafka `ConsumeResult` through the pipeline
- Also carries the consumer instance for manual offset commits
- Accessed by both `KafkaParsingMiddleware` (to parse) and `KafkaCommitMiddleware` (to commit)

**Pooling:**
- Implements `IPooledFeature` for zero-allocation reuse
- Initialized/reset by the feature collection pool

---

## Retry Middleware — Not Present

A search for retry-related code found:
- `BatchRetryExceededException` (in batch consumer context, not receive pipeline)
- **No general retry middleware** in the receive pipeline

The framework provides:
- **Fault handling:** Routes exceptions to error topic/endpoint
- **Dead letter:** Routes unprocessable messages to dead-letter
- **Circuit breaker:** Stops processing after repeated failures

But no automatic retry with backoff. Retries must be implemented via:
1. Send/publish to retry queue from error handler
2. Or dead-letter handler that republishes

---

## Summary Table

| Finding | File | Key Lines | Issue |
|---------|------|-----------|-------|
| #1: SetEnvelope Bug | ReceiveContext.cs | 211 | `Headers.AddRange(Headers)` — self-copy bug |
| #6: MessageEnvelope | KafkaMessageEnvelopeParser.cs | 21-44 | Intermediate structure, two-pass header building |
| #7: Header Allocations | KafkaMessageEnvelopeParser.cs, ReceiveContext.cs | 125, 135 | `new Mocha.Headers()`, `UTF8.GetString()` allocations |
| #8: Per-Message Commit | KafkaCommitMiddleware.cs | 30 | Synchronous `Consumer.Commit()` after successful pipeline |
| #9: Partition Assignment | KafkaConnectionManager.cs | 98-124 | No explicit strategy set; uses Kafka broker default |

---

## Key Implementation Details

### Consumer Group = Endpoint Name
Each receive endpoint creates a unique consumer group (equal to the endpoint name), ensuring that each endpoint independently manages partition assignment and offsets.

### Sequential Message Processing
The consume loop processes messages sequentially (one at a time), making synchronous offset commits safe and eliminating in-flight message concerns during rebalancing.

### MaxPollIntervalMs = 10 minutes
This generous timeout (600,000 ms) allows message processing and error routing to complete without triggering consumer eviction during normal operation.

### No Explicit PartitionAssignmentStrategy
The code allows configuration via `ConsumerConfig` overrides, but does not mandate a specific strategy. The Kafka broker's default applies.

### At-Least-Once Semantics
- Offset committed only after successful message processing (including error routing)
- Commit failures are logged as exceptions
- Message redelivery on processing failure


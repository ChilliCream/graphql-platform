# Pipeline-Centric Refactor Approach

## Design Philosophy

Findings #1, #3, #4, #6, #7, #8 all touch the receive/dispatch hot paths. Rather than applying 12 isolated patches, this approach redesigns the two pipelines cohesively, then addresses topology fixes as a separate, low-risk workstream.

The core insight: the receive pipeline currently creates a `MessageEnvelope` intermediate, copies it into `ReceiveContext`, then discards it -- 20+ allocations per message with a self-copy bug in the merge step. The dispatch pipeline allocates a `TaskCompletionSource` + 12+ `byte[]` headers per message. Both can be dramatically simplified.

---

## Workstream 1: Receive Pipeline Redesign

### What changes

#### 1a. Eliminate MessageEnvelope intermediate (Finding #6, partially #7)

**Before:**
```
ConsumeResult -> KafkaMessageEnvelopeParser.Parse() -> new MessageEnvelope -> context.SetEnvelope(envelope) -> copy all fields -> envelope becomes garbage
```

**After:**
```
ConsumeResult -> KafkaParsingMiddleware populates ReceiveContext directly -> no intermediate
```

**Implementation:**

Replace `KafkaParsingMiddleware` so it populates `ReceiveContext` directly from the `ConsumeResult` instead of going through `MessageEnvelope`:

```csharp
// KafkaParsingMiddleware.InvokeAsync (new)
public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
{
    var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
    var consumeResult = feature.ConsumeResult;
    var kafkaHeaders = consumeResult.Message.Headers;

    // Populate context directly -- no intermediate MessageEnvelope
    context.MessageId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.MessageId);
    context.CorrelationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CorrelationId);
    context.ConversationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.ConversationId);
    context.CausationId = GetHeaderString(kafkaHeaders, KafkaMessageHeaders.CausationId);
    context.SourceAddress = GetHeaderUri(kafkaHeaders, KafkaMessageHeaders.SourceAddress);
    context.DestinationAddress = GetHeaderUri(kafkaHeaders, KafkaMessageHeaders.DestinationAddress);
    context.ResponseAddress = GetHeaderUri(kafkaHeaders, KafkaMessageHeaders.ResponseAddress);
    context.FaultAddress = GetHeaderUri(kafkaHeaders, KafkaMessageHeaders.FaultAddress);
    context.ContentType = ParseContentType(kafkaHeaders);
    context.SentAt = ParseSentAt(kafkaHeaders);
    context.Body = consumeResult.Message.Value ?? Array.Empty<byte>();

    // Parse custom headers directly into context.Headers
    PopulateCustomHeaders(context.Headers, kafkaHeaders);

    // Build a lightweight envelope for downstream middleware that reads context.Envelope
    // (e.g., ReceiveFaultMiddleware forwards envelope to error endpoint)
    context.Envelope = BuildLazyEnvelope(context, kafkaHeaders);

    await next(context);
}
```

**Key decisions:**
- `ReceiveContext` properties are set directly. No `new MessageEnvelope()`, no `new Headers(customCount)` from the parser.
- Custom headers are written directly into the pooled `ReceiveContext._headers` collection via `PopulateCustomHeaders`, which iterates Kafka headers once (single-pass, no count-then-populate).
- A lightweight `MessageEnvelope` is still built for `context.Envelope` because downstream middleware (`ReceiveFaultMiddleware.SendToErrorEndpointAsync`) reads it to forward to error topics. This envelope reuses the strings already parsed onto the context -- no additional allocations. Alternatively, the fault middleware could be updated to read from `IReceiveContext` instead of `context.Envelope`, which would eliminate the envelope entirely. That's a follow-up optimization.

**Uri caching:** The `GetHeaderUri` method caches `Uri` instances in a `ConcurrentDictionary<string, Uri>` since endpoint addresses are a small, finite set. This eliminates 4 `Uri` allocations per message.

**Files changed:**
- `KafkaParsingMiddleware.cs` -- rewrite to populate context directly
- `KafkaMessageEnvelopeParser.cs` -- can be kept for backward compat or deleted; the parsing logic moves into the middleware
- `ReceiveContext.cs` -- no structural changes needed; properties are already publicly settable

**Allocations eliminated per receive:** ~15 (1 MessageEnvelope + 1 Headers + 1 List<HeaderValue> + 12 strings that were duplicated between envelope and context)

#### 1b. Fix SetEnvelope self-copy bug (Finding #1)

**The bug:** `ReceiveContext.SetEnvelope` line 211: `Headers.AddRange(Headers)` copies the context's own headers into itself.

**Fix:** Remove the line entirely. The `foreach` loop below already handles merging envelope headers into the context via `.Set()`. Even though the new pipeline bypasses `SetEnvelope` for Kafka, `SetEnvelope` is still used by InMemory transport and potentially other transports, so the bug must be fixed.

```csharp
// Before:
if (envelope.Headers is not null)
{
    Headers.AddRange(Headers);  // BUG: self-copy
    foreach (var header in envelope.Headers)
    {
        Headers.Set(header.Key, header.Value);
    }
}

// After:
if (envelope.Headers is not null)
{
    foreach (var header in envelope.Headers)
    {
        Headers.Set(header.Key, header.Value);
    }
}
```

**Files changed:** `ReceiveContext.cs` (1 line removed)

#### 1c. Add retry middleware (Finding #3)

**New file:** `ReceiveRetryMiddleware.cs` in `Mocha/Middlewares/Receive/Retry/`

**Pipeline position:** Between `CircuitBreaker` and `Fault` in the receive pipeline. This means:
- Circuit breaker monitors failure rate across all messages (including retried ones)
- Retry wraps the inner pipeline (DeadLetter -> Fault -> Expiry -> MessageTypeSelection -> Routing -> Consumer)
- If all retries exhaust, the exception propagates to `Fault`, which routes to error topic
- The circuit breaker sees the final failure, not each retry attempt

**Registration order (execution is outermost-first after reversal):**
```
TransportCircuitBreaker -> ConcurrencyLimiter -> Instrumentation -> CircuitBreaker -> Retry (NEW) -> DeadLetter -> Fault -> Expiry -> MessageTypeSelection -> Routing
```

**Implementation pattern:**

```csharp
public sealed class ReceiveRetryMiddleware(
    int maxRetries,
    TimeSpan initialDelay,
    TimeProvider timeProvider)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                context.DeliveryCount = attempt + 1;
                await next(context);
                return;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                attempt++;
                var delay = CalculateDelay(attempt, initialDelay);
                await Task.Delay(delay, timeProvider, context.CancellationToken);
            }
        }
    }

    private static TimeSpan CalculateDelay(int attempt, TimeSpan initialDelay)
    {
        // Exponential backoff with jitter, capped at 30 seconds
        var baseMs = initialDelay.TotalMilliseconds * Math.Pow(2, attempt - 1);
        var jitter = Random.Shared.NextDouble() * 0.2 * baseMs;
        return TimeSpan.FromMilliseconds(Math.Min(baseMs + jitter, 30_000));
    }
}
```

**Configuration:** Via `RetryFeature` following the existing feature cascade pattern (endpoint -> transport -> bus):

```csharp
public sealed class RetryFeature
{
    public bool? Enabled { get; set; }
    public int? MaxRetries { get; set; }
    public TimeSpan? InitialDelay { get; set; }
}
```

**Defaults:** `MaxRetries = 3`, `InitialDelay = 100ms`, `Enabled = true`

**Interaction with error routing:** After all retries exhaust, the exception propagates past the retry middleware. The `ReceiveFaultMiddleware` catches it and routes to the error topic. The `DeliveryCount` header reflects the total attempts, so error topic consumers can see how many times the message was tried.

**Interaction with circuit breaker:** The circuit breaker sits *outside* retry. It sees the final outcome: if retry succeeds, the circuit breaker sees success. If all retries fail, it sees one failure. This prevents retry storms from immediately tripping the breaker.

**Files added:**
- `Mocha/Middlewares/Receive/Retry/ReceiveRetryMiddleware.cs`
- `Mocha/Middlewares/Receive/Retry/RetryFeature.cs`

**Files changed:**
- `ReceiveMiddlewares.cs` -- add `Retry` entry
- `MessagingDefaults.cs` or equivalent -- register retry in default pipeline

#### 1d. Concurrent processing via channel-based pattern (Finding #4)

**Problem:** `KafkaReceiveEndpoint.ConsumeLoopAsync` processes messages sequentially: `consumer.Consume()` -> `await ExecuteAsync()` -> loop. Throughput is limited to one message at a time per consumer.

**Solution:** Decouple the Kafka consume loop from message processing using a `Channel<ConsumeResult>`, following the same pattern as `InMemoryReceiveEndpoint` + `ChannelProcessor<T>`.

**Architecture:**

```
Kafka Consumer Thread (single)          Processing Workers (N)
┌──────────────────────────┐           ┌──────────────────────┐
│ while (!cancelled)       │           │ Worker 1:            │
│   result = Consume()     │──write──> │   await ExecuteAsync │
│   channel.Writer.Write() │           │   StoreOffset()      │
│                          │           ├──────────────────────┤
│                          │           │ Worker 2:            │
│                          │           │   await ExecuteAsync │
│                          │           │   StoreOffset()      │
│                          │           ├──────────────────────┤
│                          │           │ Worker N:            │
│                          │           │   await ExecuteAsync │
│                          │           │   StoreOffset()      │
└──────────────────────────┘           └──────────────────────┘
                                              │
                                     Periodic Commit Timer
                                     ┌──────────────────────┐
                                     │ Every 100ms:         │
                                     │   Commit(maxOffset)  │
                                     └──────────────────────┘
```

**Key design decisions:**

1. **Single consumer, multiple workers.** Kafka's `IConsumer<K,V>` is not thread-safe. The consume loop stays single-threaded. Messages are dispatched to N workers via a bounded channel. This is the same approach MassTransit uses for Kafka concurrency.

2. **Bounded channel for back-pressure.** `Channel.CreateBounded<ConsumeResult>(capacity)` with `capacity = maxConcurrency * 2`. When the channel is full, the consume loop blocks on `WriteAsync`, providing natural back-pressure to Kafka (the consumer stops polling, which is safe within `MaxPollIntervalMs`).

3. **Offset management changes.** With concurrent processing, per-message synchronous `Commit()` is no longer correct because messages complete out of order. Instead:
   - Each worker calls `consumer.StoreOffset(consumeResult)` after successful processing (thread-safe in librdkafka)
   - A periodic timer calls `consumer.Commit()` every 100ms to flush stored offsets
   - On failure (exception not caught by fault middleware), the offset is NOT stored, so it will be reprocessed after the next commit
   - This trades "reprocess at most 1 message on crash" for "reprocess at most 100ms worth of messages on crash" -- an acceptable trade for concurrent throughput

4. **MaxConcurrency configuration.** Follows the same pattern as InMemory: `KafkaReceiveEndpointConfiguration.MaxConcurrency` with default `1` (preserving current sequential behavior). Users opt in to concurrency.

**Implementation sketch for KafkaReceiveEndpoint:**

```csharp
// New fields
private Channel<ConsumeResult<byte[], byte[]>>? _channel;
private Task? _consumeLoopTask;
private Task[]? _workerTasks;
private Timer? _commitTimer;

protected override ValueTask OnStartAsync(...)
{
    var maxConcurrency = _maxConcurrency; // from configuration, default 1

    if (maxConcurrency <= 1)
    {
        // Preserve existing sequential behavior
        _consumeLoopTask = Task.Factory.StartNew(
            () => SequentialConsumeLoopAsync(_consumer, _cts.Token),
            ...);
    }
    else
    {
        _channel = Channel.CreateBounded<ConsumeResult<byte[], byte[]>>(
            new BoundedChannelOptions(maxConcurrency * 2)
            {
                SingleWriter = true
            });

        _consumeLoopTask = Task.Factory.StartNew(
            () => ProducerLoopAsync(_consumer, _channel.Writer, _cts.Token),
            ...);

        _workerTasks = new Task[maxConcurrency];
        for (var i = 0; i < maxConcurrency; i++)
        {
            _workerTasks[i] = Task.Run(
                () => WorkerLoopAsync(_consumer, _channel.Reader, _cts.Token));
        }

        // Periodic commit every 100ms
        _commitTimer = new Timer(_ => TryCommitStoredOffsets(), null,
            TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(100));
    }

    return ValueTask.CompletedTask;
}
```

**Rebalance handling with concurrent processing:**

The `PartitionsRevokedHandler` must now commit stored offsets for revoked partitions before they are reassigned:

```csharp
.SetPartitionsRevokedHandler((consumer, partitions) =>
{
    // With concurrent processing, there may be in-flight messages.
    // Commit whatever offsets have been stored so far.
    try
    {
        consumer.Commit();
    }
    catch (KafkaException)
    {
        // Best-effort
    }
    logger.KafkaPartitionsRevoked(groupId, partitions);
})
```

**Files changed:**
- `KafkaReceiveEndpoint.cs` -- add channel-based concurrent processing path
- `KafkaReceiveEndpointConfiguration.cs` -- add `MaxConcurrency` property
- `KafkaConnectionManager.cs` -- update rebalance handler (may need callback injection)

**Files changed (commit middleware):**
- `KafkaCommitMiddleware.cs` -- split into two modes: synchronous commit (maxConcurrency=1) and store-offset (maxConcurrency>1)

#### 1e. Batch offset commits (Finding #8)

This is addressed as part of 1d above. When `MaxConcurrency > 1`, the commit middleware uses `StoreOffset` + periodic `Commit()`. When `MaxConcurrency = 1` (default), the existing synchronous per-message `Commit()` is preserved for maximum safety.

---

## Workstream 2: Dispatch Pipeline Optimization

#### 2a. Pool IValueTaskSource for dispatch (Finding #5)

**Problem:** Every `DispatchAsync` allocates `new TaskCompletionSource()` + accesses `.Task` (another allocation). At 100K+ msg/s this is the primary GC pressure source on the dispatch path.

**Implementation:** Create a `PooledDeliveryPromise` that implements `IValueTaskSource` backed by `ManualResetValueTaskSourceCore<bool>`:

```csharp
internal sealed class PooledDeliveryPromise : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<bool> _core;
    private static readonly ObjectPool<PooledDeliveryPromise> s_pool =
        ObjectPool.Create<PooledDeliveryPromise>();

    public static PooledDeliveryPromise Rent() => s_pool.Get();

    public void SetResult() => _core.SetResult(true);
    public void SetException(Exception ex) => _core.SetException(ex);
    public void SetCanceled() => _core.SetException(new OperationCanceledException());

    public ValueTask AsValueTask() => new(this, _core.Version);

    void IValueTaskSource.GetResult(short token)
    {
        try
        {
            _core.GetResult(token);
        }
        finally
        {
            _core.Reset();
            s_pool.Return(this);
        }
    }

    ValueTaskSourceStatus IValueTaskSource.GetStatus(short token)
        => _core.GetStatus(token);

    void IValueTaskSource.OnCompleted(Action<object?> continuation, object? state,
        short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}
```

**Changes to KafkaDispatchEndpoint.DispatchAsync:**

```csharp
// Before:
var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
connectionManager.TrackInflight(tcs);
await using var ctr = cancellationToken.Register(..., tcs);
producer.Produce(topicName, message, report => { tcs.TrySetResult(); ... });
await tcs.Task;

// After:
var promise = PooledDeliveryPromise.Rent();
connectionManager.TrackInflight(promise);

if (cancellationToken.CanBeCanceled)
{
    await using var ctr = cancellationToken.Register(
        static state => ((PooledDeliveryPromise)state!).SetCanceled(), promise);

    producer.Produce(topicName, message, report =>
    {
        if (report.Error.IsError)
            promise.SetException(new KafkaException(report.Error));
        else
            promise.SetResult();
        connectionManager.UntrackInflight(promise);
    });

    await promise.AsValueTask();
}
else
{
    producer.Produce(topicName, message, report =>
    {
        if (report.Error.IsError)
            promise.SetException(new KafkaException(report.Error));
        else
            promise.SetResult();
        connectionManager.UntrackInflight(promise);
    });

    await promise.AsValueTask();
}
```

**Inflight tracking change:** `ConcurrentDictionary<TaskCompletionSource, byte>` becomes `ConcurrentDictionary<PooledDeliveryPromise, byte>`. The shutdown path calls `promise.SetCanceled()` instead of `tcs.TrySetCanceled()`.

**Allocations eliminated per dispatch:** 2 (TaskCompletionSource + Task). The `CanBeCanceled` check also avoids the `CancellationTokenRegistration` allocation when the token is `CancellationToken.None` (common in fire-and-forget scenarios).

**Files added:**
- `Mocha.Transport.Kafka/PooledDeliveryPromise.cs`

**Files changed:**
- `KafkaDispatchEndpoint.cs` -- replace TCS with pooled promise
- `KafkaConnectionManager.cs` -- update inflight tracking type and shutdown logic

#### 2b. Reduce header byte[] allocations (Finding #7)

**Problem:** `BuildKafkaHeaders` calls `Encoding.UTF8.GetBytes()` 12+ times per dispatch, each allocating a new `byte[]`.

**Constraint:** Confluent.Kafka's `Headers.Add(string key, byte[] value)` takes ownership of the `byte[]`. We cannot use pooled arrays because the Kafka client retains the reference until the message is delivered. However, we can reduce the number of distinct allocations.

**Approach: Single shared buffer with slicing**

Since Confluent.Kafka stores `byte[]` references, we cannot fully eliminate allocations. But we can reduce the cost:

1. **Cache well-known static values.** `ContentType` is almost always `"application/json"` -- cache the encoded bytes as a static `byte[]`. Same for other values with small finite sets.

2. **Use `Utf8Formatter` for DateTimeOffset.** Avoid the `ToString("O")` intermediate string for `SentAt`:

```csharp
// Before: 2 allocations (string + byte[])
headers.Add(KafkaMessageHeaders.SentAt, Encoding.UTF8.GetBytes(envelope.SentAt.Value.ToString("O")));

// After: 1 allocation (byte[] only)
Span<byte> buffer = stackalloc byte[33]; // "O" format is always 33 bytes
Utf8Formatter.TryFormat(envelope.SentAt.Value, buffer, out var bytesWritten, new StandardFormat('O'));
headers.Add(KafkaMessageHeaders.SentAt, buffer[..bytesWritten].ToArray());
```

3. **Use `Encoding.UTF8.GetBytes(string, byte[])` overload with pre-sized arrays.** For GUID-length strings (MessageId, CorrelationId, ConversationId -- always 36 chars = 36 bytes UTF-8), allocate exact-size arrays without the intermediate measurement:

```csharp
// Before:
headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes(envelope.MessageId));

// After: same allocation count but avoids GetByteCount + resize inside GetBytes
var bytes = new byte[36]; // GUIDs are always 36 bytes in default format
Encoding.UTF8.GetBytes(envelope.MessageId, bytes);
headers.Add(KafkaMessageHeaders.MessageId, bytes);
```

4. **Cache EnclosedMessageTypes encoding.** Since enclosed types are typically the same set per message type, cache the encoded `byte[]` keyed on the `ImmutableArray<string>` identity:

```csharp
private static readonly ConcurrentDictionary<int, byte[]> s_enclosedTypesCache = new();

// Cache key: hash of the joined enclosed types
```

**Realistic allocation reduction:** From ~13+ per dispatch to ~5-8 per dispatch. The Confluent.Kafka API constraint means we cannot achieve zero header allocations, but we can halve them.

**Files changed:**
- `KafkaDispatchEndpoint.cs` -- optimize `BuildKafkaHeaders` and `SelectKey`

---

## Workstream 3: Topology Fixes (Independent)

These are simple configuration changes that can be done independently of the pipeline work.

#### 3a. Default ReplicationFactor = -1 (Finding #2)

Change the hardcoded fallback in `KafkaTopic.OnInitialize`:

```csharp
// Before:
ReplicationFactor = config.ReplicationFactor ?? 1;

// After:
ReplicationFactor = config.ReplicationFactor ?? -1;
```

`-1` tells the Kafka broker to use its `default.replication.factor` setting, which is typically 3 in production clusters.

**Files changed:**
- `KafkaTopic.cs` line 46 -- change `?? 1` to `?? -1`

#### 3b. CooperativeSticky partition assignment (Finding #9)

Add to consumer config defaults:

```csharp
var config = new ConsumerConfig
{
    // ... existing settings ...
    PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
};
```

**Files changed:**
- `KafkaConnectionManager.cs` -- add one line to `CreateConsumer`

#### 3c. Error topic retention policy (Finding #10)

Add retention config when creating error topics in both topology conventions:

```csharp
// Before:
topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });

// After:
topology.AddTopic(new KafkaTopicConfiguration
{
    Name = errorTopicName,
    TopicConfigs = new Dictionary<string, string>
    {
        ["retention.ms"] = "2592000000",  // 30 days
        ["cleanup.policy"] = "delete"
    }
});
```

**Files changed:**
- `KafkaReceiveEndpointTopologyConvention.cs` -- add retention to error topic
- `KafkaDispatchEndpointTopologyConvention.cs` -- add retention to error topic

#### 3d. Consumer group collision warning (Finding #11)

Log a warning at startup when `ServiceName` is null and subscribe endpoints exist:

```csharp
// In DefaultNamingConventions or KafkaMessagingTransport validation
if (host.ServiceName is null && routes.Any(r => r.Kind == InboundRouteKind.Subscribe))
{
    logger.LogWarning(
        "ServiceName is not configured. Subscribe endpoints will use handler " +
        "names as consumer group IDs, which may collide across services.");
}
```

**Files changed:**
- `KafkaMessagingTransport.cs` -- add validation warning on startup

#### 3e. Reply topic cleanup on shutdown (Finding #12)

Delete temporary topics during graceful shutdown:

```csharp
// In KafkaConnectionManager.DisposeAsync or KafkaMessagingTransport.DisposeAsync
public async ValueTask DeleteTemporaryTopicsAsync(IEnumerable<KafkaTopic> topics)
{
    var tempTopics = topics
        .Where(t => t.IsTemporary)
        .Select(t => t.Name)
        .ToList();

    if (tempTopics.Count == 0)
    {
        return;
    }

    try
    {
        var adminClient = GetOrCreateAdminClient();
        await adminClient.DeleteTopicsAsync(tempTopics);
    }
    catch (Exception)
    {
        // Best-effort -- topics will self-clean via retention
    }
}
```

**Files changed:**
- `KafkaConnectionManager.cs` -- add `DeleteTemporaryTopicsAsync`
- `KafkaMessagingTransport.cs` -- call it during `DisposeAsync`

---

## Implementation Order

### Phase 1: Bug fixes and topology (low risk, immediate value)
1. **Finding #1** -- Fix SetEnvelope self-copy bug (1 line)
2. **Finding #2** -- Default ReplicationFactor = -1 (1 line)
3. **Finding #9** -- CooperativeSticky assignment (1 line)
4. **Finding #10** -- Error topic retention (2 files, ~10 lines each)
5. **Finding #11** -- ServiceName warning (1 log statement)

### Phase 2: Receive pipeline (high impact, moderate risk)
6. **Finding #6** -- Eliminate MessageEnvelope intermediate (rewrite KafkaParsingMiddleware)
7. **Finding #3** -- Add retry middleware (new middleware, pipeline registration)
8. **Finding #4 + #8** -- Concurrent processing + batch commits (KafkaReceiveEndpoint rewrite)

### Phase 3: Dispatch pipeline (moderate impact, low risk)
9. **Finding #5** -- Pool IValueTaskSource (new PooledDeliveryPromise, update dispatch)
10. **Finding #7** -- Header allocation reduction (optimize BuildKafkaHeaders)

### Phase 4: Cleanup (low risk)
11. **Finding #12** -- Reply topic cleanup on shutdown

---

## Receive Pipeline: Before vs After

### Before (registration order, execution is outermost-first):
```
[Bus]        TransportCircuitBreaker
[Bus]        ConcurrencyLimiter
[Bus]        Instrumentation
[Bus]        CircuitBreaker
[Bus]        DeadLetter
[Bus]        Fault
[Bus]        Expiry
[Bus]        MessageTypeSelection
[Bus]        Routing -> DefaultPipeline (consumer handlers)
[Transport]  KafkaCommit (after: ConcurrencyLimiter)
[Transport]  KafkaParsing (after: KafkaCommit)
```

### After:
```
[Bus]        TransportCircuitBreaker
[Bus]        ConcurrencyLimiter
[Bus]        Instrumentation
[Bus]        CircuitBreaker
[Bus]        Retry (NEW -- after: CircuitBreaker)
[Bus]        DeadLetter
[Bus]        Fault
[Bus]        Expiry
[Bus]        MessageTypeSelection
[Bus]        Routing -> DefaultPipeline (consumer handlers)
[Transport]  KafkaCommit (after: ConcurrencyLimiter) -- now uses StoreOffset when concurrent
[Transport]  KafkaParsing (after: KafkaCommit) -- now populates context directly
```

---

## Performance Impact Summary

| Metric | Before | After | Improvement |
|--------|--------|-------|-------------|
| **Receive allocations/msg** | ~20+ | ~5-8 | 60-75% reduction |
| **Dispatch allocations/msg** | ~25+ | ~15-18 | 30-40% reduction |
| **Receive throughput (sequential)** | ~100 msg/s/partition (commit-limited) | ~100 msg/s/partition | Same (opt-in concurrency) |
| **Receive throughput (concurrent)** | N/A | ~5,000-10,000 msg/s/partition | New capability |
| **Retry coverage** | None (straight to error topic) | 3 retries with backoff | Transient failure recovery |
| **Replication safety** | RF=1 (data loss risk) | RF=-1 (broker default, typically 3) | Production-safe default |

---

## Risk Assessment

| Change | Risk | Mitigation |
|--------|------|------------|
| Eliminate MessageEnvelope | **Medium** -- downstream middleware reads `context.Envelope` | Build lightweight envelope from context fields; verify all Envelope consumers |
| Retry middleware | **Low** -- additive middleware, default-on | Feature flag to disable; configurable max retries |
| Concurrent processing | **Medium** -- offset ordering, rebalance handling | Default MaxConcurrency=1 preserves existing behavior; opt-in only |
| Batch commits | **Low** -- only active when MaxConcurrency>1 | Tied to concurrency; sequential path unchanged |
| Pooled IValueTaskSource | **Medium** -- lifecycle management, thread-safety | Return-on-GetResult pattern; existing TCS tests validate behavior |
| Topology fixes | **Low** -- simple config changes | All are additive or default changes |

---

## Testing Strategy

1. **Existing tests must pass unchanged** -- all current behavior tests (fault, batch, request-reply, send, publish, concurrency, correlation, headers, volume, inbox, error queue) validate the pipeline end-to-end.

2. **New tests for retry middleware:**
   - `Retry_Should_RetryTransientFailure_When_HandlerThrowsOnce`
   - `Retry_Should_RouteToErrorTopic_When_AllRetriesExhausted`
   - `Retry_Should_SetDeliveryCount_When_Retrying`
   - `Retry_Should_BeDisabled_When_FeatureDisabled`

3. **New tests for concurrent processing:**
   - `ConcurrentConsumer_Should_ProcessInParallel_When_MaxConcurrencyGreaterThanOne`
   - `ConcurrentConsumer_Should_CommitOffsetsInOrder_When_MessagesCompleteOutOfOrder`
   - `ConcurrentConsumer_Should_HandleRebalance_When_PartitionsRevoked`

4. **New tests for pooled delivery promise:**
   - `PooledDeliveryPromise_Should_CompleteValueTask_When_ResultSet`
   - `PooledDeliveryPromise_Should_ReturnToPool_When_Consumed`

5. **Regression test for SetEnvelope bug:**
   - `SetEnvelope_Should_NotDuplicateHeaders_When_ContextHasPreExistingHeaders`

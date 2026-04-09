# Consumer Model Overhaul: Detailed Approach

## Thesis

Findings #3 (no retry), #4 (sequential processing), and #8 (per-message commit) are symptoms of a consumer model that doesn't match modern Kafka patterns. Rather than patching each independently, redesign the consumer model around a **channel-based concurrent worker architecture** with batched offset tracking and inline retry.

This approach also naturally solves:
- #8 (batch commits -- inherent in the offset tracker)
- #4 (concurrent processing -- channel workers)
- #3 (retry -- per-worker retry loop)

The remaining findings (#1, #2, #5, #6, #7, #9, #10, #11, #12) are independent fixes that layer on top.

---

## Architecture

### Current Model

```
Kafka Partition(s)
    |
    v
[IConsumer.Consume()] -- single thread, blocking poll
    |
    v
[await ExecuteAsync()] -- sequential, inline
    |
    v
[Middleware Pipeline: Parse -> ... -> Commit]
    |
    v
[consumer.Commit(result)] -- synchronous, per-message
```

**Problems**: One message at a time. Commit round-trip (~5-20ms) per message caps throughput at ~50-200 msg/s per partition. No retry before error topic. A slow handler blocks all partitions assigned to this consumer.

### New Model

```
Kafka Partition(s)
    |
    v
[Consumer Thread] -- single IConsumer, blocking Consume()
    |  writes to
    v
[BoundedChannel<ConsumeResult>] -- backpressure to consumer thread
    |  N workers drain concurrently
    v
[Worker 1] [Worker 2] ... [Worker N]
    |           |              |
    v           v              v
[Retry Loop: up to R attempts with backoff]
    |
    v
[ExecuteAsync -> Middleware Pipeline]
    |
    v
[PartitionOffsetTracker.MarkCompleted(partition, offset)]
    |
    v
[Periodic Commit Timer] -- commits highest contiguous offset per partition
```

### Key Components

#### 1. KafkaConsumerWorker

The new internal class that replaces the current `ConsumeLoopAsync` method in `KafkaReceiveEndpoint`. It owns the consumer thread, channel, workers, offset tracker, and commit timer.

```
KafkaReceiveEndpoint
  |
  +-- KafkaConsumerWorker (new)
        |
        +-- Consumer Thread (reads from Kafka)
        +-- BoundedChannel<KafkaWorkItem>
        +-- N Worker Tasks (drain channel, process messages)
        +-- PartitionOffsetTracker
        +-- Commit Timer
```

**KafkaWorkItem** is a lightweight struct holding the `ConsumeResult<byte[], byte[]>` and the `IConsumer<byte[], byte[]>` reference. It replaces the current `KafkaReceiveFeature`-based approach for passing data to the pipeline.

#### 2. BoundedChannel for Backpressure

```csharp
var channel = Channel.CreateBounded<KafkaWorkItem>(new BoundedChannelOptions(maxConcurrency * 2)
{
    FullMode = BoundedChannelFullMode.Wait,
    SingleWriter = true,      // only the consumer thread writes
    SingleReader = maxConcurrency == 1  // optimization for single-worker case
});
```

**Why `maxConcurrency * 2` capacity**: Small buffer absorbs jitter between consumer poll and worker processing speed without excessive memory. `FullMode.Wait` provides natural backpressure -- when workers are slow, the consumer thread blocks on `WriteAsync`, which effectively pauses Kafka polling. This prevents `MaxPollIntervalMs` violations more naturally than explicit `consumer.Pause()`.

**SingleWriter = true**: Only the consumer thread writes, enabling lock-free fast path in the channel.

#### 3. PartitionOffsetTracker

Tracks the highest **contiguous** completed offset per partition, so we only commit offsets where all prior messages have been processed.

```
Partition 0: received offsets [0, 1, 2, 3, 4]
  Worker A completes offset 0 -> committable = 0
  Worker B completes offset 2 -> committable = 0 (gap at 1)
  Worker C completes offset 1 -> committable = 2 (contiguous 0,1,2)
  Worker A completes offset 4 -> committable = 2 (gap at 3)
  Worker B completes offset 3 -> committable = 4 (contiguous 0,1,2,3,4)
```

**Data structure**: Per-partition sorted set or bitmap of completed offsets. When a new offset is marked completed, scan forward from the current commit point to find the new contiguous high-water mark.

```csharp
internal sealed class PartitionOffsetTracker
{
    // Partition -> offset tracking state
    private readonly ConcurrentDictionary<int, PartitionState> _partitions = new();

    // Called by workers after successful processing
    public void MarkCompleted(int partition, long offset) { ... }

    // Called by commit timer to get committable offsets
    public IReadOnlyList<TopicPartitionOffset> GetCommittableOffsets() { ... }

    // Called on rebalance to clear revoked partitions
    public void RevokePartitions(IEnumerable<int> partitions) { ... }

    // Called on rebalance to register newly assigned partitions
    public void AssignPartitions(IEnumerable<TopicPartition> partitions) { ... }
}

private sealed class PartitionState
{
    private long _commitPoint = -1;       // highest committed offset
    private readonly SortedSet<long> _completed = new();  // completed but not yet committable
    private readonly object _lock = new();

    public long MarkCompleted(long offset)
    {
        lock (_lock)
        {
            _completed.Add(offset);

            // Advance commit point through contiguous completed offsets
            while (_completed.Count > 0 && _completed.Min == _commitPoint + 1)
            {
                _commitPoint = _completed.Min;
                _completed.Remove(_completed.Min);
            }

            return _commitPoint;
        }
    }
}
```

**Why SortedSet**: With concurrent workers, offsets complete out of order. A sorted set lets us efficiently find the contiguous frontier. For typical concurrency levels (4-16 workers), the set is small (at most maxConcurrency entries) and the scan is trivial.

**Thread safety**: Each `PartitionState` uses a lock since workers complete on different threads. The lock protects only the SortedSet mutation and commit-point advance -- the critical section is microseconds.

#### 4. Periodic Commit

Instead of `consumer.Commit(result)` after every message, a timer fires periodically and commits the highest contiguous offset per partition.

```csharp
// In KafkaConsumerWorker
private readonly Timer _commitTimer;

// Fires every commitIntervalMs (default: 100ms, configurable)
private void OnCommitTimer(object? state)
{
    var offsets = _offsetTracker.GetCommittableOffsets();
    if (offsets.Count > 0)
    {
        _consumer.Commit(offsets);
    }
}
```

**Default interval**: 100ms. This means on crash, at most ~100ms worth of messages are reprocessed (at-least-once semantics preserved). For 10K msg/s throughput, this is ~1000 messages re-delivered on crash vs. 1 with per-message commit. This tradeoff is standard for high-throughput Kafka consumers.

**Configurable**: Exposed via `KafkaReceiveEndpointConfiguration.CommitIntervalMs` with a sensible default. Users who need stricter at-least-once can lower to 10ms; users optimizing for throughput can raise to 1000ms.

**Also commits on shutdown and rebalance**: The timer alone is not sufficient. We must also commit when:
1. The consumer is stopping (graceful shutdown)
2. Partitions are being revoked (rebalance)

#### 5. Inline Retry

Retry happens inside each worker, wrapping the `ExecuteAsync` call. This means retry is per-message, not per-batch, and happens before the message ever reaches the error topic.

```csharp
// Inside each worker's processing loop
private async Task ProcessWithRetryAsync(
    KafkaWorkItem item,
    CancellationToken cancellationToken)
{
    var retryCount = 0;
    var maxRetries = _retryOptions.MaxRetries;  // default: 3

    while (true)
    {
        try
        {
            await _endpoint.ExecuteAsync(
                static (context, state) =>
                {
                    var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
                    feature.ConsumeResult = state.item.ConsumeResult;
                    feature.Consumer = state.item.Consumer;
                    feature.RetryAttempt = state.retryCount;
                },
                (item, retryCount),
                cancellationToken);

            // Success -- mark offset as completed
            _offsetTracker.MarkCompleted(
                item.ConsumeResult.Partition.Value,
                item.ConsumeResult.Offset.Value);
            return;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            retryCount++;
            if (retryCount > maxRetries)
            {
                // All retries exhausted -- the fault middleware in the pipeline
                // should have already routed to the error topic. Mark completed
                // so the offset advances.
                _offsetTracker.MarkCompleted(
                    item.ConsumeResult.Partition.Value,
                    item.ConsumeResult.Offset.Value);
                return;
            }

            // Exponential backoff: 100ms, 200ms, 400ms, ...
            var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, retryCount - 1));
            await Task.Delay(delay, cancellationToken);
        }
    }
}
```

**Interaction with existing middleware**: The retry loop wraps `ExecuteAsync`, which runs the full middleware pipeline. This means:
- On retry attempt N, the pipeline runs fresh (new DI scope, fresh ReceiveContext from pool)
- `KafkaReceiveFeature.RetryAttempt` is set so middleware/handlers can inspect the attempt number
- The fault middleware only fires if the pipeline throws -- on retry, the retry loop catches the exception first
- After all retries are exhausted, we let the exception propagate **once more** through the pipeline so the fault middleware routes to the error topic, then mark offset complete

**Revised retry flow** (cleaner interaction with fault middleware):

```
Worker picks message from channel
  |
  +-- Attempt 1: ExecuteAsync -> pipeline throws -> catch, retry
  +-- Attempt 2: ExecuteAsync -> pipeline throws -> catch, retry  
  +-- Attempt 3: ExecuteAsync -> pipeline throws -> catch, retry
  +-- Attempt 4 (final): ExecuteAsync -> pipeline throws -> catch
       |
       +-- retryCount > maxRetries
       +-- Set context header "x-retry-exhausted: true"
       +-- Run ExecuteAsync one final time, but with retry-exhausted flag
       +-- Fault middleware sees the flag (or the original exception propagates)
       +-- Error topic receives the message
       +-- Mark offset complete regardless
```

Actually, this is overcomplicating it. The simpler and correct design:

**The retry loop wraps the pipeline execution. The fault middleware is the last line of defense.** The retry catches exceptions from the handler layer (inside the pipeline). If the fault middleware successfully routes the message to the error topic, `ExecuteAsync` returns normally (no exception) -- so there is nothing to retry. The retry only fires when the pipeline itself throws, which means even fault routing failed.

This is actually how it works today: the fault middleware catches handler exceptions and routes them to the error topic. The outer `catch` in the consume loop only fires for catastrophic failures. So the retry should wrap at the **handler level**, not at the pipeline level.

**Better approach**: Make retry a middleware, not a loop around `ExecuteAsync`.

### Retry as Middleware (ReceiveRetryMiddleware)

```csharp
internal sealed class ReceiveRetryMiddleware(int maxRetries, TimeSpan baseDelay)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var attempt = 0;

        while (true)
        {
            try
            {
                await next(context);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException && attempt < maxRetries)
            {
                attempt++;
                context.DeliveryCount = (context.DeliveryCount ?? 0) + 1;

                var delay = TimeSpan.FromMilliseconds(
                    baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                await Task.Delay(delay, context.CancellationToken);
            }
        }
    }
}
```

**Pipeline position**: Between `CircuitBreaker` and `Fault`:

```
TransportCircuitBreaker
  ConcurrencyLimiter
    KafkaCommit (removed -- replaced by offset tracker)
      KafkaParsing
        Instrumentation
          CircuitBreaker
            >>> ReceiveRetry (NEW) <<<
              DeadLetter
                Fault
                  Expiry
                    MessageTypeSelection
                      Routing -> Consumer Handlers
```

**Why here**: 
- Retry wraps the fault middleware, so if retry is exhausted, the exception propagates to fault, which routes to error topic
- Circuit breaker is outside retry, so repeated failures across messages still trip the breaker
- Instrumentation is outside retry, so each message attempt is traced (but the overall message is one receive span)

Wait -- re-examining the middleware order. The current registration is:

1. `KafkaCommit` registered `after: ConcurrencyLimiter`  
2. `KafkaParsing` registered `after: KafkaCommit`

And MiddlewareCompiler reverses the list. So actual **execution** order (outermost to innermost):

```
TransportCircuitBreaker (outermost)
  ConcurrencyLimiter
    KafkaCommit  <-- wraps everything below, commits on success
      KafkaParsing  <-- parses ConsumeResult into envelope
        Instrumentation
          CircuitBreaker
            DeadLetter
              Fault
                Expiry
                  MessageTypeSelection
                    Routing -> Consumer Handlers (innermost)
```

Wait, that's not right either. The registration order matters for the `after:` placement, but the MiddlewareCompiler reverses the full list. Let me trace through carefully.

**Registration order** (from ReceiveMiddlewares + Kafka extensions):
1. TransportCircuitBreaker (bus-level)
2. ConcurrencyLimiter (bus-level)  
3. Instrumentation (bus-level)
4. CircuitBreaker (bus-level)
5. DeadLetter (bus-level)
6. Fault (bus-level)
7. Expiry (bus-level)
8. MessageTypeSelection (bus-level)
9. Routing (bus-level)
10. KafkaCommit (transport-level, `after: ConcurrencyLimiter` = inserted at position 3)
11. KafkaParsing (transport-level, `after: KafkaCommit` = inserted at position 4)

After `after:` ordering:
1. TransportCircuitBreaker
2. ConcurrencyLimiter
3. **KafkaCommit**
4. **KafkaParsing**
5. Instrumentation
6. CircuitBreaker
7. DeadLetter
8. Fault
9. Expiry
10. MessageTypeSelection
11. Routing

Reversed (for right-to-left fold, so first in reversed list = innermost):
11. Routing (innermost)
10. MessageTypeSelection
9. Expiry
8. Fault
7. DeadLetter
6. CircuitBreaker
5. Instrumentation
4. **KafkaParsing**
3. **KafkaCommit**
2. ConcurrencyLimiter
1. TransportCircuitBreaker (outermost)

So **execution order** is outermost-first:
```
TransportCircuitBreaker
  ConcurrencyLimiter
    KafkaCommit    <-- commits after next() succeeds
      KafkaParsing <-- parses ConsumeResult -> envelope -> SetEnvelope
        Instrumentation
          CircuitBreaker
            DeadLetter
              Fault
                Expiry
                  MessageTypeSelection
                    Routing -> Handlers
```

This confirms: KafkaCommit wraps the entire pipeline. It calls `next(context)` which runs KafkaParsing -> ... -> Handlers, and then commits on success.

### Retry Middleware Placement

The retry middleware should be placed **after Fault** (inside fault). This way:

```
...
  Fault                    <-- catches exceptions, routes to error topic
    >>> ReceiveRetry <<<   <-- retries handler failures before they reach Fault
      Expiry
        MessageTypeSelection
          Routing -> Handlers
```

If all retries exhaust, the exception propagates up to Fault, which routes to the error topic. This is the clean separation.

**Registration**: `UseReceive(ReceiveRetryMiddleware.Create(), after: ReceiveMiddlewares.Fault.Key)`

---

## KafkaCommit Middleware Changes

### Current (Remove)

The current `KafkaCommitMiddleware` calls `consumer.Commit(consumeResult)` synchronously per message. This is replaced by the `PartitionOffsetTracker`.

### New Behavior

Replace `KafkaCommitMiddleware` with `KafkaOffsetTrackingMiddleware`:

```csharp
internal sealed class KafkaOffsetTrackingMiddleware
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();

        try
        {
            await next(context);
        }
        finally
        {
            // Always mark offset complete, even on failure.
            // The fault middleware upstream has already routed to error topic.
            // Not committing would cause infinite redelivery of a permanently
            // failing message.
            feature.OffsetTracker.MarkCompleted(
                feature.ConsumeResult.Partition.Value,
                feature.ConsumeResult.Offset.Value);
        }
    }
}
```

**Key change**: The offset is marked complete in `finally`, not just on success. This is because:
1. The fault middleware upstream catches handler exceptions and routes to error topic
2. If fault routing succeeds, `next()` returns normally -- the offset is committed
3. If fault routing itself fails, the exception propagates, but we **still** mark complete -- otherwise the message is redelivered forever. The old model had the same problem: if fault routing failed, the commit didn't happen, causing infinite redelivery of an unprocessable message.

Actually, we should preserve the current semantics where a catastrophic failure (fault routing itself fails) does NOT commit. This prevents message loss. The message will be redelivered, and hopefully the fault routing succeeds on the next attempt.

**Revised**: Keep `try/catch` semantics from current `KafkaCommitMiddleware`:

```csharp
try
{
    await next(context);
    // Success (including successful error routing) -- mark complete
    feature.OffsetTracker.MarkCompleted(partition, offset);
}
catch
{
    // Catastrophic failure -- do NOT mark complete.
    // Message will be redelivered.
    throw;
}
```

This preserves the exact same semantics as today, but replaces synchronous commit with offset tracking.

---

## Rebalance Handling

With concurrent workers, rebalance becomes more complex. The key invariant: **we must not commit offsets for partitions that have been revoked**.

### Rebalance Flow

```
1. Kafka triggers rebalance
2. SetPartitionsRevokedHandler fires on consumer thread
   a. Stop writing to channel (set draining flag)
   b. Wait for channel to drain (all in-flight items for revoked partitions complete)
   c. Commit final offsets for revoked partitions
   d. Clear revoked partitions from offset tracker
3. SetPartitionsAssignedHandler fires
   a. Register new partitions in offset tracker
   b. Resume writing to channel
4. Consumer thread resumes polling
```

**Implementation detail**: The consumer thread is the one that calls `consumer.Consume()`, so the revoked/assigned handlers fire **on the consumer thread** (this is how Confluent.Kafka works). This means the consumer thread is naturally paused during rebalance. The challenge is draining in-flight messages for revoked partitions.

### Draining Strategy

When partitions are revoked:

1. The consumer thread stops writing new items to the channel
2. We need to wait for items already in the channel (for revoked partitions) to complete
3. Items for non-revoked partitions can continue processing

**Simple approach**: On revoke, drain the entire channel. Since the channel is bounded at `maxConcurrency * 2`, this is at most `maxConcurrency * 2` messages. At typical processing speeds, this takes milliseconds to seconds.

```csharp
// In PartitionsRevokedHandler (runs on consumer thread)
.SetPartitionsRevokedHandler((consumer, partitions) =>
{
    // Signal workers to stop after current items
    _rebalanceCts.Cancel();
    
    // Wait for all in-flight work to complete
    // (channel.Writer.Complete signals no more items)
    // Workers will finish their current items and exit
    SpinWait.SpinUntil(() => _inflightCount == 0, TimeSpan.FromSeconds(30));
    
    // Commit final offsets for revoked partitions
    var offsets = _offsetTracker.GetCommittableOffsets(partitions.Select(p => p.Partition.Value));
    if (offsets.Count > 0)
    {
        consumer.Commit(offsets);
    }
    
    // Clear tracking state for revoked partitions
    _offsetTracker.RevokePartitions(partitions.Select(p => p.Partition.Value));
    
    logger.KafkaPartitionsRevoked(groupId, partitions);
})
```

**Important**: The revoked handler is synchronous (Confluent.Kafka requirement). We cannot await async operations inside it. `SpinWait.SpinUntil` with a timeout is the pragmatic approach.

**After revoke**: Create a new channel and new set of workers for the newly assigned partitions. This is simpler than trying to selectively drain items for specific partitions.

### Simplified Rebalance: Channel Replacement

Instead of selective draining, replace the entire channel on rebalance:

```
Revoke:
  1. Complete old channel writer (stops consumer thread from writing)
  2. Wait for workers to drain remaining items
  3. Commit final offsets
  4. Dispose old workers

Assign:
  1. Create new channel
  2. Create new workers
  3. Register new partitions in offset tracker
  4. Consumer thread resumes writing to new channel
```

This is simpler and avoids partial-drain complexity. The cost is re-creating N worker tasks, which is trivial.

---

## Configuration API

### New Options

```csharp
public sealed class KafkaReceiveEndpointConfiguration : ReceiveEndpointConfiguration
{
    // Existing
    public string? TopicName { get; set; }
    public string? ConsumerGroupId { get; set; }

    // New
    public int? MaxConcurrency { get; set; }        // default: 1 (preserves current behavior)
    public int? CommitIntervalMs { get; set; }       // default: 100
    public int? MaxRetries { get; set; }             // default: 3
    public TimeSpan? RetryBaseDelay { get; set; }    // default: 100ms
}
```

**Default MaxConcurrency = 1**: This preserves backward compatibility. Users opt into concurrency explicitly. When MaxConcurrency = 1, the channel and offset tracker still work, but there is only one worker, so offset ordering is trivially preserved and commit behavior matches today.

### Descriptor API

```csharp
public interface IKafkaReceiveEndpointDescriptor
{
    // Existing
    IKafkaReceiveEndpointDescriptor Topic(string name);
    IKafkaReceiveEndpointDescriptor ConsumerGroup(string groupId);
    IKafkaReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);

    // New
    IKafkaReceiveEndpointDescriptor CommitInterval(TimeSpan interval);
    IKafkaReceiveEndpointDescriptor Retry(int maxRetries, TimeSpan? baseDelay = null);
}
```

### Bus-Level Defaults

```csharp
public sealed class KafkaBusDefaults
{
    public KafkaDefaultTopicOptions Topic { get; set; } = new();

    // New
    public int? DefaultMaxConcurrency { get; set; }
    public int? DefaultCommitIntervalMs { get; set; }
    public int? DefaultMaxRetries { get; set; }
    public TimeSpan? DefaultRetryBaseDelay { get; set; }
}
```

---

## Migration Path

### Phase 1: Internal Refactor (No API Change)

1. Extract `ConsumeLoopAsync` into `KafkaConsumerWorker`
2. Add `BoundedChannel` between consumer and processing
3. Add `PartitionOffsetTracker` (with maxConcurrency=1, this behaves identically to per-message commit)
4. Replace `KafkaCommitMiddleware` with `KafkaOffsetTrackingMiddleware`
5. Add periodic commit timer
6. **All existing tests pass unchanged** because MaxConcurrency defaults to 1

### Phase 2: Concurrent Workers

1. Add `MaxConcurrency` configuration
2. Wire MaxConcurrency to channel capacity and worker count
3. Update rebalance handlers for channel replacement
4. Add tests for concurrent processing correctness

### Phase 3: Retry Middleware

1. Add `ReceiveRetryMiddleware` in core Mocha (not Kafka-specific)
2. Register in default pipeline between CircuitBreaker and Fault
3. Add configuration API (MaxRetries, BaseDelay)
4. Add tests for retry behavior

### Phase 4: Wire Configuration

1. Add `CommitInterval`, `Retry` to descriptor API
2. Add bus-level defaults
3. Documentation

---

## Interaction with Existing Middleware

### ConcurrencyLimiterMiddleware

The existing `ConcurrencyLimiterMiddleware` uses a `SemaphoreSlim` to gate concurrent pipeline execution. With the new model, there are two levels of concurrency control:

1. **Worker count** (N workers draining the channel) -- this is the primary concurrency mechanism
2. **ConcurrencyLimiter** -- can further throttle within the pipeline

These are complementary, not conflicting. A user might set MaxConcurrency=8 (8 workers) but ConcurrencyLimiter=4 (only 4 pipelines executing at once). The extra 4 workers would be waiting on the semaphore. This is useful when you want to buffer messages in workers (keeping them out of the channel) but limit actual processing pressure on downstream systems.

**Default**: If MaxConcurrency is set, the ConcurrencyLimiter should be set to match (or disabled). The framework should handle this automatically unless the user explicitly overrides.

### CircuitBreakerMiddleware

The circuit breaker tracks failure rates and stops processing when the threshold is exceeded. With concurrent workers:

- Multiple workers may hit the breaker simultaneously
- The Polly circuit breaker is thread-safe
- When the circuit opens, all workers will see `BrokenCircuitException` and enter the delay loop
- This is correct behavior -- all workers pause during the break

### KafkaReceiveFeature

Currently carries `ConsumeResult` and `Consumer`. With the new model, also carries the `PartitionOffsetTracker` reference:

```csharp
public sealed class KafkaReceiveFeature : IPooledFeature
{
    public ConsumeResult<byte[], byte[]> ConsumeResult { get; set; } = null!;
    public IConsumer<byte[], byte[]> Consumer { get; set; } = null!;
    public PartitionOffsetTracker OffsetTracker { get; set; } = null!;  // NEW
    public int RetryAttempt { get; set; }  // NEW -- set by retry middleware

    // ... existing members ...
}
```

---

## Impact on Other Findings

### Finding #1: SetEnvelope Bug
Independent fix. Remove `Headers.AddRange(Headers)` line. No interaction with consumer model.

### Finding #2: ReplicationFactor Default
Independent fix. Change `?? 1` to `?? -1` in `KafkaTopic.OnInitialize`.

### Finding #5: TCS per dispatch
Independent fix on dispatch path. No interaction with consumer model.

### Finding #6: MessageEnvelope intermediate
Independent fix. Can be done before or after consumer overhaul.

### Finding #7: Header byte[] allocations
Independent fix on both dispatch and receive paths. No interaction.

### Finding #9: CooperativeSticky
Independent fix. One line in `KafkaConnectionManager.CreateConsumer`:
```csharp
config.PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky;
```
**Note**: CooperativeSticky interacts well with the new rebalance handling. Cooperative rebalancing means only affected partitions are revoked, so the channel drain is smaller.

### Finding #10: Error topic retention
Independent fix. Add `retention.ms` to error topic configs.

### Finding #11: Consumer group collision
Independent fix. Warn when ServiceName is null for subscribe endpoints.

### Finding #12: Reply topic cleanup
Independent fix. Delete temporary topics on shutdown.

---

## Testing Strategy

### Unit Tests

1. **PartitionOffsetTracker**: Test contiguous offset advancement with out-of-order completions, multiple partitions, revoke/assign
2. **ReceiveRetryMiddleware**: Test retry count, backoff timing, exhaustion -> exception propagation
3. **KafkaOffsetTrackingMiddleware**: Test mark-complete on success, no-mark on catastrophic failure

### Integration Tests (Kafka)

1. **Concurrent processing**: Send N messages, verify all consumed with maxConcurrency > 1
2. **Offset correctness**: Kill consumer mid-processing, verify redelivery of only uncommitted messages
3. **Retry behavior**: Handler fails transiently, verify retry before error topic
4. **Rebalance**: Add/remove consumers, verify no message loss and no duplicate processing (within at-least-once guarantees)
5. **Backpressure**: Slow handler, verify consumer doesn't exceed MaxPollIntervalMs

### Backward Compatibility

- All existing tests must pass with `MaxConcurrency=1` (default)
- No API changes to existing public types
- New configuration options are additive and optional

---

## Risk Assessment

### Low Risk
- PartitionOffsetTracker: Well-understood data structure, easy to test in isolation
- ReceiveRetryMiddleware: Simple retry loop, follows existing middleware pattern
- Periodic commit: Standard Kafka pattern, timer-based

### Medium Risk
- Rebalance handling: Must drain in-flight work correctly. Mitigated by channel replacement approach (simpler than selective drain).
- Concurrent offset tracking: Must correctly handle out-of-order completion. Mitigated by contiguous offset tracking (proven pattern).

### Mitigation
- Phase 1 (internal refactor with maxConcurrency=1) validates the architecture with zero behavior change
- Each phase is independently testable and deployable
- Backward compatibility preserved through defaults

---

## Summary of Changes by File

### New Files
- `KafkaConsumerWorker.cs` -- consumer thread + channel + workers + commit timer
- `PartitionOffsetTracker.cs` -- per-partition contiguous offset tracking
- `KafkaOffsetTrackingMiddleware.cs` -- replaces KafkaCommitMiddleware
- `ReceiveRetryMiddleware.cs` -- inline retry with backoff (in core Mocha, not Kafka-specific)

### Modified Files
- `KafkaReceiveEndpoint.cs` -- delegate to KafkaConsumerWorker instead of inline ConsumeLoopAsync
- `KafkaReceiveEndpointConfiguration.cs` -- add MaxConcurrency, CommitIntervalMs, MaxRetries, RetryBaseDelay
- `KafkaTransportDescriptorExtensions.cs` -- register new middleware (offset tracking, retry)
- `KafkaReceiveFeature.cs` -- add OffsetTracker, RetryAttempt properties
- `KafkaConnectionManager.cs` -- update rebalance handlers to support offset tracker

### Removed
- `KafkaCommitMiddleware.cs` -- replaced by KafkaOffsetTrackingMiddleware

### Unchanged
- All dispatch-path code
- All topology code (except error topic retention -- independent fix)
- All core middleware (circuit breaker, fault, dead letter, etc.)
- InMemory transport

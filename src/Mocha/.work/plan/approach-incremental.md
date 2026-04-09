# Approach: Phased Incremental Fixes

**Strategy:** Ship each phase as an independent, self-contained PR. Phases ordered by risk: correctness first, then topology/safety, then performance, then features. Each change is minimal and testable in isolation.

---

## Phase 1: Correctness Fixes (Bug + Unsafe Defaults)

**Goal:** Fix the two issues that produce incorrect behavior today.
**Risk:** Low -- small, well-scoped changes with clear before/after semantics.
**PR scope:** ~10 lines changed across 2 files.

### 1a. ReceiveContext.SetEnvelope self-copy bug (Finding #1)

**What's wrong:** `ReceiveContext.SetEnvelope` calls `Headers.AddRange(Headers)` on line 211, which copies the context's headers into itself. Because `ReceiveContext` is pooled, this can cause cross-message header contamination if the context had residual headers from a prior message (the `foreach` loop below only overwrites keys present in the new envelope, leaving stale duplicates).

**File:** `src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs:211`

**Change:** Remove the `Headers.AddRange(Headers)` line entirely. The `foreach` loop on lines 213-216 already iterates `envelope.Headers` and calls `Headers.Set(key, value)`, which overwrites by key. This is sufficient -- envelope headers replace any same-keyed context headers, and stale keys from the pool are already cleared by `Reset()` (line 175: `_headers.Clear()`).

```csharp
// BEFORE (line 209-217):
if (envelope.Headers is not null)
{
    Headers.AddRange(Headers);  // BUG: self-copy

    foreach (var header in envelope.Headers)
    {
        Headers.Set(header.Key, header.Value);
    }
}

// AFTER:
if (envelope.Headers is not null)
{
    foreach (var header in envelope.Headers)
    {
        Headers.Set(header.Key, header.Value);
    }
}
```

**Testing:**
- Add a unit test that creates a `ReceiveContext`, calls `SetEnvelope` with headers `{A=1}`, resets it, then calls `SetEnvelope` again with headers `{B=2}`. Assert headers contain only `{B=2}`, not `{A=1, B=2}`.
- Run existing tests with `--filter ReceiveContext` or `--filter SetEnvelope` to confirm no regressions.
- All existing Kafka integration tests should still pass (the bug was masked because `Reset()` clears headers before each reuse, so in practice the self-copy line was a no-op on fresh-from-pool contexts).

**Rollback:** Revert the single line removal.

---

### 1b. Default ReplicationFactor=1 should be -1 (Finding #2)

**What's wrong:** `KafkaTopic.OnInitialize` falls back to `config.ReplicationFactor ?? 1`, meaning any topic without an explicit RF gets RF=1 (no redundancy). Combined with `Acks=All` on the producer, this gives a false sense of safety -- there's only one replica to ack. RF=-1 tells the Kafka broker to use its own `default.replication.factor` setting, which is typically 3 in production clusters.

**Files:**
- `src/Mocha.Transport.Kafka/Topology/KafkaTopic.cs:21,46`

**Change:** Change the fallback from `1` to `-1` in both the property initializer and `OnInitialize`.

```csharp
// BEFORE (line 21):
public short ReplicationFactor { get; private set; } = 1;

// AFTER:
public short ReplicationFactor { get; private set; } = -1;

// BEFORE (line 46):
ReplicationFactor = config.ReplicationFactor ?? 1;

// AFTER:
ReplicationFactor = config.ReplicationFactor ?? -1;
```

**Testing:**
- Add a unit test for `KafkaTopic` that initializes with `ReplicationFactor = null` and asserts the result is `-1`.
- Add a unit test that initializes with explicit `ReplicationFactor = 3` and asserts it's `3` (not overridden).
- Run existing topology provisioning tests to confirm topics are still created successfully (the broker handles RF=-1 transparently).

**Rollback:** Revert to `1`. No data loss -- existing topics retain their configuration; only new topic creation is affected.

---

## Phase 2: Topology & Safety (Error topics, consumer groups, partition assignment)

**Goal:** Fix topology defaults that could cause production incidents.
**Risk:** Low-medium -- config changes that only affect new topic/consumer creation.
**PR scope:** ~40 lines changed across 4 files.

### 2a. Error topics have no retention policy (Finding #10)

**What's wrong:** Error topics (`_error`) are created with empty `KafkaTopicConfiguration`, inheriting broker defaults (often 7 days). Error topics should retain messages longer for investigation but not grow unbounded. The reply topic convention already sets retention -- error topics should follow the same pattern.

**Files:**
- `src/Mocha.Transport.Kafka/Conventions/KafkaReceiveEndpointTopologyConvention.cs:59-63`
- `src/Mocha.Transport.Kafka/Conventions/KafkaDispatchEndpointTopologyConvention.cs:35-39`

**Change:** When creating error topics in both conventions, set `TopicConfigs` with a 30-day retention and `delete` cleanup policy:

```csharp
// BEFORE (KafkaReceiveEndpointTopologyConvention.cs:62):
topology.AddTopic(new KafkaTopicConfiguration { Name = errorTopicName });

// AFTER:
topology.AddTopic(new KafkaTopicConfiguration
{
    Name = errorTopicName,
    TopicConfigs = new Dictionary<string, string>
    {
        ["retention.ms"] = "2592000000",   // 30 days
        ["cleanup.policy"] = "delete"
    }
});
```

Apply the same change in `KafkaDispatchEndpointTopologyConvention.cs:38`.

**Testing:**
- Add a unit test that invokes `DiscoverTopology` and asserts the error topic has `retention.ms=2592000000` and `cleanup.policy=delete`.
- Existing integration tests should pass -- the only change is additional topic config on error topics.

**Rollback:** Revert to empty config. Existing error topics are unaffected (Kafka doesn't retroactively change topic config on `CreateTopicsAsync` if the topic already exists).

---

### 2b. Consumer group collision when ServiceName is null (Finding #11)

**What's wrong:** For subscribe endpoints, `DefaultNamingConventions.GetReceiveEndpointName` prefixes the handler name with `{serviceName}.` only when `host.ServiceName is not null`. Two services with null `ServiceName` and the same handler name would get the same consumer group ID, competing for messages instead of each receiving all messages.

**File:** `src/Mocha/src/Mocha/Naming/DefaultNamingConventions.cs:33`

**Change:** Log a warning at configuration time when `ServiceName` is null for subscribe endpoints. We do NOT want to change the naming behavior (that would be a breaking change for existing deployments) -- instead, validate and warn.

The warning should be emitted during endpoint configuration, not in the naming convention itself (naming conventions should be pure functions). The right place is `KafkaMessagingTransport.CreateEndpointConfiguration` for subscribe routes.

**File:** `src/Mocha.Transport.Kafka/KafkaMessagingTransport.cs`

**Change:** In `CreateEndpointConfiguration`, after the reply-kind check, when the route is `InboundRouteKind.Subscribe` and `context.Host.ServiceName` is null, log a warning:

```csharp
if (route.Kind == InboundRouteKind.Subscribe && context.Host.ServiceName is null)
{
    _logger.ServiceNameNotConfiguredForSubscribe(endpointName);
}
```

Add the corresponding `LoggerMessage`:

```csharp
[LoggerMessage(LogLevel.Warning,
    "Subscribe endpoint '{EndpointName}' created without a ServiceName. " +
    "This may cause consumer group collisions if multiple services use the same handler name.")]
public static partial void ServiceNameNotConfiguredForSubscribe(this ILogger logger, string endpointName);
```

**Testing:**
- Add a test that configures a subscribe route without ServiceName and verifies the warning is logged.
- Add a test that configures a subscribe route with ServiceName and verifies no warning.

**Rollback:** Remove the warning log. No behavioral change to roll back.

**Note:** A future PR could make `ServiceName` required for subscribe endpoints, but that's a breaking API change that needs discussion.

---

### 2c. No CooperativeSticky partition assignment (Finding #9)

**What's wrong:** `KafkaConnectionManager.CreateConsumer` doesn't set `PartitionAssignmentStrategy` in the `ConsumerConfig`. The Confluent.Kafka default is `Range`, which causes stop-the-world rebalances and uneven partition distribution. `CooperativeSticky` (Kafka 2.4+) enables incremental rebalancing, better cache locality, and no stop-the-world pauses.

**File:** `src/Mocha.Transport.Kafka/Connection/KafkaConnectionManager.cs:98-106`

**Change:** Add `PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky` to the `ConsumerConfig`:

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _bootstrapServers,
    GroupId = groupId,
    EnableAutoCommit = false,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnablePartitionEof = false,
    MaxPollIntervalMs = 600_000,
    PartitionAssignmentStrategy = PartitionAssignmentStrategy.CooperativeSticky
};
```

**Testing:**
- The user-provided `_consumerConfigOverrides` callback (line 108) runs after this, so users can still override the strategy. Add a test verifying the default is `CooperativeSticky` and that a user override takes precedence.
- Run existing Kafka integration tests. CooperativeSticky is compatible with all Kafka 2.4+ clusters.

**Rollback:** Remove the line; reverts to Confluent.Kafka default (`Range`).

**Compatibility note:** `CooperativeSticky` requires all consumers in a group to use the same strategy. During a rolling upgrade, mixed strategies will cause errors. This is safe for new deployments. For existing deployments, users would need to perform a rolling restart. Document this in the PR description.

---

## Phase 3: Retry Middleware (New Feature)

**Goal:** Add immediate retry support -- the single biggest functional gap vs competitors.
**Risk:** Medium -- new middleware, but follows established patterns exactly.
**PR scope:** ~150 lines across 4-5 new files + 2 modified files.

### 3. Add ReceiveRetryMiddleware (Finding #3)

**What's wrong:** There is no retry middleware in the receive pipeline. Every transient failure (database timeout, HTTP 503, brief network blip) goes straight to the error topic, requiring manual intervention. NServiceBus defaults to 5 immediate retries; MassTransit and Wolverine both have configurable retry. This is the single biggest gap.

**New files:**
- `src/Mocha/src/Mocha/Middlewares/Receive/Retry/ReceiveRetryMiddleware.cs`
- `src/Mocha/src/Mocha/Middlewares/Receive/Retry/RetryOptions.cs`
- `src/Mocha/src/Mocha/Middlewares/Receive/Retry/RetryFeature.cs`
- `src/Mocha/src/Mocha/Middlewares/Receive/Retry/RetryConfigurationExtensions.cs`

**Modified files:**
- `src/Mocha/src/Mocha/Middlewares/Receive/ReceiveMiddlewares.cs` -- add `Retry` static field
- `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs` -- add `Retry` to default middleware list (between `CircuitBreaker` and `Fault`)

**Design:**

The middleware wraps the `next` delegate in a retry loop. On exception, it increments the attempt count stored in a `RetryFeature` on the context's feature collection and retries up to `MaxRetries` times with configurable backoff.

```csharp
// ReceiveRetryMiddleware (sketch)
internal sealed class ReceiveRetryMiddleware(int maxRetries, TimeSpan[] intervals, TimeProvider timeProvider)
{
    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        var feature = context.Features.GetOrSet<RetryFeature>();

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                feature.AttemptNumber = attempt;
                await next(context);
                return;
            }
            catch (Exception) when (attempt < maxRetries)
            {
                var delay = attempt < intervals.Length
                    ? intervals[attempt]
                    : intervals[^1];
                await Task.Delay(delay, timeProvider, context.CancellationToken);
            }
        }
    }
}
```

**Configuration (RetryOptions):**

```csharp
public class RetryOptions
{
    public bool? Enabled { get; set; }
    public int? MaxRetries { get; set; }
    public TimeSpan[]? Intervals { get; set; }

    public static class Defaults
    {
        public static int MaxRetries = 3;
        public static TimeSpan[] Intervals = [
            TimeSpan.FromMilliseconds(200),
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(5)
        ];
    }
}
```

**Pipeline position:** Between `CircuitBreaker` and `Fault`. This means:
1. Circuit breaker evaluates first (if circuit is open, message is delayed, not retried)
2. Retry wraps everything downstream including fault handling
3. If all retries are exhausted, the exception propagates to `Fault` middleware which routes to the error topic

**Why not use Polly?** The circuit breaker uses Polly, but retry is simpler: a for-loop with delay. Using Polly adds an allocation (ResiliencePipeline + context) per message. The middleware pattern here is zero-overhead when retries are not needed (fast path: try once, succeed, return). Polly retry would be over-engineering for immediate retries with static intervals.

**Feature cascading:** Same pattern as CircuitBreakerMiddleware -- resolve from endpoint features, then transport features, then bus features. This allows per-endpoint retry configuration.

**Testing:**
- Unit test: handler throws N times then succeeds -- assert `RetryFeature.AttemptNumber` reflects correct count.
- Unit test: handler throws more than MaxRetries -- assert exception propagates (fault middleware catches it).
- Unit test: retry disabled via feature -- assert no retry, immediate propagation.
- Integration test: Kafka message handler throws once, succeeds on retry -- assert message is processed, offset committed, no message in error topic.
- Integration test: handler always throws -- assert message lands in error topic after MaxRetries+1 attempts.

**Rollback:** Remove the middleware from `ReceiveMiddlewares` defaults. Behavior reverts to pre-retry (faults go straight to error topic).

---

## Phase 4: Concurrent Consumer Support (New Feature)

**Goal:** Allow multiple consumer instances per endpoint for partition-level parallelism.
**Risk:** Medium-high -- changes consume loop architecture, affects offset management.
**PR scope:** ~100 lines changed across 3 files.

### 4. Sequential single-consumer processing (Finding #4)

**What's wrong:** `KafkaReceiveEndpoint` runs a single `consumer.Consume()` loop on one `LongRunning` Task, processing messages one at a time (line 106: `await ExecuteAsync(...)`). Throughput is capped at 1 message per processing duration per partition. MassTransit, Wolverine, and NServiceBus all support concurrent consumers.

**Approach:** Add a `ConcurrentConsumerCount` configuration option that creates N consumer instances in the same consumer group, each running its own consume loop. This is the simplest path to partition-level parallelism -- Kafka's consumer group protocol handles partition assignment automatically.

This is explicitly NOT within-partition concurrency (dispatching to a channel/worker pool). That's a separate, more complex change. This is N-consumer parallelism, where each consumer gets exclusive partitions from the group protocol.

**Files:**
- `src/Mocha.Transport.Kafka/Configurations/KafkaReceiveEndpointConfiguration.cs` -- add `ConcurrentConsumerCount` property (default: 1)
- `src/Mocha.Transport.Kafka/KafkaReceiveEndpoint.cs` -- modify `OnStartAsync` to create N consumers and N consume loops; modify `OnStopAsync` to tear down all of them
- `src/Mocha.Transport.Kafka/Descriptors/IKafkaReceiveEndpointDescriptor.cs` -- add `ConcurrentConsumerCount(int count)` fluent API

**Change in KafkaReceiveEndpoint.cs:**

```csharp
// BEFORE: Single consumer + single task
private IConsumer<byte[], byte[]>? _consumer;
private Task? _consumeLoopTask;

// AFTER: Multiple consumers + multiple tasks
private readonly List<IConsumer<byte[], byte[]>> _consumers = [];
private readonly List<Task> _consumeLoopTasks = [];
```

In `OnStartAsync`, loop N times:

```csharp
var count = Configuration.ConcurrentConsumerCount ?? 1;
for (var i = 0; i < count; i++)
{
    var consumer = kafkaTransport.ConnectionManager.CreateConsumer(ConsumerGroupId, _logger);
    consumer.Subscribe(Topic.Name);
    _consumers.Add(consumer);

    var task = Task.Factory.StartNew(
        () => ConsumeLoopAsync(consumer, _cts.Token),
        CancellationToken.None,
        TaskCreationOptions.LongRunning,
        TaskScheduler.Default).Unwrap();
    _consumeLoopTasks.Add(task);
}
```

`OnStopAsync` tears down all consumers and awaits all tasks.

**Constraints:**
- Each consumer gets a subset of partitions from the group protocol.
- If `ConcurrentConsumerCount > partition count`, some consumers will be idle (standard Kafka behavior).
- Each consumer still processes sequentially within its assigned partitions (offset ordering preserved).
- The `KafkaCommitMiddleware` commits per consumer, which is already scoped via `KafkaReceiveFeature.Consumer`.

**Testing:**
- Integration test: topic with 3 partitions, `ConcurrentConsumerCount=3`. Publish 30 messages, assert all 30 are consumed. Assert each partition was consumed by a different consumer (log partition assignment).
- Integration test: `ConcurrentConsumerCount=1` (default) -- existing behavior preserved.
- Unit test: `ConcurrentConsumerCount=0` or negative -- throw `ArgumentOutOfRangeException`.

**Rollback:** Revert to single consumer. Default is 1, so no behavioral change for existing users who don't set the option.

**Future:** Within-partition concurrency (channel-based dispatch with ordered offset commits) is a separate Phase 5 change, not included in this plan.

---

## Phase 5: Performance Optimizations (Allocations)

**Goal:** Reduce per-message allocations on both dispatch and receive hot paths.
**Risk:** Low-medium -- internal implementation changes, no API surface changes.
**PR scope:** Each sub-phase is its own PR.

### 5a. Pool IValueTaskSource for dispatch (Finding #5)

**What's wrong:** Every `DispatchAsync` call allocates a `new TaskCompletionSource` + its internal `Task<TResult>`. The existing TODO comment on line 86 of `KafkaDispatchEndpoint.cs` acknowledges this.

**Files:**
- New: `src/Mocha.Transport.Kafka/PooledDispatchAwaitable.cs` -- implements `IValueTaskSource` backed by `ManualResetValueTaskSourceCore<bool>`, with `ObjectPool<T>` integration.
- Modified: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs` -- replace `new TaskCompletionSource()` with pool get/return.
- Modified: `src/Mocha.Transport.Kafka/Connection/KafkaConnectionManager.cs` -- change `_inflightDispatches` from `ConcurrentDictionary<TaskCompletionSource, byte>` to `ConcurrentDictionary<PooledDispatchAwaitable, byte>`.

**Design:**

```csharp
internal sealed class PooledDispatchAwaitable : IValueTaskSource
{
    private ManualResetValueTaskSourceCore<bool> _core;
    private static readonly ObjectPool<PooledDispatchAwaitable> s_pool = ObjectPool.Create<PooledDispatchAwaitable>();

    public static PooledDispatchAwaitable Rent() => s_pool.Get();
    public void Return() { _core.Reset(); s_pool.Return(this); }

    public void SetResult() => _core.SetResult(true);
    public void SetException(Exception ex) => _core.SetException(ex);
    public void SetCanceled() => _core.SetException(new OperationCanceledException());

    public ValueTask AsValueTask() => new(this, _core.Version);

    // IValueTaskSource implementation
    public void GetResult(short token) => _core.GetResult(token);
    public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);
    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
        => _core.OnCompleted(continuation, state, token, flags);
}
```

Usage in `DispatchAsync`:

```csharp
var awaitable = PooledDispatchAwaitable.Rent();
try
{
    producer.Produce(topicName, message, report =>
    {
        if (report.Error.IsError)
            awaitable.SetException(new KafkaException(report.Error));
        else
            awaitable.SetResult();
        connectionManager.UntrackInflight(awaitable);
    });
    await awaitable.AsValueTask();
}
finally
{
    awaitable.Return();
}
```

**Saves:** 2 allocations per dispatch (TaskCompletionSource + Task).

**Testing:**
- Existing dispatch integration tests should pass unchanged.
- Add a stress test that dispatches 10K messages and verifies GC Gen0 collection count is lower than with TCS approach (or use `[MemoryDiagnoser]` benchmark).
- Verify cancellation still works via `CancellationTokenRegistration`.

**Rollback:** Revert to `new TaskCompletionSource()`.

---

### 5b. Eliminate MessageEnvelope intermediate allocation (Finding #6)

**What's wrong:** `KafkaMessageEnvelopeParser.Parse` creates a `new MessageEnvelope` with ~15 string fields, plus a `new Headers(customCount)`. The envelope is immediately consumed by `context.SetEnvelope(envelope)` which copies all fields out of it, making the envelope garbage.

**Files:**
- Modified: `src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaParsingMiddleware.cs` -- instead of `Parse → SetEnvelope`, directly populate the `ReceiveContext` fields.
- Modified: `src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs` -- add a `ParseInto(ConsumeResult, ReceiveContext)` method that writes directly to the context, or refactor `Parse` to return fields individually.
- Modified: `src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs` -- may need a method like `SetFromKafkaHeaders(...)` or make individual setters accessible.

**Approach:** Add a `PopulateContext(ConsumeResult, IReceiveContext)` method to `KafkaMessageEnvelopeParser` that extracts headers from the `ConsumeResult` directly into `ReceiveContext` properties, bypassing the intermediate `MessageEnvelope`.

The existing `SetEnvelope` method and `MessageEnvelope` class remain for other transports. The Kafka-specific optimization adds a parallel path.

**Saves:** 1 `MessageEnvelope` class + 1 `Headers` + 1 `List<HeaderValue>` per received message.

**Testing:**
- All existing receive integration tests must pass unchanged.
- The receive context should have identical state after `PopulateContext` as after `Parse + SetEnvelope`.

**Rollback:** Revert to `Parse + SetEnvelope` path.

---

### 5c. Reduce header byte[] allocations on dispatch (Finding #7)

**What's wrong:** `BuildKafkaHeaders` calls `Encoding.UTF8.GetBytes(...)` for each non-null header field (12+ calls), each allocating a `byte[]`. Additionally, `SelectKey` allocates a `byte[]` for the message key.

**File:** `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:171-250`

**Approach:** This is constrained by `Confluent.Kafka.Headers.Add(string, byte[])` which takes and stores a `byte[]` reference. We cannot eliminate allocations entirely, but we can reduce them:

1. **Cache well-known constant values:** Content types like `"application/json"` are repeated across every message. Cache their UTF-8 byte representations as static `byte[]` fields. Same for message type strings that repeat per message type.

2. **Use stackalloc + Encoding for GUIDs:** MessageId, CorrelationId, ConversationId are GUIDs (36 bytes UTF-8). Use `stackalloc byte[36]` + `Encoding.UTF8.GetBytes(span, destination)`, then copy to a rented/new byte[] only for the Confluent.Kafka API.

3. **SentAt formatting:** Use `Utf8Formatter.TryFormat` to write `DateTimeOffset` directly to a byte buffer, avoiding the intermediate `ToString("O")` string allocation.

**Saves:** Variable -- depends on how many fields repeat across messages. At minimum: 1 string allocation for SentAt, cached byte[] for content type.

**Testing:**
- Existing dispatch tests pass unchanged.
- Round-trip test: dispatch a message, consume it, verify all header values are identical.

**Rollback:** Revert to `Encoding.UTF8.GetBytes` for each field.

---

### 5d. Batch offset commits (Finding #8)

**What's wrong:** `KafkaCommitMiddleware` calls `feature.Consumer.Commit(feature.ConsumeResult)` synchronously after every single message. Each commit is a round-trip to the Kafka coordinator (~5-20ms), capping throughput at ~50-200 msg/s per partition.

**Approach:** Replace per-message synchronous commit with periodic batch commit. Use `StoreOffset` (stores offset locally without committing to broker) after each message, then commit accumulated offsets periodically.

**File:** `src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaCommitMiddleware.cs`

**Change:**

```csharp
// BEFORE:
feature.Consumer.Commit(feature.ConsumeResult);

// AFTER:
feature.Consumer.StoreOffset(feature.ConsumeResult);
```

Combined with enabling `EnableAutoCommit = false` + `AutoCommitIntervalMs` via a periodic commit timer in the consume loop, or manually calling `Commit()` every N messages or every T milliseconds.

**Design decision:** Use a simple counter-based approach in the commit middleware:

```csharp
internal sealed class KafkaCommitMiddleware
{
    private int _uncommittedCount;
    private DateTimeOffset _lastCommitTime;
    private const int BatchSize = 100;
    private static readonly TimeSpan CommitInterval = TimeSpan.FromSeconds(5);

    public async ValueTask InvokeAsync(IReceiveContext context, ReceiveDelegate next)
    {
        await next(context);

        var feature = context.Features.GetOrSet<KafkaReceiveFeature>();
        feature.Consumer.StoreOffset(feature.ConsumeResult);
        _uncommittedCount++;

        if (_uncommittedCount >= BatchSize || DateTimeOffset.UtcNow - _lastCommitTime >= CommitInterval)
        {
            feature.Consumer.Commit();
            _uncommittedCount = 0;
            _lastCommitTime = DateTimeOffset.UtcNow;
        }
    }
}
```

**Trade-off:** On crash, up to `BatchSize` messages may be redelivered (vs 1 with per-message commit). This is consistent with at-least-once semantics and is the standard Kafka pattern.

**Testing:**
- Integration test: process 200 messages, verify all offsets committed (consumer.Position after test).
- Integration test: simulate crash mid-batch, verify redelivery of uncommitted messages.
- Benchmark: compare throughput with per-message vs batch commit.

**Rollback:** Revert to per-message `Commit(consumeResult)`.

---

## Phase 6: Reply Topic Lifecycle (Finding #12)

**Goal:** Clean up temporary reply topics on graceful shutdown to prevent topic proliferation.
**Risk:** Low -- additive change in shutdown path; failure to delete is benign (1-hour retention handles cleanup).
**PR scope:** ~20 lines changed in 2 files.

### 6. Reply topic cleanup on shutdown

**What's wrong:** Reply topics (`response-{guid:N}`) are created with `IsTemporary=true` and 1-hour retention, but are never deleted on shutdown. Over time, with frequent deployments or scaling events, orphaned reply topics accumulate in the Kafka cluster.

**Files:**
- `src/Mocha.Transport.Kafka/KafkaMessagingTransport.cs:300-307` (DisposeAsync)
- `src/Mocha.Transport.Kafka/Connection/KafkaConnectionManager.cs` -- add `DeleteTopicsAsync` method

**Change in KafkaConnectionManager:**

```csharp
public async Task DeleteTemporaryTopicsAsync(IReadOnlyList<string> topicNames)
{
    if (topicNames.Count == 0) return;

    try
    {
        var adminClient = GetOrCreateAdminClient();
        await adminClient.DeleteTopicsAsync(topicNames);
    }
    catch (Exception)
    {
        // Best-effort cleanup -- failure is non-fatal since retention handles it
    }
}
```

**Change in KafkaMessagingTransport.DisposeAsync:**

```csharp
public override async ValueTask DisposeAsync()
{
    if (ConnectionManager is not null)
    {
        // Delete temporary topics (reply topics) on graceful shutdown
        var temporaryTopics = Topology.Topics
            .Where(t => t.IsTemporary)
            .Select(t => t.Name)
            .ToList();

        if (temporaryTopics.Count > 0)
        {
            await ConnectionManager.DeleteTemporaryTopicsAsync(temporaryTopics);
        }

        await ConnectionManager.DisposeAsync();
    }
}
```

**Testing:**
- Integration test: start a bus with a reply endpoint, verify reply topic exists, stop the bus, verify reply topic is deleted.
- Integration test: simulate shutdown failure (admin client authorization error) -- verify shutdown completes without throwing.

**Rollback:** Remove the deletion call. Behavior reverts to relying on 1-hour retention for cleanup.

---

## Phase Summary

| Phase | Findings | Risk | Lines Changed | Independent? |
|-------|----------|------|---------------|-------------|
| 1: Correctness | #1, #2 | Low | ~10 | Yes |
| 2: Topology & Safety | #10, #11, #9 | Low-Med | ~40 | Yes |
| 3: Retry Middleware | #3 | Medium | ~150 (new) | Yes |
| 4: Concurrent Consumers | #4 | Med-High | ~100 | Yes |
| 5a: Pool IValueTaskSource | #5 | Low-Med | ~80 (new) | Yes |
| 5b: Eliminate envelope | #6 | Low-Med | ~60 | Yes |
| 5c: Header allocations | #7 | Low | ~40 | Yes |
| 5d: Batch commits | #8 | Medium | ~30 | Yes (after Phase 4 design settled) |
| 6: Reply cleanup | #12 | Low | ~20 | Yes |

**Total: 9 independent PRs, each shippable on its own.**

---

## Dependency Notes

- Phase 1 has zero dependencies. Ship immediately.
- Phase 2 has zero dependencies on Phase 1. Can ship in parallel.
- Phase 3 has zero dependencies on Phases 1-2. Can ship in parallel.
- Phase 4 has zero dependencies on Phases 1-3. Can ship in parallel. However, **Phase 5d (batch commits) should be designed with Phase 4 in mind** -- batch commits with concurrent consumers need per-consumer offset tracking.
- Phase 5a-5c have zero dependencies on each other. Can ship in parallel.
- Phase 5d depends on Phase 4's design being settled (needs to know if offset tracking is per-consumer or per-endpoint).
- Phase 6 has zero dependencies. Can ship anytime.

**Recommended implementation order:** Phase 1 first (smallest, highest correctness value), then Phases 2+3 in parallel, then Phase 4, then Phase 5 sub-phases, then Phase 6.

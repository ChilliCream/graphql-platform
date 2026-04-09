# Minimal-Change Approach

## Fix 1 (CRITICAL): Consumer groups never auto-provisioned for dynamic receive endpoints

**Files changed:** `EventHubReceiveEndpointTopologyConvention.cs`

**Change:** After the existing topic-creation block (line 23-30), add a subscription-creation block that ensures a `EventHubSubscription` exists for the endpoint's consumer group + hub name combination. Check `_subscriptions` for an existing match on `(TopicName, ConsumerGroup)` before adding to avoid duplicates.

```
// After the existing topic block at line 30:
if (configuration.ConsumerGroup is not "$Default"
    && topology.Subscriptions.All(s => s.TopicName != configuration.HubName
        || s.ConsumerGroup != configuration.ConsumerGroup))
{
    topology.AddSubscription(new EventHubSubscriptionConfiguration
    {
        TopicName = configuration.HubName,
        ConsumerGroup = configuration.ConsumerGroup,
        AutoProvision = configuration.AutoProvision
    });
}
```

**Why minimal:** Single insertion point, reuses existing `AddSubscription` method and `EventHubSubscriptionConfiguration` type. No new classes. The `$Default` skip matches what `EventHubProvisioner.ProvisionSubscriptionAsync` already does (it returns early for `$Default`), so this avoids pointless no-op entries.

**Risks:** None significant. `AddSubscription` is already called from `OnAfterInitialized` for statically-configured subscriptions -- this follows the same path. The `topology.Subscriptions` list could have duplicates if called concurrently, but topology discovery runs single-threaded in the setup phase.

---

## Fix 2 (CRITICAL): Batch dispatcher can violate partition key ordering

**Files changed:** `EventHubBatchDispatcher.cs`

**Change:** The existing code at lines 164-178 already flushes the current batch when the partition key changes. The scout report's "ordering violation" scenario is actually about cross-batch ordering across time -- Event A (user-1) in batch 1 and Event C (user-1) in batch 3 could reorder if batch 2 (user-2) experiences a delay. However, this is inherent to Event Hubs: the SDK's `SendAsync` for a given partition key guarantees the events arrive in the order they're sent. Since `DrainAndSendAsync` is single-threaded and `SendBatchAsync` is awaited, batches are sent sequentially. Events A and C will be sent in order; a network hiccup on batch 2 would block batch 3 from sending until batch 2 completes.

**Re-analysis:** The current code is actually correct for same-producer ordering. The single-threaded drain loop + sequential `await SendBatchAsync` ensures A is sent before C. The only real risk is if `SendAsync` returns success but the broker hasn't persisted -- but that's an SDK-level guarantee, not something we control.

**Actual change needed: None.** The current implementation is correct. The flush-on-partition-change + sequential sends preserves ordering within the same partition key across batch boundaries. Mark this as "reviewed, no change needed" in the plan.

**Alternative if team disagrees:** If we truly want belt-and-suspenders, we could add a comment documenting the ordering guarantee. But no code change is necessary.

---

## Fix 3 (HIGH): No time-based checkpoint flushing

**Files changed:** `MochaEventProcessor.cs`

**Change:** Add a `ConcurrentDictionary<string, long> _partitionLastCheckpointTime` that tracks the last checkpoint time per partition. In `OnProcessingEventBatchAsync`, alongside the existing `counter >= _checkpointInterval` check, also checkpoint if time since last checkpoint exceeds a threshold (e.g., 30 seconds).

Specifically, modify the checkpoint condition at line 120:

```csharp
// Before:
if (counter >= _checkpointInterval || index == lastIndex)

// After:
var now = Environment.TickCount64;
var lastTime = _partitionLastCheckpointTimes.GetOrAdd(partition.PartitionId, now);
var timeSinceCheckpoint = now - lastTime;

if (counter >= _checkpointInterval || index == lastIndex || timeSinceCheckpoint >= _checkpointTimeoutMs)
```

Add a new field `_partitionLastCheckpointTimes` (ConcurrentDictionary<string, long>) and `_checkpointTimeoutMs` (long, default 30_000). Reset `_partitionLastCheckpointTimes[partitionId]` alongside the existing `_partitionCounters[partitionId] = 0` reset.

The `_checkpointTimeoutMs` can be hardcoded to 30_000 (30 seconds). No need for a configuration option yet -- it can be made configurable later if needed.

**Why minimal:** Adds 2 fields and ~5 lines of logic to the existing checkpoint block. No new methods, no new classes, no configuration surface changes.

**Risks:** `Environment.TickCount64` has millisecond precision which is fine for 30-second intervals. The time check adds a tiny overhead per event but is negligible compared to the checkpoint I/O itself.

---

## Fix 4 (HIGH): No graceful shutdown checkpoint flushing

**Files changed:** `MochaEventProcessor.cs`

**Change:** Add a public `FlushCheckpointsAsync` method that iterates `_partitionCounters` and writes a final checkpoint for any partition with a non-zero counter. This requires tracking the last successful sequence number per partition.

Add a new field: `ConcurrentDictionary<string, long> _partitionLastSequence` that is updated in `OnProcessingEventBatchAsync` whenever `lastSuccessfulSequence > -1`, set to the sequence number after each event is processed.

```csharp
internal async Task FlushCheckpointsAsync(CancellationToken cancellationToken)
{
    foreach (var (partitionId, counter) in _partitionCounters)
    {
        if (counter > 0 && _partitionLastSequence.TryGetValue(partitionId, out var seq))
        {
            await _checkpointStore.SetCheckpointAsync(
                _fullyQualifiedNamespace,
                _eventHubName,
                _consumerGroup,
                partitionId,
                seq,
                cancellationToken);
            _partitionCounters[partitionId] = 0;
        }
    }
}
```

**Files changed:** `EventHubReceiveEndpoint.cs` (line 141)

Call `_processor.FlushCheckpointsAsync()` after `StopProcessingAsync()`:

```csharp
await _processor.StopProcessingAsync(cancellationToken);
await _processor.FlushCheckpointsAsync(cancellationToken);
_processor = null;
```

**Why minimal:** One new field on MochaEventProcessor, one new method, one call site in the existing OnStopAsync. The `StopProcessingAsync` already waits for in-flight processing to complete, so by the time we flush, `_partitionLastSequence` is stable.

**Risks:** If `StopProcessingAsync` throws, we skip the flush. This is acceptable -- if stop fails, the state is indeterminate anyway.

---

## Fix 5 (HIGH): `EventHubReceiveEndpoint.Subscription` property never populated

**Files changed:** `EventHubReceiveEndpoint.cs`

**Change:** In `OnComplete` (line 51-61), after resolving Topic, also look up the subscription in the topology:

```csharp
// After: Source = Topic;
Subscription = topology.Subscriptions
    .FirstOrDefault(s => s.TopicName == configuration.HubName
        && s.ConsumerGroup == (configuration.ConsumerGroup ?? "$Default"));
```

This is nullable -- it will remain null for `$Default` consumer groups that have no explicit subscription entry, which is correct behavior.

**Why minimal:** Single line addition in existing method. No new abstractions.

**Risks:** None. The property is already nullable, callers already handle null.

---

## Fix 6 (HIGH): No multi-instance deployment safety checks

**Files changed:** `MochaEventProcessor.cs`

**Change:** Add a log warning in `ClaimOwnershipAsync` when `_ownershipStore is null` and the number of claimed partitions suggests multi-instance conflict. However, we can't detect multi-instance from a single instance's perspective without an ownership store.

The minimal approach: Add a warning log at processor start time when ownership store is null, so operators know they're in single-instance mode.

**Files changed:** `EventHubReceiveEndpoint.cs`

In `OnStartAsync`, after creating the processor but before `StartProcessingAsync`, log a warning if `ownershipStore is null`:

```csharp
if (ownershipStore is null)
{
    logger.LogWarning(
        "Event Hub processor for '{HubName}' consumer group '{ConsumerGroup}' is running "
        + "in single-instance mode (no OwnershipStore configured). If multiple instances of "
        + "this service are deployed, configure an OwnershipStore to prevent duplicate processing.",
        Topic.Name, _consumerGroup);
}
```

**Why minimal:** Single log statement, no behavior change, no new classes. Operators get a clear signal in logs.

**Risks:** None. Log-only change.

---

## Fix 7 (MEDIUM): String allocations on hot paths

### 7a: `ParseEnclosedMessageTypes` allocation

**Files changed:** `EventHubMessageEnvelopeParser.cs`

**Change:** Optimize the common single-type case (most messages have exactly one enclosed type). The current code always calls `Split(';')` which allocates a string array:

```csharp
// Before (line 97):
return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];

// After:
if (!typesStr.Contains(';'))
{
    return [typesStr];
}
return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
```

The single-element `ImmutableArray` is still an allocation, but we skip the `Split` + intermediate array allocation.

### 7b: `string.Join` on dispatch for single type

**Files changed:** `EventHubDispatchEndpoint.cs`

**Change:** Optimize the common single-type case:

```csharp
// Before (line 142-145):
if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
{
    appProps[EventHubMessageHeaders.EnclosedMessageTypes] = string.Join(";", types);
}

// After:
if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
{
    appProps[EventHubMessageHeaders.EnclosedMessageTypes] =
        types.Length == 1 ? types[0] : string.Join(";", types);
}
```

### 7c: Reply hub name allocation

**Files changed:** `EventHubDispatchEndpoint.cs`

The `new string(lastSegment)` at line 53 allocates for every reply dispatch. This is unavoidable without caching since the hub name comes from the destination address which varies per message. Leave as-is -- the allocation is per-reply-message, not per-event, and replies are low-throughput.

**Why minimal:** Two single-line conditional checks in existing methods. No new types, no span gymnastics, no breaking changes.

**Risks:** None. The behavior is identical -- just avoids unnecessary allocations in the common case.

---

## Summary of Changes

| # | Severity | Files Changed | Lines Changed (approx) |
|---|----------|---------------|----------------------|
| 1 | Critical | EventHubReceiveEndpointTopologyConvention.cs | +10 |
| 2 | Critical | None (reviewed, correct as-is) | 0 |
| 3 | High | MochaEventProcessor.cs | +12 |
| 4 | High | MochaEventProcessor.cs, EventHubReceiveEndpoint.cs | +20, +1 |
| 5 | High | EventHubReceiveEndpoint.cs | +3 |
| 6 | High | EventHubReceiveEndpoint.cs | +7 |
| 7 | Medium | EventHubMessageEnvelopeParser.cs, EventHubDispatchEndpoint.cs | +4, +1 |

**Total: ~58 lines added across 5 files. No new files. No new classes. No new abstractions.**

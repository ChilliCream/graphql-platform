# Azure Event Hub Transport — Fix Plan

## Problem Summary

A three-way review (efficiency, topology, competitive analysis) identified 7 issues in the Event Hub transport. After divergent design (3 architecture proposals) and cross-review (2 independent reviewers verifying each against the source code), the consensus plan is below.

**Key finding**: Fix 2 (batch dispatcher ordering) was unanimously found to be **NOT a bug** after all 4 reviewers traced the execution path in `EventHubBatchDispatcher.cs`. Sends are fully serialized via a single background task with awaited `SendBatchAsync` calls. No change needed.

## Scope

~58 lines added across 5 files. No new files, no new classes, no new configuration surface.

## Fixes

### Phase 1: Topology Correctness

#### Fix 1 — Auto-provision consumer groups for dynamic receive endpoints (CRITICAL)

**Root cause**: `EventHubReceiveEndpointTopologyConvention.DiscoverTopology()` creates `EventHubTopic` entries but never creates `EventHubSubscription` entries. When `OnBeforeStartAsync()` provisions resources, `_topology.Subscriptions` is empty for convention-discovered endpoints. Non-`$Default` consumer groups are never created in Azure.

**File**: `Conventions/EventHubReceiveEndpointTopologyConvention.cs`

**Change**: After the existing topic-creation block (~line 30), add a subscription-creation block. Skip `$Default` (it always exists on every Event Hub — adding it is dead code that hits the provisioner's early return).

```
// Pseudo:
var consumerGroup = configuration.ConsumerGroup ?? "$Default";
if (consumerGroup != "$Default" 
    && !topology.Subscriptions.Any(s => s.TopicName == hubName && s.ConsumerGroup == consumerGroup))
{
    topology.AddSubscription(new EventHubSubscriptionConfiguration { ... });
}
```

#### Fix 5 — Populate `EventHubReceiveEndpoint.Subscription` property (HIGH)

**Root cause**: Property declared but never assigned. Depends on Fix 1 creating subscription entries.

**File**: `EventHubReceiveEndpoint.cs`

**Change**: In `OnComplete`, after `Source = Topic`, add:
```
Subscription = topology.Subscriptions
    .FirstOrDefault(s => s.TopicName == hubName && s.ConsumerGroup == _consumerGroup);
```

Remains null for `$Default`-only endpoints (correct — no explicit subscription entry exists).

### Phase 2: Checkpoint Reliability

#### Fix 4 — Graceful shutdown checkpoint flushing (HIGH)

**Root cause**: `OnStopAsync` calls `StopProcessingAsync()` which waits for in-flight processing, but any events processed since the last count-based checkpoint are lost. Every restart/deployment reprocesses them.

**Files**: `Connection/MochaEventProcessor.cs`, `EventHubReceiveEndpoint.cs`

**Changes**:
1. Add `ConcurrentDictionary<string, long> _partitionLastSequence` to MochaEventProcessor
2. Update it after each successful event processing in `OnProcessingEventBatchAsync`
3. Add `FlushCheckpointsAsync()` method that iterates partitions with counter > 0 and writes final checkpoints
4. Call it from `EventHubReceiveEndpoint.OnStopAsync` after `StopProcessingAsync()`

```
// In OnStopAsync:
await _processor.StopProcessingAsync(ct);     // waits for in-flight
await _processor.FlushCheckpointsAsync(ct);   // flushes remaining
_processor = null;
```

Edge case: if all events in the last batch failed, `_partitionLastSequence` still holds the previous successful sequence — flush checkpoints at that point, failed events will be redelivered (correct at-least-once behavior).

**Known limitation**: This flush only runs during full endpoint shutdown (`OnStopAsync`). During partition rebalancing, the Azure SDK's `EventProcessor<TPartition>` releases partitions via `OnPartitionClosingAsync`, which `MochaEventProcessor` does not override. Events processed since the last count-based checkpoint on a rebalanced-away partition are NOT flushed. Adding rebalancing-aware flush is a follow-up concern — it requires per-partition state tracking in `OnPartitionClosingAsync` and is out of scope for this fix pass.

#### Fix 3 — Time-based checkpoint flushing (HIGH)

**Root cause**: Only count-based checkpointing (every 100 events). Low-throughput periods leave events uncheckpointed indefinitely.

**File**: `Connection/MochaEventProcessor.cs`

**Change**: Add `ConcurrentDictionary<string, long> _partitionLastCheckpointTime` and a hardcoded 30-second timeout. In `OnProcessingEventBatchAsync`, alongside the existing `counter >= _checkpointInterval` check, also checkpoint if elapsed time exceeds 30s.

```
// Pseudo:
var now = Environment.TickCount64;
var lastTime = _partitionLastCheckpointTimes.GetOrAdd(partitionId, now);
if (counter >= _checkpointInterval || index == lastIndex || (now - lastTime) >= 30_000)
{
    // checkpoint + reset counter + update lastTime
}
```

Uses `Environment.TickCount64` (monotonic, ~5ns) rather than `DateTimeOffset.UtcNow` (~20ns, non-monotonic).

No configuration surface — hardcoded 30s is a reasonable default. Can be made configurable later if needed.

### Phase 3: Safety & Optimization

#### Fix 6 — Multi-instance deployment safety warning (HIGH)

**Root cause**: Single-instance mode (no `IPartitionOwnershipStore`) silently claims all partitions. Multi-instance deployments duplicate-process every message with no warning.

**File**: `EventHubReceiveEndpoint.cs`

**Change**: In `OnStartAsync`, before `StartProcessingAsync`, log a warning when persistent checkpoints are configured but no ownership store is present. This avoids noisy warnings in dev/test (where `InMemoryCheckpointStore` is the default and single-instance is expected):

```
if (ownershipStore is null && checkpointStore is not InMemoryCheckpointStore)
{
    logger.LogWarning(
        "Event Hub processor for '{HubName}' consumer group '{ConsumerGroup}' has "
        + "persistent checkpoints but no OwnershipStore. Multiple instances "
        + "will duplicate event processing.", hubName, consumerGroup);
}
```

The heuristic: if you've configured `BlobStorageCheckpointStore` (persistent, shared), you're deploying for real and should have an ownership store. If you're using the default `InMemoryCheckpointStore`, you're in dev/test mode — no warning needed.

Log-only change. No behavior change.

#### Fix 7 — String allocation optimizations on hot paths (MEDIUM)

**Files**: `EventHubMessageEnvelopeParser.cs`, `EventHubDispatchEndpoint.cs`

**7a — Receive path**: In `ParseEnclosedMessageTypes`, check for `;` before calling `Split()`. Single-type messages (the common case) skip the Split + array allocation:
```
if (!typesStr.Contains(';'))
    return [typesStr];
return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
```

**7b — Dispatch path**: In `DispatchAsync`, avoid `string.Join` for single-type:
```
appProps[...] = types.Length == 1 ? types[0] : string.Join(";", types);
```

Saves 2-3 allocations per message in the common single-type case.

### Fix 2 — Batch dispatcher ordering: NO CHANGE NEEDED

All 4 reviewers independently verified: sends are fully serialized. The single background task (`Task.Run(ProcessLoopAsync)`) with awaited `DrainAndSendAsync` and awaited `SendBatchAsync` calls guarantees partition key ordering. The original topology review's concern was based on a misread of the concurrency model.

## Implementation Order

```
Fix 1 (consumer groups)     ──┐
Fix 5 (subscription prop)   ──┘ Phase 1: Topology
Fix 4 (shutdown flush)      ──┐
Fix 3 (time checkpoints)    ──┘ Phase 2: Checkpoints
Fix 6 (multi-instance warn) ──┐
Fix 7 (string allocations)  ──┘ Phase 3: Safety & Perf
```

Fix 5 depends on Fix 1 (subscriptions must exist in topology before lookup). Fix 3 builds on Fix 4 (same code area — `_partitionLastSequence` field from Fix 4 is reused). All other fixes are independent.

## What Was Considered and Rejected

| Proposal | Rejected Because |
|----------|-----------------|
| Extract `CheckpointManager` class | ~25 lines of inline changes don't justify a new type. No other Mocha transport uses separate checkpoint management. |
| Add `CheckpointTimeInterval` configuration option | Premature. Hardcoded 30s is reasonable. Config can be added later if users need tuning. |
| Group-by-partition-key batch restructuring | Solves a non-existent problem. Adds dictionary allocation per drain cycle, changes batching semantics. |
| Partition key boundary debug logging | Low value — fires on every interleaved key change, which is normal operation. |
| Reply hub name string interning via ConcurrentDictionary | Reply path is low-throughput, not a hot path. Memory cost of intern pool unjustified. |

## Verification

Each fix should be verified by:
1. Building: `dotnet build src/Mocha/src/Mocha.Transport.AzureEventHub`
2. Running existing tests with `--filter EventHub`
3. Fix 1: Verify dynamically-discovered endpoints have their consumer groups in `_topology.Subscriptions` after topology discovery
4. Fix 3/4: Verify checkpoints are written on time interval expiry and on graceful shutdown

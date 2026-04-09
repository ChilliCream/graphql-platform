# Structural Approach: Azure Event Hub Transport Fixes

## Philosophy

Several of the 7 issues share root causes in missing structural pieces rather than simple bugs. Patching each individually would leave the architecture fragile — the same gaps would produce new issues as the transport evolves. This approach identifies three structural gaps and proposes targeted restructuring for each.

---

## Area 1: Topology Participation Gap (Issues #1, #5)

### Structural Issue

The Event Hub receive endpoint does not fully participate in the topology lifecycle. Compare with RabbitMQ:

- **RabbitMQ**: `RabbitMQReceiveEndpointTopologyConvention` creates queues, exchanges, AND bindings — every resource the endpoint needs to consume is represented in topology.
- **Event Hub**: `EventHubReceiveEndpointTopologyConvention` creates Topics but never creates Subscriptions (consumer groups). The `Subscription` property on `EventHubReceiveEndpoint` is declared but never populated.

This is not two separate bugs — it's one structural omission: the convention doesn't know about consumer groups, and `OnComplete` doesn't wire them up.

### Why Patching Isn't Sufficient

Adding a subscription in `OnComplete` alone would fix #5 but not #1 — provisioning runs before endpoints start, iterating `_topology.Subscriptions`. The subscription must be in topology before `OnBeforeStartAsync` runs. Adding it in two separate places (convention + endpoint) creates a fragile dual-path that will diverge.

### Proposed Restructuring

**Single fix point**: the topology convention, which is the canonical place where endpoints declare their resource needs. This mirrors how RabbitMQ's convention creates queues AND bindings.

**Changes to `EventHubReceiveEndpointTopologyConvention.DiscoverTopology`**:
After creating the Topic entry (existing line 25), also create a Subscription entry for the endpoint's consumer group:

```
topology.AddSubscription(new EventHubSubscriptionConfiguration
{
    TopicName = configuration.HubName,
    ConsumerGroup = configuration.ConsumerGroup,
    AutoProvision = configuration.AutoProvision
});
```

Need to add deduplication: `EventHubMessagingTopology.AddSubscription` currently allows duplicates (unlike `AddTopic` which throws). Add a check — if a subscription with the same (TopicName, ConsumerGroup) already exists, return it rather than creating a duplicate. This matches how `AddTopic` handles the "already exists" case (though it throws — for subscriptions, idempotent return is more appropriate since multiple endpoints may share a consumer group).

**Changes to `EventHubReceiveEndpoint.OnComplete`**:
After resolving `Topic` from topology, also resolve `Subscription`:

```
Subscription = topology.Subscriptions
    .FirstOrDefault(s => s.TopicName == configuration.HubName
        && s.ConsumerGroup == _consumerGroup);
```

This populates the property (fixing #5) without creating a second path for subscription creation.

### Files Changed

| File | Change |
|------|--------|
| `EventHubReceiveEndpointTopologyConvention.cs` | Add subscription creation after topic creation |
| `EventHubMessagingTopology.cs` | Add deduplication to `AddSubscription` (return existing if same topic+group) |
| `EventHubReceiveEndpoint.cs` | Populate `Subscription` in `OnComplete` via topology lookup |

### Trade-offs

- **Gained**: Consumer groups auto-provisioned for all dynamically created endpoints; `Subscription` property always populated; single source of truth for subscription creation.
- **Complexity**: Minimal — ~10 lines added across 3 files.
- **Justification**: This directly mirrors the RabbitMQ pattern (convention creates all topology resources), not a new abstraction.

---

## Area 2: Checkpoint Management (Issues #3, #4)

### Structural Issue

`MochaEventProcessor.OnProcessingEventBatchAsync` directly manages checkpoint state (counters, interval checks, store calls). This monolithic approach makes it impossible to:
- Add time-based flushing (no timer infrastructure)
- Flush on shutdown (processor doesn't expose its checkpoint state)
- Test checkpoint logic independently

The Azure SDK's `EventProcessor<T>` base class owns the processing loop — we can't insert a timer into its event delivery path. Checkpoint management needs to be a collaborator, not inline code.

### Why Patching Isn't Sufficient

Adding a timer directly to `MochaEventProcessor` would work for #3, but shutdown flush (#4) requires the receive endpoint to trigger "flush all partitions now" — which means exposing checkpoint state outside the processor. Once you expose state, you have a checkpoint manager whether you call it one or not. Better to make it explicit.

### Proposed Restructuring

Extract a `CheckpointManager` class that owns all checkpoint state and policy.

**New type: `CheckpointManager`** (internal, in `Connection/` directory)

Purpose: Encapsulates per-partition checkpoint tracking with both count-based and time-based flush triggers, plus an explicit `FlushAsync` for graceful shutdown.

```
internal sealed class CheckpointManager : IAsyncDisposable
{
    // Constructor: ICheckpointStore, namespace, hub, consumerGroup, 
    //              countInterval, timeInterval, ILogger
    
    // Called by MochaEventProcessor after each successful event
    ValueTask TrackAsync(string partitionId, long sequenceNumber, CancellationToken ct);
    
    // Called by EventHubReceiveEndpoint.OnStopAsync before stopping processor
    ValueTask FlushAsync(CancellationToken ct);
}
```

Internal behavior:
- Maintains `ConcurrentDictionary<string, PartitionCheckpointState>` where state tracks: last sequence number, count since last checkpoint, last checkpoint time.
- `TrackAsync`: increments count, checks if count >= interval OR elapsed time >= time interval. If either, calls `_checkpointStore.SetCheckpointAsync` and resets state.
- Time-based: Uses a simple elapsed-time check on each `TrackAsync` call (piggybacks on event processing — no background timer needed). This is cheaper than a timer and sufficient because if no events arrive, there's nothing to checkpoint.
- `FlushAsync`: iterates all partitions with pending (uncheckpointed) sequences, writes them all. Called during shutdown.
- `IAsyncDisposable`: no-op (no timer to dispose), but follows the pattern for future extensibility.

**Changes to `MochaEventProcessor`**:
- Remove `_partitionCounters` dictionary and checkpoint logic from `OnProcessingEventBatchAsync`.
- Accept `CheckpointManager` as constructor parameter instead of `ICheckpointStore` + `checkpointInterval`.
- After successful `_messageHandler` call, delegate to `_checkpointManager.TrackAsync(partitionId, sequenceNumber, ct)`.

**Changes to `EventHubReceiveEndpoint`**:
- Create `CheckpointManager` in `OnStartAsync` (where checkpoint store is resolved).
- Pass it to `MochaEventProcessor` constructor.
- In `OnStopAsync`: call `_checkpointManager.FlushAsync(ct)` BEFORE `_processor.StopProcessingAsync(ct)`. This ensures all processed-but-uncheckpointed events are flushed before the processor shuts down.

**Changes to `EventHubReceiveEndpointConfiguration`**:
- Add `TimeSpan? CheckpointTimeInterval { get; set; }` — defaults to `TimeSpan.FromSeconds(30)`.
- Add corresponding descriptor method.

### Files Changed

| File | Change |
|------|--------|
| `Connection/CheckpointManager.cs` | **New file** — checkpoint state + count/time flush logic |
| `Connection/MochaEventProcessor.cs` | Remove checkpoint logic, accept CheckpointManager |
| `EventHubReceiveEndpoint.cs` | Create CheckpointManager, flush before stop |
| `Configurations/EventHubReceiveEndpointConfiguration.cs` | Add `CheckpointTimeInterval` property |
| `Descriptors/EventHubReceiveEndpointDescriptor.cs` | Add `CheckpointTimeInterval` method |

### Trade-offs

- **Gained**: Time-based checkpoint flushing; graceful shutdown flush; testable checkpoint logic in isolation; clean separation of concerns in MochaEventProcessor.
- **Complexity**: One new type (~80 lines), modest refactoring of existing code.
- **Justification**: The checkpoint state is already managed — it's just inlined in the processor where it can't be accessed externally. Extracting it makes the existing responsibility boundary explicit. The piggyback-on-event-processing approach for time checks avoids background timers entirely.

---

## Area 3: Batch Dispatcher Partition Integrity (Issue #2)

### Structural Issue

The batch dispatcher uses a sequential "flush on partition change" approach. When events with different partition keys are interleaved, the dispatcher:
1. Accumulates events for key A
2. Sees key B, flushes A, starts B
3. Sees key A again, flushes B, starts new A batch

This creates multiple small batches for the same partition key, and the send operations for these batches can complete out of order, violating the ordering guarantee that partition keys are supposed to provide.

### Why Patching Isn't Sufficient

The sequential flush-on-change approach is fundamentally incompatible with ordering guarantees across batch boundaries. Making the flush synchronous (wait for send to complete before starting next batch) would fix ordering but destroy throughput — the whole point of batching.

### Proposed Restructuring

**Group by partition key before batching.** Instead of a single linear drain, the dispatcher maintains a dictionary of pending-event lists keyed by `(PartitionKey, PartitionId)`. When the timer fires or the channel drains, each group is batched and sent independently.

**Changes to `EventHubBatchDispatcher.DrainAndSendAsync`**:

Replace the sequential drain with:

1. **Drain phase**: Read all available events from the channel (up to timer expiry), grouping them into a `Dictionary<(string? PartitionKey, string? PartitionId), List<PendingEvent>>`.

2. **Send phase**: For each group, create a batch with the appropriate options and send. Groups are independent — they can be sent sequentially (simple) or in parallel (optimization for later). Within each group, events are added to the batch in arrival order, preserving per-key ordering.

This eliminates the interleaving problem entirely. Events for key A are always batched together regardless of how events for key B are interspersed.

**No new types needed.** The grouping uses a local dictionary within the existing `DrainAndSendAsync` method. The `PendingEvent` record is unchanged.

The `_pending` list field becomes a local dictionary per drain cycle. The existing `CreateBatchForOptionsAsync` and `SendBatchAsync` methods are reused unchanged.

### Files Changed

| File | Change |
|------|--------|
| `Connection/EventHubBatchDispatcher.cs` | Restructure `DrainAndSendAsync` to group-then-batch |

### Trade-offs

- **Gained**: Partition key ordering preserved across batch boundaries; fewer small batches (better throughput for interleaved workloads).
- **Complexity**: The drain logic is restructured but not longer. Dictionary allocation per drain cycle is negligible (drain cycles are ~100ms apart).
- **Risk**: Memory use during drain — if thousands of unique partition keys are in flight simultaneously, the dictionary grows. In practice, partition key cardinality within a 100ms window is bounded. If this becomes a concern, add a max-groups limit that falls back to sequential flush.
- **Justification**: The current approach is actively broken for its stated purpose (partition key ordering). The fix is a data structure change, not a new abstraction.

---

## Area 4: Multi-Instance Safety (Issue #6)

### Structural Issue

There is no validation that the transport configuration is safe for multi-instance deployment. If `OwnershipStoreFactory` is null (single-instance mode) but the service is deployed with multiple replicas, all instances claim all partitions and race on checkpoints.

### Why Patching Isn't Sufficient

A runtime warning would help, but developers may not see warnings. The safer approach is a configuration-time check.

### Proposed Restructuring

**Add validation in `OnBeforeStartAsync`** (the transport's pre-start hook, which already exists for provisioning). This is not a new hook — it's adding a check to the existing method.

The check: if `OwnershipStoreFactory` is null AND `CheckpointStoreFactory` is not null (meaning they've configured persistent checkpoints, implying they care about durability, which implies production, which implies possibly multi-instance), log a warning:

```
"Persistent checkpoint store configured without an ownership store. 
 In multi-instance deployments, configure an OwnershipStore to prevent 
 duplicate event processing."
```

This is a **warning, not an error** — single-instance with persistent checkpoints is a valid (and common) development configuration. The warning surfaces the potential issue without blocking legitimate use.

Additionally: document this on the `OwnershipStoreFactory` property and in the descriptor's fluent API.

### Files Changed

| File | Change |
|------|--------|
| `EventHubMessagingTransport.cs` | Add warning check in `OnBeforeStartAsync` |

### Trade-offs

- **Gained**: Developers get a clear signal about a non-obvious deployment risk.
- **Complexity**: ~5 lines.
- **Justification**: Warning-level, not blocking. Zero risk of breaking existing code.

---

## Area 5: String Allocations (Issue #7)

### Structural Issue

String allocations on the dispatch hot path (`new string(lastSegment)` in reply routing, `new string(name)` in dynamic endpoint creation) are avoidable.

### Why Patching Isn't Sufficient

These are isolated allocation sites — patching is sufficient here. No structural change needed.

### Proposed Changes

1. **`EventHubDispatchEndpoint.DispatchAsync` line 53**: The `lastSegment` is a `ReadOnlySpan<char>` extracted from a URI. Use `string.Create` or `stackalloc` + `ToString` is not better — but we can cache reply hub names since they repeat. However, the reply hub name comes from `envelope.DestinationAddress` which varies per message. The allocation is unavoidable for truly dynamic routing.

   **Better approach**: For the reply endpoint specifically (`Kind == DispatchEndpointKind.Reply`), the hub name extracted from the destination address is likely to be one of a small set of known hubs. Use a `ConcurrentDictionary<string, string>` as a string intern pool on the endpoint instance. This avoids repeated allocations for the same destination.

2. **`EventHubMessagingTransport.CreateEndpointConfiguration` lines 221-222**: The `new string(name)` allocations happen during endpoint creation (configuration phase), not the hot dispatch path. These are one-time costs. **No change needed.**

3. **`EventHubBatchDispatcher.CreateBatchForOptionsAsync`**: The `PartitionKey` and `PartitionId` strings are already allocated in `SendEventOptions` — no additional allocation happens here. The `CreateBatchOptions` object is a small allocation but unavoidable (Azure SDK API requires it). **No change needed.**

4. **Partition key comparison** (line 165): Standard `string !=` uses ordinal comparison by default in this context. Already optimal. **No change needed.**

**Net assessment**: Only the reply-path intern pool in `EventHubDispatchEndpoint` is worth doing. The other sites are either one-time costs or unavoidable.

### Files Changed

| File | Change |
|------|--------|
| `EventHubDispatchEndpoint.cs` | Add string intern pool for reply hub name resolution |

### Trade-offs

- **Gained**: Zero allocations on the reply dispatch hot path for repeated destinations.
- **Complexity**: ~5 lines (one `ConcurrentDictionary` field + `GetOrAdd` call).
- **Justification**: Framework code, performance matters. But scoped to where it actually matters.

---

## Summary: Change Matrix

| Area | Issues Fixed | New Types | Files Changed | Lines Added (est.) |
|------|-------------|-----------|---------------|-------------------|
| Topology participation | #1, #5 | None | 3 | ~15 |
| Checkpoint management | #3, #4 | `CheckpointManager` | 5 | ~100 |
| Batch partition integrity | #2 | None | 1 | ~30 (net rewrite of drain method) |
| Multi-instance safety | #6 | None | 1 | ~5 |
| String allocations | #7 | None | 1 | ~5 |
| **Total** | **7** | **1** | **8 unique files** | **~155** |

## Key Design Decisions

### Q1: Should MochaEventProcessor own checkpointing, or delegate to CheckpointManager?

**Delegate.** The processor's job is to receive events from the Azure SDK and route them to the Mocha pipeline. Checkpoint policy (when to flush, time vs count, shutdown flush) is a separate concern. The processor calls `_checkpointManager.TrackAsync()` after each successful event — it doesn't know or care about the flush policy.

### Q2: Should the batch dispatcher group by partition key upfront or keep sequential flush?

**Group upfront.** The sequential approach is fundamentally broken for ordering. Grouping by `(PartitionKey, PartitionId)` into a dictionary before batching is simple, correct, and actually improves throughput for interleaved workloads (fewer, larger batches per key).

### Q3: Is there a base class hook for startup validation?

**No dedicated hook, but `OnBeforeStartAsync` is the right place.** It's already used for provisioning and runs before any endpoint starts. Adding a warning check there is idiomatic — the RabbitMQ transport also does pre-start validation in its equivalent hook (connection verification).

### Q4: How tightly should the Event Hub endpoint mirror RabbitMQ's topology participation?

**Follow the same convention pattern, adapted for Event Hub concepts.** RabbitMQ's convention creates queues + exchanges + bindings. Event Hub's convention should create topics + subscriptions. The endpoint's `OnComplete` resolves both from topology, just as RabbitMQ resolves its queue. The mapping is: Topic = Exchange, Subscription = Queue Binding. The pattern is the same; only the resource types differ.

## Implementation Order

1. **Topology participation** (#1, #5) — smallest change, unblocks provisioning correctness
2. **Checkpoint management** (#3, #4) — largest change, most value
3. **Batch partition integrity** (#2) — isolated to one file, high correctness value
4. **Multi-instance safety** (#6) — trivial addition
5. **String allocations** (#7) — minor optimization

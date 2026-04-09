# Performance Review: Azure Event Hub Transport

Reviewer focus: hot-path allocations, LINQ on hot paths, string allocations, zero-alloc patterns, connection lifecycle, blocking in async, unbounded collections, thread contention, batch dispatcher efficiency.

Cross-referenced against plan.md Section 10 (Performance Design) and RabbitMQ transport patterns.

---

## Critical

### 1. `InMemoryCheckpointStore.BuildKey` allocates a string per message on the receive hot path

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/InMemoryCheckpointStore.cs:41`

`SetCheckpointAsync` is called for every single message processed (from `MochaEventProcessor.OnProcessingEventBatchAsync`). Each call hits `BuildKey`, which does:

```csharp
private static string BuildKey(string ns, string hub, string cg, string pid)
    => string.Concat(ns, "/", hub, "/", cg, "/", pid);
```

This allocates a new string on every message. For a given partition, `ns`, `hub`, `cg`, and `pid` are all constant for the lifetime of the processor. The key should be computed once per partition and cached.

**Suggested fix**: Cache the key per partition. The simplest approach: since `MochaEventProcessor` already knows the namespace/hub/consumerGroup, and partitions are stable, change `InMemoryCheckpointStore` to use a `ConcurrentDictionary<(string ns, string hub, string cg, string pid), long>` with a tuple key (struct, no allocation), or pre-compute and cache the string key per partition using a secondary dictionary. A `ConcurrentDictionary<string, long>` keyed by `partitionId` alone would work if the store is scoped per hub/consumer-group (which it currently is not, but could be).

Alternatively, the simplest zero-alloc fix: change the dictionary key to `(string, string, string, string)` value tuple:
```csharp
private readonly ConcurrentDictionary<(string, string, string, string), long> _checkpoints = new();
```
This avoids the string concatenation entirely. The tuple is a value type and the `ConcurrentDictionary` will use structural equality.

---

### 2. `BlobStorageCheckpointStore.SetCheckpointAsync` allocates per message on the receive hot path

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/BlobStorageCheckpointStore.cs:53-64`

Every message processed calls `SetCheckpointAsync`, which:
1. Calls `GetBlobClient` with an interpolated string (allocation) at line 73
2. Calls `sequenceNumber.ToString()` (allocation) at line 62
3. Calls `Encoding.UTF8.GetBytes(...)` (allocation) at line 62
4. Creates a `new BinaryData(...)` (allocation) at line 62
5. Calls `UploadAsync` (network I/O)

The network I/O per message is the real killer here -- this makes a Blob Storage HTTP call for every single event processed. This will bottleneck throughput severely.

**Suggested fix**: Checkpoint at intervals (every N events or every T seconds), not per message. The plan mentions `CheckpointInterval` in the receive endpoint configuration. This should be implemented: accumulate a counter per partition and only flush the checkpoint when the threshold is reached, or on a timer. The `MochaEventProcessor.OnProcessingEventBatchAsync` should track a counter and only call `SetCheckpointAsync` at the configured interval. The `InMemoryCheckpointStore` per-message checkpointing is fine (fast in-memory), but the blob store needs batching.

For the string allocations: cache the blob client per partition (it's the same blob path every time), and use `stackalloc` + `Utf8Formatter` to format the sequence number without allocating.

---

### 3. Batch dispatcher `DrainAndSendAsync` allocates a new `List<PendingEvent>` and two `CancellationTokenSource` objects per drain cycle

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/EventHubBatchDispatcher.cs:117-202`

Every drain cycle allocates:
- `new List<PendingEvent>()` at line 122
- `new CancellationTokenSource(_maxWaitTime)` at line 126
- `CancellationTokenSource.CreateLinkedTokenSource(...)` at line 127

Under high throughput, `DrainAndSendAsync` runs continuously. The `List<PendingEvent>` could be reused (cleared between cycles). The CTS allocations are harder to avoid but the linked CTS is particularly expensive.

Additionally, when a batch is full and a new batch is started (line 164-165), a new `List<PendingEvent>` is allocated with `pending = []` (default capacity). This can happen multiple times per drain cycle.

**Suggested fix**:
- Reuse the `List<PendingEvent>` by clearing it instead of allocating a new one. Keep it as a field on the class.
- Consider `CancellationTokenSource.TryReset()` (.NET 8+) to reuse the timer CTS across drain cycles.
- Pre-size the pending list to a reasonable capacity matching expected batch size.

---

## Major

### 4. `PendingEvent` record allocates a `TaskCompletionSource` per enqueued event

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/EventHubBatchDispatcher.cs:232-236`

```csharp
private sealed record PendingEvent(EventData EventData, SendEventOptions? SendOptions)
{
    public TaskCompletionSource Completion { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
```

Every message dispatched through the batch dispatcher allocates a `PendingEvent` record and a `TaskCompletionSource`. The TCS itself allocates internal `Task<TResult>` state. For high-throughput batch mode, this is significant.

**Suggested fix**: Consider whether callers actually need to await individual completion. If fire-and-forget semantics are acceptable (the batch dispatcher guarantees delivery or throws), the per-event TCS could be eliminated. If individual awaiting is required, consider pooling the TCS objects via `ObjectPool<T>`.

---

### 5. `EventHubMessageEnvelopeParser.ParseEnclosedMessageTypes` allocates `string[]` + `ImmutableArray<string>` per message

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubMessageEnvelopeParser.cs:85-101`

```csharp
return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
```

`string.Split` allocates a `string[]`, and the collection expression `[.. ]` creates an `ImmutableArray<string>`. This happens on every received message that has enclosed types (which is most messages). The split strings themselves are also allocated.

**Suggested fix**: This is consistent with how the plan describes it (bounded by type count, typically 1-3), and the RabbitMQ parser does the same via `GetStringArray`. Acceptable for now but could be improved with a `Span<Range>`-based split that avoids the intermediate array if profiling shows it matters.

---

### 6. `Headers.Empty()` allocates a new `Headers` object (with a new `List<HeaderValue>`) per message

**File**: `src/Mocha/src/Mocha/Headers/Headers.cs:174-177`
**Called from**: `EventHubMessageEnvelopeParser.cs:107,124`

```csharp
public static Headers Empty()
{
    return new Headers();
}
```

This allocates a `new Headers()` which allocates a `new List<HeaderValue>()` (empty, but still an allocation). On the receive hot path, most messages with only well-known headers will hit this path. In contrast, the dispatch path correctly returns `Headers.Empty()` as well.

**Suggested fix**: Return a cached singleton empty `Headers` instance. This would require making `Headers` immutable when empty, or using a `ReadOnlyHeaders` wrapper, or simply caching: `private static readonly Headers s_empty = new(); public static Headers Empty() => s_empty;`. However, since `Headers` is mutable, returning a singleton could cause mutations to leak. The better fix is to accept `null` for empty headers on `MessageEnvelope.Headers` and skip the allocation entirely. The parser already returns `null` for individual properties when they're not present.

Note: This is a framework-level issue, not specific to Event Hub. The RabbitMQ parser has the same pattern. Flag but don't block on this.

---

### 7. `DispatchAsync` single-element array allocation per send

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs:194,198`

```csharp
await producer.SendAsync([eventData], sendOptions, cancellationToken);
// ...
await producer.SendAsync([eventData], cancellationToken);
```

Each non-batched dispatch allocates a single-element `EventData[]`. The plan acknowledges this as a "known unavoidable allocation" because the SDK has no single-EventData `SendAsync` overload.

**Suggested fix**: This is documented in the plan as unavoidable. One potential mitigation: cache a reusable single-element array on the endpoint and replace the element each call (safe because `DispatchAsync` is the terminal action and the array won't be held after `SendAsync` returns). However, this requires confirming the SDK doesn't retain the array reference. Alternatively, the batch mode path avoids this entirely.

---

### 8. `SendEventOptions` allocated on every partition-targeted dispatch

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs:170-184`

Three separate paths all do `new SendEventOptions { ... }`:
- Line 174: `x-partition-id` header
- Line 178: configuration-level partition
- Line 183: `x-partition-key` header

For the configuration-level partition case (line 176-178), the partition ID is static for the endpoint's lifetime. This `SendEventOptions` could be created once in `OnComplete` and reused.

**Suggested fix**: Cache the `SendEventOptions` for the static `Configuration.PartitionId` case in `OnComplete`. The per-message header cases (lines 174, 183) necessarily allocate per-dispatch because the value changes.

---

## Minor

### 9. `EventHubDispatchEndpoint.DispatchAsync` string interpolation in error message

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs:72-75`

```csharp
throw new InvalidOperationException(
    $"Message body size ({envelope.Body.Length} bytes) exceeds the Event Hubs "
    + "maximum message size of 1MB. ...");
```

String interpolation + concatenation for error path. Not a hot path issue since this only fires on oversized messages, but the concatenation with `+` means multiple string objects.

**Suggested fix**: Fine as-is. Error path only.

---

### 10. `EventHubMessageHeaders.IsWellKnown` uses `HashSet<string>` -- could use `FrozenSet<string>`

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubMessageHeaders.cs:43-52`

```csharp
private static readonly HashSet<string> s_wellKnown = [ ... ];
```

`HashSet<string>` works but `FrozenSet<string>` (available since .NET 8) provides better lookup performance for read-only sets, particularly for small sets with known-at-compile-time contents. `FrozenSet` optimizes the hash function for the specific set of strings.

**Suggested fix**: Replace with `FrozenSet<string>`:
```csharp
private static readonly FrozenSet<string> s_wellKnown = FrozenSet.ToFrozenSet([
    ConversationId, CausationId, SourceAddress, DestinationAddress,
    FaultAddress, EnclosedMessageTypes, SentAt
]);
```

This is called per custom header per message in `BuildHeaders`, so the lookup optimization is worthwhile on the receive path.

---

### 11. `EventHubMessageEnvelopeParser.BuildHeaders` iterates `appProps` twice

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubMessageEnvelopeParser.cs:103-139`

The method first iterates all app properties to check if any custom headers exist (lines 113-119), then iterates again to build the `Headers` collection (lines 128-136). This is O(2n) instead of O(n).

**Suggested fix**: Single-pass approach: always create the `Headers` with capacity, iterate once, add non-well-known entries. If the result is empty at the end, return `Headers.Empty()`. This saves one iteration at the cost of potentially creating and discarding an empty `Headers` when there are no custom headers. Given that the common case (per the plan) is that messages have only well-known headers, the current two-pass approach is actually optimized for the common case (early exit without allocating `Headers`). Keep as-is -- the optimization is valid.

---

### 12. `EnclosedMessageTypes` join on dispatch uses `string.Join`

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubDispatchEndpoint.cs:140`

```csharp
appProps[EventHubMessageHeaders.EnclosedMessageTypes] = string.Join(";", types);
```

Allocates a joined string per dispatch. The plan documents this as acceptable ("bounded by message type count, typically 1-3"). Agreed -- this is fine.

---

### 13. `BlobStorageCheckpointStore.GetBlobClient` allocates an interpolated string per call

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/BlobStorageCheckpointStore.cs:73`

```csharp
var blobName = $"{fullyQualifiedNamespace}/{eventHubName}/{consumerGroup}/checkpoint/{partitionId}";
```

Similarly in `BlobStorageOwnershipStore.GetOwnershipBlobClient` (line 148) and `BuildOwnershipPrefix` (line 156). These allocate interpolated strings, but the bigger concern is the per-message network I/O (covered in Critical #2). Once checkpointing is batched, this becomes infrequent.

**Suggested fix**: Cache the `BlobClient` per partition since the blob name never changes.

---

## Nit

### 14. `MochaEventProcessor.OnProcessingEventBatchAsync` checkpoints after every single event

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/Connection/MochaEventProcessor.cs:85-101`

```csharp
foreach (var eventData in events)
{
    await _messageHandler(eventData, partition.PartitionId, cancellationToken);
    await _checkpointStore.SetCheckpointAsync(...);
}
```

For `InMemoryCheckpointStore` this is a fast dictionary update, so it's acceptable. But this establishes a pattern where every `ICheckpointStore` implementation must be fast enough for per-message calls. When `BlobStorageCheckpointStore` is plugged in, this becomes Critical #2.

**Suggested fix**: Move checkpoint interval logic into `MochaEventProcessor` itself rather than relying on every store implementation being fast. Track a per-partition counter and only call the store every N events or on batch boundaries. The plan's `EventHubReceiveEndpointConfiguration` mentions `CheckpointInterval` -- this should be wired in here.

---

### 15. `EventHubHealthCheck.CheckHealthAsync` uses LINQ `.OfType<>().ToList()`

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubHealthCheck.cs:26-28`

```csharp
var receiveEndpoints = _transport.ReceiveEndpoints
    .OfType<EventHubReceiveEndpoint>()
    .ToList();
```

Allocates a list every health check call. Not a hot path (health checks are infrequent), but unnecessarily allocating. Could iterate directly.

**Suggested fix**: Not a hot path -- acceptable as-is.

---

### 16. `Describe()` uses LINQ `.Select(...).ToList()` and allocates multiple dictionaries

**File**: `src/Mocha/src/Mocha.Transport.AzureEventHub/EventHubMessagingTransport.cs:338-339`

```csharp
var receiveEndpoints = ReceiveEndpoints.Select(e => e.Describe()).ToList();
var dispatchEndpoints = DispatchEndpoints.Select(e => e.Describe()).ToList();
```

Plus `new Dictionary<string, object?>` at lines 353 and 371 inside the loop.

**Suggested fix**: `Describe()` is a diagnostic/admin operation, not a hot path. Acceptable.

---

## Summary

| Category | Count | Key Themes |
|----------|-------|------------|
| Critical | 3 | Per-message string allocation in checkpoint key, per-message blob I/O, per-drain-cycle allocations in batch dispatcher |
| Major | 5 | Per-event TCS in batch mode, ImmutableArray from Split, Headers.Empty() allocation, single-element array, SendEventOptions on static partition |
| Minor | 5 | Double iteration in BuildHeaders, string.Join for types, blob client caching, FrozenSet optimization |
| Nit | 3 | Checkpoint interval missing in processor, LINQ in health check, LINQ in Describe |

### Top 3 Actionable Items

1. **Critical #2 + Nit #14**: Implement checkpoint interval in `MochaEventProcessor` so `BlobStorageCheckpointStore` doesn't do a network round-trip per message. This is the single biggest throughput risk.
2. **Critical #1**: Use value-tuple key `(string, string, string, string)` in `InMemoryCheckpointStore` to eliminate per-message string allocation.
3. **Critical #3 + Major #4**: Pool/reuse `List<PendingEvent>` and consider CTS reuse in the batch dispatcher drain loop.

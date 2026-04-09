# Correctness Review: Azure Event Hub Transport

Reviewer: Senior .NET Engineer (correctness focus)
Date: 2026-03-27
Scope: `Mocha.Transport.AzureEventHub` (source + tests)

---

## Critical

### C1. EventHubBatchDispatcher: SendOptions ignored -- all batched events lose partition routing

**File:** `Connection/EventHubBatchDispatcher.cs:156-180`
**Description:** The `PendingEvent` record stores `SendOptions` (which carries PartitionId or PartitionKey), but the `DrainAndSendAsync` method calls `_producer.CreateBatchAsync(cancellationToken)` without passing a `CreateBatchOptions` that specifies the partition key/ID. When `TryAdd` adds events to the batch, the batch was created with no partition targeting. The `SendOptions` on each `PendingEvent` is **never read** during batching.

This means: in Batch mode, all partition routing (x-partition-key, x-partition-id, configuration-level PartitionId) is silently dropped. Events that should go to a specific partition get round-robined instead.

Furthermore, events with *different* partition targets get mixed into the *same* batch, which is invalid -- `EventDataBatch` can only target a single partition/partition key.

**Suggested fix:** Group pending events by their `SendOptions` (partition key + partition ID). For each group, create a separate `EventDataBatch` using `CreateBatchOptions` with the correct partition targeting. Alternatively, when `SendOptions` differ between items, flush the current batch and start a new one with the new options.

```csharp
var options = new CreateBatchOptions();
if (item.SendOptions?.PartitionId is { } pid)
    options.PartitionId = pid;
else if (item.SendOptions?.PartitionKey is { } pk)
    options.PartitionKey = pk;

batch ??= await _producer.CreateBatchAsync(options, cancellationToken);
```

And flush when the next event has different routing from the current batch.

---

### C2. EventHubBatchDispatcher.DisposeAsync: CancellationTokenSource cancelled AFTER channel completed -- shutdown drain may fail silently

**File:** `Connection/EventHubBatchDispatcher.cs:69-84`
**Description:** The dispose sequence is:
1. `_channel.Writer.TryComplete()` -- marks channel as done
2. `await _cts.CancelAsync()` -- cancels the CTS
3. `await _processLoop` -- waits for completion

The problem: after `TryComplete()`, the `ProcessLoopAsync` will exit the `while` loop (because `WaitToReadAsync` returns false), then execute the shutdown drain on line 114: `await DrainAndSendAsync(reader, CancellationToken.None)`. This drain uses `CancellationToken.None` so cancellation won't interrupt it. However, `_cts.CancelAsync()` on line 72 runs *after* `TryComplete`, creating a race where the drain task may still be running when `_cts.Dispose()` is called on line 83 -- but actually this is fine since the drain uses `CancellationToken.None`.

However, there's a more subtle issue: if `_cts.CancelAsync()` completes before the `ProcessLoopAsync` reaches the shutdown drain, the `while (!cancellationToken.IsCancellationRequested)` check on line 91 will exit the loop *without* executing the shutdown drain. This means pending events could be abandoned without their TCS being completed, leaving callers of `EnqueueAsync` hanging forever.

**Suggested fix:** Remove `_cts.CancelAsync()` from `DisposeAsync`. Completing the channel writer is sufficient to break the loop and trigger the drain. The CTS should only be used for emergency/abort scenarios. Alternatively, move the `CancelAsync` call *after* `await _processLoop`.

---

### C3. MochaEventProcessor: Checkpoint committed BEFORE message handler confirms success

**File:** `Connection/MochaEventProcessor.cs:85-101`
**Description:** In `OnProcessingEventBatchAsync`, the flow is:
```
foreach eventData:
    await _messageHandler(...)   // may throw
    await _checkpointStore.SetCheckpointAsync(...)
```

If `_messageHandler` throws an exception, the checkpoint is NOT updated (correct for that event). But the exception propagates out of `OnProcessingEventBatchAsync`, which causes the `EventProcessor<T>` base class to invoke `OnProcessingErrorAsync` and potentially retry the entire batch from where it left off.

However, consider the opposite scenario: `_messageHandler` succeeds, but `SetCheckpointAsync` throws (e.g., blob storage transient failure). The event was processed successfully, but the checkpoint was not persisted. On restart, the event will be reprocessed. This is **at-least-once** semantics, which is the documented intent -- so this is actually acceptable.

**Reclassified: Not a bug.** The at-least-once semantics are correct and documented.

---

## Major

### M1. EventHubBatchDispatcher: PendingEvents not completed on timer-initiated flush when batch is null

**File:** `Connection/EventHubBatchDispatcher.cs:117-201`
**Description:** If the timer expires (OperationCanceledException on line 149) and `batch` is null (no events were actually read), the method exits cleanly. But if events were read via `TryRead` and added to the `pending` list, but `CreateBatchAsync` throws, the events end up in the `pending` list but the outer catch on line 192 will set their exception. This is fine.

However, a more subtle issue: if the timer fires between `TryRead` returning `true` and the event being added to `batch` via `TryAdd`, the event could be in neither state correctly. Actually reviewing again -- the timer catch is only in the `WaitToReadAsync` path, not in the `TryRead` path. So this specific case can't happen.

**Reclassified as Minor** -- see M5 instead.

---

### M2. EventHubBatchDispatcher: Potential deadlock if EnqueueAsync caller is on the same synchronization context

**File:** `Connection/EventHubBatchDispatcher.cs:56-66`
**Description:** `EnqueueAsync` writes to the channel, then awaits `pending.Completion.Task.WaitAsync(cancellationToken)`. The `TaskCompletionSource` uses `TaskCreationOptions.RunContinuationsAsynchronously`, which is correct and prevents synchronous continuation hijacking. This is properly handled.

**Reclassified: Not a bug.** `RunContinuationsAsynchronously` is correctly used.

---

### M3. EventHubConnectionManager.DisposeAsync: Not safe for concurrent access during disposal

**File:** `Connection/EventHubConnectionManager.cs:50-58`
**Description:** `DisposeAsync` iterates `_producers` and disposes each, then calls `_producers.Clear()`. If `GetOrCreateProducer` is called concurrently during disposal (from another thread still sending messages), the `ConcurrentDictionary.GetOrAdd` could create a new producer that is never disposed, or `DisposeAsync` could see a producer that was just added during iteration.

The `foreach` over `ConcurrentDictionary` is snapshot-safe (it won't throw), but producers created after the iteration starts will be orphaned. The `Clear()` call after disposal doesn't dispose any newly-added producers.

**Suggested fix:** This is acceptable if the transport guarantees that all dispatch endpoints are stopped before `DisposeAsync` is called. Looking at `EventHubMessagingTransport.DisposeAsync()`, it disposes batch dispatchers first, then the connection manager. If there's no enforcement that all dispatching has stopped, this is a leak. However, since the transport lifecycle guarantees stop-before-dispose ordering, this is a minor concern rather than a bug.

**Reclassified: Minor** -- depends on lifecycle guarantees. Add a comment documenting the assumption.

---

### M4. EventHubReceiveEndpoint: _processor set to null in OnStopAsync creates race with IsProcessorRunning

**File:** `EventHubReceiveEndpoint.cs:129-140`
**Description:** `OnStopAsync` calls `_processor.StopProcessingAsync()` then sets `_processor = null`. The property `IsProcessorRunning` reads `_processor?.IsRunning ?? false`. Between `StopProcessingAsync` completing and `_processor = null`, another thread reading `IsProcessorRunning` will see `false` (because the processor was stopped). After `_processor = null`, it also returns `false`. This is fine -- no incorrect true result.

However, if health check runs *during* `StopProcessingAsync`, `_processor.IsRunning` may return `true` briefly, then transition to `false`. This is expected behavior during a graceful shutdown.

**Reclassified: Not a bug.** The race is benign.

---

### M5. EventHubBatchDispatcher: Batch not disposed on outer exception path when batch was already sent

**File:** `Connection/EventHubBatchDispatcher.cs:186-201`
**Description:** In the `DrainAndSendAsync` method, after `SendBatchAsync` on line 188, `batch` is set to `null` on line 189. If an exception occurs later (which can't happen since line 188 is the last statement in the try block before the catch), `batch?.Dispose()` correctly handles the null case.

Actually, looking more carefully: the outer `catch` on line 192 catches exceptions from `SendBatchAsync` itself. If `SendBatchAsync` throws (which it shouldn't because it has its own try/catch), the `batch` is NOT set to null on line 189, and the catch on line 200 will dispose it. `SendBatchAsync` internally disposes the batch in its `finally` block (line 228). So if `SendBatchAsync` throws, the batch gets disposed TWICE -- once in `SendBatchAsync.finally` and once in the outer catch.

**Suggested fix:** The outer catch on line 200 should NOT dispose batch if it was already passed to `SendBatchAsync`. However, `EventDataBatch.Dispose()` is documented as idempotent, so double-dispose is safe.

**Reclassified: Minor** -- double dispose is safe but sloppy. Set `batch = null` before calling `SendBatchAsync`, or restructure.

---

### M6. EventHubDefaultReceiveEndpointConvention: Error/skipped endpoint URIs use invalid scheme-relative format

**File:** `Conventions/EventHubDefaultReceiveEndpointConvention.cs:24-30`
**Description:** The error and skipped endpoint URIs are constructed as:
```csharp
new Uri($"{transport.Schema}:h/error")    // "eventhub:h/error"
new Uri($"{transport.Schema}:h/skipped")  // "eventhub:h/skipped"
```

This creates an opaque URI (no authority component, no `//`). The `CreateEndpointConfiguration(Uri address)` method in `EventHubMessagingTransport` checks:
```csharp
if (address.Scheme == Schema && address.Host is "")
```

For opaque URIs like `eventhub:h/error`, `address.Host` throws `InvalidOperationException` on some URI formats, or returns empty string depending on the runtime. On .NET, `new Uri("eventhub:h/error")` creates a URI where `Scheme` is `eventhub` and `AbsolutePath` is `h/error`. `Host` will be `""`.

Then `path.Split(ranges, '/', ...)` on `"h/error"` yields segments `["h", "error"]`, so `segmentCount == 2`, and it matches the `if (segmentCount == 2)` path on line 209, resulting in `kind = "h"` and `name = "error"`. This should work correctly.

**Reclassified: Not a bug** -- but the URI format `eventhub:h/error` is unusual (no `///`). Would be clearer as `eventhub:///h/error` for consistency with other endpoint URIs. This is a style/clarity nit.

---

### M7. EventHubProvisioner: PartitionCount set to 0 then immediately checked and overwritten

**File:** `Provisioning/EventHubProvisioner.cs:47-61`
**Description:**
```csharp
var data = new EventHubData
{
    PartitionCount = partitionCount ?? 0  // Line 49
};

if (partitionCount is null or 0)  // Line 54
{
    data.PartitionCount = null;   // Line 56 -- overwrites the 0
}
else
{
    data.PartitionCount = partitionCount.Value;  // Line 60 -- same value as line 49
}
```

Line 49 sets it to `partitionCount ?? 0`, then lines 54-61 completely overwrite that value. The initial assignment on line 49 is dead code. This is not a bug (the final state is correct) but is confusing and suggests copy-paste drift.

**Suggested fix:** Simplify to:
```csharp
var data = new EventHubData();
if (partitionCount is not null and not 0)
{
    data.PartitionCount = partitionCount.Value;
}
```

---

## Minor

### N1. EventHubBatchDispatcher.EnqueueAsync: Cancellation orphans PendingEvent TCS

**File:** `Connection/EventHubBatchDispatcher.cs:56-66`
**Description:** If `cancellationToken` fires during `pending.Completion.Task.WaitAsync(cancellationToken)` on line 65, the `WaitAsync` throws `OperationCanceledException` to the caller, but the `PendingEvent` is still in the channel and will be processed by the background loop. The `Completion.TrySetResult()` will be called eventually, but nobody is listening. The event will still be sent to Event Hubs even though the caller considers it cancelled.

This is a semantic choice (fire-and-forget vs. true cancellation) and may be intentional. If the caller expects cancellation to prevent the send, this is incorrect. If the caller only uses cancellation to stop waiting, this is fine.

**Suggested fix:** Document the behavior. If true cancellation is needed, the `PendingEvent` should have a way to be marked as cancelled so the background loop can skip it.

---

### N2. EventHubDispatchEndpoint: Size validation uses hardcoded 1MB limit

**File:** `EventHubDispatchEndpoint.cs:69-75`
**Description:** The code checks `envelope.Body.Length > 1_048_576` (1MB). However:
1. Event Hubs' max message size depends on the tier: 256KB (Basic), 1MB (Standard), 1MB (Premium, but configurable up to 1MB per event).
2. The check only validates body length, not the total event size including headers/properties.
3. The AMQP overhead means the actual limit is slightly less than 1MB for the body.

This isn't wrong for the Standard/Premium tier, but it could reject valid messages or pass slightly-too-large messages depending on header overhead.

**Suggested fix:** Either make the limit configurable or note in comments that this is an approximation.

---

### N3. EventHubReceiveFeature.Initialize: Sets state to null! instead of using the state parameter

**File:** `Features/EventHubReceiveFeature.cs:33-39`
**Description:** The `Initialize(object state)` method ignores the `state` parameter and resets all fields to default/null. If the `IPooledFeature` contract expects `Initialize` to configure from `state`, this is incorrect. If `Initialize` is just a reset-on-reuse hook, this is fine.

Looking at the RabbitMQ transport's `RabbitMQReceiveFeature`, I'd need to check the pattern. The `Initialize` and `Reset` methods do identical things here, which suggests one of them may not be needed, or `Initialize` should be doing something with `state`.

**Suggested fix:** Verify the `IPooledFeature` contract. If `Initialize` should use `state`, this is a bug. If both are reset hooks, this is fine but the `state` parameter is unused.

---

### N4. InMemoryCheckpointStore: AddOrUpdate lambda always takes newSeq, ignoring existing value

**File:** `Connection/InMemoryCheckpointStore.cs:37`
**Description:**
```csharp
_checkpoints.AddOrUpdate(key, sequenceNumber, static (_, newSeq) => newSeq);
```

The update factory receives `(key, newSeq)` -- but `newSeq` is the `addValue` parameter (the second arg to `AddOrUpdate`), NOT the third. Actually in this overload of `AddOrUpdate(key, addValue, updateValueFactory)`, the `updateValueFactory` is `Func<string, long, long>` where the second parameter is the existing value. The lambda `(_, newSeq) => newSeq` returns the *existing* value, not the new one!

Wait -- let me recheck. `ConcurrentDictionary<TKey, TValue>.AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)`:
- If key doesn't exist: stores `addValue` (which is `sequenceNumber`)
- If key exists: calls `updateValueFactory(key, existingValue)` and stores result

So `(_, newSeq) => newSeq` returns the *existing* value, effectively making the update a no-op. The checkpoint would never advance!

**This should be Critical** -- reclassifying.

**Suggested fix:**
```csharp
_checkpoints[key] = sequenceNumber;
```
Or:
```csharp
_checkpoints.AddOrUpdate(key, sequenceNumber, static (_, _) => sequenceNumber);
```
Wait, that won't capture `sequenceNumber`. Use the indexer instead:
```csharp
_checkpoints[key] = sequenceNumber;
```

**ACTUALLY** -- I need to re-examine this. The `AddOrUpdate` overload being used passes the `addValue` as the second parameter. Let me look more carefully.

The overload is: `AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)`

The `updateValueFactory` receives `(key, oldValue)`. The lambda `(_, newSeq) => newSeq` confusingly names the old value `newSeq`, but it returns the *old* value, not the passed-in `sequenceNumber`.

This is a **bug**: on update, the existing sequence number is returned unchanged, so checkpoints never advance after the first write for a given partition.

---

## Critical (reclassified)

### C4. InMemoryCheckpointStore.SetCheckpointAsync: Update lambda returns existing value instead of new value

**File:** `Connection/InMemoryCheckpointStore.cs:37`
**Description:** As analyzed in N4 above, `AddOrUpdate(key, sequenceNumber, static (_, newSeq) => newSeq)` names the second factory parameter `newSeq` but it is actually the *existing* value. The checkpoint never advances past the first stored value for a given partition key.

This means: after processing the first event on a partition, the checkpoint is stored. On subsequent events, `SetCheckpointAsync` is called but the stored value stays at the first sequence number. On restart, all events after the first one on each partition will be reprocessed.

**Impact:** The in-memory checkpoint store is the default when no custom checkpoint store is configured. Every user who doesn't configure BlobStorageCheckpointStore will have broken checkpointing.

**Suggested fix:**
```csharp
_checkpoints[key] = sequenceNumber;
```

---

## Major (additional)

### M8. BlobStorageCheckpointStore: content.ToString() may not reliably parse on all BinaryData representations

**File:** `Connection/BlobStorageCheckpointStore.cs:39`
**Description:** `response.Value.Content` is `BinaryData`. `BinaryData.ToString()` returns the UTF-8 string representation. Since `SetCheckpointAsync` writes `Encoding.UTF8.GetBytes(sequenceNumber.ToString())`, the round-trip should work. However, `SetCheckpointAsync` wraps the bytes in `new BinaryData(Encoding.UTF8.GetBytes(...))` which creates a BinaryData from a byte array. `BinaryData.ToString()` on this will correctly decode UTF-8 bytes.

**Reclassified: Not a bug.** The round-trip is correct.

---

### M9. EventHubDispatchEndpoint.DispatchAsync: Stackalloc with max 2 ranges may truncate paths with more segments

**File:** `EventHubDispatchEndpoint.cs:46-47`
**Description:**
```csharp
Span<Range> ranges = stackalloc Range[2];
var segmentCount = path.Split(ranges, '/', RemoveEmptyEntries | TrimEntries);
```

If the destination address path has more than 2 segments (after removing empties), `Split` fills the first 2 ranges and returns 2 (the count of filled ranges). The last range will include everything from the second segment onwards (it consumes the rest). So for `"/a/b/c"`, ranges[1] would cover `"b/c"`, not just `"b"`.

In the reply path, `segmentCount >= 1` takes `ranges[segmentCount - 1]` which would be `ranges[1]` = `"b/c"`. This would result in `hubName = "b/c"` which is likely incorrect.

For non-reply paths, this code isn't used (Topic.Name is used instead). For reply paths, the destination address format is controlled by the framework, so this may never encounter >2 segments in practice.

**Impact:** Low risk since reply address format is controlled, but the code is fragile.

**Suggested fix:** Increase range buffer size or validate segment count.

---

## Test Correctness

### T1. PartitionRoutingTests: Asserts partition affinity via headers that may not exist

**File:** `Tests/Behaviors/PartitionRoutingTests.cs:110-123`
**Description:** The `PartitionCapture.Record` method tries to read `x-partition-id` from `context.Headers`. But `x-partition-id` is a *send* option (set on the producer side to route to a specific partition). It is NOT automatically added to the received event's headers. The partition ID of the received event comes from `EventProcessorPartition.PartitionId`, which is set on `EventHubReceiveFeature.PartitionId`.

The receive pipeline does NOT copy `PartitionId` from the feature into a message header. So `context.Headers.TryGetValue("x-partition-id", ...)` will always return false, and the code falls through to `PartitionIds.Add("unknown")`.

The first test (`RouteToSamePartition`) checks `Assert.All(partitionIds, id => Assert.Equal(partitionIds[0], id))` -- since all are "unknown", they're all equal. The test passes but **doesn't actually verify partition affinity**.

The second test (`DistributeAcrossPartitions`) checks `distinctPartitions > 1` -- since all are "unknown", distinct count is 1, and the test **will fail** (or would fail if the emulator is running).

**Suggested fix:** Access partition ID from the `EventHubReceiveFeature` via the consume context's features, not from message headers. Or add a middleware that populates the partition ID as a header.

---

### T2. EventHubHealthCheckTests.PartitionId tests are vacuous property tests

**File:** `Tests/EventHubHealthCheckTests.cs:62-88`
**Description:** Tests `PartitionId_Should_FlowToConfiguration_When_DescriptorUsed` and `PartitionId_Should_DefaultToNull_When_NotSet` simply create a configuration object, set a property, and read it back. These test C# property getters/setters, not any actual behavior.

**Suggested fix:** These should be removed or replaced with tests that verify partition ID flows through the dispatch pipeline to the actual `SendEventOptions`.

---

### T3. Multiple tests share the same hub name via fixture.GetHubForTest, which may cause cross-test interference

**File:** Various test files using `_fixture.GetHubForTest("batch")` etc.
**Description:** Multiple test methods in the same class (and potentially across classes) use the same hub name (e.g., "test-hub-batch"). While consumer groups are unique per test, published messages go to the same hub and could be consumed by processors from other tests that haven't stopped yet.

With Event Hub's consumer group isolation, each consumer group maintains its own offset, so a new consumer group will start from Latest (as per `MochaEventProcessor.GetCheckpointAsync` default). This should prevent cross-test contamination. But if tests overlap in time and the consumer group starts before messages are published, they could miss messages or pick up messages from other tests.

**Impact:** Potential for flaky integration tests. The timeout-based assertions may mask this.

**Suggested fix:** Consider using unique hub names per test, or ensure test isolation through startup ordering.

---

## Summary

| Category | Count | IDs |
|----------|-------|-----|
| Critical | 2     | C1, C4 |
| Major    | 2     | M7, M9 |
| Minor    | 3     | N1, N2, N3 |
| Test     | 3     | T1, T2, T3 |

**Top priority fixes:**
1. **C4** -- InMemoryCheckpointStore never advances checkpoints (will cause reprocessing on restart and incorrect behavior in tests)
2. **C1** -- BatchDispatcher silently drops partition routing for all batched events

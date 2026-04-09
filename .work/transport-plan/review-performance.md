# Performance Review: Azure Event Hub Transport Implementation Plan

**Reviewer**: Senior .NET Performance Engineer
**Date**: 2026-03-27
**Scope**: Hot-path allocation analysis, threading model, serialization strategy

---

## Performance Concerns

### Critical

#### 1. `SendEventOptions` allocated on every dispatch (plan line ~838)

```csharp
var sendOptions = new SendEventOptions();
```

This allocates a new `SendEventOptions` on **every single dispatch call**, even when no partition key is set. This is the dispatch hot path.

**Fix**: Only allocate `SendEventOptions` when a partition key is actually needed. Use a `static readonly` default instance or pass `null`/skip the overload when no options are required:

```csharp
SendEventOptions? sendOptions = null;
if (envelope.Headers?.TryGet("x-partition-key", out string? partitionKey) == true && partitionKey is not null)
{
    sendOptions = new SendEventOptions { PartitionKey = partitionKey };
}
else if (envelope.CorrelationId is not null)
{
    sendOptions = new SendEventOptions { PartitionKey = envelope.CorrelationId };
}

if (sendOptions is not null)
{
    await producer.SendAsync([eventData], sendOptions, cancellationToken);
}
else
{
    await producer.SendAsync([eventData], cancellationToken);
}
```

However, note that `CorrelationId` is almost always set on messages, so in practice this allocation will happen on most dispatches. Still, the conditional avoids the allocation on the truly no-options path and makes the intent clear.

#### 2. Implicit array allocation `[eventData]` on every dispatch (plan line ~850)

```csharp
await producer.SendAsync([eventData], sendOptions, cancellationToken);
```

The collection expression `[eventData]` allocates a new `EventData[]` on every dispatch. The `SendAsync(IEnumerable<EventData>, ...)` overload will enumerate this.

**Fix**: Check if `EventHubProducerClient` has a single-`EventData` overload. If not, consider caching a reusable single-element array or using `EventDataBatch` even for single messages. At minimum, document this as a known unavoidable allocation from the SDK's API surface.

**Update**: The SDK does not have a single-EventData `SendAsync` overload -- `IEnumerable<EventData>` is the minimum. The array allocation is unavoidable without batching. This is acceptable for v1 but should be noted as a reason to move to `EventDataBatch` in Phase 5.

#### 3. `string.Join(";", types)` for EnclosedMessageTypes (plan line ~812)

```csharp
appProps[EventHubMessageHeaders.EnclosedMessageTypes] = string.Join(";", types);
```

`string.Join` allocates a new string on every dispatch when `EnclosedMessageTypes` is set (which it typically always is). The `ImmutableArray<string>` is also iterable.

**Fix**: Consider caching the joined string on the `MessageEnvelope` or computing it once during envelope construction. Alternatively, since AMQP ApplicationProperties supports arrays of strings, store the types as individual indexed keys (e.g., `x-type-0`, `x-type-1`) to avoid the join/split entirely -- though this trades one allocation for dictionary entry overhead. The current approach is acceptable for v1 given that `EnclosedMessageTypes` is typically small (1-3 entries).

#### 4. `new string(lastSegment)` / `new string(name)` in URI parsing on reply dispatch (plan lines ~732, ~271, ~298-301)

In `DispatchAsync` for reply endpoints and in `CreateEndpointConfiguration(Uri)`:

```csharp
hubName = new string(lastSegment);          // line ~732
configuration.HubName = new string(name);   // lines ~271, ~298
configuration.Name = "h/" + new string(name); // string concat + alloc
```

Reply dispatch runs on the hot path. Each `new string(ReadOnlySpan<char>)` allocates. The string concatenation `"h/" + new string(name)` allocates twice.

**Fix for reply dispatch**: This is harder to avoid because `GetOrCreateProducer(string)` needs a string key. For `CreateEndpointConfiguration`, this runs once at setup time, so it's fine. For the reply dispatch hot path, consider whether the hub name can be pre-resolved and cached on the endpoint during initialization rather than parsed from the URI on every dispatch. If not, at minimum use `string.Concat("h/", name)` to reduce to one allocation (but the RabbitMQ transport has the same pattern so this is consistent).

### Nice-to-Have

#### 5. `ParseEnclosedMessageTypes` uses `Split` + collection expression (plan line ~1299)

```csharp
return [.. typesStr.Split(';', StringSplitOptions.RemoveEmptyEntries)];
```

This allocates: (1) the `string[]` from `Split`, (2) copies into `ImmutableArray<string>`. On the receive hot path.

**Fix**: Use `StringSplitOptions.RemoveEmptyEntries` with a `Span<Range>` + `stackalloc` pattern (like the URI parsing), then build the `ImmutableArray` directly from spans. However, this is bounded by message type count (typically 1-3) so the practical impact is low.

#### 6. `BuildHeaders` iterates dictionary and allocates new `Headers` (plan line ~1305-1324)

```csharp
var result = new Headers(appProps.Count);
foreach (var (key, value) in appProps)
{
    if (EventHubMessageHeaders.IsWellKnown(key))
    {
        continue;
    }
    result.Set(key, value);
}
```

Allocates a `Headers` object + iterates the full `ApplicationProperties` dictionary on every receive, even when there are no custom headers. The `Headers.Empty()` fast path only triggers when `appProps` is null or empty, but if there are only well-known headers (the common case), we still allocate and iterate.

**Fix**: Pre-count non-well-known headers before allocating, or check `appProps.Count <= wellKnownCount` to return `Headers.Empty()` early. This avoids allocation in the common case where all app properties are well-known transport headers.

#### 7. `EventHubMessageEnvelopeParser` creates a new `MessageEnvelope` per receive (plan line ~1251)

This is unavoidable given `MessageEnvelope` is a class with init-only setters. Consistent with the RabbitMQ transport. No action needed, but noted for awareness -- if `MessageEnvelope` ever becomes poolable, this transport should adopt it.

#### 8. `GetRawAmqpMessage()` called on both send and receive paths

The plan correctly uses `GetRawAmqpMessage()` for structured AMQP property access. Verify that the SDK does not allocate a new `AmqpAnnotatedMessage` wrapper on each call -- if it's lazily created and cached on the `EventData` instance (likely), this is fine. If it allocates each time, it would be a concern on the receive path where it's called once per message.

**Recommendation**: Verify SDK behavior. If allocating, cache the result in a local variable (the plan already does `var amqp = eventData.GetRawAmqpMessage()` which is correct -- just ensure it's only called once per message).

---

## Missed Optimization Opportunities

### 1. No batch dispatch support in v1

The plan acknowledges this and defers to Phase 5. For high-throughput scenarios, `EventDataBatch` is significantly more efficient than individual `SendAsync` calls because:
- One network round-trip per batch vs. per message
- Internal AMQP framing is amortized
- The SDK pre-serializes on `TryAdd`, making `SendAsync` a single write

**Recommendation**: Acceptable for v1, but the dispatch pipeline should be designed so that batching can be added without changing the middleware contract.

### 2. Producer per hub name via `ConcurrentDictionary.GetOrAdd` with delegate

```csharp
return _producers.GetOrAdd(eventHubName, name => { ... });
```

`GetOrAdd` with a lambda captures `this` (for `_logger` and `_connectionProvider`). On cache hits this is fine -- the delegate is never invoked. On cache misses, the closure allocates. Since producers are created once per hub and cached, this is acceptable. But for consistency with zero-alloc patterns, could use `GetOrAdd(key, static (name, state) => ..., state)` overload with a state tuple.

### 3. Topology lookups use `FirstOrDefault` with LINQ (plan lines ~871, ~919, ~1013, ~1564, ~1588-1598)

```csharp
topology.Topics.FirstOrDefault(t => t.Name == configuration.HubName)
```

These allocate a closure + delegate per call. They run during initialization (not hot path) so this is acceptable. But the topology convention `DiscoverTopology` is called per endpoint, and it does multiple `FirstOrDefault` calls with LINQ predicates. If there are many endpoints, this could add up.

**Recommendation**: Acceptable for v1. If topology grows large, consider a `Dictionary<string, EventHubTopic>` for O(1) lookups.

---

## Zero-Allocation Patterns: Assessment

| Pattern | Status | Notes |
|---------|--------|-------|
| Body as `ReadOnlyMemory<byte>` (send) | **GOOD** | `new EventData(envelope.Body)` wraps without copy |
| Body as `ReadOnlyMemory<byte>` (receive) | **GOOD** | `eventData.EventBody.ToMemory()` is zero-copy |
| URI parsing with `stackalloc Range[]` | **GOOD** | Consistent with RabbitMQ transport |
| Structured AMQP properties (no dictionary) | **GOOD** | MessageId, CorrelationId, ContentType, Subject, ReplyTo all use structured fields |
| `HasSection()` guard before ApplicationProperties | **GOOD** | Avoids lazy dictionary allocation on receive |
| Producer singleton per hub | **GOOD** | Thread-safe, long-lived, no channel pooling needed |
| Header constants as `const string` | **GOOD** | No allocation |
| `HashSet<string>` for `IsWellKnown` | **GOOD** | O(1) lookup, singleton |
| `SendEventOptions` per dispatch | **NEEDS FIX** | Allocates unconditionally |
| `EventData[]` per dispatch | **UNAVOIDABLE** | SDK API requires `IEnumerable<EventData>` |
| `string.Join` for EnclosedMessageTypes | **ACCEPTABLE** | Low item count bounds the cost |

---

## Threading Model Assessment

The threading model is **sound**:

1. **`EventHubProducerClient`** is documented thread-safe and singleton per hub -- correct.
2. **`ConcurrentDictionary`** for producer cache -- correct, lock-free on reads.
3. **`ImmutableArray` + `ImmutableInterlocked`** for consumer registration -- correct, lock-free reads with atomic updates.
4. **One async task per partition** for reading -- correct, matches the Event Hub SDK's constraint of one reader per partition per consumer group.
5. **`CancellationTokenSource.CreateLinkedTokenSource`** for graceful shutdown -- correct pattern.
6. **No unnecessary locks** -- the only lock is on topology mutation (`_lock` in `EventHubMessagingTopology`), which runs at initialization time, not on the hot path.
7. **`Task.WhenAll` with `SuppressThrowing`** for stop -- correct, prevents one failed partition reader from blocking shutdown of others.

**One concern**: `RegisteredConsumer.StartAsync` creates `CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)` where `cancellationToken` comes from the startup call. If the startup cancellation token is short-lived (e.g., a timeout token for startup), the linked source will cancel when it expires, killing all partition readers. Verify that the token passed to `RegisterConsumerAsync` is the application lifetime token, not a startup-scoped timeout.

---

## Verdict: **APPROVE with minor revisions**

The plan demonstrates strong performance awareness. The body is passed as `ReadOnlyMemory<byte>` without copying on both send and receive. URI parsing uses `stackalloc Range[]`. Structured AMQP properties avoid dictionary allocation for the most common headers. Connections/producers are properly singleton. The threading model is correct.

**Required before implementation**:
1. Fix `SendEventOptions` unconditional allocation -- make it conditional
2. Acknowledge the `[eventData]` array allocation as unavoidable and document as motivation for Phase 5 batching

**Recommended but not blocking**:
3. Optimize `BuildHeaders` to return `Headers.Empty()` when all app properties are well-known
4. Verify `GetRawAmqpMessage()` caching behavior in the SDK
5. Verify the `CancellationToken` lifetime in `RegisterConsumerAsync`

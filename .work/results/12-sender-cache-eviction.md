# Sender Cache: span-based lookup + eviction

File: `src/Mocha/src/Mocha.Transport.AzureServiceBus/Connection/AzureServiceBusClientManager.cs`

## TL;DR

1. **Premise correction.** `ConcurrentDictionary<TKey,TValue>.GetAlternateLookup<TAlternateKey>` **does** exist in .NET 9 — both `Dictionary<,>` and `ConcurrentDictionary<,>` ship it. We do **not** need to drop the concurrent dictionary. (Source: [ConcurrentDictionary.GetAlternateLookup](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getalternatelookup?view=net-10.0).)
2. **Multi-targeting.** This project follows `src/Directory.Build.props` → `net10.0; net9.0; net8.0`. Alternate-lookup APIs are only available on net9.0+, so the optimized path needs an `#if NET9_0_OR_GREATER` fallback for the `net8.0` TFM.
3. **Eviction is unnecessary in practice.** `ServiceBusSender` is documented as "safe to cache and use for the lifetime of an application … Caching the sender is recommended when the application is publishing messages regularly or semi-regularly." The set of destinations is bounded by the topology, and the SDK auto-recovers idle AMQP links on the next send. **Recommendation: do not add eviction.** Just bound the cache and emit a warning if it is ever exceeded.
4. **The `new string(name)` allocation in `AzureServiceBusDispatchEndpoint.cs:75` is *only* needed because the cache key is `string`.** Once the cache supports `ReadOnlySpan<char>` lookup, the dispatch endpoint can keep the span and only materialize a `string` on the cache miss (cold path) — which is exactly what `Dictionary.AlternateLookup<ReadOnlySpan<char>>` does internally via `IAlternateEqualityComparer<ReadOnlySpan<char>, string>.Create(span)`.

## Current shape

```csharp
private readonly ConcurrentDictionary<string, ServiceBusSender> _senders = new();

public ServiceBusSender GetSender(string entityPath)
{
    ObjectDisposedException.ThrowIf(_isDisposed, this);

    if (_senders.TryGetValue(entityPath, out var sender))
    {
        return sender;
    }

    return _senders.GetOrAdd(entityPath, path => _client.CreateSender(path));
}
```

Concurrency: `ConcurrentDictionary` is fully thread-safe; `GetOrAdd` may invoke the factory more than once under contention but only one result is stored. Disposal: `DisposeAsync` walks `_senders.Values` and disposes each, then disposes `_client`.

Callers:
- `AzureServiceBusDispatchEndpoint.cs:96` — main hot path. For replies, `entityPath` is built from a `ReadOnlySpan<char>` carved out of `destinationAddress.AbsolutePath` at line 75 with `new string(name)`. For configured topics/queues, `entityPath` is already an interned-by-topology `string` (cheap).
- `Scheduling/AzureServiceBusScheduledMessageStore.cs:42` — cancellation path; not nearly as hot, and already has a `string` from token parsing.

So the "save the allocation" win is concentrated in the **reply dispatch path**.

## ServiceBusSender lifecycle facts

From the official docs:

- `ServiceBusSender` (Azure.Messaging.ServiceBus 7.20.1): *"safe to cache and use for the lifetime of an application or until the `ServiceBusClient` that it was created by is disposed. Caching the sender is recommended when the application is publishing messages regularly or semi-regularly. The sender is responsible for ensuring efficient network, CPU, and memory use."*
  ([source](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussender?view=azure-dotnet))
- AMQP idle behavior: link closes after **10 min** of no calls; if all links of the connection are closed and no new link is created within **5 min**, the **connection** closes too.
  ([source](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-troubleshoot))
- The .NET SDK auto-recreates the link/connection on the next operation. Idle senders are not "broken"; they just transparently re-establish on next use. The `Spring JMS` workaround discussed in the docs is specific to JMS pooling, **not** the .NET SDK.

**Implication.** The cost of holding an "idle" `ServiceBusSender` is essentially:
- A small managed object (a few hundred bytes).
- Possibly an open AMQP link until the 10-min idle close, after which the link is gone but the sender object remains valid and will reconnect on next send.

There is **no AMQP-level reason to evict** senders proactively. Eviction would only matter if (a) destinations are dynamically generated and unbounded (not the case for Mocha — the topology is the source of truth, plus per-instance reply queues), or (b) we wanted to free the managed object weight (insignificant).

## Eviction strategy options (and why we should not add one)

| Option | Pros | Cons | Verdict |
|---|---|---|---|
| **Keep all senders** (current) | Simple. Matches SDK's cache-for-lifetime guidance. Zero per-send overhead. | Unbounded if destinations are dynamic. | **Recommended.** Add a soft cap (see below). |
| LRU with max size | Bounds memory. | LRU bookkeeping per send is the kind of thing this PR is trying to *eliminate*. We'd add an allocation/Volatile ops on the hot path to save ~200 bytes per evicted sender. | Reject — net cost > benefit. |
| Time-based (no use for N min → dispose) | Cleans up truly dead destinations. | Requires `LastUsedTicks` per entry and a periodic Timer. Race against in-flight sends needs a lock or `SemaphoreSlim`. Complicated. | Reject for now. Revisit only if production telemetry shows the cache growing unbounded. |
| Periodic background sweep | Clean shutdown. | Same as above. | Reject. |

**Recommended posture: bounded growth + observability, not eviction.**

- Hard cap = `1024` senders (configurable on `AzureServiceBusTransportConfiguration`). Beyond it, log a warning and *fall through to creating a non-cached sender* (caller-disposed) so we don't OOM but also don't silently degrade. In practice nobody hits 1024 distinct destinations from a single process.
- If we ever add a real eviction need, it should be opt-in: e.g. `IMemoryCache` wrapping the dictionary, with the cap and TTL coming from configuration.

## Span-based lookup design

`Dictionary<TKey,TValue>.GetAlternateLookup<TAlternateKey>` and the corresponding `ConcurrentDictionary` API both require the dictionary's comparer to implement `IAlternateEqualityComparer<TAlternateKey, TKey>`. **`StringComparer.Ordinal` (and `OrdinalIgnoreCase`) implement `IAlternateEqualityComparer<ReadOnlySpan<char>, string>` out of the box.** `EqualityComparer<string>.Default` does *not*; we must pass `StringComparer.Ordinal` explicitly.

### Proposed `AzureServiceBusClientManager`

```csharp
using System.Collections.Concurrent;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;

namespace Mocha.Transport.AzureServiceBus;

public sealed class AzureServiceBusClientManager : IAsyncDisposable
{
    // Default soft cap — well above any realistic Mocha topology.
    private const int DefaultMaxSenders = 1024;

    private readonly ServiceBusClient _client;
    private readonly ServiceBusAdministrationClient? _adminClient;

    // Ordinal comparer is required so we can use the ReadOnlySpan<char> alternate lookup
    // (StringComparer.Ordinal implements IAlternateEqualityComparer<ReadOnlySpan<char>, string>).
    private readonly ConcurrentDictionary<string, ServiceBusSender> _senders =
        new(StringComparer.Ordinal);

    private readonly int _maxSenders;
    private volatile bool _isDisposed;

    public AzureServiceBusClientManager(AzureServiceBusTransportConfiguration configuration)
    {
        // ... existing client construction ...
        _maxSenders = configuration.MaxCachedSenders ?? DefaultMaxSenders;
    }

    /// <summary>
    /// Gets a cached <see cref="ServiceBusSender"/> for the specified entity path.
    /// Use this overload when the caller already has a span (e.g. from a parsed URI)
    /// to avoid materializing a string on cache hits.
    /// </summary>
    public ServiceBusSender GetSender(ReadOnlySpan<char> entityPath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

#if NET9_0_OR_GREATER
        var lookup = _senders.GetAlternateLookup<ReadOnlySpan<char>>();

        // Fast path: cache hit — no allocation.
        if (lookup.TryGetValue(entityPath, out var sender))
        {
            return sender;
        }

        // Cold path: materialize the string exactly once and add.
        return GetOrAddSlow(entityPath.ToString());
#else
        return GetSender(entityPath.ToString());
#endif
    }

    /// <summary>
    /// String overload retained for callers that already have a string (e.g. configured queue/topic names).
    /// </summary>
    public ServiceBusSender GetSender(string entityPath)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        if (_senders.TryGetValue(entityPath, out var sender))
        {
            return sender;
        }

        return GetOrAddSlow(entityPath);
    }

    private ServiceBusSender GetOrAddSlow(string entityPath)
    {
        // Soft cap: do not let the cache grow without bound.
        if (_senders.Count >= _maxSenders && !_senders.ContainsKey(entityPath))
        {
            // Bypass the cache. Caller does not own this instance; the sender will be GC'd
            // (and its AMQP link closed) when the last in-flight send completes.
            // We intentionally do not dispose here because that races with concurrent sends.
            // This branch should be unreachable in correctly configured topologies.
            return _client.CreateSender(entityPath);
        }

        return _senders.GetOrAdd(entityPath, static (path, client) => client.CreateSender(path), _client);
    }

    // ... CreateProcessor, AdminClient, DisposeAsync unchanged ...
}
```

Notes on the sketch above:

- **Comparer change.** `new ConcurrentDictionary<string, ServiceBusSender>()` defaults to `EqualityComparer<string>.Default`, which is `StringComparer.Ordinal` *behaviorally* but is **not** detected as such for `GetAlternateLookup`. We must pass `StringComparer.Ordinal` to enable the alternate lookup. Behavior is identical for the hash/equality semantics already in use.
- **`static` lambda + state arg.** The current `path => _client.CreateSender(path)` allocates a closure capturing `this`. Switching to `GetOrAdd(string, Func<TKey, TArg, TValue>, TArg)` with `_client` as the arg lets us use a `static` lambda and removes the per-cold-miss allocation. (Cheap micro-opt; only fires on cache miss but trivial to add while we're in there.)
- **TFM gating.** `GetAlternateLookup` is .NET 9+, so the span overload's fast path is wrapped in `#if NET9_0_OR_GREATER`. On `net8.0` we fall back to allocating a string — which is what we do today. Acceptable; net8.0 is the legacy TFM.
- **`IAlternateEqualityComparer` semantics.** The hash of `ReadOnlySpan<char>` produced by `StringComparer.Ordinal` is identical to the hash of the equivalent `string`, so a span lookup hits the same bucket as the string the value was stored under. This is the whole point of the API.

### Caller refactor — `AzureServiceBusDispatchEndpoint`

Today, lines 53–96:

```csharp
string entityPath;

if (Kind == DispatchEndpointKind.Reply)
{
    // ... parse URI ...
    var name = path[ranges[1]];
    entityPath = new string(name);                  // ← allocation
    // ...
}
else if (Topic is not null) { entityPath = Topic.Name; }
else if (Queue is not null) { entityPath = Queue.Name; }

var sender = clientManager.GetSender(entityPath);

// downstream:
//   sender.SendMessageAsync(...)
//   sender.ScheduleMessageAsync(...)
//   token = $"asb:{entityPath}:{sequenceNumber}"   // ← still needs a string here
```

Refactor:

```csharp
ReadOnlySpan<char> entityPath;
string? entityPathString = null; // lazily materialized for the scheduled-message token

if (Kind == DispatchEndpointKind.Reply)
{
    // ... parse URI ...
    entityPath = path[ranges[1]];
    // Validate kind as before.
}
else if (Topic is not null)
{
    entityPathString = Topic.Name;
    entityPath = entityPathString.AsSpan();
}
else if (Queue is not null)
{
    entityPathString = Queue.Name;
    entityPath = entityPathString.AsSpan();
}
else
{
    throw new InvalidOperationException("Destination not configured");
}

var sender = clientManager.GetSender(entityPath);
var message = CreateMessage(envelope);

if (envelope.ScheduledTime is { } scheduledTime)
{
    var sequenceNumber = await sender.ScheduleMessageAsync(message, scheduledTime, cancellationToken);
    // Token construction still needs a string. Pull it from the sender so we don't re-allocate
    // from the span (sender.EntityPath is the same interned string we cached under).
    entityPathString ??= sender.EntityPath;
    context.Features.Configure<ScheduledMessageFeature>(f =>
        f.Token = $"asb:{entityPathString}:{sequenceNumber.ToString(CultureInfo.InvariantCulture)}");
}
else
{
    await sender.SendMessageAsync(message, cancellationToken);
}
```

Why the `sender.EntityPath` trick: `ServiceBusSender` exposes its entity path as a `string` property. On the reply path, even when we want to build the scheduled-message token, we can pull the already-materialized string from the sender instead of allocating a second one from the span. For the non-scheduled hot path (the common case) we never allocate at all.

### `AzureServiceBusScheduledMessageStore`

This caller already holds a `string entityPath` (parsed from the token). No change needed — the existing string overload still works. If we want it span-clean for symmetry, parse to span and call the span overload, but it's not on a hot path.

## Concurrency note

`Dictionary<,>` is *not* thread-safe, so if we were to swap to it we would need an external lock around adds. **We don't need to.** `ConcurrentDictionary<,>.GetAlternateLookup` returns a struct `AlternateLookup<TAlternateKey>` whose operations are themselves thread-safe (they call into the underlying dictionary's lock-striped infrastructure). All existing thread-safety guarantees of the current implementation carry over verbatim. The struct itself is `readonly` and tiny — fine to materialize per call, or once at construction time and cache as a field if we want to avoid even that.

If we cared about the lookup-struct cost we could cache it:

```csharp
private readonly ConcurrentDictionary<string, ServiceBusSender>.AlternateLookup<ReadOnlySpan<char>> _spanLookup;
// in ctor (NET9_0_OR_GREATER):
_spanLookup = _senders.GetAlternateLookup<ReadOnlySpan<char>>();
```

But the JIT will inline `GetAlternateLookup` and the struct is essentially a wrapper around the dictionary reference — so the field is a stylistic preference, not a measurable win.

## Disposal

`DisposeAsync` is unchanged. It walks `_senders.Values` (synchronous snapshot under `ConcurrentDictionary`'s lock) and disposes each sender, then disposes the client. Senders created via the "soft cap exceeded" bypass are *not* in `_senders` and therefore not disposed by us; they'll be cleaned up when the underlying `ServiceBusClient` is disposed (the client owns the AMQP connection; sender disposal closes the link, but client disposal closes the connection and all dependent links).

## What I'd ship

1. Change the dictionary to `new(StringComparer.Ordinal)`.
2. Add the `GetSender(ReadOnlySpan<char>)` overload with `#if NET9_0_OR_GREATER` fast path.
3. Add a `MaxCachedSenders` knob to `AzureServiceBusTransportConfiguration` (nullable int, default `1024`) and the soft-cap bypass.
4. Convert the `GetOrAdd` factory to a `static` lambda with `_client` as the state argument.
5. Refactor `AzureServiceBusDispatchEndpoint.DispatchAsync` to keep `entityPath` as `ReadOnlySpan<char>` and only materialize a string for the scheduled-message token (using `sender.EntityPath`).
6. Leave `AzureServiceBusScheduledMessageStore` alone (already string).
7. **Do not add eviction.** Do not add a Timer. Do not add LRU bookkeeping. The SDK is explicit that long-lived caching is correct, and the AMQP idle close is transparent to callers.

Net effect: zero allocations on the dispatch hot path for both reply and configured destinations, no behavior change, no new background work, no change to disposal semantics, no change to thread-safety.

## References

- [Dictionary<TKey,TValue>.GetAlternateLookup](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2.getalternatelookup?view=net-10.0)
- [ConcurrentDictionary<TKey,TValue>.GetAlternateLookup](https://learn.microsoft.com/dotnet/api/system.collections.concurrent.concurrentdictionary-2.getalternatelookup?view=net-10.0)
- [What's new in .NET 9 — Collection lookups with spans](https://learn.microsoft.com/dotnet/core/whats-new/dotnet-9/libraries#collections)
- [ServiceBusSender (caching guidance in Remarks)](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussender?view=azure-dotnet)
- [AMQP errors in Azure Service Bus (10-min link idle / 5-min connection idle)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-troubleshoot)

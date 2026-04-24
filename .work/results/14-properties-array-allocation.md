# 14 — `ServiceBusMessage.ApplicationProperties` array support, and the `EnclosedMessageTypes` allocation question

## TL;DR

- **Modern `Azure.Messaging.ServiceBus` does NOT allow arrays / lists in `ApplicationProperties`.** The supported set is a closed list of scalar primitives. Putting a `string[]`, `List<string>`, `ImmutableArray<string>`, or `ReadOnlyMemory<byte>` into the dictionary will throw `SerializationException` on `SendMessageAsync` / `TryAddMessage` / `ScheduleMessageAsync`. (`byte[]` is technically in the AMQP type system but is broken end-to-end in the broker — the SDK throws `ServiceBusException(MessageSizeExceeded)` for it. Microsoft documents this as a known service bug.)
- The legacy `WindowsAzure.ServiceBus` package *did* round-trip `IList`/`Array` to AMQP `list`/`array`, but that library is being retired on **30 September 2026** and the modern SDK deliberately dropped support. Anything we send goes through the modern SDK, so we must encode collections as scalar primitives.
- Conclusion: **the join is unavoidable in the wire format**. We can, however, eliminate the allocation cost on both sides.

## Sources

| Claim | Source |
| --- | --- |
| Modern `ServiceBusMessage.ApplicationProperties` supported types: `string, bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, decimal, double, char, Guid, DateTime, DateTimeOffset, Stream, Uri, TimeSpan` | <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.applicationproperties?view=azure-dotnet> |
| Same restriction confirmed for `ServiceBusReceivedMessage.ApplicationProperties` and `CorrelationRuleFilter.ApplicationProperties` (the latter additionally lists `byte[]` because filters are persisted, not transmitted as messages) | <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage.applicationproperties> |
| Throws `SerializationException` from `SendMessageAsync`, `SendMessagesAsync`, `ScheduleMessageAsync`, `ScheduleMessagesAsync`, and `TryAddMessage` when an unsupported type is present in `ApplicationProperties` | All five method docs explicitly list this exception, e.g. <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussender.sendmessageasync> |
| `byte[]` / `ArraySegment<byte>` is a special-case: SDK throws `ServiceBusException(MessageSizeExceeded)` because the broker rejects binary application properties. "A fix is planned, but the time line is currently unknown. The recommended workaround is to encode the binary data as a Base64 string." | Same `ApplicationProperties` page (Exceptions section) |
| Legacy `WindowsAzure.ServiceBus` mapped `System.Collections.IList → AMQP list`, `System.Array → AMQP array`, `System.Collections.IDictionary → AMQP map` — items restricted to the scalar table | <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-dotnet#message-serialization> |
| Legacy SDK retirement: 30 Sep 2026 | <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions> |
| AMQP 1.0 itself has `list`, `array`, `map` — Service Bus uses them internally for management operations (e.g. `sequence-numbers: array of long` in the schedule/cancel-scheduled paths) but does not expose them through `application-properties` | <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-request-response> |

So the AMQP protocol can carry arrays — but the public Azure SDK + broker contract for application properties cannot. We are stuck with a single scalar.

## Implications for `EnclosedMessageTypes`

Three of the four ideas raised in the prompt are dead on arrival:

| Idea | Verdict |
| --- | --- |
| Pass `string[]` directly to the property bag | **Fails at send time** with `SerializationException` |
| Pass `List<string>` / `ImmutableArray<string>` directly | **Fails at send time** with `SerializationException` |
| Pass `ReadOnlyMemory<byte>` (single concatenated buffer) | **Fails at send time** — `ReadOnlyMemory<byte>` is not in the supported set, and the underlying `byte[]` path is itself broken end-to-end |

The remaining fork is encoding-only:

1. Single string with a separator (status quo: `;`-joined).
2. Base64-of-binary (workaround for the `byte[]` bug). Adds ~33% wire bytes and CPU; pointless unless we want to skip UTF-8 conversions, which we don't because everything else in the pipeline is UTF-8 strings.

**Recommendation: keep the `;`-separated string.** Optimise the *allocation*, not the wire format.

## Receive-side analysis: is `new string(span[range])` necessary?

Yes. There is no zero-alloc way to materialise a `ReadOnlyMemory<char>` slice of an existing `string` as a `string` without copying. `string.Create` only helps when you already control the final length and char count, and when you'd otherwise call `new string(...)` *plus* `string.Concat`/manipulation — for a single-segment slice it does nothing better than `new string(span)`. The current code is already doing the right thing for the per-string allocation.

What we *can* avoid:

1. The `ImmutableArray.CreateBuilder<string>(count)` allocation (one allocation: the builder itself, plus an internal `T[]` of length `count`).
2. The `ImmutableArray<string>` wrapping when consumers don't need it.
3. The 32-element `Span<Range>` is fine — stack-allocated and free — but limits us to 32 enclosed types. Worth noting (no fix needed unless we ever expect more).

The downstream code in `MessageTypeSelectionMiddleware` only enumerates `EnclosedMessageTypes` with `foreach`, so the only requirement is that we expose an enumerable of strings. `ImmutableArray<string>` is the current envelope contract (`MessageEnvelope.EnclosedMessageTypes`), so we keep it — but we can build it directly without a builder.

The lowest-allocation path on receive is:

```csharp
private static ImmutableArray<string> ParseEnclosedMessageTypes(
    IDictionary<string, object?> props)
{
    if (!props.TryGetValue(AzureServiceBusMessageHeaders.EnclosedMessageTypes, out var value)
        || value is not string encoded
        || encoded.Length == 0)
    {
        return [];
    }

    // Fast path: no separator -> the encoded string IS the single type, no copy needed.
    var firstSep = encoded.IndexOf(';');
    if (firstSep < 0)
    {
        return ImmutableArray.Create(encoded);
    }

    var span = encoded.AsSpan();
    Span<Range> ranges = stackalloc Range[32];
    var count = span.Split(ranges, ';', StringSplitOptions.RemoveEmptyEntries);

    // Build the backing array directly and wrap with ImmutableCollectionsMarshal — avoids the
    // Builder allocation and the internal capacity/Count check of MoveToImmutable.
    var array = new string[count];
    for (var i = 0; i < count; i++)
    {
        array[i] = new string(span[ranges[i]]);
    }
    return ImmutableCollectionsMarshal.AsImmutableArray(array);
}
```

Wins vs. the existing implementation:
- **Single-type fast path**: when the producer sent only one type (the `types.Length == 1` branch on the dispatch side), we bypass `Span.Split` entirely and reuse the original interned-by-property-bag string. Zero allocations on the hot path for the most common case.
- **No builder allocation** in the multi-type path: we allocate exactly the `string[]` plus the per-element strings. `MoveToImmutable` enforces `Count == Capacity` then internally also yields a `string[]`, so we're not skipping any work that matters.
- Same per-string allocation as today (unavoidable — `new string(ReadOnlySpan<char>)` is the canonical and minimal copy).

## Send-side analysis: is the `string.Join(";", types)` allocation avoidable?

Partially. `string.Join` for `ImmutableArray<string>` allocates:
1. A `StringBuilder` (or, in modern .NET, uses `string.FastAllocateString` after a length pass via the array overload — but `ImmutableArray<string>` doesn't hit the optimised `string[]` overload).
2. The final string.

Two cleanups, both small:

1. **Already-special-cased: 1-element case.** Existing code already hands `types[0]` directly. Good.
2. **Use `string.Join` against a `ReadOnlySpan<string>` via `ImmutableCollectionsMarshal.AsArray(types)`** — this hits the `string.Join(string?, string?[]?)` overload which is the most optimised path (single buffer alloc, fills with `string.FillStringChecked`). Saves one enumerator allocation vs. iterating the `ImmutableArray<string>` as `IEnumerable`.

```csharp
if (envelope.EnclosedMessageTypes is { Length: > 0 } types)
{
    var joined = types.Length == 1
        ? types[0]
        : string.Join(';', ImmutableCollectionsMarshal.AsArray(types)!);
    props[AzureServiceBusMessageHeaders.EnclosedMessageTypes] = joined;
}
```

(Note: `ImmutableCollectionsMarshal.AsArray` returns `string[]?` — it can return `null` only for `default(ImmutableArray<string>)`, and we have already guarded with `Length > 0` so the `!` is correct here. `string.Join(char, string?[])` is the most-optimised overload and was added in .NET Core 3.0+.)

This avoids the `IEnumerable`-based path entirely. It cannot avoid the final `string` allocation — the SDK requires a `string` value.

## Concrete recommendations

### 1. Send (`AzureServiceBusDispatchEndpoint.CreateMessage`)

Replace lines 164–168 with the marshal-array variant above. Net effect: one fewer enumerator allocation per multi-type send. The 1-element fast path is preserved.

Files:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs` (lines 164–168)

Add `using System.Runtime.InteropServices;` at the top of the file.

### 2. Receive (`AzureServiceBusMessageEnvelopeParser.ParseEnclosedMessageTypes`)

Replace the body with the marshal-array variant above. Wins:
- **Zero-alloc fast path** for single-type messages (the common case in the codebase based on the dispatch-side branch).
- Removes the `ImmutableArray.Builder` allocation (saves one heap allocation per multi-type receive).

Files:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessageEnvelopeParser.cs` (lines 55–76)

Add `using System.Runtime.InteropServices;` at the top of the file.

### 3. Do NOT pursue

- Do not switch to `string[]`, `List<string>`, or `byte[]` on the wire — all fail at send time.
- Do not switch to a length-prefixed binary buffer — same problem (the value type isn't supported, and binary props are broker-broken regardless).
- Do not introduce a separator other than `;` — type URNs in this codebase don't contain `;` and changing the separator only relocates the problem; gains nothing.
- Do not change `MessageEnvelope.EnclosedMessageTypes` away from `ImmutableArray<string>?`. The downstream `MessageTypeSelectionMiddleware` uses a normal `foreach` — no span/`ReadOnlyMemory<char>` consumer exists, so there's no payoff to keeping slices around as memory references and we'd just complicate the public envelope contract for the other transports.

## What is and isn't fixed by this

| Concern | After fix |
| --- | --- |
| Wire-side `string.Join` allocation per multi-type send | Reduced (best-overload path), final string still allocated — unavoidable |
| Wire-side string allocation per single-type send | Already zero-alloc; preserved |
| Receive-side `ImmutableArray.Builder` allocation per multi-type receive | Eliminated |
| Receive-side per-string allocation (`new string(span[range])`) | Unchanged — this is the minimum-cost copy from a `ReadOnlySpan<char>` to a `string` |
| Receive-side allocation when only one enclosed type | Eliminated (fast path returns `ImmutableArray.Create(encoded)` reusing the original string) |

## Verification plan

- Add an inline-snapshot or fact-style test in `Mocha.Transport.AzureServiceBus.Tests` proving that:
  - A single-type envelope round-trips and the receive side returns an `ImmutableArray<string>` whose element is reference-equal to the property-bag string (proves the fast path).
  - A 3-type envelope round-trips and parses to the right values.
  - An unsupported type *would* throw — sanity check that we kept the contract (just write a test that puts a `string[]` in `props` and confirm `SendMessageAsync` throws `SerializationException`; this is documentation as code).
- Run the relevant test class with `--filter` per the project's testing convention.

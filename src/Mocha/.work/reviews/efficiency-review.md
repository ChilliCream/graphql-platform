# Kafka Transport Efficiency Review

## Critical

### 1. ReceiveContext.SetEnvelope has a bug that causes infinite recursive header copy

**File:Line**: `src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs:211`

**Current**: When the envelope has headers, the code calls `Headers.AddRange(Headers)` -- copying the context's own headers into itself before merging envelope headers. This is a self-copy bug (should be `_headers.AddRange(envelope.Headers)` or just the foreach loop below). On a fresh context from the pool, `_headers` is empty so this is a no-op, but if the context were ever pre-populated with headers before `SetEnvelope`, this would duplicate them.

```csharp
if (envelope.Headers is not null)
{
    Headers.AddRange(Headers);  // <-- BUG: copies itself

    foreach (var header in envelope.Headers)
    {
        Headers.Set(header.Key, header.Value);
    }
}
```

**Recommended**: Remove the `Headers.AddRange(Headers)` line entirely. The foreach loop below already handles merging envelope headers into the context.

**Why**: Correctness bug, not just efficiency. If headers were pre-populated, this is an infinite-growth loop on subsequent calls. The allocator impact is secondary to the correctness issue.

---

### 2. `new TaskCompletionSource` allocated per dispatch (KafkaDispatchEndpoint)

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:87`

**Current**: Every `DispatchAsync` call allocates a `TaskCompletionSource` + its internal `Task<TResult>` object. The code has a TODO comment acknowledging this.

```csharp
var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
```

**Recommended**: Pool `ManualResetValueTaskSourceCore<bool>`-based objects (implement `IValueTaskSource`) to avoid per-dispatch `Task` allocation entirely. At minimum, consider a `ValueTaskCompletionSource` pattern backed by `ObjectPool`.

**Why**: At high throughput (e.g., 100K+ msg/s), `TaskCompletionSource` + `Task` allocation is a primary GC pressure source. The existing TODO acknowledges this. This is the single largest allocation on the dispatch hot path.

---

### 3. `new MessageEnvelope()` allocated per receive (KafkaMessageEnvelopeParser.Parse)

**File:Line**: `src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs:25-42`

**Current**: Every consumed message creates a new `MessageEnvelope` class instance with ~15 string fields, plus a `new Headers(customCount)` and the `List<HeaderValue>` inside it.

**Recommended**: Make `MessageEnvelope` a struct, or pool it. Since `ReceiveContext` is already pooled and owns all the same fields, consider having the Kafka parsing middleware populate `ReceiveContext` directly instead of creating an intermediate envelope object. The envelope is only used to call `context.SetEnvelope(envelope)` which copies all fields out of it, making the envelope itself garbage immediately.

**Why**: This is the largest per-message allocation on the receive hot path: one `MessageEnvelope` class + one `Headers` + one `List<HeaderValue>` + N `HeaderValue` structs boxed inside the list. All immediately become garbage after `SetEnvelope` copies data out.

---

## Important

### 4. Repeated `Encoding.UTF8.GetBytes` allocations in BuildKafkaHeaders

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:171-249`

**Current**: Every dispatch calls `Encoding.UTF8.GetBytes(...)` for each non-null envelope field (up to 12+ calls). Each call allocates a new `byte[]`.

```csharp
headers.Add(KafkaMessageHeaders.MessageId, Encoding.UTF8.GetBytes(envelope.MessageId));
headers.Add(KafkaMessageHeaders.CorrelationId, Encoding.UTF8.GetBytes(envelope.CorrelationId));
// ... 10+ more
```

**Recommended**: Use a shared `ArrayBufferWriter<byte>` or stackalloc + `Encoding.UTF8.GetBytes(span, destination)` overload for short headers. For frequently-repeated values (content type, message type), consider caching the encoded bytes. At minimum, use `Encoding.UTF8.GetByteCount` + rent from `ArrayPool<byte>` pattern.

Note: Confluent.Kafka's `Headers.Add` takes `byte[]` and stores a reference, so the allocation cannot be fully eliminated without custom Kafka header handling, but the number of separate allocations can be reduced.

**Why**: 12+ `byte[]` allocations per dispatch. For typical messages with all standard fields populated, this is ~12 small arrays per message that immediately become Gen0 garbage.

---

### 5. Repeated `Encoding.UTF8.GetString` allocations in KafkaMessageEnvelopeParser

**File:Line**: `src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs:46-58`

**Current**: `GetHeaderString` calls `Encoding.UTF8.GetString(bytes)` for each well-known header (up to 12+ calls per message). Each allocates a new string.

```csharp
private static string? GetHeaderString(Confluent.Kafka.Headers? headers, string key)
{
    if (headers.TryGetLastBytes(key, out var bytes))
    {
        return Encoding.UTF8.GetString(bytes);
    }
    return null;
}
```

**Recommended**: These strings are immediately stored in `MessageEnvelope` and then copied to `ReceiveContext`. If the intermediate `MessageEnvelope` is eliminated (see finding #3), the strings could be created once and stored directly on `ReceiveContext`. Also consider: for fields like `ContentType` which have a small finite set of values ("application/json"), intern or cache them to avoid repeated allocations of identical strings.

**Why**: 12+ string allocations per consumed message. Combined with the envelope allocation overhead, this means ~25+ allocations per message on the receive hot path.

---

### 6. `Encoding.UTF8.GetBytes(keySource)` for Kafka message key

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:165-169`

**Current**: `SelectKey` allocates a `byte[]` from `Encoding.UTF8.GetBytes` for the correlation ID or message ID on every dispatch.

```csharp
private static byte[]? SelectKey(MessageEnvelope envelope)
{
    var keySource = envelope.CorrelationId ?? envelope.MessageId;
    return keySource is not null ? Encoding.UTF8.GetBytes(keySource) : null;
}
```

**Recommended**: Use `ArrayPool<byte>.Shared.Rent` + `Encoding.UTF8.GetBytes(chars, bytes)` and return the array after `Produce()`. Or use the stackalloc + span overload if the key is guaranteed short (GUIDs are 36 chars = 36 bytes UTF-8).

**Why**: One more `byte[]` allocation per dispatch. This is the message key which is typically a GUID string (36 bytes).

---

### 7. `CancellationTokenRegistration` via `Register` on every dispatch

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:91-95`

**Current**: Every dispatch creates a `CancellationTokenRegistration` via `cancellationToken.Register(...)`. This allocates internally (delegate + state capture).

```csharp
await using var ctr = cancellationToken.Register(static state =>
{
    var t = (TaskCompletionSource)state!;
    t.TrySetCanceled();
}, tcs);
```

**Recommended**: If the `TaskCompletionSource` is replaced with a pooled `IValueTaskSource` (finding #2), cancellation can be baked into the pooled object's lifecycle, eliminating the per-dispatch registration. Alternatively, check `cancellationToken.CanBeCanceled` before registering.

**Why**: One allocation per dispatch for the registration. With the static callback + state pattern it's already optimized, but the registration itself still allocates.

---

### 8. `new Confluent.Kafka.Headers()` allocated per dispatch

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:173`

**Current**: `BuildKafkaHeaders` creates a new `Confluent.Kafka.Headers` collection on every dispatch. This is a Confluent.Kafka class with an internal `List<IHeader>`.

**Recommended**: This is hard to avoid since `Message<K,V>` takes ownership of headers. However, if the Confluent.Kafka `Headers` class supports capacity hints, pre-size it based on the number of non-null fields to avoid list resizing.

**Why**: One `Headers` + one `List<IHeader>` + N `Header` objects per dispatch. Combined with the byte[] allocations, the total per-dispatch overhead is substantial.

---

### 9. `new Message<byte[], byte[]>` struct is fine, but the body copy path can be improved

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:66-77`

**Current**: The code already uses `MemoryMarshal.TryGetArray` to avoid `.ToArray()` when the body is backed by a contiguous array. This is good. However, the fallback `envelope.Body.ToArray()` still allocates when the memory is not backed by a byte array.

```csharp
byte[] body;
if (MemoryMarshal.TryGetArray(envelope.Body, out var segment)
    && segment.Offset == 0
    && segment.Count == segment.Array!.Length)
{
    body = segment.Array;
}
else
{
    body = envelope.Body.ToArray();
}
```

**Recommended**: The `MemoryMarshal.TryGetArray` optimization is well-done. The only improvement would be to relax the offset/length check -- even if offset != 0 or count != array.Length, you could use the array with `Span<byte>`, but Confluent.Kafka requires `byte[]` (not `Span`), so this is a reasonable tradeoff. Consider: if the `PooledArrayWriter` always produces contiguous byte[] backing (which it likely does), document that this fallback path is effectively dead code.

**Why**: Minor. The `TryGetArray` optimization already handles the common case well.

---

### 10. `SentAt` formatted as string round-trip

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:229` and `src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs:61-69`

**Current**: On dispatch, `SentAt` is formatted via `DateTimeOffset.ToString("O")`, allocating a string. On receive, it's parsed back via `DateTimeOffset.TryParse`. The "O" format for a `DateTimeOffset` produces a 33-character string.

```csharp
// Dispatch
headers.Add(KafkaMessageHeaders.SentAt, Encoding.UTF8.GetBytes(envelope.SentAt.Value.ToString("O")));

// Receive
var value = GetHeaderString(headers, KafkaMessageHeaders.SentAt);
if (value is not null && DateTimeOffset.TryParse(value, out var result)) { ... }
```

**Recommended**: Use `Utf8Formatter.TryFormat` to write the `DateTimeOffset` directly to a stackalloc'd byte buffer, avoiding the intermediate string allocation. On receive, use `Utf8Parser.TryParse` directly on the byte span from `TryGetLastBytes`.

**Why**: Two allocations saved per message (one string + one byte[] on dispatch, one string on receive).

---

### 11. `string.Join(",", enclosed)` for EnclosedMessageTypes

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:233-234`

**Current**: Allocates an intermediate string via `string.Join` and then encodes it to bytes.

```csharp
headers.Add(KafkaMessageHeaders.EnclosedMessageTypes,
    Encoding.UTF8.GetBytes(string.Join(",", enclosed)));
```

**Recommended**: Write directly to a byte buffer using `Encoding.UTF8.GetBytes` for each element with comma separators, avoiding the intermediate string. Or use `string.Create` for a single allocation.

**Why**: One extra string allocation per dispatch when enclosed types are present (most messages).

---

### 12. `Uri.TryCreate` per receive for each address field

**File:Line**: `src/Mocha/src/Mocha/Middlewares/ReceiveContext.cs:199-202`

**Current**: `SetEnvelope` calls `string.ToUri()` which internally calls `Uri.TryCreate` for each of 4 address fields. `Uri` construction is relatively expensive (parsing, normalization, allocations).

```csharp
SourceAddress = envelope.SourceAddress.ToUri();
DestinationAddress = envelope.DestinationAddress.ToUri();
ResponseAddress = envelope.ResponseAddress.ToUri();
FaultAddress = envelope.FaultAddress.ToUri();
```

**Recommended**: Cache `Uri` instances per unique address string. Since addresses are typically a small, finite set (the bus's own endpoints), a `ConcurrentDictionary<string, Uri>` cache would eliminate repeated parsing. Or do lazy `Uri` creation -- only parse when the property is actually accessed.

**Why**: Up to 4 `Uri` allocations per received message, each involving string parsing and normalization.

---

### 13. `DispatchSerializerMiddleware` instantiated per endpoint instead of singleton

**File:Line**: `src/Mocha/src/Mocha/Middlewares/Dispatch/DispatchSerializerMiddleware.cs:53-58`

**Current**: The factory lambda `static (_, next) => { var middleware = new DispatchSerializerMiddleware(); ... }` creates a new instance per endpoint pipeline compilation.

```csharp
public static DispatchMiddlewareConfiguration Create()
    => new(
        static (_, next) =>
        {
            var middleware = new DispatchSerializerMiddleware();
            return ctx => middleware.InvokeAsync(ctx, next);
        },
        "Serialization");
```

**Recommended**: Use a singleton like `KafkaCommitMiddleware` and `KafkaParsingMiddleware` already do. The middleware is stateless.

```csharp
private static readonly DispatchSerializerMiddleware s_instance = new();
public static DispatchMiddlewareConfiguration Create()
    => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next), "Serialization");
```

**Why**: Minor -- one-time setup allocation, not per-message. But inconsistent with the pattern used elsewhere.

---

### 14. `ConsumeContextAccessor` resolved via `GetRequiredService` twice per receive

**File:Line**: `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:134,146`

**Current**: `ExecuteAsync` calls `GetRequiredService<ConsumeContextAccessor>()` in both the try and finally blocks.

```csharp
var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
accessor.Context = context;

// ... later in finally:
var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
accessor.Context = null;
```

**Recommended**: Resolve once and reuse across both blocks.

```csharp
var accessor = scope.ServiceProvider.GetRequiredService<ConsumeContextAccessor>();
try
{
    accessor.Context = context;
    // ...
}
finally
{
    accessor.Context = null;
    pools.ReceiveContext.Return(context);
}
```

**Why**: DI container lookup has non-trivial overhead (dictionary lookup + type checks). Two lookups per message is unnecessary.

---

### 15. `Guid.NewGuid().ToString()` for MessageId, CorrelationId, ConversationId

**File:Line**: `src/Mocha/src/Mocha/Middlewares/DispatchContext.cs:233-235`

**Current**: Three GUID allocations + three string allocations per dispatch context initialization.

```csharp
MessageId ??= Guid.NewGuid().ToString();
CorrelationId ??= Guid.NewGuid().ToString();
ConversationId ??= Guid.NewGuid().ToString();
```

**Recommended**: Use `Guid.NewGuid().ToString("N")` for shorter strings (32 chars vs 36), or use `Guid.CreateVersion7()` for time-ordered IDs (better for Kafka partitioning). For even lower allocation: consider using `stackalloc` + `Guid.TryFormat` with `string.Create` to avoid the default format.

**Why**: 3 string allocations per dispatch (each 36 bytes). Minor individually but adds up at high throughput.

---

## Minor

### 16. `Confluent.Kafka.Headers` iterated twice in `BuildHeaders`

**File:Line**: `src/Mocha.Transport.Kafka/KafkaMessageEnvelopeParser.cs:109-139`

**Current**: The parser iterates Kafka headers twice: once to count custom headers, once to extract them. The `FrozenSet` lookup in `IsWellKnownHeader` is efficient, but the double iteration adds CPU overhead.

**Recommended**: Single-pass: use `List<HeaderValue>` or `ArrayBuilder`, then check count at the end. The capacity hint optimization from the two-pass approach only saves one list resize at most.

**Why**: CPU efficiency. Two iterations over ~12-20 headers. The FrozenSet lookup is O(1) so this is minor.

---

### 17. `CreateAsyncScope` per receive message

**File:Line**: `src/Mocha/src/Mocha/Endpoints/ReceiveEndpoint.cs:125`

**Current**: Every received message creates a DI scope. This is standard practice and correct for scoped services (DbContext, etc.), but the scope creation itself allocates.

**Recommended**: This is architecturally correct and expected. No change recommended unless a scope-free fast path is needed for consumers that don't require scoped services.

**Why**: N/A -- this is by design. Noted for completeness.

---

### 18. `new string(path[ranges[1]])` allocation in ResolveTopicName

**File:Line**: `src/Mocha.Transport.Kafka/KafkaDispatchEndpoint.cs:155`

**Current**: For reply messages, `ResolveTopicName` allocates a new string from a span slice. This happens per-reply dispatch.

**Recommended**: Cache the topic name after first resolution, or use a dictionary lookup. For reply endpoints, the topic name is typically the same for all messages to the same destination.

**Why**: One string allocation per reply dispatch. Minor since reply frequency is typically lower than publish/send.

---

### 19. `consumer.Commit(consumeResult)` is synchronous on the consume loop

**File:Line**: `src/Mocha.Transport.Kafka/Middlewares/Receive/KafkaCommitMiddleware.cs:30`

**Current**: Per-message synchronous commit. The comment acknowledges this is safe for sequential processing, but it's a throughput limiter.

**Recommended**: Consider batch commit: accumulate offsets and commit periodically (e.g., every N messages or every T milliseconds). This dramatically improves throughput. Confluent.Kafka's `StoreOffset` + auto-commit is one pattern; another is manual batch commit in the consume loop.

**Why**: Synchronous commit per message round-trips to the broker, adding ~5-20ms latency per message. For a 10ms commit, throughput caps at ~100 msg/s per partition. Batch commit can achieve 10,000+ msg/s.

---

## Summary

### Receive Path (per message):
- 1x `MessageEnvelope` class (Critical #3)
- 1x `Headers` + `List<HeaderValue>` (Critical #3)
- 12+ `string` from UTF-8 decode (Important #5)
- 4x `Uri` from address parsing (Important #12)
- 1x DI scope (expected)
- 2x `GetRequiredService<ConsumeContextAccessor>` (Important #14)
- **Total**: ~20+ allocations per received message

### Dispatch Path (per message):
- 1x `TaskCompletionSource` + `Task` (Critical #2)
- 1x `CancellationTokenRegistration` (Important #7)
- 12+ `byte[]` from UTF-8 encode (Important #4)
- 1x `byte[]` for message key (Important #6)
- 1x `Confluent.Kafka.Headers` + N `Header` objects (Important #8)
- 1x `MessageEnvelope` class (from `CreateEnvelope`)
- 3x `Guid.ToString()` (Important #15)
- 1x `string` for SentAt format (Important #10)
- **Total**: ~25+ allocations per dispatched message

### Priority Recommendations:
1. **Eliminate intermediate `MessageEnvelope` on receive** -- parse Kafka headers directly into `ReceiveContext` (saves ~15 allocations)
2. **Pool `IValueTaskSource` for dispatch delivery reports** (saves 2 allocations: TCS + Task)
3. **Batch Kafka offset commits** (throughput improvement, not allocation)
4. **Fix `SetEnvelope` self-copy bug** (correctness)
5. **Cache `Uri` instances for endpoint addresses** (saves 4 allocations per receive)
6. **Intern/cache well-known strings** (content type, message type) on receive

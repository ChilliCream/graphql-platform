# Azure Event Hub .NET SDK: High-Performance API Research

**Package**: `Azure.Messaging.EventHubs` v5.12.2
**Namespace**: `Azure.Messaging.EventHubs`

---

## 1. ReadOnlyMemory<byte> / Memory<byte> Support

**YES** -- first-class support via `BinaryData` and direct constructor.

### EventData Constructor
```csharp
// Direct ReadOnlyMemory<byte> constructor -- zero-copy wrap
new EventData(ReadOnlyMemory<byte> eventBody)

// BinaryData constructor (BinaryData wraps ReadOnlyMemory<byte> without copy)
new EventData(BinaryData eventBody)
```

### EventData.EventBody (BinaryData)
The primary body accessor is `EventBody` which returns `BinaryData`. BinaryData provides:
- `ToMemory()` -- returns `ReadOnlyMemory<byte>` (zero-copy, returns the wrapped memory)
- Implicit conversion operator to `ReadOnlyMemory<byte>` (zero-copy)
- Implicit conversion operator to `ReadOnlySpan<byte>` (zero-copy)
- `ToArray()` -- allocates a new `byte[]` (AVOID on hot paths)
- `ToStream()` -- returns a read-only stream over the data (no copy)

### EventData.Body (legacy)
Type: `ReadOnlyMemory<byte>` (get-only). Backward-compatible property, returns the body directly as `ReadOnlyMemory<byte>`.

### Key Insight: BinaryData is a Zero-Copy Wrapper
`BinaryData` constructors and `FromBytes()` accepting `ReadOnlyMemory<byte>` **wrap** without copying:
```csharp
// All of these wrap, NOT copy:
new BinaryData(ReadOnlyMemory<byte> data)
BinaryData.FromBytes(ReadOnlyMemory<byte> data)
new BinaryData(byte[] data)          // wraps the array
BinaryData.FromBytes(byte[] data)    // wraps the array
```

---

## 2. IBufferWriter<byte> / ReadOnlySequence<byte> Support

**NO** -- the SDK does not expose `IBufferWriter<byte>` or `ReadOnlySequence<byte>` anywhere in its public API.

The SDK operates exclusively with `BinaryData` / `ReadOnlyMemory<byte>` / `byte[]` for message bodies. There is no way to write directly into an SDK-owned buffer.

**Implication for Mocha transport**: Serialization must produce a `byte[]` or `ReadOnlyMemory<byte>` first, then wrap it in `BinaryData` or pass to `EventData(ReadOnlyMemory<byte>)`. If using `IBufferWriter<byte>` internally (e.g., `ChunkedArrayWriter`), the writer's output must be converted to `ReadOnlyMemory<byte>` before constructing `EventData`.

---

## 3. Span<T> Support

**LIMITED** -- only via `BinaryData` implicit conversions:
```csharp
BinaryData body = eventData.EventBody;
ReadOnlySpan<byte> span = body; // implicit operator, zero-copy
```

No methods in the Event Hub client accept `Span<T>` or `ReadOnlySpan<T>` as parameters. Constructors and setters require `ReadOnlyMemory<byte>`, `byte[]`, `BinaryData`, or `string`.

---

## 4. Zero-Copy Send/Receive Analysis

### Sending (Allocation Profile)
```csharp
// Best case: pre-serialized bytes
byte[] serialized = ...; // already have this from serializer
var eventData = new EventData(serialized); // wraps, no copy
// OR
ReadOnlyMemory<byte> mem = ...;
var eventData = new EventData(mem); // wraps, no copy
```

**Unavoidable allocations on send:**
- `EventData` object allocation (class, not struct)
- `BinaryData` object allocation (class, not struct) -- created internally
- `IDictionary<string, object>` for `Properties` (lazily allocated, only if used)
- `EventDataBatch` serializes EventData to AMQP wire format internally (copy into batch buffer)

**Avoidable allocations:**
- Don't use `new EventData(string)` -- causes UTF-8 encoding allocation
- Don't call `.ToArray()` on received bodies
- Reuse `EventHubProducerClient` -- it pools connections internally

### Receiving (Allocation Profile)
```csharp
// Zero-copy body access
ReadOnlyMemory<byte> body = receivedEvent.EventBody.ToMemory(); // no copy
ReadOnlySpan<byte> span = receivedEvent.EventBody;              // no copy
```

**Unavoidable allocations on receive:**
- `EventData` object per received event
- `BinaryData` for the body
- `IReadOnlyDictionary<string, object>` for `SystemProperties`
- AMQP deserialization buffers

**Cannot achieve true zero-copy** end-to-end because EventData is a class that gets allocated per message. The body access itself is zero-copy from BinaryData though.

---

## 5. Object Pooling for Connections/Channels/Producers

**Built-in connection management, no explicit pooling API.**

- `EventHubConnection` can be shared across multiple producers and consumers
- Clients manage their own AMQP link lifecycle internally
- All clients (`EventHubProducerClient`, `EventHubConsumerClient`, `PartitionReceiver`) are designed to be **cached for application lifetime**
- Internal connection pooling is handled by the AMQP transport layer

```csharp
// Shared connection pattern (reduces TCP/TLS connections)
var connection = new EventHubConnection(connectionString);
var producer = new EventHubProducerClient(connection);
var consumer = new EventHubConsumerClient(consumerGroup, connection);
```

**No EventData pooling**: EventData cannot be pooled/recycled as there's no Reset() or Clear() method. Each message creates a new EventData instance.

---

## 6. Most Allocation-Efficient Patterns

### 6a. Send a Message with Pre-Serialized Body
```csharp
// BEST: wrap existing byte[] or ReadOnlyMemory<byte>
byte[] serialized = Serialize(message); // your serialization
var eventData = new EventData(serialized); // wraps, no copy

// If you have ReadOnlyMemory<byte> from a buffer writer:
ReadOnlyMemory<byte> mem = bufferWriter.WrittenMemory;
var eventData = new EventData(mem);

// Set properties only if needed (lazy dict allocation)
eventData.Properties["Type"] = "SomeType";
eventData.ContentType = "application/octet-stream";
```

### 6b. Receive and Access Body as ReadOnlyMemory<byte>
```csharp
// BEST: implicit conversion, zero-copy
ReadOnlyMemory<byte> body = receivedEvent.EventBody.ToMemory();

// Or use the legacy property directly:
ReadOnlyMemory<byte> body = receivedEvent.Body;

// For Span access (stack only):
ReadOnlySpan<byte> span = receivedEvent.EventBody;

// AVOID: allocates a new byte[]
byte[] copy = receivedEvent.EventBody.ToArray(); // BAD on hot paths
```

### 6c. Set/Read Message Headers Without Dictionary Allocation

**Cannot fully avoid dictionary allocation.** `EventData.Properties` is `IDictionary<string, object>`, lazily initialized on first access. However:

- If you don't touch `Properties`, no dictionary is allocated on the send side
- `SystemProperties` on received events is always allocated by the SDK
- For AMQP-level header access, use `GetRawAmqpMessage()`:

```csharp
// AMQP-level access (still uses dictionaries but gives more control)
AmqpAnnotatedMessage amqp = eventData.GetRawAmqpMessage();

// Structured header (no dictionary, dedicated properties):
AmqpMessageHeader header = amqp.Header;
header.DeliveryCount; // uint?
header.Durable;       // bool?
header.FirstAcquirer; // bool?
header.Priority;      // byte?
header.TimeToLive;    // TimeSpan?

// AmqpMessageProperties (structured, no dictionary):
AmqpMessageProperties props = amqp.Properties;
props.MessageId;       // AmqpMessageId
props.CorrelationId;   // AmqpMessageId
props.ContentType;     // string
props.Subject;         // string
props.To;              // AmqpAddress
props.ReplyTo;         // AmqpAddress
props.GroupId;         // string

// Application properties (dictionary-based, IDictionary<string, object>):
amqp.ApplicationProperties["key"] = value;

// Check if section exists before accessing (avoids lazy allocation):
if (amqp.HasSection(AmqpMessageSection.ApplicationProperties))
{
    // only then access amqp.ApplicationProperties
}
```

**Strategy**: For transport-level metadata (message type, correlation), prefer the structured `AmqpMessageProperties` fields (`Subject`, `CorrelationId`, `ContentType`, `MessageId`, `GroupId`) over `Properties` dictionary to avoid dictionary allocations.

---

## 7. Batch APIs (EventDataBatch)

### EventDataBatch
```csharp
// Create batch (first call queries service for max size, subsequent calls cached)
await using var batch = await producer.CreateBatchAsync();

// Try-add pattern (serializes to AMQP internally, measures actual wire size)
if (!batch.TryAdd(eventData))
{
    // Event too large for remaining batch space
    // Send current batch and start a new one
}

// Atomic send -- all succeed or all fail
await producer.SendAsync(batch);
```

**Properties:**
- `Count` -- number of events in batch
- `MaximumSizeInBytes` -- max allowed size (queried from service)
- `SizeInBytes` -- current wire-format size

**Allocation patterns:**
- `EventDataBatch` implements `IDisposable` (manages unmanaged AMQP buffer)
- `TryAdd()` serializes EventData to AMQP wire format and copies into internal buffer
- The internal buffer is the actual wire payload -- no additional serialization on `SendAsync`
- Each `TryAdd()` call involves AMQP serialization cost
- Batch creation allocates internal transport buffer

**Alternative: Direct Send (no batch)**
```csharp
// Send IEnumerable<EventData> directly -- service validates size
await producer.SendAsync(new[] { event1, event2, event3 });

// With partition options:
await producer.SendAsync(events, new SendEventOptions { PartitionKey = "key" });
```
Direct send skips client-side size validation. Use when you know events fit within limits.

### EventHubBufferedProducerClient
Higher-level alternative that auto-manages batching:
- Automatically groups events into optimally-sized batches
- Publishes in background on a timer or when batch is full
- Less control but reduces complexity
- **Not recommended for Mocha transport** due to non-deterministic publish timing

---

## 8. Threading Model & Client Safety

### Thread Safety Summary

| Type | Thread-Safe? | Notes |
|------|-------------|-------|
| `EventHubProducerClient` | **YES** | All methods thread-safe. Cache for app lifetime. |
| `EventHubConsumerClient` | **YES** | All methods thread-safe. Cache for app lifetime. |
| `PartitionReceiver` | **YES** | All methods thread-safe. Cache for app lifetime. |
| `EventHubConnection` | **YES** | Shareable across multiple clients. |
| `EventData` | **NO** | Not thread-safe. Don't share across threads. |
| `EventDataBatch` | **Partially** | Thread-safe during publish, but don't concurrent TryAdd. |

### Client Lifecycle
```csharp
// Recommended: singleton per application, share connection
var connection = new EventHubConnection(connectionString);
var producer = new EventHubProducerClient(connection, options);

// At shutdown:
await producer.DisposeAsync();
await connection.DisposeAsync();
```

---

## 9. BinaryData and EventBody Deep Dive

### EventData.EventBody (BinaryData) Internals
```
EventData
  └── EventBody : BinaryData
        └── internally stores ReadOnlyMemory<byte>
             └── .ToMemory() returns it (zero-copy)
             └── implicit ReadOnlyMemory<byte> (zero-copy)
             └── implicit ReadOnlySpan<byte> (zero-copy)
             └── .ToArray() copies to new byte[] (AVOID)
             └── .ToStream() wraps in MemoryStream (no copy of data)
```

### Construction Path (Send)
```
byte[]/ReadOnlyMemory<byte>
  → new EventData(bytes)
    → internally: new BinaryData(bytes)  // wraps, no copy
      → stores as ReadOnlyMemory<byte>
```

### Access Path (Receive)
```
AMQP wire bytes (received from service)
  → SDK deserializes → EventData created
    → EventBody = new BinaryData(receivedBytes)  // wraps deserialized buffer
      → .ToMemory() → ReadOnlyMemory<byte>       // zero-copy access
```

---

## 10. EventDataBatch Internal Allocation Patterns

Based on the SDK design:

1. **CreateBatchAsync()**: Allocates transport-level buffer (AMQP frame). First call per client makes a network round-trip to query `MaximumSizeInBytes`. Subsequent calls use cached value.

2. **TryAdd(EventData)**:
   - Serializes EventData to AMQP wire format (allocates serialization buffer)
   - Measures serialized size against remaining capacity
   - If fits: copies serialized bytes into batch's internal buffer
   - Returns bool (no exception on failure)
   - **Each TryAdd is a serialization + copy operation**

3. **SendAsync(batch)**:
   - Sends the internal buffer directly (already in wire format)
   - No additional serialization needed
   - Atomic operation

4. **Dispose()**: Releases the internal AMQP buffer.

**Key takeaway**: The batch pre-serializes on TryAdd, so `SendAsync` is a single network write. This is efficient for throughput but means each event is serialized/copied during batch building.

---

## Summary: Recommendations for Mocha Transport

### Sending
1. Use `EventHubProducerClient` (singleton, thread-safe)
2. Serialize message body to `byte[]` or `ReadOnlyMemory<byte>` using Mocha's serializer
3. Construct `EventData(ReadOnlyMemory<byte>)` -- zero-copy wrap
4. Use structured AMQP properties (`ContentType`, `Subject`, `CorrelationId`, `MessageId`) for transport metadata instead of `Properties` dictionary where possible
5. Use `EventDataBatch` + `TryAdd` for batched sends
6. Share `EventHubConnection` across producer and consumer

### Receiving
1. Use `PartitionReceiver.ReceiveBatchAsync()` for low-level control, or `EventHubConsumerClient.ReadEventsFromPartitionAsync()` for IAsyncEnumerable
2. Access body via `eventData.EventBody.ToMemory()` or `eventData.Body` -- both zero-copy
3. Deserialize directly from `ReadOnlyMemory<byte>` / `ReadOnlySpan<byte>`
4. Use `HasSection()` on AMQP message to avoid lazy dictionary allocation when checking for optional metadata

### What Cannot Be Avoided
- EventData object allocation per message (class, not struct)
- BinaryData wrapper allocation per message
- AMQP serialization in EventDataBatch.TryAdd()
- Dictionary allocation if Properties/ApplicationProperties are used
- No IBufferWriter<byte> support -- must materialize bytes before constructing EventData

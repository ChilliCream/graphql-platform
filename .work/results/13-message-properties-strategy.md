# ASB `ServiceBusMessage` Property Strategy

Research for the Mocha Azure Service Bus transport
(`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/`).

The question: how do we set `PartitionKey`, `SessionId`, `MessageId`, `CorrelationId`, `Subject`,
`To`, `ReplyTo`, `ReplyToSessionId`, `ContentType`, `TimeToLive`, `ScheduledEnqueueTime` when
constructing a `ServiceBusMessage`, in a way that's symmetric with how RabbitMQ derives its
routing key?

## 1. Current ASB send path (`AzureServiceBusDispatchEndpoint.CreateMessage`)

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs`
(lines 111-196).

Today the dispatch endpoint maps the envelope to `ServiceBusMessage` with these conventions:

| `ServiceBusMessage` property | Source | Notes |
| --- | --- | --- |
| `MessageId` | `envelope.MessageId` | always set |
| `CorrelationId` | `envelope.CorrelationId` | always set |
| `ContentType` | `envelope.ContentType` | always set |
| `Subject` | `envelope.MessageType` | doubles as the `Label` / `Subject` property |
| `ReplyTo` | `envelope.ResponseAddress` | URI string, not session-aware |
| `TimeToLive` | `envelope.DeliverBy - DateTimeOffset.UtcNow` | only when DeliverBy set and positive |
| `ScheduledEnqueueTime` | not on `ServiceBusMessage` — `envelope.ScheduledTime` is passed to `sender.ScheduleMessageAsync(...)` instead | line 99-103 |
| `PartitionKey` | not set | gap |
| `SessionId` | not set | gap — blocks session-aware queues entirely |
| `ReplyToSessionId` | not set | gap — blocks multiplexed request/reply |
| `To` | not set | gap — broker reserved, but useful for autoforward chaining |

Everything else (`ConversationId`, `CausationId`, source/destination/fault address, `EnclosedMessageTypes`,
`SentAt`, user headers) is shoved into `ApplicationProperties` under the `x-mocha-*` namespace.

## 2. RabbitMQ comparison — how routing keys are derived

The RabbitMQ transport uses a small, three-piece pattern that I recommend cloning for ASB.

### 2a. A typed extractor stored as a feature on `MessageType`

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/RabbitMQRoutingKeyExtractor.cs`

```csharp
internal sealed class RabbitMQRoutingKeyExtractor(Func<object, string?> extractor)
{
    public string? Extract(object message) => extractor(message);

    public static RabbitMQRoutingKeyExtractor Create<TMessage>(Func<TMessage, string?> extractor)
        => new(msg => extractor((TMessage)msg));
}
```

### 2b. A user-facing extension on `IMessageTypeDescriptor`

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/RabbitMQRoutingKeyExtensions.cs`

```csharp
public static IMessageTypeDescriptor UseRabbitMQRoutingKey<TMessage>(
    this IMessageTypeDescriptor descriptor,
    Func<TMessage, string?> extractor)
{
    var features = descriptor.Extend().Configuration.Features;
    features.Set(RabbitMQRoutingKeyExtractor.Create(extractor));
    return descriptor;
}
```

The extractor lives on the `MessageType.Features` collection (loaded from
`MessagingConfiguration.Features` during `MessageType.Initialize`, see
`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/MessageTypes/MessageType.cs:80`).

### 2c. A dispatch middleware that runs the extractor and stashes the result on the dispatch context headers

File: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/Middlewares/Dispatch/RabbitMQRoutingKeyMiddleware.cs`

```csharp
if (context.MessageType is not null
    && context.Message is not null
    && context.MessageType.Features.TryGet<RabbitMQRoutingKeyExtractor>(out var extractor))
{
    var routingKey = extractor.Extract(context.Message);
    if (routingKey is not null)
    {
        context.Headers.Set(RabbitMQMessageHeaders.RoutingKey, routingKey);
    }
}
return next(context);
```

It's wired into the pipeline `before: DispatchMiddlewares.Serialization` in
`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMq/Topology/Extensions/RabbitMQTransportDescriptorExtensions.cs:23`.

The dispatch endpoint then reads the result from `envelope.Headers[x-routing-key]`
(`RabbitMQDispatchEndpoint.cs:113`).

### Key design decisions to inherit

- One extractor per typed *concept* (routing key for Rabbit, partition/session for ASB), not a
  monolithic "build me a message" delegate.
- Configuration lives on `IMessageTypeDescriptor`, next to the message contract — not on the
  endpoint, transport, or per-call.
- Middleware writes to `context.Headers` so it round-trips through the existing serializer pipeline
  and lands on `envelope.Headers`. The dispatch endpoint reads back from there.
- Strict typing via `TMessage` so the lambda is `Func<TMessage, string?>`, not `Func<object, ...>`.

## 3. ASB property semantics — which are required vs optional

Sources: `https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messages-payloads`,
`ServiceBusMessage.PartitionKey`, `ServiceBusMessage.ScheduledEnqueueTime`, `service-bus-partitioning`.

| Property | Required? | Semantics / Gotchas |
| --- | --- | --- |
| `MessageId` | Optional, but auto-assigned by the SDK if you don't set it | Used by **duplicate detection** when enabled on the entity. Free-form; GUID-friendly. Falls back as the partition key when neither `SessionId` nor `PartitionKey` is set on a partitioned entity with dup-detection. |
| `CorrelationId` | Optional | Pure metadata. Convention: reflect the inbound `MessageId` of the message you're replying to. |
| `Subject` (AMQP `subject`, also called `Label`) | Optional | Application-defined "what is this" tag, like an email subject. Doubles as the message-type discriminator for Functions/Logic Apps. |
| `To` | Optional | Reserved by the broker — currently ignored. Safe to set for autoforward chaining; do not depend on it for routing. |
| `ReplyTo` | Optional | Address (queue or topic path) where replies should go. URI-as-string. |
| `ReplyToSessionId` | Optional | The `SessionId` to set on a *reply* sent to `ReplyTo`. Required for multiplexed request/reply — see "Message routing and correlation". Max 128 chars. |
| `SessionId` | **Required if** the destination queue/subscription has `RequiresSession = true`. Otherwise ignored. | Max 128 chars. Acts as partition key for partitioned + session-aware entities. The receiving session lock guarantees in-order, per-session processing. |
| `PartitionKey` | Optional. Required for transactions across a partitioned entity. Ignored on non-partitioned entities. | Max 128 chars. **Must equal `SessionId` if both are set, else `InvalidOperationException`.** Service Bus uses the partition key as the hash input — clients cannot pick the partition directly. |
| `ContentType` | Optional | RFC 2045 string, e.g. `application/json;charset=utf-8`. |
| `TimeToLive` | Optional, defaults to entity's `DefaultMessageTimeToLive` | Relative duration from `EnqueuedTimeUtc`. Server silently clamps to entity max. For scheduled messages, `ExpiresAt = ScheduledEnqueueTime + TimeToLive`, **not** `Now + TimeToLive`. |
| `ScheduledEnqueueTime` | Optional | The earliest UTC time at which the message becomes available. Setting this on `ServiceBusMessage` *and* using `SendMessageAsync` works; using `ScheduleMessageAsync(msg, scheduledTime)` is the equivalent. Either way the broker stores the message immediately and hides it until that time. |

Constraints worth encoding into the strategy:

1. `PartitionKey` MUST equal `SessionId` when both are set. Either reject in middleware or
   default `PartitionKey = SessionId` when only `SessionId` is configured (this is what
   the SDK effectively wants).
2. Both `SessionId` and `PartitionKey` are capped at 128 chars — validate at extract time.
3. If the receive endpoint's queue/subscription has `RequiresSession = true` and an outbound
   message has no `SessionId`, the broker rejects it with `ServiceBusFailureReason.SessionCannotBeLocked`.
   The framework should fail fast at dispatch, not in the broker.
4. Setting `ScheduledEnqueueTime` on the `ServiceBusMessage` is redundant when we also call
   `ScheduleMessageAsync(scheduledTime)` — the SDK overwrites/uses the explicit argument. We
   should pick one path. Recommendation: keep using `ScheduleMessageAsync` and *not* set
   `ScheduledEnqueueTime` on the message body.

## 4. Strategy — hybrid (conventions + per-type extractor hooks)

Two orthogonal levers, in increasing order of overrideability:

### Level A — Convention defaults from the envelope (no user code)

What the dispatch endpoint already does, plus tightening:

- `MessageId` ← `envelope.MessageId`
- `CorrelationId` ← `envelope.CorrelationId`
- `Subject` ← `envelope.MessageType` (URN of the contract)
- `ContentType` ← `envelope.ContentType`
- `ReplyTo` ← `envelope.ResponseAddress`
- `TimeToLive` ← `envelope.DeliverBy - now` when positive
- `ScheduledEnqueueTime` — handled by `ScheduleMessageAsync`; **not** set on the message

These are the right defaults for any message; they should not be overridable per-type because the
envelope is the source of truth for the bus.

### Level B — Per-message-type extractors on `IMessageTypeDescriptor`

For the four ASB-specific properties that have no envelope analog and depend on the *message
payload*:

- `SessionId`
- `PartitionKey` (auto-derives from `SessionId` if not set)
- `ReplyToSessionId`
- `To`

Each is a `Func<TMessage, string?>` registered as a feature on the `MessageType`, mirroring the
RabbitMQ pattern verbatim.

Why per-type and not per-endpoint: session affinity is a *property of the contract*. An
`OrderEvent` is session-keyed by `OrderId` regardless of which endpoint dispatches it. Same logic
RabbitMQ already uses for routing keys.

**User-set headers short-circuit the extractor.** If an earlier caller (send extension, pipeline
middleware, test harness) has already written `x-mocha-session-id` / `x-mocha-partition-key` /
`x-mocha-reply-to-session-id` / `x-mocha-to` onto `context.Headers`, the extractor is *not*
invoked. This is the single override channel that works for both imperative callers (headers)
and per-type defaults (extractors), and is why the header round-trip is worth its cost. No
separate endpoint-level customizer hook is needed — per-dispatch overrides go through
`context.Headers` instead.

## 5. API sketch

### 5a. Per-type extractors — mirrors `UseRabbitMQRoutingKey`

```csharp
namespace Mocha.Transport.AzureServiceBus;

public static class AzureServiceBusMessageTypeExtensions
{
    /// Configures a SessionId extractor. Required for messages routed to session-aware
    /// queues or subscriptions. Returning null produces no SessionId.
    public static IMessageTypeDescriptor UseAzureServiceBusSessionId<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor);

    /// Configures a PartitionKey extractor. If a SessionId is also configured, the
    /// PartitionKey must equal it; otherwise dispatch fails fast.
    public static IMessageTypeDescriptor UseAzureServiceBusPartitionKey<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor);

    /// Configures a ReplyToSessionId extractor — set this on the request type so replies
    /// land on a specific session in the reply queue.
    public static IMessageTypeDescriptor UseAzureServiceBusReplyToSessionId<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor);

    /// Configures a logical "To" extractor — for autoforward chains. Reserved by the
    /// broker; consumers may inspect it but it isn't currently used for routing.
    public static IMessageTypeDescriptor UseAzureServiceBusTo<TMessage>(
        this IMessageTypeDescriptor descriptor,
        Func<TMessage, string?> extractor);
}
```

### 5b. Internal extractor type (one per property, all same shape)

```csharp
internal sealed class AzureServiceBusSessionIdExtractor(Func<object, string?> extractor)
{
    public string? Extract(object message) => extractor(message);

    public static AzureServiceBusSessionIdExtractor Create<TMessage>(Func<TMessage, string?> extractor)
        => new(msg => extractor((TMessage)msg));
}
// ...same shape for PartitionKey, ReplyToSessionId, To
```

(One generic type with a marker would be tempting but the RabbitMQ code prefers a distinct CLR
type per concept so `Features.TryGet<T>` is unambiguous. Stay consistent.)

### 5c. Dispatch middleware that runs all four

The middleware runs each extractor only when the corresponding header is **not** already
present on `context.Headers`. This is the key design point: callers (or upstream middleware)
that explicitly set `x-mocha-session-id` etc. on the outbound headers bypass the extractor
entirely. User-set wins; the extractor is a default producer, not a mandate.

```csharp
internal sealed class AzureServiceBusMessagePropertiesMiddleware
{
    public ValueTask InvokeAsync(IDispatchContext context, DispatchDelegate next)
    {
        if (context.MessageType is { } messageType && context.Message is { } message)
        {
            var headers = context.Headers;

            // SessionId: extractor runs only when the user hasn't already set the header.
            if (!headers.TryGet(AzureServiceBusMessageHeaders.SessionId, out _)
                && messageType.Features.TryGet<AzureServiceBusSessionIdExtractor>(out var s))
            {
                var sessionId = s.Extract(message);
                if (sessionId is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.SessionId, sessionId);
                }
            }

            // PartitionKey: same — user-set wins. Validation against SessionId happens
            // in the dispatch endpoint where we have the final resolved values from both
            // headers (whether user-set or extractor-produced).
            if (!headers.TryGet(AzureServiceBusMessageHeaders.PartitionKey, out _)
                && messageType.Features.TryGet<AzureServiceBusPartitionKeyExtractor>(out var p))
            {
                var partitionKey = p.Extract(message);
                if (partitionKey is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.PartitionKey, partitionKey);
                }
            }

            if (!headers.TryGet(AzureServiceBusMessageHeaders.ReplyToSessionId, out _)
                && messageType.Features.TryGet<AzureServiceBusReplyToSessionIdExtractor>(out var r))
            {
                var v = r.Extract(message);
                if (v is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.ReplyToSessionId, v);
                }
            }

            if (!headers.TryGet(AzureServiceBusMessageHeaders.To, out _)
                && messageType.Features.TryGet<AzureServiceBusToExtractor>(out var t))
            {
                var v = t.Extract(message);
                if (v is not null)
                {
                    headers.Set(AzureServiceBusMessageHeaders.To, v);
                }
            }
        }

        return next(context);
    }

    private static readonly AzureServiceBusMessagePropertiesMiddleware s_instance = new();

    public static DispatchMiddlewareConfiguration Create()
        => new(static (_, next) => ctx => s_instance.InvokeAsync(ctx, next),
            "AzureServiceBusMessageProperties");
}
```

The `PartitionKey` = `SessionId` invariant is checked in `CreateMessage` (see 5d), not in the
middleware, because that's the single point where both final values are known — regardless
of whether each came from the extractor or the caller.

Wired in `AzureServiceBusTransportDescriptorExtensions.AddDefaults`:

```csharp
descriptor.UseDispatch(
    AzureServiceBusDispatchMiddlewares.MessageProperties,
    before: DispatchMiddlewares.Serialization.Key);
```

### 5d. Dispatch endpoint reads back from envelope headers

`AzureServiceBusDispatchEndpoint.CreateMessage` becomes:

```csharp
private static ServiceBusMessage CreateMessage(MessageEnvelope envelope)
{
    var message = new ServiceBusMessage(envelope.Body)
    {
        MessageId = envelope.MessageId,
        CorrelationId = envelope.CorrelationId,
        ContentType = envelope.ContentType,
        Subject = envelope.MessageType,
        ReplyTo = envelope.ResponseAddress,
    };

    if (envelope.DeliverBy is { } deliverBy)
    {
        var ttl = deliverBy - DateTimeOffset.UtcNow;
        if (ttl > TimeSpan.Zero)
        {
            message.TimeToLive = ttl;
        }
    }

    var headers = envelope.Headers;
    if (headers is not null)
    {
        string? sessionId = null;
        if (headers.TryGet(AzureServiceBusMessageHeaders.SessionId, out sessionId))
        {
            message.SessionId = sessionId;
        }

        if (headers.TryGet(AzureServiceBusMessageHeaders.PartitionKey, out var partitionKey))
        {
            if (sessionId is not null && partitionKey != sessionId)
            {
                throw new InvalidOperationException(
                    "PartitionKey must equal SessionId when both are set on a Service Bus message.");
            }
            message.PartitionKey = partitionKey;
        }
        else if (sessionId is not null)
        {
            // Default PartitionKey to SessionId for partitioned + session-aware entities.
            message.PartitionKey = sessionId;
        }

        if (headers.TryGet(AzureServiceBusMessageHeaders.ReplyToSessionId, out var replyToSessionId))
        {
            message.ReplyToSessionId = replyToSessionId;
        }

        if (headers.TryGet(AzureServiceBusMessageHeaders.To, out var to))
        {
            message.To = to;
        }
    }

    // ...existing ApplicationProperties population (ConversationId, CausationId, addresses,
    // MessageType, EnclosedMessageTypes, SentAt, user headers) — unchanged.

    return message;
}
```

`AzureServiceBusMessageHeaders` gains four new private constants:

```csharp
public const string SessionId = "x-mocha-session-id";
public const string PartitionKey = "x-mocha-partition-key";
public const string ReplyToSessionId = "x-mocha-reply-to-session-id";
public const string To = "x-mocha-to";
```

These are stripped on receive (the parser already filters all `x-mocha-*` keys at line 89-93 of
`AzureServiceBusMessageEnvelopeParser.cs`), so they don't pollute the receive-side header bag.

### 5e. End-user usage

Session-keyed events from the message contract:

```csharp
builder.AddMessage<OrderEvent>(m => m
    .UseAzureServiceBusSessionId<OrderEvent>(e => e.OrderId)
    .Send(r => r.ToAzureServiceBusQueue("orders")));
```

Multiplexed request/reply on a shared reply queue:

```csharp
builder.AddMessage<GetOrderRequest>(m => m
    .UseAzureServiceBusReplyToSessionId<GetOrderRequest>(r => r.RequesterId));
```

Per-call override via `context.Headers`:

```csharp
// Before dispatch, pipeline code or a send extension can short-circuit the extractor
// by setting the framework header directly. The middleware sees it and skips the extractor.
context.Headers.Set(AzureServiceBusMessageHeaders.SessionId, tenantId);
```

Validation that the destination queue actually accepts sessions (fail fast at startup, not at
dispatch) is handled separately by checking
`AzureServiceBusQueue.RequiresSession == true` against the presence of an `IsSessionExtractor`
feature on each registered message type that targets that queue. Out of scope for this strategy
doc but worth flagging for the implementation.

## 6. Property-by-property mapping table

| `ServiceBusMessage` property | Source | Mechanism | Per-call override |
| --- | --- | --- | --- |
| `MessageId` | `envelope.MessageId` | Convention default in `CreateMessage` | Set `envelope.MessageId` |
| `CorrelationId` | `envelope.CorrelationId` | Convention default in `CreateMessage` | Set `envelope.CorrelationId` |
| `Subject` | `envelope.MessageType` (URN) | Convention default in `CreateMessage` | — |
| `ContentType` | `envelope.ContentType` | Convention default in `CreateMessage` | Set `envelope.ContentType` |
| `ReplyTo` | `envelope.ResponseAddress` | Convention default in `CreateMessage` | Set `envelope.ResponseAddress` |
| `TimeToLive` | `envelope.DeliverBy - now` (clamped to >0) | Convention default in `CreateMessage` | Set `envelope.DeliverBy` |
| `ScheduledEnqueueTime` | `envelope.ScheduledTime` passed to `sender.ScheduleMessageAsync(msg, scheduledTime)` | Existing path; do **not** set on the message | Set `envelope.ScheduledTime` |
| `SessionId` | `Func<TMessage, string?>` via `UseAzureServiceBusSessionId<T>` → header `x-mocha-session-id` → read in `CreateMessage` | Per-type extractor (Level B) | Set header `x-mocha-session-id` on `context.Headers` before middleware runs |
| `PartitionKey` | `Func<TMessage, string?>` via `UseAzureServiceBusPartitionKey<T>` → header `x-mocha-partition-key` → read in `CreateMessage`. Defaults to `SessionId` when `SessionId` is set and `PartitionKey` is not. | Per-type extractor (Level B) with auto-coercion | Set header `x-mocha-partition-key` on `context.Headers` |
| `ReplyToSessionId` | `Func<TMessage, string?>` via `UseAzureServiceBusReplyToSessionId<T>` → header `x-mocha-reply-to-session-id` → read in `CreateMessage` | Per-type extractor (Level B) | Set header `x-mocha-reply-to-session-id` on `context.Headers` |
| `To` | `Func<TMessage, string?>` via `UseAzureServiceBusTo<T>` → header `x-mocha-to` → read in `CreateMessage` | Per-type extractor (Level B) | Set header `x-mocha-to` on `context.Headers` |
| `ApplicationProperties` | `envelope.Headers` (minus any `x-mocha-*` keys) + framework keys `x-mocha-*` | Existing convention in `CreateMessage` | Set user headers on `context.Headers` / `envelope.Headers` |

Order of evaluation:

1. Convention defaults from envelope (`MessageId`, `CorrelationId`, `Subject`, etc.).
2. Per-type extractor middleware — runs **only for headers not already set by the caller**.
   User-set `context.Headers[x-mocha-session-id]` etc. short-circuit the extractor entirely.
3. `CreateMessage` reads the resolved headers and assigns `ServiceBusMessage` properties,
   applying the `PartitionKey`/`SessionId` invariant check and filtering `x-mocha-*` keys
   out of `ApplicationProperties`.

## 7. Why this shape

- **Symmetry with RabbitMQ.** Same descriptor, same feature-on-MessageType mechanism, same
  middleware-then-endpoint split. Anyone who has wired `UseRabbitMQRoutingKey` will recognize
  `UseAzureServiceBusSessionId` instantly.
- **Type safety.** `Func<TMessage, string?>` instead of envelope-bag stringly-typed lookup. The
  compiler protects you when the contract evolves.
- **Co-location.** Session affinity is a property of the contract — it belongs in
  `AddMessage<T>(...)` next to the type, not scattered across endpoints.
- **One override channel.** Per-dispatch overrides go through `context.Headers` — set the
  `x-mocha-*` header before the middleware runs and it wins over the extractor. No separate
  endpoint-level customizer is needed; the header channel already covers it and avoids a
  second escape hatch that duplicates the first.
- **Testability.** Each extractor is a pure function over a typed payload — trivial to unit-test
  without spinning up a transport, exactly mirroring `RabbitMQRoutingKeyTests`.
- **No envelope bloat.** We deliberately do **not** add `SessionId`/`PartitionKey` fields to the
  envelope. Those are ASB-specific; routing them through `envelope.Headers` keeps the envelope
  transport-neutral. The `x-mocha-*` namespace is already filtered on receive
  (`AzureServiceBusMessageEnvelopeParser.cs:89-93`) so they don't leak.

## 8. Files that change to implement this

- New: `Mocha.Transport.AzureServiceBus/AzureServiceBusSessionIdExtractor.cs`
- New: `Mocha.Transport.AzureServiceBus/AzureServiceBusPartitionKeyExtractor.cs`
- New: `Mocha.Transport.AzureServiceBus/AzureServiceBusReplyToSessionIdExtractor.cs`
- New: `Mocha.Transport.AzureServiceBus/AzureServiceBusToExtractor.cs`
- New: `Mocha.Transport.AzureServiceBus/AzureServiceBusMessageTypeExtensions.cs` (the four `Use*` methods)
- New: `Mocha.Transport.AzureServiceBus/Middlewares/Dispatch/AzureServiceBusMessagePropertiesMiddleware.cs`
- New: `Mocha.Transport.AzureServiceBus/Middlewares/Dispatch/AzureServiceBusDispatchMiddlewares.cs`
- Edit: `Mocha.Transport.AzureServiceBus/AzureServiceBusMessageHeaders.cs` (add the four new constants)
- Edit: `Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs` — `CreateMessage` reads new headers and feature, applies SessionId/PartitionKey coercion
- Edit: `Mocha.Transport.AzureServiceBus/Topology/Extensions/AzureServiceBusTransportDescriptorExtensions.cs` — register the new dispatch middleware
- New tests: `Mocha.Transport.AzureServiceBus.Tests/AzureServiceBusMessagePropertiesTests.cs` mirroring `RabbitMQRoutingKeyTests.cs`

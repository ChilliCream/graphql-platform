# Azure Service Bus Sessions — Research Findings (07)

Research done against Microsoft Learn (current as of Service Bus SDK 7.20.1)
and the current Mocha ASB transport tree at
`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/`.

---

## 1. What a Session is in Azure Service Bus

A **session** is a server-side mechanism that groups messages by an
application-defined string key (the **`SessionId`**), and gives a single
receiver an **exclusive, ordered, lock-protected** view of every message
carrying that key.

Conceptually a session is "a sub-queue inside a queue/subscription". From the
broker's perspective:

- A session **exists** whenever there is at least one message in the entity
  with that `SessionId`. There is no `CREATE SESSION` API; sessions appear
  implicitly the moment a message with a new `SessionId` lands.
- There is **no defined lifetime** for a session. A message today and a
  message a year later with the same `SessionId` belong to the same session.
- A receiver "accepts" a session, which acquires an **exclusive lock on the
  session** (an umbrella over all the per-message peek-locks). While that
  lock is held, no other receiver in the consumer group sees those messages.
- AMQP wire-level: `SessionId` maps to the AMQP 1.0 `group-id` property.

Source: <https://learn.microsoft.com/azure/service-bus-messaging/message-sessions>

### Guarantees

| Guarantee | Provided by |
| --- | --- |
| **FIFO per session** | Session lock — only one receiver consumes a session at a time, so the relative order of messages with the same `SessionId` is preserved end-to-end. |
| **Exclusive per session** | Acquiring the session lock is an exclusive operation across *all* competing receivers. Service Bus guarantees only one consumer per session at a time, even across machines. |
| **Concurrent demultiplex** | Different sessions can be processed in parallel by different receivers — the FIFO guarantee is *per session*, not per queue. |
| **Per-session state** | Optional opaque `BinaryData` blob stored server-side and bound to the session lifetime, recoverable on the next acquirer. |

### Important behavioural rules

- **Sessions are entity-scoped, not message-scoped.** They must be enabled at
  entity-creation time via `RequiresSession = true`, and **cannot** be
  toggled later (the broker rejects the update — `Sub code=40000`).
- Once `RequiresSession = true`, the entity rejects "regular" sends and
  receives. **Senders must set `SessionId`**, **receivers must use
  `ServiceBusSessionProcessor` / `ServiceBusSessionReceiver`**.
  (`peek` still works on a session-enabled queue.)
- Standard or Premium tier only — Basic does not support sessions.

---

## 2. Topology flag: `RequiresSession`

`RequiresSession` is a **per-entity** property on both queues and topic
subscriptions:

- `Azure.Messaging.ServiceBus.Administration.QueueProperties.RequiresSession`
- `Azure.Messaging.ServiceBus.Administration.SubscriptionProperties.RequiresSession`
- ARM/Bicep/Resource Manager mirror property: `requiresSession`.

Setting it on a subscription means the subscription forwards only sessioned
deliveries; the source topic itself does not have a `RequiresSession` flag.
A topic with multiple subscriptions can therefore have some sessioned and
some plain subscriptions side by side, **provided every message published
already carries a `SessionId`** (because every sessioned subscription will
require one).

Sources:
- <https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions>
- <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-resource-manager-exceptions>

---

## 3. Per-message: the `SessionId` property

```csharp
public class ServiceBusMessage
{
    public string SessionId { get; set; } // max 128 chars
}
```

- For session-aware entities, `SessionId` chooses the session; messages with
  the same `SessionId` are processed in order by the same consumer.
- For non-session entities the value is **silently ignored on receive**, but
  if `EnablePartitioning = true` and there is no `PartitionKey`, the broker
  uses `SessionId` (then `PartitionKey`, then `MessageId`) as the
  partition key.
- **Composes with partitioning**: If both `SessionId` and `PartitionKey`
  are set on the same message, they **must be identical**, otherwise Service
  Bus throws `InvalidOperationException`. This is also the rule for
  transactional sends: every message in a single transaction must share the
  same partition key (which can be `SessionId`).
- `ReplyToSessionId` (AMQP `reply-to-group-id`) is the companion field used
  in the request/response pattern — it tells the responder which
  `SessionId` to put on the reply.

Source: <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-protocol-guide>

---

## 4. `ServiceBusProcessor` vs `ServiceBusSessionProcessor`

Two **different** classes — they are not configurable variants of the same
client. The choice is made at construction time and binds the receiver to
either the standard "competing consumers" pull loop or the session-aware
demultiplexer.

| Aspect | `ServiceBusProcessor` | `ServiceBusSessionProcessor` |
| --- | --- | --- |
| Created by | `ServiceBusClient.CreateProcessor(queueOrSub)` | `ServiceBusClient.CreateSessionProcessor(queueOrSub)` |
| Required entity flag | `RequiresSession = false` | `RequiresSession = true` |
| Concurrency knob | `MaxConcurrentCalls` (default 1) | `MaxConcurrentSessions` (default 8) **and** `MaxConcurrentCallsPerSession` (default 1). Total = product of the two. |
| Ordering | None across receivers | FIFO **within each session** |
| Lifecycle hooks | `ProcessMessageAsync`, `ProcessErrorAsync` | Same, plus `SessionInitializingAsync`, `SessionClosingAsync` |
| Per-delivery args | `ProcessMessageEventArgs` | `ProcessSessionMessageEventArgs` (extends with `SessionId`, `SessionLockedUntil`, `GetSessionStateAsync`, `SetSessionStateAsync`, `RenewSessionLockAsync`, `ReleaseSession()`) |
| Session idle behaviour | n/a | `SessionIdleTimeout` — how long to wait for the next message in the current session before releasing it and rolling to another session. |
| Lock loss reason | `MessageLockLost` | `SessionLockLost` (entire session must be re-acquired; in-flight messages cannot be settled) |
| Dead-lettering of bad message | Per-message `MaxDeliveryCount` | Same per-message `MaxDeliveryCount` — bad message DLQ'd, **session continues** with the next message. (Caveat: session lock expiring **does** count as a delivery for every locked message.) |

Sources:
- <https://learn.microsoft.com/dotnet/api/overview/azure/messaging.servicebus-readme>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessor>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessoroptions.maxconcurrentsessions>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessoroptions.maxconcurrentcallspersession>

---

## 5. Session state (server-stored, application-defined)

`SetSessionStateAsync(BinaryData)` / `GetSessionStateAsync()` exist on:

- `ServiceBusSessionReceiver`
- `ProcessSessionEventArgs` (Session init/close handlers)
- `ProcessSessionMessageEventArgs` (per-message handler)

Properties:

- Opaque blob, max **256 KB** (Standard) / **100 MB** (Premium).
- Stored on the broker, **survives forever** until set to `null` (counts
  against the entity's storage quota).
- Only readable/writable while the **session lock** is held; otherwise
  `ServiceBusException(SessionLockLost)`.
- Designed for: a workflow/saga handler that may crash mid-session — the
  next acquirer of the session reads the state and resumes. Microsoft
  pitches this as their "exactly-once-ish" lever (settle + state update in
  the same transaction).

Source: <https://learn.microsoft.com/azure/service-bus-messaging/message-sessions#message-session-state>

---

## 6. Trade-offs

### Throughput & scale-out

- **Per-session throughput is hard-capped**: only one receiver, in-order,
  serial unless you opt into `MaxConcurrentCallsPerSession > 1` (which
  removes ordering — the SDK explicitly warns that within-session ordering
  requires the default value of 1).
- **Maximum parallelism per consumer process =**
  `MaxConcurrentSessions × MaxConcurrentCallsPerSession`.
  Default 8 × 1 = 8 parallel handler invocations.
- Adding more consumer hosts only helps when there are at least as many
  *active sessions* as sessions held by existing consumers. If your
  `SessionId` cardinality is low, scale-out is bounded by that cardinality —
  this is the same gotcha as Kafka under-partitioned topics.
- Session lock is held until either explicit close or `SessionIdleTimeout`
  fires. Very chatty sessions starve other sessions if `MaxConcurrentSessions`
  is under-sized; very sparse sessions waste a slot until `SessionIdleTimeout`.

### Locking & failure semantics

- **`SessionLockLost`** is a real, non-rare error: lock timer expiry,
  partition movement during scale events, OS updates, app restarts. The SDK
  documents it as expected, the recovery is "drop the session, the next
  consumer gets it".
- Session lock loss invalidates **every in-flight message lock** in that
  session. You cannot settle anything; on the next acquisition the broker
  redelivers from where you left off.
- **Delivery count quirk**: when a session is accepted and the *session*
  lock expires (without anyone abandoning the messages), every locked
  message in the session has its delivery count incremented. So a flaky
  consumer can DLQ an entire session's worth of messages by losing the
  session lock 10 times — with the standard `MaxDeliveryCount = 10`.

### Dead-letter behaviour on session

- A poison message (per-message DLQ via max-delivery-count) is moved to the
  entity's DLQ *individually*. The session continues with the next message,
  preserving FIFO for the survivors.
- A whole-session failure (e.g. TTL expiry, given session-level lock) can
  cause **every message in the session** to be DLQ'd as a unit, depending
  on `DeadLetteringOnMessageExpiration`. This is the "all-or-nothing per
  session" failure mode; it does not exist in non-session queues.
- Replaying a DLQ'd message back into the original queue **breaks
  ordering** for that session — the resubmit gets a new sequence number.
- Sessionful entities cannot use the request/response *with* native
  message-deferral cleanly; the session lock is required to fetch by
  sequence number.

### Session unavailable

- If you call `AcceptSessionAsync(sessionId)` for a session that does not
  exist (no messages with that ID), the receiver waits up to
  `TryTimeout` and then throws `ServiceBusException(ServiceTimeout)`.
- A `ServiceBusSessionProcessor` instead performs `AcceptNextSessionAsync`
  in a loop — it never targets a specific ID, it grabs whatever has
  unconsumed messages.

---

## 7. Comparison to other broker partitioning models

| Concept | Azure Service Bus sessions | Kafka partition keys | RabbitMQ consistent-hash exchange |
| --- | --- | --- | --- |
| **Where the key lives** | `SessionId` on the message | `key` on the producer record | `routing-key` on the publish |
| **What the broker does with it** | Pins messages to a session-lock; one consumer per session at a time | Hashes to a fixed partition; one consumer per partition (per group) | Hashes to one of the bound queues |
| **Cardinality bound** | Unbounded (any string, sessions appear/disappear with messages) | **Fixed at topic creation** (number of partitions) | Fixed at exchange-binding time (number of bound queues) |
| **Ordering scope** | Per session, dynamic | Per partition, static | Per queue (so per hash bucket) |
| **Scale-out unit** | Add receivers — broker hands out one session per receiver | Add consumers up to partition count; beyond that, idle | Add queue + binding; rebalance is manual |
| **Lock vs assignment** | Server-side **lock** with renewal/expiry; transient | Server-side **assignment** to one consumer in group; sticky until rebalance | No lock — queue is exclusive; consumer-side competition within a queue |
| **State per key** | Native `Set/GetSessionState` blob | Only via external store (Kafka Streams / RocksDB) | None native |
| **Failure mode** | Session lock loss → reacquire from broker, redelivery | Consumer death → group rebalance, redeliver from last commit | Consumer death → message requeue per AMQP rules |
| **Cardinality changes** | Free — sessions are dynamic | Repartitioning is operationally expensive (rewrite + downtime) | Add another bound queue + extend hash-ring |
| **Best-fit shape** | High-cardinality keys with bursty per-key sequences (orders by orderId, devices by deviceId) | Bounded high-throughput key space, long-lived consumers | Bounded fan-out across queues for ordered work |

The closest analogue is Kafka. The Service Bus advantage: sessions are
**logical, dynamic, unbounded** in cardinality, and **don't require
repartitioning**. The trade is server-side lock management overhead and the
8-by-default session concurrency limit per consumer.

---

## 8. Current state in the Mocha ASB transport

### What already exists

- **Topology-side `RequiresSession` flag is plumbed end-to-end**:
  - `AzureServiceBusQueueConfiguration.RequiresSession` (input)
  - `AzureServiceBusDefaultQueueOptions.RequiresSession` (default override)
  - `AzureServiceBusQueueDescriptor.WithRequiresSession(bool = true)` (fluent API)
  - `AzureServiceBusQueue.RequiresSession` (resolved value)
  - Provisioning copies it onto `CreateQueueOptions.RequiresSession` at
    `AzureServiceBusQueue.cs:167-170`.
  - The same is true for subscriptions: `AzureServiceBusSubscription.cs:152-155`.
- **`SessionLockLost` is recognised as transient**:
  `AzureServiceBusReceiveEndpoint.cs:159` already classifies it under
  transient processor errors so it logs at warning level and does not crash
  the host.

### What is NOT implemented

The three pieces that actually make sessions usable are missing:

1. **Send path: there is no way to set `SessionId` on outgoing messages.**
   `AzureServiceBusDispatchEndpoint.CreateMessage(...)` (lines 111–196)
   never assigns `message.SessionId` (or `PartitionKey`). So even if a queue
   is provisioned with `RequiresSession = true`, every send to it will be
   rejected by the broker (`InvalidOperationException`: session-aware
   entities require `SessionId`).
2. **Receive path: only `ServiceBusProcessor` is used.**
   `AzureServiceBusReceiveEndpoint.OnStartAsync` always calls
   `clientManager.CreateProcessor(...)` (line 89). There is no branch for
   `Queue.RequiresSession == true`, no `CreateSessionProcessor`, no
   `MaxConcurrentSessions`, no `MaxConcurrentCallsPerSession`, no
   `SessionIdleTimeout`. Starting the existing endpoint against a sessioned
   queue **will throw** at the SDK boundary because `ServiceBusProcessor`
   refuses to start on a `RequiresSession = true` entity.
3. **Envelope: `MessageEnvelope` has no session/partition/ordering field.**
   `MessageEnvelope.cs` exposes `MessageId`, `CorrelationId`,
   `ConversationId`, `CausationId` but nothing that maps to `SessionId`.
   There is also no header constant in `AzureServiceBusMessageHeaders`.

In other words: the topology surface ships the flag, but the data and
runtime path that would actually exercise it do not exist. Today, setting
`WithRequiresSession()` on a Mocha queue produces a queue you cannot send to
or receive from with the rest of the transport.

### Reference for the contrast

The RabbitMQ transport already solved the analogous problem with a
**per-message-type extractor + dispatch middleware** pattern:

- `RabbitMQRoutingKeyExtractor` — `Func<object, string?>` keyed by
  message type via the message-type feature collection.
- `UseRabbitMQRoutingKey<TMessage>(...)` — fluent registration on
  `IMessageTypeDescriptor`.
- `RabbitMQRoutingKeyMiddleware` — runs in the dispatch pipeline, calls
  the extractor, writes the value into the dispatch context headers; the
  terminal then reads it and uses it as the AMQP routing key.
- Wired in `RabbitMQTransportDescriptorExtensions.AddDefaults(...)`:
  `descriptor.UseDispatch(RabbitMQDispatchMiddlewares.RoutingKey, before: DispatchMiddlewares.Serialization.Key);`

This is the right shape to copy.

---

## 9. Recommendation: how Mocha should expose sessions

The three options the question lists are not mutually exclusive; the right
answer is **all three layers, with clean defaults**.

### 9a. Per-message: a `SessionId` extractor (primary surface)

Mirror the RabbitMQ routing-key pattern, because the user need is identical
("derive a string key from the message instance"):

```csharp
descriptor
    .AddMessage<OrderEvent>(d => d
        .UseAzureServiceBusSessionId<OrderEvent>(msg => msg.OrderId));
```

Implementation shape:

- `AzureServiceBusSessionIdExtractor` — internal type holding
  `Func<object, string?>` keyed by message type via the feature collection.
- `UseAzureServiceBusSessionId<TMessage>(...)` extension on
  `IMessageTypeDescriptor`.
- `AzureServiceBusSessionIdMiddleware` — dispatch middleware that runs
  before serialization, calls the extractor, and sets a well-known header
  (e.g. `AzureServiceBusMessageHeaders.SessionId = "x-asb-session-id"`)
  on the dispatch context.
- `AzureServiceBusDispatchEndpoint.CreateMessage(...)` reads that header
  and assigns `message.SessionId`. Same place we already assign `Subject`,
  `ReplyTo`, etc.
- Wire the middleware in
  `AzureServiceBusTransportDescriptorExtensions.AddDefaults(...)` the same
  way `RabbitMQRoutingKey` is wired:
  `before: DispatchMiddlewares.Serialization.Key`.

Why per-message-type and not on the envelope? Because the producer code
(`bus.Publish(orderEvent)`) usually doesn't construct an envelope —
declaring "the session ID for `OrderEvent` is `msg.OrderId`" once at
configuration time is the lower-friction API and matches how Mocha already
treats RabbitMQ routing keys.

**Open question: do we also want an envelope field?** Yes, but a thin one.
Add an optional `string? PartitionKey` (transport-neutral name) on
`MessageEnvelope`. Reasoning:

- Used by the dispatch endpoint as the *fallback* if the per-type extractor
  hasn't run (e.g. raw envelope sends, replies, scheduled redrives).
- It's the natural carrier for cross-transport semantics — Kafka can use it
  as the partition key, RabbitMQ consistent-hash can route on it. Naming it
  `PartitionKey` (not `SessionId`) reflects that it's the
  transport-neutral concept, while ASB will still mint
  `message.SessionId = envelope.PartitionKey` at the wire boundary.
- Header round-trip: in `AzureServiceBusMessageEnvelopeParser` the inbound
  `received.SessionId` should be projected back onto
  `envelope.PartitionKey` for consumers that care.

### 9b. Per-endpoint: a different consumer mode (`SessionProcessor`)

Sessions cannot be a "soft" feature on the receive side — you must
construct a different SDK type. Two clean ways to express it:

1. **Auto-detect from topology** (recommended for ergonomics):
   `AzureServiceBusReceiveEndpoint.OnStartAsync` already has access to
   `Queue.RequiresSession`. Branch on it:
   - `Queue.RequiresSession == true` →
     `clientManager.CreateSessionProcessor(Queue.Name, sessionOptions)`.
   - else → existing `CreateProcessor` path.
   This makes
   `topology.DeclareQueue("orders").WithRequiresSession()` a single
   declaration that flips both provisioning and consumer mode.
2. **Explicit opt-in on the receive endpoint configuration** (for the
   case where the queue was provisioned externally):
   add `bool ConsumeAsSessions { get; set; }` to
   `AzureServiceBusReceiveEndpointConfiguration` and use
   `ConsumeAsSessions ?? Queue.RequiresSession` as the trigger.

In either case, the receive endpoint config grows three knobs that map
onto `ServiceBusSessionProcessorOptions`:

```csharp
public sealed class AzureServiceBusReceiveEndpointConfiguration
{
    // ... existing ...
    public int? MaxConcurrentSessions { get; set; }            // SDK default 8
    public int? MaxConcurrentCallsPerSession { get; set; }     // SDK default 1; MUST stay 1 to preserve order
    public TimeSpan? SessionIdleTimeout { get; set; }          // when to release a quiet session
}
```

When sessions are active, **`MaxConcurrency`** (the existing knob) needs a
defined meaning. The cleanest mapping:

- `MaxConcurrentSessions = MaxConcurrency` if the user did not override
  `MaxConcurrentSessions`.
- `MaxConcurrentCallsPerSession = 1` always, unless the user explicitly
  opts out of ordering.
- The previous `PrefetchCount` heuristic (`MaxConcurrency * 2`) becomes
  per-session prefetch and should probably be smaller (e.g. 5–10) — large
  per-session prefetch is wasteful when most sessions only have a few
  messages.

The receive feature also needs to grow:
`ProcessSessionMessageEventArgs` is a different type from
`ProcessMessageEventArgs`. Either add a sibling
`AzureServiceBusSessionReceiveFeature` or generalise the existing feature
to hold both via a discriminated union / interface. The downstream
acknowledgement and parsing middlewares only call methods that exist on
both (`CompleteMessageAsync`, `AbandonMessageAsync`, `Message`), so
factoring a common interface (or extracting the shared subset behind an
adapter) keeps the receive pipeline unchanged.

### 9c. What to expose to users (API summary)

```csharp
// Topology — already exists, leave as-is
t.DeclareQueue("orders")
 .WithRequiresSession()
 .WithMaxDeliveryCount(10);

// Per-message: derive SessionId from the message
b.AddMessage<OrderPlaced>(d =>
    d.UseAzureServiceBusSessionId<OrderPlaced>(o => o.OrderId));

// Per-endpoint: tune the session processor
t.DeclareReceiveEndpoint("orders", e =>
{
    e.MaxConcurrentSessions = 16;        // tune per host
    e.SessionIdleTimeout = TimeSpan.FromSeconds(30);
});
```

Auto-detection via `Queue.RequiresSession` makes the receive side "just
work" with no extra opt-in.

### 9d. Things to call out in the implementation

- **Validation at startup**: if `Queue.RequiresSession == true` and there
  is no `SessionId` extractor for any message type targeting that queue,
  log a warning. Sending without a `SessionId` to such a queue is a
  guaranteed runtime failure — better to surface it during topology
  validation.
- **Reply endpoints**: `AzureServiceBusDispatchEndpoint` falls into the
  `Reply` branch when sending responses. If the request used `SessionId`,
  the response should set `message.SessionId = envelope.PartitionKey`
  (mapped from `ReplyToSessionId` on the inbound message). This is the
  request/response pattern Microsoft documents — wire it up rather than
  leave it to user code.
- **Don't try to support `MaxConcurrentCallsPerSession > 1`** unless we
  expose a separate "ordered = false" knob — it silently breaks the
  ordering contract that motivated using sessions in the first place.
- **Session state** (`Set/GetSessionStateAsync`) is a saga-shaped feature
  and intentionally out of scope for an initial sessions PR. If we add it
  later, the natural place is on a session-receive feature that exposes
  the underlying `ProcessSessionMessageEventArgs` to user middleware.
- **Native DLQ + sessions interact** with the `UseNativeDeadLetterForwarding`
  flag added recently: the broker DLQs *individual* messages within a
  session normally, but a TTL/lock failure can DLQ the whole session as a
  unit. The integration test for native DLQ should grow a sessioned variant.

---

## 10. Key file references

- Mocha topology where `RequiresSession` already lives:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusSubscription.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusQueueConfiguration.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Descriptors/AzureServiceBusQueueDescriptor.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Descriptors/IAzureServiceBusQueueDescriptor.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusDefaultQueueOptions.cs`
- Mocha receive/dispatch path that needs to grow session support:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusReceiveEndpoint.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusReceiveEndpointConfiguration.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Features/AzureServiceBusReceiveFeature.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessageHeaders.cs`
- Mocha envelope (needs an optional `PartitionKey` field):
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs`
- The pattern to copy from the RabbitMQ transport:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQRoutingKeyExtractor.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQRoutingKeyExtensions.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/Middlewares/Dispatch/RabbitMQRoutingKeyMiddleware.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/Topology/Extensions/RabbitMQTransportDescriptorExtensions.cs`

---

## 11. Authoritative sources

- Message sessions overview: <https://learn.microsoft.com/azure/service-bus-messaging/message-sessions>
- Enable message sessions (queues/subscriptions): <https://learn.microsoft.com/azure/service-bus-messaging/enable-message-sessions>
- `ServiceBusSessionProcessor`: <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessor>
- `ServiceBusSessionProcessorOptions.MaxConcurrentSessions` / `MaxConcurrentCallsPerSession`:
  <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionprocessoroptions>
- `ServiceBusMessage.SessionId`: <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.sessionid>
- Session state methods: <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebussessionreceiver.setsessionstateasync>
- AMQP mapping (`group-id` ↔ `SessionId`): <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-protocol-guide>
- Session lock loss: <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest>
- Sessions vs sequencing: <https://learn.microsoft.com/azure/service-bus-messaging/message-sessions#sequencing-vs-sessions>
- Sequential Convoy pattern (architectural framing): <https://learn.microsoft.com/azure/architecture/patterns/sequential-convoy>
- Partitioning + sessions (composability rules): <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning>

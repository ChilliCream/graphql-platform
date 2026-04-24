# Should Mocha always go through `IScheduledMessageStore`?

## TL;DR

**Recommendation: Hybrid — keep the native fast-path, but reframe the abstraction
so that "scheduling provider" is the single seam, not "scheduled message store".**

Specifically: rename `IScheduledMessageStore` to something like
`IScheduledMessageProvider` (mirroring MassTransit's `IScheduleMessageProvider`),
register **one provider per transport**, route `Persist`/`Cancel` through that
provider, and have a default *aggregating* provider that key-routes by
token-prefix. The `DispatchSchedulingMiddleware` skip logic for native transports
goes away because for native transports the provider is just a thin wrapper that
calls the dispatch endpoint's normal send path with `ScheduledTime` set.

This preserves zero-overhead native scheduling (no extra DB write or polling) while
giving us the uniform pipeline, single cancellation surface, and clean multi-transport
story that the current "skip the middleware entirely" branch trades away.

The current dual approach (middleware path **OR** transport native path) is the
**worst of both worlds** today — see the consistency / devil's-advocate review notes
already in `.work/review/`. M3, C6, and M2 in `.work/review/review.md` are all
symptoms of the same root cause: the abstraction has two incompatible shapes
(`IScheduledMessageStore` *for storage-backed*, transport native code path *for
broker-backed*) sharing the same DI registration slot.

---

## 1. Current design summary

### Core contracts (in `src/Mocha/src/Mocha/Scheduling/`)

- `IScheduledMessageStore` — `PersistAsync(envelope, scheduledTime, ct)` returns an
  opaque cancellation token; `CancelAsync(token, ct)` returns `bool`. Single-instance
  DI contract.
- `DispatchSchedulingMiddleware` — when the dispatch context has `ScheduledTime`
  set, calls `store.PersistAsync` and stops the pipeline (does not call `next`).
- `SchedulingTransportFeature.SupportsSchedulingNatively` — when set on the
  transport feature collection, the **middleware factory** returns `next` directly
  during pipeline construction (`DispatchSchedulingMiddleware.Create`,
  `DispatchSchedulingMiddleware.cs:70`). The middleware is therefore physically
  absent from the pipeline.
- `ScheduledMessageFeature.Token` — pooled feature; the native dispatch endpoint
  writes the broker-issued token here so `DefaultMessageBus.ScheduleSendAsync`
  can read it back into `SchedulingResult.Token`.
- `SchedulingDispatchContextExtensions.SkipScheduler()` — third escape hatch used
  by the Postgres `ScheduledMessageDispatcher` when **replaying** a scheduled
  message (it must reach the transport, not loop back into the store).

### How each transport handles scheduled messages today

| Transport | Native flag | Reads `envelope.ScheduledTime` | `IScheduledMessageStore` registered? | Persistence path |
| --- | --- | --- | --- | --- |
| InMemory | no | no | no | `IScheduledMessageStore` (if user registered one) → middleware persists, transport never sees it |
| RabbitMQ | no | no | no | Same as InMemory — no native delay; relies on user-registered store |
| Postgres | **yes** | yes (`PostgresDispatchEndpoint.cs:55,75,81,92,96`) | yes (`UsePostgresScheduling()` opt-in via EF Core builder) | Native: dispatch endpoint writes to its own scheduled-message table; the `IScheduledMessageStore` registration is for **cancellation only** (and the middleware is bypassed for the persist) |
| Azure Service Bus | **yes** | yes (`AzureServiceBusDispatchEndpoint.cs:99-104`) | yes (auto-registered in `AddAzureServiceBus`) | Native: `sender.ScheduleMessageAsync` returns sequence number; `AzureServiceBusScheduledMessageStore.PersistAsync` **throws** because it's unreachable; `CancelAsync` calls `sender.CancelScheduledMessageAsync` |

### How `CancelScheduledMessageAsync` resolves

`DefaultMessageBus.CancelScheduledMessageAsync` (`DefaultMessageBus.cs:414-428`) does
a single `services.GetService<IScheduledMessageStore>()`. There is no token-prefix
routing. Whichever store DI hands back wins.

### The five clear pain points already captured in review notes

1. **`AzureServiceBusScheduledMessageStore.PersistAsync` throws** —
   "registered as `IScheduledMessageStore` but never callable" is structurally
   contradictory. Documented as M3 / consistency item 5.
2. **Multi-transport cancellation is broken-by-default** — `TryAddScoped` on a
   single contract means ASB+Postgres in the same app silently lose cancellation
   for one of them. Documented as C6 / devil's-advocate item 5.
3. **Token format divergence** — `"asb:{entity}:{seq}"` vs `"postgres-scheduler:{guid}"`.
   No agreed prefix convention. m6.
4. **Native flag is set in `OnAfterInitialized`** — temporally coupled to pipeline
   compile order. Any lifecycle change tips the middleware path back on, and
   `PersistAsync`'s throw is suddenly the user-visible failure. M2.
5. **Hot-path closure allocations** in the native dispatch branch
   (`AzureServiceBusDispatchEndpoint.cs:102-104`) — these wouldn't exist if the
   abstraction normalized the token-write step. M5.

Every one of these is a direct consequence of the abstraction being shaped around
**"persist to a store"** when half the implementations don't persist anything in
Mocha-managed storage at all.

---

## 2. Trade-offs of "always go through IScheduledMessageStore"

### Pros (real)

- **Single seam for cancellation, inspection, retry, observability.** A unified
  `CancelAsync(token)` call resolves the right backend through the abstraction
  rather than through token-prefix sniffing in `CancelScheduledMessageAsync`.
- **Multi-transport composition becomes natural.** Each transport registers its
  own provider keyed by transport name / token prefix; the bus routes by token
  prefix. The C6 problem disappears.
- **Test seam improves.** Replacing `IScheduledMessageStore` with a fake stops
  the dispatch endpoint from making real broker calls during scheduled-send
  unit tests. Today, ASB's native path bypasses any store you might register and
  goes straight to `ServiceBusSender.ScheduleMessageAsync`.
- **The `SchedulingTransportFeature` capability flag and the
  `DispatchSchedulingMiddleware.Create` factory-time skip both go away.** Two
  fewer load-bearing implicit contracts.
- **Removes the `PersistAsync` throws** that exist purely because the abstraction
  is registered but unreachable for native transports.
- **Clean place to land cross-cutting features** — scheduled-message inspection
  endpoints (admin UI), bulk cancel, replay, dead-letter for scheduled messages,
  scheduled-message metrics. Today these have to be implemented twice (once in
  the polling worker, once in each native transport).

### Cons (real)

- **Extra indirection on hot path.** Native ASB scheduled send today is
  one virtual call (`endpoint.DispatchAsync`) → `ScheduleMessageAsync`. With
  unification it'd be `endpoint.ExecuteAsync` → middleware → provider → endpoint
  helper → `ScheduleMessageAsync`. That's 1–2 extra delegate invocations per
  scheduled send. Scheduled sends are not the hot path (they're rare relative
  to immediate sends), so this is largely cosmetic.
- **Native scheduler quirks leak.** ASB's `SequenceNumber` is `long`, can be
  partition-scoped, and is per-namespace — they don't fit neatly into a
  `string token` opaque blob. **However:** the current code already opaque-encodes
  `"asb:{entity}:{seq}"` into a `string`. The leakage exists today; unification
  doesn't make it worse. The token-format-design problem (M4) is orthogonal.
- **"Provider per transport" requires routing logic in the bus.** Either by
  convention (token prefix) or by capability (the dispatch endpoint hands the
  bus its provider). Both are simple; the prefix one is what MassTransit and
  Postgres already do.
- **Confusing for storage-backed transports.** Postgres' provider would have a
  real `PersistAsync` (writes to its scheduled_messages table); ASB's provider
  would have a `PersistAsync` that calls the dispatch endpoint with `ScheduledTime`
  set. The shape is identical but the semantic is "persist to my store" vs
  "send to the broker, who'll persist for me". Documenting this clearly costs
  some prose.
- **Migration surface.** `IScheduledMessageStore` is a public surface area today.
  Renaming or repurposing it is a breaking change.

### Hybrid: "always store, with native fast-path"

A literal "everyone uses the store, native path is just an internal fast-path"
implementation has all the **pros** above, plus:

- The **fast-path is invisible** — the provider for ASB is `[NativeProvider]
  → AzureServiceBusDispatchEndpoint` directly; for Postgres it's
  `[StorageProvider] → ScheduledMessageDispatcher polling loop`. Both look
  identical to the bus.
- The middleware no longer has a "skip if native" branch. It always calls
  `provider.PersistAsync`. Whether that ends up writing to a DB or calling a
  broker SDK is the provider's concern.

That is the recommendation, fleshed out in §4.

---

## 3. Comparable framework approaches

### MassTransit — already does this (and ships it as their public extension point)

`IScheduleMessageProvider` is the per-transport / per-scheduler interface
(`src/MassTransit.Abstractions/Scheduling/IScheduleMessageProvider.cs`):

```csharp
public interface IScheduleMessageProvider
{
    Task<ScheduledMessage<T>> ScheduleSend<T>(Uri destinationAddress, DateTime scheduledTime,
        T message, IPipe<SendContext<T>> pipe, CancellationToken ct) where T : class;
    Task CancelScheduledSend(Guid tokenId, CancellationToken ct);
    Task CancelScheduledSend(Uri destinationAddress, Guid tokenId, CancellationToken ct);
}
```

Implementations include:
- `ServiceBusScheduleMessageProvider` — calls `ISendEndpointProvider` with a
  `ScheduleSendPipe` that injects `ScheduledEnqueueTimeUtc` on the underlying
  `ServiceBusMessage`. Cancellation sends a `CancelScheduledMessage` system message
  to the destination, which a built-in consumer turns into
  `sender.CancelScheduledMessageAsync(seq)`.
- `DelayedScheduleMessageProvider` — RabbitMQ delayed exchange.
- `SqlScheduleMessageProvider` — SQL transport.
- `MessageScheduler` (the public `IMessageScheduler`) — wraps **any**
  `IScheduleMessageProvider`. User code only sees `IMessageScheduler`.

The factory wires the right provider per transport at bus configuration:
```csharp
return new MessageScheduler(new ServiceBusScheduleMessageProvider(bus), bus.Topology);
```

Quartz / Hangfire register a different provider via the
**same `IScheduleMessageProvider` interface** — the abstraction holds across
both broker-native and external-scheduler backends. This is the "uniform
abstraction over native fast-path and storage-backed slow-path" pattern Mocha
should adopt.

### NServiceBus — uniform send-options API, transport selects path

`SendOptions.DelayDeliveryWith(TimeSpan)` and `DoNotDeliverBefore(DateTime)` are
the **only** user-facing API. NServiceBus then either:
- Honors via native delayed delivery on transports that support it (ASB, RabbitMQ,
  ASQ ≥ 7.4), or
- Falls back to the (now-legacy) timeout manager persistence layer for transports
  that don't (MSMQ).

The abstraction is at the **send-options** level, not at a "store" level.
Cancellation is **not generically exposed**. NSBus relies on saga timeouts being
the cancellation mechanism and treats one-shot cancel as a niche case.

For Mocha that's instructive: NServiceBus does *not* have an
`IScheduledMessageStore` equivalent in the public API. The abstraction is purely
"request a delayed send"; the framework picks the path. There's no "PersistAsync
throws" problem because there's nothing user-visible to throw.

### Wolverine — precedence-based, no unified store contract

Wolverine has three modes per the docs:
1. **Native transport scheduling** if the destination supports it (ASB, SQL Server).
2. **Durable envelope storage** if the endpoint is enrolled in the transactional
   outbox.
3. **In-memory** (default fallback, "only for delayed retries — you'll lose
   messages on crash").

The selection happens **inside the endpoint**, not in middleware. User code uses
`messaging.ScheduleAsync(...)` regardless. There's no public `IScheduledMessageStore`;
the storage is part of the durability subsystem (one of `IMessageStore`'s many
roles), and the transport's native path bypasses it without sharing an
abstraction.

This is closest to **Mocha's current dual-path design** — Wolverine doesn't have
the `IScheduledMessageStore`-but-throws problem because it never asked storage
backends to share an interface with broker backends in the first place.

### Brighter — hybrid by design, two distinct interfaces

Brighter has `IAmAMessageScheduler` (for outgoing transport messages) and
`IAmARequestScheduler` (for in-process commands/events). It supports both native
delays (RabbitMQ, ASB) and external schedulers (Quartz.NET, Hangfire, AWS
Scheduler). Cancellation support is documented as varying by backend
(Quartz/Hangfire/InMemory: full; AWS Scheduler/ASB: limited).

Internally Brighter uses sentinel system messages (`FireSchedulerMessage`,
`FireSchedulerRequest`) routed through whichever scheduler the user configured,
similar to MassTransit's `CancelScheduledMessage`. The abstraction is at the
"scheduler" level; transport-native is one implementation among many.

### Summary

| Framework | Single abstraction across native + storage? | Cancellation generic? | Multi-transport in one app? |
| --- | --- | --- | --- |
| MassTransit | **Yes** — `IScheduleMessageProvider` | Yes (via `IMessageScheduler`) | Yes — provider chosen per send-endpoint URI |
| NServiceBus | Partial — at the `SendOptions` level, no store interface | No (saga timeouts only) | Less common; one transport per endpoint typical |
| Wolverine | **No** — three independent paths, selected by precedence | Limited (no public cancel API for native) | Yes, but storage and native are independent |
| Brighter | **Yes** — `IAmAMessageScheduler` over native + Quartz/Hangfire | Varies by backend, exposed | Yes |

**MassTransit and Brighter both vote "uniform abstraction".** Wolverine votes
"precedence" but pays for it with no generic cancel and a more complex internal
selection. NServiceBus votes "API-level abstraction with framework-internal
dispatch", which works only because they sacrifice generic cancellation.

Mocha's current design is closest to Wolverine's — and inherits Wolverine's
trade-offs (no clean cross-transport cancellation, native vs storage are
distinct shapes) without Wolverine's mitigations (Wolverine doesn't try to
share an interface between the two; Mocha does, and the result is
`PersistAsync` throws + multi-store DI collisions).

---

## 4. Recommendation: hybrid, reshape the abstraction

### What to do

1. **Rename `IScheduledMessageStore` → `IScheduledMessageProvider`** (or
   `IScheduledMessageBackend`). The "store" name is misleading for ASB —
   nothing is stored in Mocha-controlled state. MassTransit's
   `IScheduleMessageProvider` is the prior art.

2. **Make every transport register its own provider.** Native transports'
   provider implementations call the dispatch endpoint's send path with
   `envelope.ScheduledTime` set; storage transports' provider implementations
   write to their store and signal the polling worker. Same interface either way.

3. **Replace the `SchedulingTransportFeature.SupportsSchedulingNatively` skip
   branch in `DispatchSchedulingMiddleware.Create` with always-on middleware.**
   The middleware always calls `provider.PersistAsync`. For native transports,
   that call routes back into the dispatch endpoint's normal send path (which
   already handles `envelope.ScheduledTime` correctly today — see
   `AzureServiceBusDispatchEndpoint.cs:99-104`). The "skip the middleware,
   handle scheduling inline in the endpoint" branch goes away.

4. **Introduce token-prefix routing in `DefaultMessageBus.CancelScheduledMessageAsync`.**
   Either via an aggregate `IScheduledMessageProvider` registered as the public
   one, or via a `keyed-services` DI registration. This kills C6 and m6 in one
   move. The aggregate provider is ~30 lines.

5. **Adopt a token-prefix convention** before a third transport lands.
   Suggestion: `"{transport-name}:{transport-specific-payload}"` where
   `transport-name` matches the transport's `Schema` property (e.g. `"azuresb"`,
   `"postgres"`). Documented in `IScheduledMessageProvider`'s XML doc.

6. **Drop `PersistAsync` throws.** With unification, ASB's provider's
   `PersistAsync` is a real method that calls the dispatch endpoint's send path
   with `ScheduledTime` set on the context. No "unreachable" comment, no
   load-bearing capability flag.

7. **The `SkipScheduler` extension stays** — Postgres' polling worker still
   needs it for replay (it's calling the endpoint with an already-due
   `ScheduledTime` and must avoid loop-persisting). But the meaning shifts from
   "skip middleware" to "skip provider routing for this dispatch".

### Why hybrid (not "always polling-store-then-dispatch")

A literal "always persist to durable storage, polling worker dispatches" approach
would force every Mocha user with ASB to also configure a Postgres / SQL Server
schema for scheduled-message storage. That **defeats the entire reason ASB is
attractive for scheduled messages** (broker handles scheduling, no second
infrastructure dependency, sub-millisecond cancellation). The native fast-path
is genuinely valuable; the **abstraction shape**, not the dual-implementation,
is what's broken.

### Why this is worth doing now (not deferring)

- The current code has **3 critical and 4 major design issues** flagged in
  `.work/review/review.md` (M2, M3, C6, M4, m6, M5, M8) that are all symptoms
  of the dual-shape abstraction. Fixing each one independently is more code than
  reshaping the abstraction once.
- This is `pse/adds-azure-serivce-ubs` — adding ASB is **the moment** the
  multi-transport scheduling problem becomes real. Postgres-only or InMemory-only
  apps don't hit it. After ASB ships, every breaking change to
  `IScheduledMessageStore` becomes much more expensive.
- A future Kafka / Redis / NATS transport will face exactly the same fork
  in the road. Establishing the provider pattern now means each new transport
  is a single `IScheduledMessageProvider` implementation, not "native flag +
  inline endpoint code + DI conflict + PersistAsync stub + token prefix
  collision audit".

### Cost estimate

- Rename `IScheduledMessageStore` → `IScheduledMessageProvider`: pure rename,
  ~15 files affected.
- Aggregate provider with prefix routing: ~50 lines + tests.
- ASB provider rewrite: replace the throw with a `PersistAsync` that sets
  `ScheduledTime` on a dispatch context and calls the endpoint, plus the
  existing `CancelAsync`. ~40 lines net (deletes more than adds, since
  `AzureServiceBusDispatchEndpoint.cs:99-104` becomes a single
  unconditional code path).
- Postgres provider: zero net change — `EfCoreScheduledMessageStore` is already
  shaped right.
- Middleware: delete the `SchedulingTransportFeature` skip branch in
  `DispatchSchedulingMiddleware.Create`. Add a single test that scheduled sends
  for native transports still hit the broker without DB persistence.
- Public-API impact: `IScheduledMessageStore` rename is a SemVer break. Mocha
  appears to be pre-1.0 / actively iterating, so this is acceptable; a `[Obsolete]`
  type alias for one release would smooth migration.

---

## 5. Counter-arguments worth taking seriously

**"This is over-engineering. We have one storage backend (Postgres) and one
native (ASB). Just keep the dual-path."**

Reasonable for two transports. The cost grows linearly per transport added,
and we already have `Mocha.Transport.AzureEventHub`, `Mocha.Transport.RabbitMQ`,
and `Mocha.Transport.InMemory` in the tree, plus inevitable Kafka/SQS/etc.
The break-even is around three transports.

**"MassTransit's `CancelScheduledMessage` consumer pattern is overkill —
they need it because cancellation must traverse the broker for transports
without a synchronous cancel. ASB has synchronous cancel."**

Correct. Mocha doesn't need to copy the `CancelScheduledMessage` system-message
machinery. The provider's `CancelAsync` can be synchronous (against the broker
SDK) for ASB and synchronous (against the DB) for Postgres. The point is the
*interface*, not the cancellation transport.

**"`PersistAsync` for ASB is semantically wrong — there's no Mocha-side
persistence. We're lying."**

The interface name is the problem (point 1 above). Rename it. `IScheduledMessageProvider`
or `IScheduledMessageBackend` doesn't promise persistence. MassTransit's
`IScheduleMessageProvider` is the precedent.

**"What about transports with limited cancellation (Wolverine doc cites RabbitMQ
delayed-exchange as having no cancel; Brighter cites ASB-via-AWS-Scheduler as
limited)?"**

The provider returns a `SchedulingResult` with `IsCancellable` already
(see `src/Mocha/src/Mocha/SchedulingResult.cs:23`). A provider that doesn't
support cancel returns `IsCancellable = false` and a no-op `CancelAsync`.
This already works in the current contract.

---

## 6. What this does NOT solve

- The token-format design problems for ASB (namespace collisions, partitioning,
  sessions — M4 in `.work/review/review.md`). Those are payload concerns of the
  ASB provider, not the abstraction shape. Solving them is downstream of this
  recommendation.
- Saga timeout integration. MassTransit and NServiceBus both have richer
  saga-timeout APIs that interact with their schedulers. Mocha doesn't yet,
  and that's out of scope for this question.
- Cross-transport routing of *scheduled-then-replayed* messages. Today Postgres'
  worker dispatches to whatever endpoint the envelope addressed; if that endpoint
  is on ASB, the DB-store-then-broker-send round-trip happens. Unifying the
  abstraction doesn't change that — and in fact it'd be weird to optimize that
  further since the user explicitly chose Postgres-storage scheduling.

---

## Files referenced

Core scheduling contracts:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/IScheduledMessageStore.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/DispatchSchedulingMiddleware.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/SchedulingTransportFeature.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/SchedulingMiddlewareFeature.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/SchedulingDispatchContextExtensions.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/SchedulerCoreServiceCollectionExtensions.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/ISchedulerSignal.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Scheduling/MessageBusSchedulerSignal.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/SchedulingResult.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Features/ScheduledMessageFeature.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Middlewares/DefaultMessageBus.cs` (lines 380-428)

Transport scheduling implementations:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Scheduling/AzureServiceBusScheduledMessageStore.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs` (lines 99-109)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessagingTransport.cs` (line 75)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/MessageBusBuilderExtensions.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.Postgres/PostgresMessagingTransport.cs` (line 117)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.Postgres/PostgresDispatchEndpoint.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.EntityFrameworkCore.Postgres/Scheduling/EfCoreScheduledMessageStore.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.EntityFrameworkCore.Postgres/Scheduling/SchedulingServiceCollectionExtensions.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.EntityFrameworkCore.Postgres/Scheduling/ScheduledMessageDispatcher.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.EntityFrameworkCore.Postgres/Scheduling/ScheduledMessageWorker.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQDispatchEndpoint.cs` (does not handle ScheduledTime)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.InMemory/InMemoryDispatchEndpoint.cs` (does not handle ScheduledTime)

Existing review notes that this recommendation supersedes / consolidates:
- `/Users/pascalsenn/kot/hc2/.work/review/review.md` (M2, M3, C6, m6 are all
  resolved by the unification; M4, M5 are orthogonal payload concerns)
- `/Users/pascalsenn/kot/hc2/.work/review/consistency.md` (items 1, 2, 5)
- `/Users/pascalsenn/kot/hc2/.work/review/devils-advocate.md` (items 4, 5)

External references:
- MassTransit `IScheduleMessageProvider`:
  https://github.com/MassTransit/MassTransit/blob/master/src/MassTransit.Abstractions/Scheduling/IScheduleMessageProvider.cs
- MassTransit `ServiceBusScheduleMessageProvider`:
  https://github.com/MassTransit/MassTransit/blob/master/src/Transports/MassTransit.Azure.ServiceBus.Core/Scheduling/ServiceBusScheduleMessageProvider.cs
- MassTransit scheduling docs: https://masstransit.io/documentation/configuration/scheduling
- NServiceBus delayed delivery: https://docs.particular.net/nservicebus/messaging/delayed-delivery
- Wolverine durable messaging: https://wolverinefx.net/guide/durability/
- Wolverine scheduled messages (Jeremy Miller blog): https://jeremydmiller.com/2023/09/06/scheduled-or-delayed-messages-in-wolverine/
- Brighter scheduler: https://brightercommand.gitbook.io/paramore-brighter-documentation/scheduler/brighterschedulersupport

# Plan: Unify Mocha Scheduling Through `IScheduledMessageStore.PersistAsync`

## 1. Problem

Today the framework has two structurally different scheduling code paths and a flag (`SchedulingTransportFeature.SupportsSchedulingNatively`) that picks between them at pipeline-construction time:

- **Path A — middleware/store:** `DispatchSchedulingMiddleware` intercepts dispatches with `ScheduledTime`, calls `IScheduledMessageStore.PersistAsync(envelope, scheduledTime, …)`, sets `ScheduledMessageFeature.Token`, and short-circuits the pipeline. Used by `EfCoreScheduledMessageStore` (Postgres outbox for *non-Postgres* transports).
- **Path B — endpoint-native:** transports that set `SupportsSchedulingNatively = true` cause the middleware to be removed from the compiled pipeline at construction time. Their dispatch endpoint inspects `envelope.ScheduledTime` and schedules in-place. Used by:
  - `AzureServiceBusDispatchEndpoint` → `ServiceBusSender.ScheduleMessageAsync`
  - `PostgresDispatchEndpoint` → `PostgresMessageStore.SendAsync(..., scheduledTime)` (writes to the same message table with a `scheduled_time` column).

Symptoms of the split:

1. `AzureServiceBusScheduledMessageStore.PersistAsync` is unreachable and throws `InvalidOperationException`. The store exists *only* to host `CancelAsync`. The `IScheduledMessageStore` interface is half-honoured.
2. The cancellation token is produced by the endpoint (`ScheduleMessageAsync` returns a sequence number) but consumed by the store (`CancelAsync(token)`) — two halves of one round-trip live in different files in different layers.
3. Adding scheduling to a new transport requires picking a side: subclass `IScheduledMessageStore` (Path A), or set the flag and wire scheduling into the endpoint (Path B). The framework offers no single answer.
4. `SupportsSchedulingNatively` is a structural lever (factory-time skip) named like a capability flag; it conflates "transport schedules itself" with "framework should not inject the middleware here." Two unrelated concerns share one bool.

The user's directive: collapse to one path. Always run the scheduling middleware for scheduled dispatches; resolve the scheduled-message store for the current transport explicitly; always call that store's `PersistAsync`; let each transport's store decide how to actually persist or schedule.

## 2. Solution: Context-Pass + Store-Resolver Unification

Single rule: **scheduled dispatches go through the scheduling middleware, and the middleware routes to exactly one `IScheduledMessageStore.PersistAsync` selected for the current dispatch transport.**

### 2.1 New `IScheduledMessageStore` shape

```csharp
public interface IScheduledMessageStore
{
    ValueTask<string> PersistAsync(
        IDispatchContext context,
        CancellationToken cancellationToken);

    ValueTask<bool> CancelAsync(
        string token,
        CancellationToken cancellationToken);
}
```

Three deliberate moves vs. today:

- `MessageEnvelope envelope` → reachable as `context.Envelope` (non-null by middleware contract — see §4).
- `DateTimeOffset scheduledTime` → reachable as `context.Envelope.ScheduledTime` (also non-null by middleware contract).
- Adds `IDispatchContext` so the store can reach `Endpoint`, `Transport`, `Services`, `Features` — the things native-scheduling stores need.

The interface remains tiny (two methods). The widening is the parameter type, not the surface.

### 2.2 New scheduled-store resolver

The middleware must not resolve a raw `IScheduledMessageStore` directly. A bus can contain multiple transports and therefore multiple valid scheduling stores. Dispatch and cancellation need different routing keys:

```csharp
internal interface IScheduledMessageStoreResolver
{
    bool TryGetForDispatch(
        IDispatchContext context,
        out IScheduledMessageStore store);

    bool TryGetForCancellation(
        string token,
        out IScheduledMessageStore store);
}
```

Store registrations carry metadata instead of competing for one unkeyed DI slot:

```csharp
internal sealed record ScheduledMessageStoreRegistration(
    Type? TransportType,
    string TokenPrefix,
    Type StoreType,
    bool IsFallback = false);
```

Resolution rules:

1. For dispatch, prefer the registration whose `TransportType` is assignable from `context.Transport.GetType()`.
2. If no transport-specific registration exists, use exactly one fallback registration (`EfCoreScheduledMessageStore` from `UsePostgresScheduling()`).
3. If no registration matches a scheduled dispatch, throw a clear `NotSupportedException`; do not send the message immediately.
4. For cancellation, route by token prefix (`asb:`, `postgres-transport:`, `postgres-scheduler:`). If the prefix is unknown, return `false`.
5. At startup, reject duplicate non-fallback transport registrations and duplicate token prefixes.

Each store type is registered as itself with its intended lifetime (for example scoped for EF). The resolver is scoped and resolves the selected `StoreType` from the current service provider.

### 2.3 Always-on scheduling middleware

`DispatchSchedulingMiddleware` becomes a default dispatch middleware registered after serialization for every bus. It loses both the `SupportsSchedulingNatively` short-circuit and the factory-time "is `IScheduledMessageStore` registered?" guard; those are replaced by resolver lookup at invoke time.

`InvokeAsync`'s contract becomes: when `context.ScheduledTime is not null` and `SchedulingMiddlewareFeature.SkipScheduler is false` and `context.Envelope is not null`, resolve the store for `context.Transport`, call `store.PersistAsync(context, ct)`, write the returned token to `ScheduledMessageFeature.Token`, return without calling `next`. Otherwise pass through.

If the resolver cannot find a store for the current transport, throw a clear scheduling-not-supported exception. This replaces today's accidental "scheduled message is sent immediately" behaviour for unsupported transports.

### 2.4 Per-transport store responsibilities

Every transport that supports scheduling registers a store. The store *is* the seam.

| Registration | Store | Dispatch match | Token prefix | What `PersistAsync` does |
|---|---|---|---|---|
| Azure Service Bus | `AzureServiceBusScheduledMessageStore` | `AzureServiceBusMessagingTransport` | `asb:` | resolve entity path + sender from context, build `ServiceBusMessage`, call `sender.ScheduleMessageAsync`, return `"asb:{path}:{seq}"` |
| Postgres (transport) | **new** `PostgresTransportScheduledMessageStore` | `PostgresMessagingTransport` | `postgres-transport:` | call new `PostgresMessageStore.SendScheduledAsync/PublishScheduledAsync` helpers with `scheduledTime`, return a token containing the inserted transport message id(s) |
| Postgres (outbox via `UsePostgresScheduling`) | `EfCoreScheduledMessageStore` (existing) | fallback | `postgres-scheduler:` | unchanged DB outbox INSERT, return `"postgres-scheduler:{guid}"` |
| In-memory / RabbitMQ without fallback | none | none | none | scheduled dispatch throws `NotSupportedException` |

The fallback store lets `UsePostgresScheduling()` continue to schedule non-Postgres transports. Transport-specific stores always win for their own transport, so a bus may use ASB native scheduling for ASB endpoints and the EF scheduler fallback for another transport in the same process.

### 2.5 Endpoints lose the scheduling branch

Both `AzureServiceBusDispatchEndpoint.DispatchAsync` and `PostgresDispatchEndpoint.DispatchAsync` stop reading `envelope.ScheduledTime`. They become "send-now" terminals. The middleware short-circuits the scheduled path before the endpoint runs, so the endpoint never sees a scheduled envelope.

Code that has to relocate:
- ASB: `CreateMessage(envelope)` extracts to `AzureServiceBusMessageFactory.Create(envelope)`. Entity-path resolution (today inside `DispatchAsync` lines 54–94) extracts to `AzureServiceBusEntityPathResolver.Resolve(endpoint, envelope)`. Both are pure functions; both are called from the store and from the simplified endpoint.
- Postgres: the `messageStore.SendAsync/PublishAsync(scheduledTime: …)` calls move into `PostgresTransportScheduledMessageStore`. The endpoint passes `scheduledTime: null` for its non-scheduled path (trivially: it never sees scheduled messages anymore).

### 2.6 Cancellation routes through the resolver

`DefaultMessageBus.CancelScheduledMessageAsync(token, ct)` resolves `IScheduledMessageStoreResolver`, routes by token prefix, and calls the selected store's `CancelAsync`. ASB still parses `"asb:{path}:{seq}"` and calls `sender.CancelScheduledMessageAsync`. EfCore store keeps `"postgres-scheduler:{guid}"`.

Postgres transport cancellation is a real part of the design, not a placeholder. `PostgresMessageStore` gains schedule-specific methods that return inserted `transport_message_id` values. `PostgresTransportScheduledMessageStore` returns a token such as `"postgres-transport:{id1},{id2}"`; `CancelAsync` parses the ids and deletes matching not-yet-delivered scheduled rows from the transport message table. Publish can produce multiple ids because it fans out to subscribed queues.

## 3. Architecture Diagrams

### Today (pre-change)

```
                       ┌─────────────────────────────────────┐
                       │ DispatchSchedulingMiddleware.Create │
                       │   (pipeline-construction time)      │
                       └───────────────┬─────────────────────┘
                                       │
            ┌──────────────────────────┴──────────────────────────┐
            │                                                     │
   SupportsSchedulingNatively                          SupportsSchedulingNatively
        is FALSE                                            is TRUE
            │                                                     │
            ▼                                                     ▼
  middleware in pipeline                              middleware NOT in pipeline
            │                                                     │
  Serialization → Scheduling → Endpoint              Serialization → Endpoint
            │                                                     │
            ▼                                                     ▼
   store.PersistAsync(envelope, t)             endpoint.DispatchAsync sees ScheduledTime
            │                                  → sender.ScheduleMessageAsync (ASB)
   short-circuits                              → messageStore.SendAsync(..., t) (Postgres)
   does not call endpoint                      → sets ScheduledMessageFeature.Token
```

### After (post-change)

```
                  Scheduling middleware is always in the dispatch pipeline
                                             │
                                             ▼
                           Serialization → Scheduling → Endpoint
                                             │
                               ScheduledTime is set?
                                             │
                       ┌─────────────────────┴─────────────────────┐
                       │                                           │
                     YES                                          NO
                       │                                           │
                       ▼                                           ▼
        resolver.TryGetForDispatch(context)             pass through to endpoint
                       │                                  (send-now terminal)
          ┌────────────┴────────────┐
          │                         │
      store found              no store found
          │                         │
          ▼                         ▼
 token = store.PersistAsync   throw NotSupportedException
 ScheduledMessageFeature.Token = token
 short-circuit; do not call endpoint
```

### ASB scheduled dispatch (after)

```
caller.SchedulePublishAsync(msg, t)
        │
        ▼
 [MessageProperties mw]   extracts SessionId / PartitionKey into context.Headers
        │
        ▼
 [Serialization mw]       writes context.Envelope (body + final headers)
        │
        ▼
 [Scheduling mw]          ScheduledTime is set →
        │                 resolver → AzureServiceBusScheduledMessageStore
        │                 store.PersistAsync(context, ct)
        │                     │
        │                     ▼
        │                 AzureServiceBusScheduledMessageStore
        │                     │
        │                     ├─ endpoint = (AzureServiceBusDispatchEndpoint)context.Endpoint
        │                     ├─ await endpoint.EnsureProvisionedAsync(ct)
        │                     ├─ entityPath = AzureServiceBusEntityPathResolver.Resolve(endpoint, envelope)
        │                     ├─ sender = clientManager.GetSender(entityPath)
        │                     ├─ message = AzureServiceBusMessageFactory.Create(envelope)
        │                     ├─ try sender.ScheduleMessageAsync → seq
        │                     ├─ catch MessagingEntityNotFound → re-provision + retry once
        │                     └─ return $"asb:{entityPath}:{seq}"
        │                     │
        ▼                     │
 set ScheduledMessageFeature.Token from return value
 stop. endpoint NOT called.
```

### Postgres-transport scheduled dispatch (after)

```
caller.SchedulePublishAsync(msg, t)
        │
        ▼
 [Serialization mw]
        │
        ▼
 [Scheduling mw]          resolver → PostgresTransportScheduledMessageStore
                              store.PersistAsync(context, ct)
                              │
                              ▼
                          PostgresTransportScheduledMessageStore
                              │
                              ├─ transport = (PostgresMessagingTransport)context.Transport
                              ├─ scheduledTime = context.Envelope.ScheduledTime!.Value
                              ├─ pick SendAsync vs PublishAsync from endpoint kind
                              ├─ await transport.MessageStore.SendScheduledAsync/PublishScheduledAsync(...)
                              ├─ collect inserted transport_message_id values
                              └─ return $"postgres-transport:{id1},{id2}"
```

## 4. Pipeline Order Invariants

After the change, the order matters for ASB:

```
[user middlewares]
MessageProperties        # writes SessionId/PartitionKey to context.Headers
Serialization            # bakes context.Headers into context.Envelope.Headers
Scheduling               # reads context.Envelope, calls store.PersistAsync
[endpoint terminal]
```

Verified today: ASB transport registers `MessageProperties` with `before: Serialization.Key`. After the change, the default bus middleware set registers `DispatchSchedulingMiddleware` with `after: "Serialization"`, not only when EF scheduling is enabled. This invariant must be locked in by a session-aware integration test (a scheduled message with a `SessionId` extractor must round-trip the session id into the broker's scheduled message) — under today's design this test would pass vacuously because scheduling is endpoint-side; after the change it's load-bearing.

`SchedulingMiddlewareFeature.SkipScheduler = true` keeps its current meaning: "send this message immediately even if `ScheduledTime` is set." After the change this is uniform across transports — including ASB, where it was effectively unreachable before.

## 5. Components: New, Changed, Deleted

### New
- `IScheduledMessageStoreResolver` (internal): selects the store for a scheduled dispatch by `context.Transport` and selects the store for cancellation by token prefix.
- `ScheduledMessageStoreRegistration` (internal): metadata describing a store's transport owner, token prefix, and implementation type. Registered with `TryAddEnumerable`/equivalent so multiple stores can coexist without competing for a single unkeyed `IScheduledMessageStore`.
- `AzureServiceBusMessageFactory` (internal static): pulls `CreateMessage(envelope)` out of `AzureServiceBusDispatchEndpoint` so both the endpoint and the store can build `ServiceBusMessage`.
- `AzureServiceBusEntityPathResolver` (internal static): pulls entity-path resolution out of `AzureServiceBusDispatchEndpoint.DispatchAsync` (lines 54–94 today) so both the endpoint (for the send path) and the store (for the schedule path) reach the same answer the same way.
- `PostgresTransportScheduledMessageStore` (internal): wraps new transport-message-store schedule helpers, returns cancellation tokens containing inserted transport message ids, and cancels by deleting not-yet-delivered scheduled rows.
- Tests: resolver routing tests for dispatch and cancellation; store-level unit tests for both ASB and Postgres-transport stores; an end-to-end "schedule + verify session id flows through" integration test for ASB; unsupported-transport scheduled-dispatch test; startup-validation tests for duplicate token prefixes and duplicate transport registrations.

### Changed
- `IScheduledMessageStore.PersistAsync` signature: `(IDispatchContext, CancellationToken)`. No shim — see §7.
- `AzureServiceBusScheduledMessageStore.PersistAsync` stops throwing; it implements scheduling against the broker. Defensive cast: `if (context.Endpoint is not AzureServiceBusDispatchEndpoint endpoint) throw new InvalidOperationException(...)` — clear error if a misregistration leaks a foreign endpoint into this store.
- `AzureServiceBusDispatchEndpoint.DispatchAsync` shrinks to send-only. Loses the `if (envelope.ScheduledTime is { })` branch. Keeps `EnsureProvisionedAsync` + the `MessagingEntityNotFound` retry around `SendMessageAsync`. `EnsureProvisionedAsync` and `InvalidateProvisioning` become `internal` so the store can call them on the schedule path.
- `EfCoreScheduledMessageStore.PersistAsync` adapts to the new signature: reads `context.Envelope` instead of receiving it as a parameter. Body is otherwise unchanged.
- `PostgresMessageStore` gains schedule-specific send/publish helpers that return inserted `transport_message_id` values, plus a cancellation helper that deletes not-yet-delivered scheduled rows by id.
- `PostgresDispatchEndpoint.DispatchAsync` stops branching on `envelope.ScheduledTime`. The header-write at line 202 stays for scheduled envelopes re-dispatched by the EF scheduler, but the endpoint passes `scheduledTime: null` to send-now store methods.
- `DispatchSchedulingMiddleware.Create()` loses the `SupportsSchedulingNatively` short-circuit and the `IServiceProviderIsService` guard. It is always present in the default dispatch pipeline and only does resolver work on scheduled dispatches.
- `DispatchSchedulingMiddleware.InvokeAsync` calls `resolver.TryGetForDispatch(context, out var store)` and then `store.PersistAsync(context, ct)` (no separate envelope/time params).
- `DefaultMessageBus.CancelScheduledMessageAsync` routes through `IScheduledMessageStoreResolver.TryGetForCancellation` instead of resolving one unkeyed `IScheduledMessageStore`.

### Deleted
- `SchedulingTransportFeature` (entire type).
- `Features.Configure<SchedulingTransportFeature>(...)` registrations in `AzureServiceBusMessagingTransport.OnAfterInitialized` (line 75) and `PostgresMessagingTransport.OnAfterInitialized` (line 117).
- The `Create_Should_ReturnNext_When_TransportHasSchedulingTransportFeature` test in `DispatchSchedulingMiddlewareTests`.
- The "PersistAsync throws InvalidOperationException" test for the ASB store (replaced by a real scheduling test).

## 6. Configuration Conflict Handling

Unkeyed `TryAddScoped<IScheduledMessageStore>` registrations go away for built-in stores. They are replaced by typed store registrations plus `ScheduledMessageStoreRegistration` metadata.

Startup validation checks the metadata and throws before the first dispatch when:

- two non-fallback registrations claim the same transport type;
- two registrations claim the same token prefix;
- more than one fallback registration exists;
- a registration points to a type that does not implement `IScheduledMessageStore`.

`PostgresTransport + UsePostgresScheduling()` is no longer inherently ambiguous. The Postgres transport store owns `PostgresMessagingTransport`; the EF store is a fallback for transports without a native scheduler. If a scheduled dispatch targets a Postgres endpoint, the resolver picks `PostgresTransportScheduledMessageStore`. If a scheduled dispatch targets an in-memory/RabbitMQ/custom endpoint and `UsePostgresScheduling()` is registered, the resolver picks `EfCoreScheduledMessageStore`. If no fallback exists, unsupported transports fail fast.

## 7. Public API: Take the Break

`IScheduledMessageStore.PersistAsync`'s signature change is binary-breaking for any external implementer. We accept the break and do not ship a default-interface-method shim:

- The framework is pre-1.0, scheduling itself is being clarified by this very change, and the migration is mechanical (read `context.Envelope` instead of the `envelope` parameter).
- A DIM shim would carry both signatures forward — exactly the dual-path confusion this work is removing. The "two paths to do one thing" pattern is the antipattern under repair.

Release-note text:

> `IScheduledMessageStore.PersistAsync(MessageEnvelope, DateTimeOffset, CancellationToken)` is now `IScheduledMessageStore.PersistAsync(IDispatchContext, CancellationToken)`. Read the envelope as `context.Envelope` and the scheduled time as `context.Envelope.ScheduledTime` (non-null when the middleware invokes you). `SupportsSchedulingNatively` and `SchedulingTransportFeature` have been removed; transports that schedule natively now register their own scheduled-message store through the scheduling store resolver. Scheduled dispatch to a transport without a native store or configured fallback scheduler now fails fast instead of being sent immediately.

## 8. Trade-offs Accepted

1. **The store now downcasts the endpoint.** ASB's store does `(AzureServiceBusDispatchEndpoint)context.Endpoint` to reach `EnsureProvisionedAsync` and the typed `Queue`/`Topic`. Postgres-transport's store casts `context.Transport`. This is acceptable because store + endpoint live in the same assembly and co-evolve. The defensive cast surfaces any misregistration.
2. **Store unit tests get heavier.** Today's stores test `PersistAsync(envelope, time, ct)`. After the change they need an `IDispatchContext` — for ASB, with `Endpoint`/`Transport`/`Features`/`Envelope` populated. We commit to integration-tier coverage (using the existing `Squadron.AzureCloudServiceBus` fixture or an Azure Service Bus emulator for ASB, and a real Postgres for the transport store) rather than building elaborate context stubs. The test fixture cost is real but bounded.
3. **`MessagingEntityNotFound` retry duplicates between ASB endpoint (send path) and ASB store (schedule path).** We extract the retry into a small shared helper rather than copy six lines twice. The helper takes `(ServiceBusSender, Func<ServiceBusSender, ValueTask>, AzureServiceBusDispatchEndpoint endpoint, AzureServiceBusClientManager, string entityPath, CancellationToken)` and re-resolves the sender on retry.
4. **`PostgresDispatchEndpoint` change ripples.** Removing the scheduling branch from a transport that's not the user's current focus is a real diff. We do it in the same change set (not a follow-up) to avoid leaving a transitional state where Postgres transport keeps the now-removed flag's semantics.
5. **Postgres transport cancellation token shape is less compact for publish.** Publish fans out to multiple queue rows, so the first implementation returns a token containing multiple inserted transport message ids. This avoids a schema migration. If token size becomes a problem, add a schedule-group column/index later.
6. **Performance:** the always-on middleware adds one nullable check + one delegate frame per dispatch. Resolver lookup only happens on scheduled dispatches. On a path dominated by network I/O this is well below measurable. We do not add allocation: the middleware's pooled features are only acquired on the scheduled path.

## 9. Phases

- **Phase 1 — Core surface change.** New `IScheduledMessageStore` signature; new resolver + registration metadata; `DispatchSchedulingMiddleware` updated and moved into the default dispatch pipeline; `DefaultMessageBus.CancelScheduledMessageAsync` routes by token prefix; `SchedulingTransportFeature` deleted; existing tests updated. `EfCoreScheduledMessageStore` adapted to the new signature and registered as the fallback store from `UsePostgresScheduling()`.
- **Phase 2 — ASB store implementation.** Extract `AzureServiceBusMessageFactory` and `AzureServiceBusEntityPathResolver`. Implement `AzureServiceBusScheduledMessageStore.PersistAsync`. Strip the scheduling branch from `AzureServiceBusDispatchEndpoint`. Lift retry into shared helper. Make `EnsureProvisionedAsync`/`InvalidateProvisioning` `internal`.
- **Phase 3 — Postgres transport store.** New `PostgresTransportScheduledMessageStore`. Add `PostgresMessageStore` schedule helpers that return inserted ids and a cancellation helper that deletes not-yet-delivered scheduled rows. Strip scheduling branch from `PostgresDispatchEndpoint`. Register the store metadata automatically from `UsePostgres()`.
- **Phase 4 — Registration validation.** Add startup-time checks for duplicate transport owners, duplicate token prefixes, invalid store types, and multiple fallback stores.
- **Phase 5 — Tests.** Resolver dispatch/cancellation routing tests. Session-aware ASB scheduled-dispatch test (locks pipeline order). Store-level tests for ASB and Postgres-transport stores. Cancellation round-trip tests for ASB, Postgres transport, and EF fallback. Unsupported-transport scheduled-dispatch test. Registration-validation tests.

The phases can be a single PR if reviewers prefer atomicity, or split if Phase 2/3 are sized to land independently. Phase 1 in isolation is unsafe (no working store left for ASB or Postgres-transport), so 1+2+3 ship together at minimum.

## 10. What This Plan Rejects

- **Endpoint-callback approach** (carry a `Func<>` schedule delegate on a feature, populated by a transport middleware, invoked by the store): rejected. It changes the same `IScheduledMessageStore` signature *and* introduces a new feature *and* a new transport middleware. Three concepts for the gain context-pass already delivers with one signature change.
- **Prepared-transport-message approach** (build `ServiceBusMessage` in a middleware and park on a feature; store reads it): rejected. The build-middleware is unjustified — the store can call `AzureServiceBusMessageFactory.Create` itself. Adds a new pooled feature, a new middleware, and an `IAzureServiceBusEndpointProvisioner` indirection without buying anything context-pass doesn't already provide.
- **Keep-flag with Postgres-only fix** (stop setting `SupportsSchedulingNatively=true` on Postgres; keep it on ASB): rejected. It does not deliver the user's stated goal of unification. It also leaves the framework with a flag whose semantics diverge from its name (Postgres transport *does* schedule natively, just at a different layer than ASB). The flag is the artefact of a missing abstraction; removing it is the answer, not sharpening it.

## 11. Open Items for Implementation

1. Verify `PostgresDispatchEndpoint` line 202 (writing `ScheduledTime` to the headers JSON) is not load-bearing for any consumer — if it is, the new store must preserve it; if not, drop it with the rest of the scheduling branch.
2. Decide the exact Postgres transport cancellation SQL predicate: delete only rows whose `scheduled_time` is not null and which have not been locked/delivered; return `true` when at least one row was cancelled.
3. Confirm no callsite assumes scheduled messages are unreachable for `Reply`-kind endpoints. The unification removes whatever (accidental?) gate today's flag-based skip provided.
4. Confirm `DefaultMessageBus` surfaces a thrown `PersistAsync` or unsupported-transport exception to the caller (it should, since it does so today for the EfCore store path).
5. Decide retry-helper shape during Phase 2 — keep it a private static or expose as `internal` for direct unit testing.

---

This plan picks one place (the middleware), one interface (`IScheduledMessageStore`), and one resolver for *all* scheduling, regardless of whether the backend is a broker, an outbox table, or a transport-owned message store. The cost is a public-API break on `IScheduledMessageStore.PersistAsync`, a resolver/registration layer, and one new internal store per native-scheduling transport. The win is removing `SupportsSchedulingNatively`, deleting the dual scheduling code path in two transport endpoints, making multi-transport scheduling deterministic, failing unsupported scheduled dispatches loudly, and giving every future transport one obvious extension point.

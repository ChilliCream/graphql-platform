# `MessagingEntityAlreadyExists` — does the existing entity's configuration match?

## TL;DR

**No.** `Create*Async` on `ServiceBusAdministrationClient` only checks the **name** of the entity, not its configuration. If a same-named entity already exists with *any* property values (matching or not, immutable or mutable), the call throws `ServiceBusException(Reason = MessagingEntityAlreadyExists)`. Catching and ignoring that exception means **the existing entity is silently kept as-is**, and the `CreateQueueOptions`/`CreateTopicOptions`/`CreateSubscriptionOptions` we passed are discarded.

For the Mocha use case (auto-provisioning, multiple instances racing, primarily emulator/dev) this is acceptable for the *race*, but it hides real configuration drift in production. Several of the properties Mocha exposes (`EnablePartitioning`, `RequiresSession`, `RequiresDuplicateDetection`) are **immutable** at the broker — there is no recovery path other than deleting the entity and recreating it.

There is also no `CreateOrUpdate` API on `ServiceBusAdministrationClient` (the Azure messaging SDK). `CreateOrUpdate` exists only in `Azure.ResourceManager.ServiceBus` (the ARM/management plane), which requires a different auth model and is the wrong tool for this job.

## 1. Where the catch lives

There are three identical catch blocks in the topology layer, all in `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/`:

| File | Line | Method |
| --- | --- | --- |
| `AzureServiceBusQueue.cs` | 196 | `ProvisionAsync` (after `CreateQueueAsync`) |
| `AzureServiceBusTopic.cs` | 152 | `ProvisionAsync` (after `CreateTopicAsync`) |
| `AzureServiceBusSubscription.cs` | 174 | `ProvisionAsync` (after `CreateSubscriptionAsync`) |

All three follow the same shape:

```csharp
try
{
    await adminClient.CreateXxxAsync(options, cancellationToken);
}
catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
{
    // Already provisioned by another instance — safe to ignore.
}
catch (Exception) when (AutoProvision is null or true)
{
    // Best-effort provisioning — the entity may already exist or the admin API
    // may be unavailable (e.g. emulator with Docker port mapping).
}
```

A few important details about *what is being passed* in each case:

- **Queue** — `LockDuration`, `MaxDeliveryCount`, `DefaultMessageTimeToLive`, `MaxSizeInMegabytes`, `RequiresSession`, `EnablePartitioning`, `ForwardTo`, `ForwardDeadLetteredMessagesTo`, `DeadLetteringOnMessageExpiration`, `AutoDeleteOnIdle`. Each is conditionally assigned only if the user set it.
- **Topic** — `DefaultMessageTimeToLive`, `MaxSizeInMegabytes`, `EnablePartitioning`, `RequiresDuplicateDetection`, `DuplicateDetectionHistoryTimeWindow`, `AutoDeleteOnIdle`, `SupportOrdering`. Same conditional pattern.
- **Subscription** — `LockDuration`, `MaxDeliveryCount`, `DefaultMessageTimeToLive`, `RequiresSession`, `ForwardTo` (always set), `ForwardDeadLetteredMessagesTo`, `DeadLetteringOnMessageExpiration`, `AutoDeleteOnIdle`. Note: `ForwardTo` is set unconditionally on every call.

The subscription case has a subtle additional risk: the `ForwardTo` we pass is **mutable**, so if a stale subscription exists with a different `ForwardTo`, the message routing is silently broken — the entity exists, our forwarding never gets applied, and the topic continues to route messages to whatever the old subscription was forwarding to.

## 2. What the SDK actually does on `Create*Async`

### `Create*Async` is name-based and rejects duplicates outright

From the official .NET reference for `ServiceBusAdministrationClient.CreateQueueAsync(CreateQueueOptions, CancellationToken)`:

> **Remarks**: Throws if a queue already exists.
>
> **Exceptions**: `MessagingEntityAlreadyExists` — An entity with the same name exists under the same service namespace.

The same is documented for `CreateTopicAsync` and `CreateSubscriptionAsync`. The check is purely on **name** (within the namespace, scoped to the topic for subscriptions). The SDK does not compare any properties, does not echo back any "differences" payload, and does not patch the existing entity. The HTTP-level behaviour is the same: the broker returns `409 Conflict` with `SubCode=40000` and the client surfaces it as `MessagingEntityAlreadyExists`. Our `Create*Options` payload is wasted.

Source citations:
- `https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.servicebusadministrationclient.createqueueasync` (`Throws if a queue already exists. QueueProperties for default values of queue properties.`)
- The same Remarks section is repeated on the topic and subscription overloads.

### There is no `CreateOrUpdate` on `ServiceBusAdministrationClient`

I confirmed this by searching `Azure.Messaging.ServiceBus.Administration` — the only methods are:

- `CreateQueueAsync` / `CreateTopicAsync` / `CreateSubscriptionAsync` / `CreateRuleAsync`
- `UpdateQueueAsync` / `UpdateTopicAsync` / `UpdateSubscriptionAsync` / `UpdateRuleAsync`
- `GetQueueAsync` / `GetTopicAsync` / `GetSubscriptionAsync` / `GetRuleAsync`
- `QueueExistsAsync` / `TopicExistsAsync` / `SubscriptionExistsAsync`
- `DeleteQueueAsync` etc.

The Java/JS SDKs are identical. The only thing called `CreateOrUpdate` lives in `Azure.ResourceManager.ServiceBus` — that is the ARM control plane (`Microsoft.ServiceBus/namespaces/queues` resource), which:

- requires Azure RBAC against the resource group / subscription, not just an SAS / a service connection string;
- is not what the Azure Service Bus emulator implements;
- does not work for connection-string-only auth, which is the common Mocha config path.

So we cannot trivially "switch to `CreateOrUpdate`" — the right idempotent pattern with the messaging SDK is `Get` + (compare) + `Update` + create-on-NotFound, which is more code and an extra round trip.

### `Update*Async` is the SDK's "drift fix" mechanism, but it cannot fix immutable properties

The `Update*Async` methods take the `*Properties` object you got from `Get*Async`, mutate it, and PUT it back. The JS SDK doc spells out the rule:

> **updateQueue**: All queue properties must be set even though only a subset of them are actually updatable. […] The properties that cannot be updated are marked as readonly in the `QueueProperties` interface.
>
> Object representing the properties of the queue and the raw response. **`requiresSession`, `requiresDuplicateDetection`, `enablePartitioning`, and `name` can't be updated after creating the queue.**

The .NET SDK has the same restriction (driven by the broker, not the client). Trying to change one of those four still issues the PUT, and the broker rejects it with `BadRequest 40000 — "The value for RequiresSession property of an existing Queue can't be changed"` (per the published Service Bus Resource Manager exceptions reference). The same doc lists `Sub code=40000. Partitioning can't be changed for Queue/Topic.`

So there is no mechanism — neither in the SDK nor at the broker — to retroactively switch an existing entity's partitioning, sessions, or duplicate detection. The only fix is delete + recreate, which Mocha must absolutely not do automatically because it destroys all queued messages.

## 3. Properties Mocha exposes vs. their mutability

Cross-referencing what Mocha sets in `Create*Options` against the official Service Bus immutability list (Azure Service Bus FAQ — *"What should I know before creating entities?"*: Partitioning, Sessions, Duplicate detection, Express entity), and the per-property notes in the REST `Create Queue` reference:

| Property | Where Mocha sets it | Mutable on existing entity? |
| --- | --- | --- |
| `RequiresSession` | Queue, Subscription | **Immutable** |
| `EnablePartitioning` | Queue, Topic | **Immutable** |
| `RequiresDuplicateDetection` | Topic | **Immutable** |
| `LockDuration` | Queue, Subscription | Mutable (but REST docs say "Settable only at queue creation time" — the .NET/SDK allows updates; the REST page is out of date) |
| `MaxDeliveryCount` | Queue, Subscription | Mutable |
| `DefaultMessageTimeToLive` | Queue, Topic, Subscription | Mutable (REST docs say "immutable after the queue has been created" — again, modern SDK allows updates) |
| `DuplicateDetectionHistoryTimeWindow` | Topic | Mutable |
| `MaxSizeInMegabytes` | Queue, Topic | Mutable (but only in certain directions per tier) |
| `AutoDeleteOnIdle` | Queue, Topic, Subscription | Mutable |
| `DeadLetteringOnMessageExpiration` | Queue, Subscription | Mutable |
| `ForwardTo` | Queue, Subscription | Mutable |
| `ForwardDeadLetteredMessagesTo` | Queue, Subscription | Mutable |
| `SupportOrdering` | Topic | Mutable (but only meaningful for partitioned topics) |

The three immutables (`RequiresSession`, `EnablePartitioning`, `RequiresDuplicateDetection`) are exactly the ones the FAQ warns about. They are also the most "load-bearing" — getting them wrong silently means a receiver may not work at all (e.g. session receiver against a non-session queue) or duplicate detection silently doesn't apply.

## 4. What this means in practice for Mocha

Three failure modes are silently masked today:

1. **Same name, different team, different intent.** Another service in the same namespace created `orders` as a partitioned, session-enabled queue with a 5 min lock. Mocha's app starts up, asks for `orders` with sessions=false, lock=30s. We catch `MessagingEntityAlreadyExists`, proceed, try to create a `ServiceBusProcessor` — and the *receiver* either fails (`Sessions are required for this entity`) or behaves subtly differently than the developer expected. The error surface is delayed and far from the cause.

2. **Drift between Mocha versions / configuration.** A queue was created last week with `MaxDeliveryCount=10`. The deployment now sets `MaxDeliveryCount=3`. We swallow `MessagingEntityAlreadyExists`. The app keeps using the broker's value of 10. Tests in dev (where the queue was freshly created) pass, prod silently keeps the wrong value. This is the classic "infra drift hides a config change" bug.

3. **Subscription `ForwardTo` drift.** The subscription `fwd-<destination>` exists but forwards to the *old* destination queue (e.g. because the destination was renamed). The new subscription create call is rejected; we proceed; the topic continues to route messages to the wrong queue.

For an emulator / single-developer use case, none of these matter. For production-grade auto-provisioning, all of them matter.

## 5. Recommendations

Listed in increasing intrusion. Pick a level explicitly; don't ship "all of them on by default" because that's a behaviour change for users running against the emulator who don't have RBAC for read access.

### R1 (Cheap, recommended unconditionally) — Better diagnostics on the swallow

Update the catch to fetch `Get*Async` for the entity and **log the existing entity's properties at warning level** when it differs from what we tried to create. Do not throw, do not retry. This is a pure observability change. Use `ILogger<TopologyType>` sourced from the bus and log:

```
warning: Service Bus queue 'orders' already exists.
  Requested:  RequiresSession=true, MaxDeliveryCount=3, LockDuration=00:00:30
  Existing:   RequiresSession=false, MaxDeliveryCount=10, LockDuration=00:01:00
```

Even this minimal change pays for itself the first time someone debugs "why isn't my session receiver getting messages".

Caveat: requires `Get*Async`, which requires `Manage` on the namespace just like `Create*Async` does — same auth surface, no escalation. Skip the get if `adminClient` is null (pure emulator / pure receive-only paths) and just log "exists" without the diff.

### R2 (Optional, opt-in) — Strict mode that fails fast on immutable drift

Add a topology-level setting (`AutoProvision = AutoProvisionMode.Strict` / `Lenient` / `Off`, or a separate `FailOnPropertyDrift` flag).

In Strict mode:

- After catching `MessagingEntityAlreadyExists`, call `Get*Async` and compare.
- If any **immutable** property differs (`RequiresSession`, `EnablePartitioning`, `RequiresDuplicateDetection`, `RequiresSession` on subscriptions), throw a clear `InvalidOperationException` describing the conflict and pointing at delete-and-recreate as the only remedy. Do not attempt to delete it — that's user data.
- If only mutable properties differ, log a warning (R1).

This is essentially what NServiceBus and MassTransit do for ASB; it surfaces real bugs without requiring users to switch to ARM-based provisioning.

### R3 (Optional, opt-in, more invasive) — "Managed by Mocha" mode that drift-fixes mutable properties

Add an opt-in `AutoUpdate` (or `AutoProvisionMode.Managed`) flag. When set:

- After catching `MessagingEntityAlreadyExists`, `Get*Async`, mutate the returned `*Properties` to match Mocha's intent for **mutable** properties only, and call `Update*Async`.
- Continue to fail/warn for immutable drift as in R2.

This is genuinely useful but materially changes who "owns" the entity. Default off. Document explicitly. The motivation is the customer who manages their bus topology entirely from Mocha's `MessageBusBuilder` and wants idempotent re-deploys to converge.

There is one significant subtlety: `Update*Async` requires PUT-ing **all** properties, including ones Mocha doesn't model (`EnableBatchedOperations`, `AuthorizationRules`, `UserMetadata`, `MaxMessageSizeInKilobytes`, `Status` etc.). The safe pattern is *fetch -> overwrite only the props we set -> push back* — exactly the Java sample shown in the SDK docs.

### R4 (Skip) — Use `Azure.ResourceManager.ServiceBus` `CreateOrUpdate`

Don't. It requires ARM auth, won't work against the emulator, and makes the `Manage` claim insufficient — you'd need full subscription-level RBAC. Mocha's existing connection-string and TokenCredential paths cover the messaging plane only.

## 6. Concrete suggested change set

Minimum viable:

1. Apply **R1** in all three `ProvisionAsync` methods. Same code shape, factored into a helper since the three branches are isomorphic.
2. Decide whether to ship **R2** in the same release. My recommendation is yes, behind `AutoProvisionMode.Strict` (default `Lenient` to preserve current behaviour). Without R2, R1 is just noise — operators get warnings but no enforcement.
3. Defer **R3** to a follow-up issue. It's a real feature, deserves its own design pass (especially around what to do with `AuthorizationRules` and `Status`).

For the immediate "should we silently swallow?" question on the existing PR:

- **Keep** the swallow — it correctly handles the race.
- **Add** the diff-log (R1) so the swallow is no longer silent.
- **Document** in the XML doc on `AutoProvision` that existing entities are not reconciled, and link to whatever new opt-in mode you ship.

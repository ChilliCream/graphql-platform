# 04 — `AutoDeleteOnIdle` and the topology manager

## TL;DR

`AutoDeleteOnIdle` is a server-side timer that the broker uses to
permanently delete an idle queue / topic / subscription **with all
remaining messages and scheduled messages**. In this transport:

- Minimum 5 minutes is **not validated client-side** before the call to
  `CreateQueueOptions.AutoDeleteOnIdle`. The broker rejects sub-5-minute
  values, but the SDK throws a `ServiceBusException` only at provisioning
  time, which the catch-all swallow at
  `AzureServiceBusQueue.ProvisionAsync` line 200 hides when
  `AutoProvision` is left at its `null` default.
- The cached `_isProvisioned` flag in `AzureServiceBusDispatchEndpoint`
  becomes stale the moment the broker auto-deletes the entity. There is
  no `MessagingEntityNotFound` recovery path — sends fail and the next
  attempt also fails because the flag stays at `1`.
- The `ServiceBusProcessor` running in `AzureServiceBusReceiveEndpoint`
  receives the `MessagingEntityNotFound` failure as a non-transient
  `ProcessErrorAsync` event. It is logged as **error** (not warning),
  and the processor never re-provisions. Practically, the receiver
  stops delivering until the host restarts.
- Subscriptions in this transport set `ForwardTo` on every subscription
  (`AzureServiceBusSubscription.ProvisionAsync` line 133). According to
  the docs, autoforwarding counts as a "receive" on the source, so the
  source subscription is never idle. **However the destination queue
  can still go idle**, and if AutoDeleteOnIdle is set on it, the
  subscription will continually fail forwarding once the queue is
  deleted — and there is no path in this transport that would recreate
  the destination queue.

The current code is safe-by-default because no user defaults set
`AutoDeleteOnIdle`. The risk shows up the first time a user opts in.

## 1 · How AutoDeleteOnIdle works (Microsoft docs)

Source: <https://learn.microsoft.com/azure/service-bus-messaging/message-expiration#temporary-entities>
plus the API reference for
[`CreateQueueOptions.AutoDeleteOnIdle`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.createqueueoptions.autodeleteonidle)
and
[`SubscriptionProperties.AutoDeleteOnIdle`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.subscriptionproperties.autodeleteonidle).

| Fact | Source |
| --- | --- |
| Minimum duration is **5 minutes** | All four ARM/SDK references and the message-expiration page. |
| Default value is `TimeSpan.MaxValue` (i.e. effectively disabled) | `QueueProperties.AutoDeleteOnIdle` and `SubscriptionProperties.AutoDeleteOnIdle` remarks. |
| The interval **resets** when there is "traffic" | "What is Azure Service Bus" / Advanced features. |
| When the timer fires, **the entity is deleted along with every message in it** (active, deferred, scheduled, dead-lettered) | Implied by "the system automatically removes these entities" + scheduled-messages doc; once the queue is gone, the message store is gone. |
| ARM `CanNotDelete` resource locks **do not block** AutoDeleteOnIdle | "Important" callout in the message-expiration doc. |

What counts as "idle":

| Entity | Resets the timer |
| --- | --- |
| Queue | sends, receives, queue updates, scheduled messages, browse / peek |
| Topic | sends, topic updates, scheduled messages, **operations on any of its subscriptions** |
| Subscription | receives, subscription updates, new rule additions, browse / peek |

Critical caveat from the autoforwarding doc
(<https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding#scenarios>):

> "When autoforwarding is set up, the value for `AutoDeleteOnIdle` on
> the **source** entity is automatically set to the maximum value …
> Autoforwarding doesn't make any changes to the destination entity. If
> `AutoDeleteOnIdle` is enabled on destination entity, the entity is
> automatically deleted if it's inactive for the specified idle
> interval. **We recommend that you don't enable AutoDeleteOnIdle on
> the destination entity** because if the destination entity is
> deleted, the source entity will continually see exceptions when
> trying to forward messages."

This applies directly to this transport because every subscription
forwards to the queue (see §3.3).

### Failure surface

`MessagingEntityNotFound` is the failure reason returned by the broker
once the entity is gone. From the
[exceptions page](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest#servicebusexception):

> 6.13 `MessagingEntityNotFound` — Service Bus service can't find a
> Service Bus resource.

It is classified as a **setup/configuration error** and explicitly
**not retriable** by the SDK (deprecated exceptions doc, "Exception
types" table). The processor will keep raising the same error in
`ProcessErrorAsync` and **does not** re-provision.

## 2 · How this transport handles AutoDeleteOnIdle

### 2.1 Topology configuration surface

`AutoDeleteOnIdle` is a `TimeSpan?` on every relevant configuration:

- `AzureServiceBusQueueConfiguration.AutoDeleteOnIdle`
  (`Topology/Configurations/AzureServiceBusQueueConfiguration.cs:27`)
- `AzureServiceBusTopicConfiguration.AutoDeleteOnIdle`
  (`Topology/Configurations/AzureServiceBusTopicConfiguration.cs:47`)
- `AzureServiceBusSubscriptionConfiguration.AutoDeleteOnIdle`
  (`Topology/Configurations/AzureServiceBusSubscriptionConfiguration.cs:63`)

Bus-level defaults expose it through:

- `AzureServiceBusDefaultQueueOptions.AutoDeleteOnIdle`
  (`Configurations/AzureServiceBusDefaultQueueOptions.cs:23`)
- `AzureServiceBusDefaultTopicOptions.AutoDeleteOnIdle`
  (`Configurations/AzureServiceBusDefaultTopicOptions.cs:42`)
- `ApplyTo` uses null-coalescing assignment, so explicit values on a
  resource win over the bus-level default.

There is **no convention or default that sets it implicitly**, including
for reply queues. Reply queues created in
`AzureServiceBusMessagingTransport.CreateEndpointConfiguration(InboundRoute)`
(line 415-426) and the dispatch-side reply endpoint (line 312-321) are
flagged `IsTemporary = true` / `AutoDelete = true`, but neither path
threads `AutoDeleteOnIdle` through to the queue. The
`AzureServiceBusReceiveEndpointTopologyConvention` builds the queue
config (line 41-52) without any `AutoDeleteOnIdle`. So today the value
is only ever set when the user opts in via either the bus default or
the per-resource descriptor.

### 2.2 Provisioning

`AzureServiceBusQueue.ProvisionAsync` (`Topology/AzureServiceBusQueue.cs:124-205`):

```csharp
if (AutoDeleteOnIdle is not null)
{
    options.AutoDeleteOnIdle = AutoDeleteOnIdle.Value;
}
…
try
{
    await adminClient.CreateQueueAsync(options, cancellationToken);
}
catch (ServiceBusException ex) when (ex.Reason == ServiceBusFailureReason.MessagingEntityAlreadyExists)
{
    // Already provisioned by another instance — safe to ignore.
}
catch (Exception) when (AutoProvision is null or true)
{
    // Best-effort provisioning … may already exist or admin API may be unavailable …
}
```

Two observations:

1. The transport **forwards the user-supplied `TimeSpan` verbatim** to
   `CreateQueueOptions`. There is no client-side validation of the
   5-minute minimum. (`Azure.Messaging.ServiceBus`'s
   `CreateQueueOptions.AutoDeleteOnIdle` setter does not validate
   client-side either; the broker rejects bad values with
   `ServiceBusFailureReason.InvalidOperation` / a 400.)
2. The broad `catch (Exception)` filter on `AutoProvision is null or true`
   means a user-induced bad value (`TimeSpan.FromMinutes(2)`) is
   silently swallowed when `AutoProvision` is left at its default
   `null`. The queue is not created; the dispatch endpoint then
   succeeds because Azure Service Bus auto-creates senders against
   non-existent entities only at first send, which then fails with
   `MessagingEntityNotFound` — and the diagnostic trail is gone. The
   identical pattern applies to topics and subscriptions
   (`Topology/AzureServiceBusTopic.cs:148-160`,
   `Topology/AzureServiceBusSubscription.cs:172-182`).

### 2.3 Subscription provisioning forwards by default

`AzureServiceBusSubscription.ProvisionAsync` always sets
`options.ForwardTo` (`Topology/AzureServiceBusSubscription.cs:133`).

Per Microsoft's autoforwarding doc (quoted in §1), autoforwarding
**counts as a receive on the source**, so the broker **forces** the
source subscription's `AutoDeleteOnIdle` back to `MaxValue`. This means:

- Setting `AutoDeleteOnIdle` on a subscription via this transport has
  no real effect — the broker overrides it because every subscription
  is a forwarder.
- The destination queue is the actual deletion target. If the user
  enables AutoDeleteOnIdle on the destination queue (the receive-side
  queue), the broker may delete it, after which **every subscription
  feeding the queue starts continuously failing forwarding**.

### 2.4 Provisioning timing

Bulk provisioning runs once in
`AzureServiceBusMessagingTransport.OnBeforeStartAsync`
(`AzureServiceBusMessagingTransport.cs:84-113`). Per-endpoint
provisioning runs lazily in
`AzureServiceBusDispatchEndpoint.EnsureProvisionedAsync`
(`AzureServiceBusDispatchEndpoint.cs:198-232`):

```csharp
private int _isProvisioned; // 0 = false, 1 = true

private async ValueTask EnsureProvisionedAsync(CancellationToken cancellationToken)
{
    if (Volatile.Read(ref _isProvisioned) == 1) { return; }
    if (Interlocked.CompareExchange(ref _isProvisioned, 1, 0) != 0) { return; }
    try { … await Queue.ProvisionAsync(…); … }
    catch { Volatile.Write(ref _isProvisioned, 0); throw; }
}
```

The flag flips to `1` on the **first successful provision attempt**.
Once it is `1` the dispatch endpoint never re-checks, even if the
underlying entity is later deleted by AutoDeleteOnIdle.

### 2.5 Receive side has no recovery either

`AzureServiceBusReceiveEndpoint.OnStartAsync`
(`AzureServiceBusReceiveEndpoint.cs:66-130`) creates the
`ServiceBusProcessor` once and never recreates it.
`ProcessErrorAsync` only logs:

```csharp
private static bool IsTransientProcessorError(Exception exception)
{
    if (exception is OperationCanceledException) return true;
    if (exception is ServiceBusException sbEx)
    {
        return sbEx.Reason
            is ServiceBusFailureReason.ServiceCommunicationProblem
                or ServiceBusFailureReason.ServiceBusy
                or ServiceBusFailureReason.ServiceTimeout
                or ServiceBusFailureReason.MessageLockLost
                or ServiceBusFailureReason.SessionLockLost;
    }
    return false;
}
```

`ServiceBusFailureReason.MessagingEntityNotFound` is **not** in the
transient set, so the processor logs `LogError` once per error and
keeps trying — the SDK's processor loop will continue to attempt to
open links to the deleted entity and surface the same
`MessagingEntityNotFound` repeatedly in `ProcessErrorAsync`. There is
no path that calls `Queue.ProvisionAsync` again.

## 3 · Specific risk scenarios

### 3.1 User sets a sub-5-minute value

- Symptom: provisioning silently no-ops (broad catch).
- First send raises `MessagingEntityNotFound` → buried in the catch in
  the user's code or surfaces from `SendMessageAsync`.
- Diagnosis is hard because the provisioning exception was swallowed.

### 3.2 Receiver is offline longer than AutoDeleteOnIdle

- Queue has `AutoDeleteOnIdle = 30m`. Receiver process is down for
  31 minutes. No senders post during that window either.
- Broker deletes the queue. Any messages still in it are gone.
- Receiver restarts → `ProvisionAsync` is called from
  `OnBeforeStartAsync`. Because we `CreateQueueAsync`, the queue is
  recreated. The processor starts cleanly.
- **Race: between the receiver and a sender process.** If the sender
  process has been up the entire time, its dispatch endpoint never
  fires `EnsureProvisionedAsync` again because `_isProvisioned == 1`.
  When the queue is recreated by the receiver, the cached
  `ServiceBusSender` in `AzureServiceBusClientManager` will recover
  (the sender holds an AMQP link by entity path; the SDK reopens the
  link transparently when the entity reappears). So this case actually
  works **as long as the receiver runs first**. If only the sender is
  alive when the queue is auto-deleted, sends will keep raising
  `MessagingEntityNotFound` forever.

### 3.3 Destination queue auto-deleted while a forwarding subscription stays alive

- This is the autoforwarding hazard in the docs.
- The subscription stays present (forwarding keeps it active). The
  destination queue is deleted because **its own** AutoDeleteOnIdle
  fired (no consumer / no sends went directly to the queue, only
  forwarded ones, which still count as receives — but if the queue is
  briefly idle on the receive side too, deletion is possible).
- The broker will then put the forwarded messages into the
  subscription's dead-letter queue with reason
  `ForwardingFailed`/`AutoForwardError` (per the autoforwarding doc:
  "the source entity will continually see exceptions when trying to
  forward messages").
- Mocha never recovers because there is no
  `MessagingEntityNotFound`-driven re-provision in the dispatch path
  and no error-driven re-provision in the receive path.

### 3.4 Scheduled messages and DeliverBy

`AzureServiceBusDispatchEndpoint.DispatchAsync` line 99 calls
`sender.ScheduleMessageAsync`. The scheduled message lives inside the
queue/topic; if AutoDeleteOnIdle deletes the entity before the
scheduled enqueue time, the scheduled message is gone with no
notification. The cancellation token (
`AzureServiceBusScheduledMessageStore.CancelAsync`) returns `false`
quietly because the broker reports `MessageNotFound` (line 49) — but in
this case the broker would actually report
`MessagingEntityNotFound`, which **is not handled**, so cancellation
throws.

### 3.5 `_isProvisioned` flag staleness

Confirmed: once flipped to `1`, the flag is only reset inside the
`catch` block on `EnsureProvisionedAsync`. There is **no path** that
flips it back to `0` in response to a runtime failure in
`DispatchAsync` (line 96-108). A `MessagingEntityNotFound` raised by
`SendMessageAsync` propagates out of `DispatchAsync` but the flag
stays at `1`, so the next dispatch jumps straight to send and fails
again.

## 4 · Recommendations

These are listed in order of priority. They all assume we want to
keep AutoDeleteOnIdle as an opt-in feature.

### 4.1 Validate the 5-minute minimum at configuration time (must)

Reject `AutoDeleteOnIdle < TimeSpan.FromMinutes(5)` (and any negative
value) in the descriptor or in `OnInitialize` of `AzureServiceBusQueue`,
`AzureServiceBusTopic`, and `AzureServiceBusSubscription`. Surface a
clear `ArgumentOutOfRangeException` at startup so it cannot be silently
swallowed by the catch-all in `ProvisionAsync`. The broker's minimum is
documented across every ARM and SDK reference.

### 4.2 Re-provision on `MessagingEntityNotFound` from the dispatch path (should)

In `AzureServiceBusDispatchEndpoint.DispatchAsync`, wrap
`SendMessageAsync` / `ScheduleMessageAsync` in a single retry that:

1. Catches `ServiceBusException { Reason: MessagingEntityNotFound }`.
2. Resets `_isProvisioned` to `0`.
3. Calls `EnsureProvisionedAsync` (which will recreate the entity).
4. Recreates the cached sender (drop it from
   `AzureServiceBusClientManager._senders` and let `GetSender`
   recreate it).
5. Retries once.

This mirrors RabbitMQ's "publish channel must reopen on entity-deleted"
pattern. Without it, the only recovery is a process restart.

### 4.3 Re-provision on `MessagingEntityNotFound` from the receive path (should)

In `AzureServiceBusReceiveEndpoint.ProcessErrorAsync`, when
`args.Exception` is
`ServiceBusException { Reason: MessagingEntityNotFound }`:

1. Stop and dispose the existing `_processor`.
2. Call `Queue.ProvisionAsync(asbTransport.ClientManager, …)`.
3. Recreate the processor and call `StartProcessingAsync` again.

The MS docs for `ProcessErrorAsync` warn against managing processor
state directly inside the handler ("requesting to start or stop the
processor may result in a deadlock scenario"). The fix is to schedule
the recovery on a background `Task.Run` rather than awaiting in the
handler.

### 4.4 Don't apply `AutoDeleteOnIdle` to durable destination queues (should)

Per the autoforwarding doc, AutoDeleteOnIdle on the destination of a
forwarder is dangerous. Two options:

- **Strict**: the topology builder rejects `AutoDeleteOnIdle` on a
  queue that has at least one inbound subscription. This is detectable
  in `AzureServiceBusQueue.OnComplete` since subscriptions are added
  via `AddSubscription`.
- **Lenient**: log a warning at provision time and recommend
  `TimeSpan.MaxValue` for any queue with subscribers.

I'd start with the warning to avoid breaking existing deployments.

### 4.5 Stop swallowing exceptions in `ProvisionAsync` (should)

The broad `catch (Exception) when (AutoProvision is null or true)` in
all three `ProvisionAsync` methods is too aggressive. It was added so
the emulator with port mapping doesn't blow up, but it also hides
configuration errors. Tighten it to:

- Always rethrow `ArgumentException` family (validation failures).
- Always rethrow `ServiceBusException` whose `Reason` is
  `InvalidOperation`, `MessagingEntityDisabled`, or `Unauthorized`.
- Keep swallowing only `ServiceBusException` with `ServiceCommunicationProblem` /
  `ServiceTimeout` / `ServiceBusy`, plus `Azure.RequestFailedException`
  for transport-level admin failures.

### 4.6 Special-case reply queues (nice to have)

Reply queues are the classic AutoDeleteOnIdle use case (instance-scoped
queues that should disappear when the instance dies). Today the
transport flags them `IsTemporary` / `AutoDelete = true` but never
sets `AutoDeleteOnIdle`. Setting a sensible default like
`TimeSpan.FromHours(1)` on reply queues would honor the intent. This
also makes the absence of an explicit "auto-delete on disconnect"
mechanism in Azure Service Bus less of a footgun. A `ForwardTo`
relationship into the reply queue is uncommon, so the §4.4 hazard does
not really apply.

## 5 · Code locations

- `src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs`
  (provisioning, line 124-205; broad catch, line 200)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusTopic.cs`
  (provisioning, line 95-161; broad catch, line 156)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusSubscription.cs`
  (always sets `ForwardTo`, line 133; broad catch, line 178)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs`
  (`_isProvisioned` flag, line 198-232; no `MessagingEntityNotFound`
  retry around `SendMessageAsync` / `ScheduleMessageAsync`, line 96-108)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusReceiveEndpoint.cs`
  (`IsTransientProcessorError`, line 145-163; processor never
  recreated)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/Scheduling/AzureServiceBusScheduledMessageStore.cs`
  (only `MessageNotFound` handled, line 49; not
  `MessagingEntityNotFound`)
- `src/Mocha/src/Mocha.Transport.AzureServiceBus/Conventions/AzureServiceBusReceiveEndpointTopologyConvention.cs`
  (reply queues do not get `AutoDeleteOnIdle`, line 41-52)

## 6 · Sources

- <https://learn.microsoft.com/azure/service-bus-messaging/message-expiration#temporary-entities>
- <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding#scenarios>
- <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-troubleshooting-guide#entity-is-no-longer-available>
- <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest#servicebusexception>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.createqueueoptions.autodeleteonidle>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.queueproperties.autodeleteonidle>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.subscriptionproperties.autodeleteonidle>
- <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusprocessor.processerrorasync>

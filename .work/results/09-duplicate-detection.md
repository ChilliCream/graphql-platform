# Azure Service Bus Duplicate Detection — Mocha ASB Transport

## TL;DR

- ASB duplicate detection is a **broker-side** "send guard": when enabled on a queue or topic, a message whose `MessageId` matches one already seen within a configured time window is **silently dropped**. The send still returns success.
- Matching key: `MessageId` only (or `MessageId + PartitionKey` when partitioning is enabled).
- Must be enabled at **create time**; cannot be toggled later. Window can be resized later.
- Defaults: 10 minutes (per portal/ARM); the .NET SDK's `CreateQueueOptions` / `CreateTopicOptions` ship a SDK default of 1 minute. Range: 20 seconds – 7 days.
- Tier: **Standard or Premium only** (Basic rejects the property at creation).
- Mocha already always sets `MessageEnvelope.MessageId` (defaulted to `Guid.NewGuid().ToString()` in `DispatchContext.Initialize`) and the ASB dispatch endpoint already maps `envelope.MessageId` to `ServiceBusMessage.MessageId`. So the data side is already correct.
- Topology side is **half-wired**: `AzureServiceBusTopic` exposes `RequiresDuplicateDetection` and `DuplicateDetectionHistoryTimeWindow`, but `AzureServiceBusQueue` does **not** — that is an asymmetry that should be closed.

---

## 1. How ASB Duplicate Detection Works

### 1.1 What it does

From [Duplicate detection](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection):

> Enabling duplicate detection helps keep track of the application-controlled `MessageId` of all messages sent into a queue or topic during a specified time window. If any new message is sent with `MessageId` that was logged during the time window, the message is reported as accepted (the send operation succeeds), but the newly sent message is instantly ignored and dropped. **No other parts of the message other than the `MessageId` are considered.**

This is a "doubt remover" for the classic at-least-once dilemma:

> If an application fails due to a fatal error immediately after sending a message, and the restarted application instance erroneously believes that the prior message delivery didn't occur, a subsequent send causes the same message to appear in the system twice.

### 1.2 What is matched

| Entity setting | Match key |
|---|---|
| Partitioning **disabled** (default) | `MessageId` only |
| Partitioning **enabled** | `MessageId + PartitionKey` (with sessions, `PartitionKey` must equal `SessionId`) |

Scheduled messages are **included** in the dedup window — sending a non-scheduled then a scheduled with the same `MessageId` (or vice-versa) drops the second one.

### 1.3 Failure mode of a duplicate send

The duplicate send **succeeds** silently (no exception, no signal). The message is dropped server-side. There is no API to retrieve "was this dropped as a duplicate" from `SendMessageAsync` / `ScheduleMessageAsync`. The caller cannot distinguish "you re-sent" from "we accepted a fresh one." This is by design — the feature is meant to make resends safe.

### 1.4 Properties

`RequiresDuplicateDetection` (`bool`):
- Settable **only at entity creation time**.
- ARM error if you try to flip it later: `Sub code=40000. The value for the 'requiresDuplicateDetection' property of an existing Queue (or Topic) can't be changed.`
- Defaults to `false`.

`DuplicateDetectionHistoryTimeWindow` (`TimeSpan`):
- The size of the rolling window of `MessageId`s the broker remembers.
- **SDK API default** (`Azure.Messaging.ServiceBus.Administration.CreateQueueOptions` / `CreateTopicOptions`): 1 minute.
- **Portal / ARM-template default** if `requiresDuplicateDetection: true` is supplied without an explicit window: 10 minutes.
- Range: minimum 20 seconds, maximum 7 days.
- Can be **updated** on an existing entity (only the on/off flag is immutable).

### 1.5 Tier compatibility

From the [Resource Manager exceptions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-resource-manager-exceptions) doc:

> On Azure Service Bus Basic Tier, the below properties can't be set or updated:
> - RequiresDuplicateDetection
> - AutoDeleteOnIdle
> - RequiresSession
> - DefaultMessageTimeToLive
> - DuplicateDetectionHistoryTimeWindow
> - …

So **Basic tier sends will fail at provision time** if we attempt to enable the flag. Standard and Premium support it.

### 1.6 Storage and throughput cost

From [Duplicate detection — Window size](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection):

> Enabling duplicate detection and the size of the window directly impact the queue (and topic) throughput, since all recorded message IDs must be matched against the newly submitted message identifier. Keeping the window small means that fewer message IDs must be retained and matched, and throughput is impacted less. For high throughput entities that require duplicate detection, you should keep the window as small as possible.

There is no separate billable "duplicate-detection storage" line item; the cost is implicit in throughput per messaging unit. Practically: keep the window short (minutes, not days) unless the business retry behavior requires a longer one.

### 1.7 Important caveat: partitioning + batching

> When using **partitioning** and sending **batches** of messages, you should ensure that they don't contain any partition identifying properties. Since deduplication relies on explicitly setting message IDs to determine uniqueness, it isn't recommended to use deduplication and batching together with partitioning.

Mocha's ASB transport currently has no partitioning support (no `PartitionKey` / `SessionId` mapping in `AzureServiceBusDispatchEndpoint.CreateMessage`), so this is moot today, but worth flagging for any future partitioning work.

---

## 2. Mocha's Current Story

### 2.1 Envelope-level concept

`MessageEnvelope.MessageId` (`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs:50`) is the **only** envelope-level identifier used for dedup-style purposes. There is no separate `IdempotencyKey` / `DedupKey` concept on the envelope or in the headers.

### 2.2 How `MessageId` is generated on the send path

`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Middlewares/DispatchContext.cs:239`:

```csharp
MessageId ??= Guid.NewGuid().ToString();
```

Set during `DispatchContext.Initialize`, called from every `IMessageBus` operation (`PublishAsync`, `SendAsync`, `RequestAsync`, `SchedulePublishAsync`, `ScheduleSendAsync`). The `??=` means callers can supply their own `MessageId` upstream (e.g. via the context); otherwise a fresh GUID is generated for each dispatch attempt.

### 2.3 How `MessageId` reaches `ServiceBusMessage`

`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs:111-121`:

```csharp
private static ServiceBusMessage CreateMessage(MessageEnvelope envelope)
{
    var message = new ServiceBusMessage(envelope.Body)
    {
        MessageId = envelope.MessageId,
        CorrelationId = envelope.CorrelationId,
        ContentType = envelope.ContentType,
        Subject = envelope.MessageType,
        ReplyTo = envelope.ResponseAddress
    };
    ...
}
```

So `envelope.MessageId` already maps 1:1 to `ServiceBusMessage.MessageId`. **The send-side mapping needed for ASB duplicate detection already works.**

Inbound: `AzureServiceBusMessageEnvelopeParser.cs:33` reads `message.MessageId` back into the envelope, so receive-side correlation is symmetrical.

### 2.4 Topology (entity-create) story

| Resource | `RequiresDuplicateDetection` | `DuplicateDetectionHistoryTimeWindow` |
|---|---|---|
| `AzureServiceBusTopic` | Yes — exposed via `IAzureServiceBusTopicDescriptor.WithRequiresDuplicateDetection` and applied to `CreateTopicOptions` in `ProvisionAsync` | Yes — exposed via `WithDuplicateDetectionHistoryTimeWindow` |
| `AzureServiceBusQueue` | **No — missing.** Neither configuration nor descriptor nor `ProvisionAsync` references it | **No — missing.** |
| `AzureServiceBusDefaultTopicOptions` | Yes (defaults wiring) | Yes |
| `AzureServiceBusDefaultQueueOptions` | (not searched, but per the queue config and provisioning, the property is not present) | (same) |

Files:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusTopic.cs:45-50,128-136`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs` (no dedup wiring)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Descriptors/AzureServiceBusTopicDescriptor.cs:54-65`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusTopicConfiguration.cs:37-42`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusQueueConfiguration.cs` (no dedup props)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusDefaultTopicOptions.cs:32-37,58-59`

### 2.5 Existing app-level dedup: the Inbox

Mocha already ships a separate **consumer-side** dedup mechanism in `Mocha.Inbox`:
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Inbox/ConsumeInboxMiddleware.cs:50-134` uses claim-before-process with `IMessageInbox.TryClaimAsync(envelope, consumerType, ct)`.
- Keys: `(MessageId, ConsumerType)` — so each consumer type independently dedupes (fan-out friendly).
- The Postgres EF backend (`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.EntityFrameworkCore.Postgres/Inbox/`) provides the storage.

This is **complementary** to ASB's broker-side detection, not a replacement:
- ASB dedup: prevents the same message from entering the queue twice within a short window. Cheap, but window-bounded and transport-specific.
- Inbox dedup: prevents the same message from being **processed** twice. Per-consumer, durable, can span longer than any ASB window, and is portable across transports.

ASB dedup catches send-retry storms cheaply at the broker; the Inbox catches everything else (e.g., redelivery after handler crash, rebalance, longer windows). Both layers are useful and independent.

---

## 3. Recommendations

### 3.1 Should the ASB transport opt-in duplicate detection via topology?

**Default: leave OFF. Make it easy to opt in. Close the queue gap.**

Reasons:
1. ASB charges for it implicitly via throughput, and the dedup window is bounded — so blanket-on is wrong for high-throughput, low-retry workloads.
2. Many Mocha users already run the Inbox middleware, where ASB dedup is largely redundant.
3. Basic tier rejects the property at create time — defaulting it on would break those users.
4. Enabling it requires re-creating the entity (immutable property), so getting the default wrong is expensive to fix.

Concrete actions:
- **Add `RequiresDuplicateDetection` and `DuplicateDetectionHistoryTimeWindow`** to `AzureServiceBusQueueConfiguration`, `AzureServiceBusQueue` (with `ProvisionAsync` mapping into `CreateQueueOptions`), `IAzureServiceBusQueueDescriptor` (with fluent `WithRequiresDuplicateDetection` / `WithDuplicateDetectionHistoryTimeWindow`), and `AzureServiceBusDefaultQueueOptions`. This closes the topic/queue asymmetry. Defaults stay `null` (= use SDK default = off).
- Document the immutability: descriptor XML doc should explicitly say "settable only at entity creation; changing it later requires recreating the entity." The topic version omits this; both should add it.
- Document the tier requirement (Standard or Premium).

### 3.2 Should `envelope.MessageId` always be set as `ServiceBusMessage.MessageId`?

**Yes — already done.** Keep it as-is and harden the contract.

What's already correct:
- `DispatchContext.Initialize` defaults `MessageId` to a GUID, so it's never null at envelope construction.
- `AzureServiceBusDispatchEndpoint.CreateMessage` sets `ServiceBusMessage.MessageId = envelope.MessageId` unconditionally.

What to consider hardening:
- If `envelope.MessageId` is ever `null` (e.g., a future code path constructs an envelope manually and skips serializer middleware), `ServiceBusMessage.MessageId` will be null and the SDK will assign a server-side GUID. That defeats duplicate detection. Worth a defensive `??= Guid.NewGuid().ToString()` in `CreateMessage` (or, better, a `Debug.Assert` and a single-line fallback) so this can never silently degrade.
- Ensure the **scheduled** path (`ScheduleMessageAsync` at line 101) gets the same `MessageId`. It does today because `CreateMessage` is called once for both branches — keep that invariant.

### 3.3 Failure mode: user re-sends a previously-acked message after retry

Two sub-cases:

**Case A: SDK-level retry (network hiccup mid-send).**
The Azure SDK's `ServiceBusSender` already does retries with exponential backoff. If the broker accepted a message but the ack didn't come back, the SDK retries with the **same** `ServiceBusMessage` instance — same `MessageId`. With `RequiresDuplicateDetection` enabled, the broker silently drops the dup. Mocha sees a successful send. **This is the textbook win for ASB dedup and works correctly today** because we set `MessageId` on send.

**Case B: Application-level resend (caller calls `bus.SendAsync(msg)` twice).**
With Mocha's current behavior, each `SendAsync` allocates a fresh `DispatchContext` and assigns a brand-new GUID `MessageId`. ASB dedup will **not** catch this — by design, because the framework cannot tell whether the second call is a retry or a deliberate resend.

Recommendations for case B:
- This is what the **Inbox** is for. Document the boundary clearly: "ASB duplicate detection covers SDK-level send retries within the dedup window. For end-to-end exactly-once processing across application crashes, use the Inbox middleware (per-consumer dedup, no window limit)."
- For users who want application-level send dedup, expose the ability to set `MessageId` deterministically via `SendOptions` / `PublishOptions` (e.g., from a business key like an order ID). This is the explicit pattern in the docs:
  > the `MessageId` can be a composite of the application-level context identifier, such as a purchase order number, and the subject of the message, for example, `12345.2017/payment`.
  Today, Mocha has no public way to seed `MessageId` from `SendOptions` (only `Headers`, scheduling, addresses, expiration). Adding `SendOptions.MessageId` (and same for `PublishOptions`) would let users opt into deterministic dedup — and `DispatchContext.Initialize` already honours an existing value via `??=`.
- The **Outbox** (`Mocha.Outbox`) should preserve `MessageId` across retries from the outbox dispatcher so a stuck outbox row that retries N times always resends the same `MessageId`. That makes the ASB broker-side dedup actually useful for outbox-driven sends. (Worth a follow-up check on `OutboxProcessor` — out of scope for this question, but a natural next stop.)

### 3.4 Documentation deltas to consider (if you write docs for the ASB transport)

- A "Duplicate detection" section that:
  - Distinguishes broker-side dedup vs. inbox-based dedup.
  - States that `MessageId` is already wired and explains how to override it via a (future) `SendOptions.MessageId` for business-key dedup.
  - Lists the operational constraints (immutable flag, 20s–7d window, Standard/Premium only, throughput cost).
  - Recommends keeping the window as short as your retry SLA.

---

## Sources

- [Duplicate detection (overview)](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection)
- [Enable duplicate message detection](https://learn.microsoft.com/azure/service-bus-messaging/enable-duplicate-detection)
- [Service Bus Resource Manager exceptions (immutability + Basic-tier rejection)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-resource-manager-exceptions)
- [Service Bus advanced features](https://learn.microsoft.com/azure/service-bus-messaging/advanced-features-overview)
- [`TopicProperties.RequiresDuplicateDetection`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.topicproperties.requiresduplicatedetection)
- [`TopicProperties.DuplicateDetectionHistoryTimeWindow` (SDK default 1 min, range 20s–7d)](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.topicproperties.duplicatedetectionhistorytimewindow)
- [`QueueProperties.DuplicateDetectionHistoryTimeWindow`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.queueproperties.duplicatedetectionhistorytimewindow)
- [`ServiceBusTopicData.DuplicateDetectionHistoryTimeWindow` (ARM default 10 min)](https://learn.microsoft.com/dotnet/api/azure.resourcemanager.servicebus.servicebustopicdata.duplicatedetectionhistorytimewindow)
- [Message transfers, locks, and settlement (`message-id` for idempotency)](https://learn.microsoft.com/azure/service-bus-messaging/message-transfers-locks-settlement)
- [Service Bus messaging exceptions (.NET)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest)

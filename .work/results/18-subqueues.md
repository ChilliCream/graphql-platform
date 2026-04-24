# Azure Service Bus Subqueues — Research

## TL;DR

A **subqueue** in Azure Service Bus is a system-managed secondary queue that is automatically attached to every queue and topic subscription. There are exactly two subqueues:

1. **Dead-letter queue (DLQ)** — `SubQueue.DeadLetter` / path suffix `/$DeadLetterQueue`. Holds messages the broker or application could not process.
2. **Transfer dead-letter queue (TDLQ)** — `SubQueue.TransferDeadLetter` / path suffix `/$Transfer/$DeadLetterQueue`. Holds messages that failed an internal transfer between entities (auto-forward chain failures, send-via failures).

Subqueues cannot be created, deleted, or managed independently of the parent entity. They are addressed by suffixing the parent path. Receiving from them works through the same SDK primitives as a normal queue, with the only difference being the `SubQueue` enum on `ServiceBusReceiverOptions`.

For Mocha, the **strong recommendation** is: **do not build a built-in DLQ replay tool, typed DLQ receiver, or auto-monitoring endpoints**. Mocha already has the right hook — `UseNativeDeadLetterForwarding()` chains the broker DLQ into the user's normal Mocha `_error` endpoint via `ForwardDeadLetteredMessagesTo`. Beyond that, replay/inspection/monitoring belong to operators using `Service Bus Explorer`, Azure Monitor, and native CLI tools — building our own is a large maintenance surface for negligible incremental value.

The one small ergonomic gap worth fixing: the **TDLQ is not surfaced anywhere** today. We should at least document it and consider exposing `SubQueue.TransferDeadLetter` on whatever DLQ replay docs we publish, so users with auto-forward chains know it exists.

---

## 1. What is a subqueue?

From the official `SubQueue` enum (`Azure.Messaging.ServiceBus`, `SubQueue.cs`):

> "Represents the possible system subqueues that can be received from."

| Field | Value | Description |
| --- | --- | --- |
| `None` | 0 | No subqueue, the queue itself will be received from. |
| `DeadLetter` | 1 | The dead-letter subqueue contains messages that have been dead-lettered. |
| `TransferDeadLetter` | 2 | The transfer dead-letter subqueue contains messages that have been dead-lettered when transfers between chained queues/topics fail. |

From the dead-letter queues overview (`learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues`):

> "Azure Service Bus queues and topic subscriptions provide a secondary subqueue, called a *dead-letter queue* (DLQ). The dead-letter queue doesn't need to be explicitly created and can't be deleted or managed independent of the main entity."

> "From an API and protocol perspective, the DLQ is mostly similar to any other queue, except that messages can only be submitted via the dead-letter operation of the parent entity. In addition, time-to-live isn't observed, and you can't dead-letter a message from a DLQ. The dead-letter queue fully supports normal operations such as peek-lock delivery, receive-and-delete, and transactional operations."

Key invariants:

- **Auto-attached**: every queue and every topic subscription has a DLQ and a TDLQ. No provisioning step required.
- **Unmanageable directly**: cannot be created, deleted, or configured separately. Properties like TTL on the DLQ are ignored.
- **Cannot dead-letter from a DLQ**: messages in the DLQ can be completed/abandoned but not re-dead-lettered.
- **No automatic cleanup**: messages stay in the DLQ until a consumer explicitly receives and completes them. Crucially, this means **the DLQ counts against the parent entity's size quota** — a runaway DLQ can throttle the live queue (`QuotaExceeded` is a documented common failure mode).

---

## 2. The two subqueues in detail

### 2.1 Dead-letter queue (DLQ)

Messages land in the DLQ for one of these reasons (from `service-bus-dead-letter-queues#moving-messages-to-the-dlq`):

| `DeadLetterReason` | Trigger |
| --- | --- |
| `MaxDeliveryCountExceeded` | Delivery attempted more than `MaxDeliveryCount` times (default 10). Cannot be disabled, only tuned. |
| `TTLExpiredException` | Message TTL expired and the entity has `DeadLetteringOnMessageExpiration = true`. |
| `HeaderSizeExceeded` | Message header exceeded the size quota. |
| `Session ID is null` | Message without `SessionId` arrived at a session-enabled entity. |
| `MaxTransferHopCountExceeded` | More than 4 hops in an auto-forward chain (also see TDLQ). |
| _user-defined_ | Application called `DeadLetterMessageAsync(message, reason, description)`. |

The ack middleware in Mocha already supports the application-level path: handlers can grab the raw `ProcessMessageEventArgs` via `IMessageContext.GetAzureServiceBusEventArgs()` and call `DeadLetterMessageAsync(...)` directly. Verified at:

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Features/AzureServiceBusContextExtensions.cs`
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Middlewares/Receive/AzureServiceBusAcknowledgementMiddleware.cs` — the `Complete` step swallows `MessageLockLost` so an already-dead-lettered message doesn't fault the pipeline.
- Tested in `/Users/pascalsenn/kot/hc2/src/Mocha/test/Mocha.Transport.AzureServiceBus.Tests/Behaviors/NativeDeadLetterApiTests.cs`.

### 2.2 Transfer dead-letter queue (TDLQ)

The TDLQ is a separate sibling of the DLQ. From `service-bus-dead-letter-queues#dead-lettering-in-forwardto-or-sendvia-scenarios`:

Messages land in the TDLQ when an **internal Service Bus transfer fails** in either an auto-forward or send-via scenario:

- A message passes through more than 4 chained queues/topics (`MaxTransferHopCountExceeded`).
- The destination queue/topic is disabled or deleted.
- The destination queue/topic exceeds its maximum entity size.

In the **send-via** transactional pattern, if the destination is disabled or oversized, the source's TDLQ catches the message instead of the destination's DLQ. This matters because the live queue and the TDLQ are different subqueues — operators monitoring only DLQ depth will miss transfer-failure incidents.

**Mocha relevance**: Mocha's `UseNativeDeadLetterForwarding()` configures `ForwardDeadLetteredMessagesTo`, which auto-forwards from the **DLQ** of the configured queue. If that forward target is down or full, the resulting messages would land in the TDLQ of the source queue's DLQ — i.e. the auto-forward of the auto-forward fails. Today we have zero visibility into this.

---

## 3. Addressing subqueues

### 3.1 Path suffixes

Direct path syntax (works in any AMQP/HTTP client, including the legacy `Microsoft.ServiceBus` SDK):

```
<queue path>/$DeadLetterQueue
<topic path>/Subscriptions/<subscription path>/$DeadLetterQueue
<queue path>/$Transfer/$DeadLetterQueue
```

### 3.2 Helpers

**Legacy SDK** (`Microsoft.Azure.ServiceBus`, deprecated):

```csharp
EntityNameHelper.FormatDeadLetterPath(queueName)
EntityNameHelper.FormatDeadLetterPath(topicPath, subscriptionName)
EntityNameHelper.FormatTransferDeadLetterPath(queueName)
```

**Modern SDK** (`Azure.Messaging.ServiceBus`, what Mocha uses): there is **no public path-formatting helper**. The supported pattern is to pass `SubQueue` on `ServiceBusReceiverOptions`:

```csharp
await using var client = new ServiceBusClient(connectionString);

// queue DLQ
await using var dlqReceiver = client.CreateReceiver(
    queueName,
    new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

// subscription DLQ
await using var subDlqReceiver = client.CreateReceiver(
    topicName,
    subscriptionName,
    new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter });

// transfer DLQ
await using var tdlqReceiver = client.CreateReceiver(
    queueName,
    new ServiceBusReceiverOptions { SubQueue = SubQueue.TransferDeadLetter });
```

The SDK rewrites the entity path internally to the appropriate `$DeadLetterQueue` or `$Transfer/$DeadLetterQueue` suffix. Mocha's existing test helper does exactly this at `NativeDeadLetterApiTests.cs:131-137`.

---

## 4. Reading from a subqueue — constraints

The DLQ "fully supports normal operations such as peek-lock delivery, receive-and-delete, and transactional operations." Consequences:

- **Same processor model is supported.** `ServiceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions { SubQueue = SubQueue.DeadLetter, ... })` works just like a regular processor — same concurrency, prefetch, lock renewal.
- **You cannot dead-letter from a DLQ.** `DeadLetterMessageAsync` on a DLQ-scoped receiver throws. You can only `Complete`, `Abandon`, or `Defer`.
- **TTL is not observed in the DLQ.** Messages stay until completed.
- **Lock semantics still apply.** Peek-lock with delivery counts still works. If your replay handler crashes 10 times, the message stays locked until the lock expires; it will not be transferred anywhere because the DLQ doesn't have a DLQ-of-its-own.
- **Auto-forward out of a DLQ is supported** (this is exactly what `ForwardDeadLetteredMessagesTo` does — chains the DLQ into a destination entity).
- **`DeadLetterSource` property** on `ServiceBusReceivedMessage` tells a downstream receiver which entity originally dead-lettered the message — useful when DLQ contents have been auto-forwarded somewhere else (e.g. our `_error` queue).

---

## 5. Monitoring DLQ depth

Two layers, with very different cost/value profiles:

### 5.1 Azure Monitor metrics (recommended)

The platform exposes a first-class metric:

- `DeadletteredMessages` — point-in-time count of dead-lettered messages in a queue or topic, dimensioned by `EntityName`. PT1M time grain. Documented in `monitor-service-bus-reference#metrics`.

This is the **right** way to monitor DLQ depth in production. It feeds Azure Monitor alerts, dashboards, autoscaling, and Grafana. It costs nothing to query and doesn't load the broker.

The Azure Well-Architected guide explicitly recommends:
> "Set up Azure Monitor alerts for critical Service Bus reliability metrics including dead letter message count thresholds."

### 5.2 Runtime properties (expensive, use sparingly)

The admin client surfaces `MessageCountDetails`:

- `ActiveMessageCount`
- `DeadLetterMessageCount`
- `ScheduledMessageCount`
- `TransferMessageCount`
- `TransferDeadLetterMessageCount`

via `ServiceBusAdministrationClient.GetQueueRuntimePropertiesAsync(queueName)` / `GetSubscriptionRuntimePropertiesAsync(topicName, subscriptionName)`.

The docs are explicit (`message-counters`):
> "If an application wants to scale resources based on the length of the queue, it should do so with a measured pace. The acquisition of the message counters is an expensive operation inside the message broker, and executing it frequently directly and adversely impacts the entity performance."

i.e. polling this from inside the application on every message, or every minute, is an anti-pattern. The Azure Monitor metric is the supported channel.

### 5.3 Topic-level note

Topic-level DLQ count is meaningless — a topic forwards to subscriptions in milliseconds, so it doesn't itself hold messages. DLQ counts only exist on the **subscription** (or queue). This is relevant for any UI we'd consider: per-subscription, never per-topic.

---

## 6. Replaying messages from the DLQ

The official guidance (`service-bus-dead-letter-queues#sending-dead-lettered-messages-to-be-reprocessed`) is to **resubmit** the message — receive it from the DLQ, send a fresh copy back to the source queue/topic, complete the DLQ message in the same transaction. There is no native "move back" primitive.

The docs explicitly point to **third-party tools** for this:

1. **Azure Service Bus Explorer** (in the Azure portal) — manual move/resubmit, available regardless of SDK.
2. **ServicePulse** for NServiceBus and MassTransit — centralized error dashboard with grouping, filtering, and individual or batch retry.

That's the bar. Anything Mocha builds in this space is competing with first-party Azure tooling and mature commercial products.

---

## 7. Mocha — what's already in place

Already shipped on `pse/adds-azure-serivce-ubs`:

| Capability | Surface | Files |
| --- | --- | --- |
| Application-level dead-lettering from a handler | `context.GetAzureServiceBusEventArgs().DeadLetterMessageAsync(...)` | `AzureServiceBusContextExtensions.cs`, `NativeDeadLetterApiTests.cs` |
| Idempotent settlement when handler already dead-lettered | `AzureServiceBusAcknowledgementMiddleware` swallows `MessageLockLost` on `Complete` | `AzureServiceBusAcknowledgementMiddleware.cs:52-56` |
| Native broker DLQ → Mocha `_error` queue forwarding | `.UseNativeDeadLetterForwarding()` on a receive endpoint | `AzureServiceBusReceiveEndpointConfiguration.cs:27`, `AzureServiceBusReceiveEndpointTopologyConvention.cs:34-65`, `NativeDeadLetterForwardingTests.cs` |
| Queue topology knobs | `MaxDeliveryCount`, `DefaultMessageTimeToLive`, `DeadLetteringOnMessageExpiration`, `ForwardDeadLetteredMessagesTo`, `ForwardTo` | `AzureServiceBusQueueConfiguration.cs`, `AzureServiceBusQueueDescriptor.cs`, `AzureServiceBusQueue.cs` |
| Cross-transport "skipped/error endpoint" pattern | Generic `ReceiveDeadLetterMiddleware` re-dispatches to `Skipped` endpoint when the consumer didn't claim the message | `ReceiveDeadLetterMiddleware.cs` |

Notably: **subscriptions on Service Bus topics also have a DLQ.** Mocha's subscription topology (`AzureServiceBusSubscription`, `AzureServiceBusSubscriptionConfiguration`) supports the same `ForwardDeadLetteredMessagesTo` knob — the test coverage focuses on queues, but the pattern extends.

The TDLQ is **not surfaced anywhere** in the codebase. There is zero code or test mention of `TransferDeadLetter` / `SubQueue.TransferDeadLetter`.

---

## 8. Recommendations

### 8.1 DLQ replay tool — **No, do not build**

Reasons:

- Service Bus Explorer in the Azure portal already does this for free, with batch operations, filtering, and a UI.
- ServicePulse (commercial) is the established product for teams that want a richer dashboard.
- A built-in tool would carry a large surface area: UI vs. CLI, identity flows, payload editing, batch retry semantics, idempotency, ordering. None of this is core to a messaging framework.
- Once you build it, you own the support burden — questions about replay semantics, message mutation, lost messages on partial replay, etc.
- The framework's existing `UseNativeDeadLetterForwarding()` already gives users a Mocha-native handler for DLQ contents (the `_error` endpoint). That's the right primitive: the user writes a normal `IConsumer<T>` for the `_error` queue and decides what "replay" means in their domain.

### 8.2 Typed DLQ receiver — **No, do not add as a first-class transport surface**

Reasons:

- The documented and supported pattern is `UseNativeDeadLetterForwarding()` → consume the `_error` queue with a regular Mocha endpoint. That gives you a typed, fully-instrumented Mocha consumer over DLQ contents, with retries, observability, and existing middleware all working unchanged. This is **strictly better** than scoping a receive endpoint to `SubQueue.DeadLetter` directly (which would bypass forwarding and force every operator to write bespoke DLQ-receiver code).
- The minority case where forwarding is undesirable (e.g. compliance scenario where DLQ messages must be inspected in place) is well-served by the raw SDK — three lines of `ServiceBusClient` + `CreateReceiver(name, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter })`. We don't need to wrap that.
- Adding a `t.Endpoint("foo").Queue("foo").FromSubQueue(SubQueue.DeadLetter)` API multiplies configuration combinations (settle modes? auto-forward?) and weakens the "one obvious way" of `UseNativeDeadLetterForwarding`.

The one **doc-only** addition worth making: in the README/docs for `UseNativeDeadLetterForwarding`, mention that for direct DLQ inspection without forwarding, users should construct a `ServiceBusClient` with `SubQueue = SubQueue.DeadLetter`. Three sentences.

### 8.3 Surface DLQ depth metrics — **No, do not poll; defer to Azure Monitor**

Reasons:

- The Azure Monitor `DeadletteredMessages` metric exists, is free, is the recommended channel, and is what Azure's own well-architected guide tells you to alert on.
- Polling `GetQueueRuntimePropertiesAsync` from inside the application is explicitly called out in the docs as expensive and as something to do "with a measured pace." Building this into the framework would make it easy for a user to ship a 1-second poll loop that throttles their namespace.
- If we ever expose DLQ depth via OpenTelemetry, the right shape is **emit the metric from a separate, opt-in `IHostedService`** (e.g. `AddAzureServiceBusDlqDepthMetric()`), not from the receive pipeline. And even that should default to a 30s+ interval.
- Critical-time and queue depth metrics are best emitted via OpenTelemetry only when the user opts in and accepts the broker cost. NServiceBus does this — that's a precedent, not a mandate.

If this is wanted later, propose it as a separate feature with an explicit `TimeSpan pollInterval` and clear docs about broker cost.

### 8.4 Auto-create DLQ-monitoring endpoints — **No, this is the wrong layer**

Reasons:

- "Auto-create a Mocha consumer for the DLQ of every endpoint" is just `UseNativeDeadLetterForwarding()` on every endpoint. That already exists; we don't need a parallel mechanism.
- Forcing every endpoint to spin up a hidden DLQ monitor would create resource and concurrency cost the user didn't ask for, and would muddy diagnostics ("why is my receive count doubled?").
- The right defaults question is whether `UseNativeDeadLetterForwarding()` should be **on by default**. Arguments in favor: it Just Works for users who want a single drain queue per endpoint. Arguments against: it creates a Mocha-named `{queue}_error` entity on the broker the user never asked for, which surprises operators who manage namespaces by hand. Status quo (opt-in) is the safer default until there's user feedback. Worth revisiting if we get real-world signal.

### 8.5 What is worth doing

Small, high-value items that fall out of this research:

1. **Document the TDLQ.** Add a docs/comment paragraph next to `UseNativeDeadLetterForwarding` and `ForwardDeadLetteredMessagesTo` explaining that the broker also maintains a `TransferDeadLetter` subqueue and that, in auto-forward chains, transfer failures land there — not in the regular DLQ. Operators should monitor both via Azure Monitor.
2. **Document the DLQ replay pattern.** Three-line snippet showing `ServiceBusClient.CreateReceiver(name, new ServiceBusReceiverOptions { SubQueue = SubQueue.DeadLetter })` in Mocha docs, cross-linked to the official tooling (Service Bus Explorer, ServicePulse).
3. **Document the Azure Monitor `DeadletteredMessages` metric** as the supported way to watch DLQ depth, with a copy-pasteable Azure CLI snippet for an alert.
4. **Add a `DeadLetterSource` note.** When users build an `_error` consumer fed by `UseNativeDeadLetterForwarding`, the `ServiceBusReceivedMessage.DeadLetterSource` property tells them the original entity. Worth surfacing through `IAzureServiceBusMessageContext` (or just naming it in the docs) so error consumers can distinguish dead-letters from many forwarded queues.

None of these are code beyond #4 (which is a one-liner if we choose to expose it explicitly), and all of them are pure value adds.

---

## 9. Key references

- Subqueue overview: `https://learn.microsoft.com/azure/service-bus-messaging/service-bus-dead-letter-queues`
- `SubQueue` enum: `https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.subqueue`
- `DeadLetterMessageAsync`: `https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusreceiver.deadlettermessageasync`
- Auto-forwarding & TDLQ: `https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding`
- Azure Monitor reference (`DeadletteredMessages`): `https://learn.microsoft.com/azure/service-bus-messaging/monitor-service-bus-reference`
- Message counters (admin client cost warning): `https://learn.microsoft.com/azure/service-bus-messaging/message-counters`
- `DeadLetterSource` property: `https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusreceivedmessage.deadlettersource`
- Mocha existing DLQ surface:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusReceiveEndpointConfiguration.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Conventions/AzureServiceBusReceiveEndpointTopologyConvention.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusQueueConfiguration.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Middlewares/Receive/AzureServiceBusAcknowledgementMiddleware.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Features/AzureServiceBusContextExtensions.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/test/Mocha.Transport.AzureServiceBus.Tests/Behaviors/NativeDeadLetterApiTests.cs`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/test/Mocha.Transport.AzureServiceBus.Tests/Behaviors/NativeDeadLetterForwardingTests.cs`

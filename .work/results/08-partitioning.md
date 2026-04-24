# Partitioning in Azure Service Bus

## TL;DR

ASB partitioning shards an entity across **multiple message brokers and messaging stores**. It is set **only at create time** (immutable) — Standard always gets 16 partitions per entity, Premium has it at the **namespace** level with a configurable count. Partition placement is driven by `SessionId` > `PartitionKey` > `MessageId` (when duplicate detection is enabled), otherwise round-robin. `ViaPartitionKey` (.NET name: `TransactionPartitionKey`) is only relevant for cross-entity transactions via a transfer queue. Mocha already exposes `WithEnablePartitioning` on queue/topic topology, but **never sets `PartitionKey` on outgoing messages and has no API for the user to derive one** — that is the gap. Recommendation: keep `EnablePartitioning` opt-in (default off), add a per-message-type `UseAzureServiceBusPartitionKey<T>(...)` extractor mirroring the existing RabbitMQ `UseRabbitMQRoutingKey` design, and have a small dispatch middleware copy the extracted key onto `ServiceBusMessage.PartitionKey`. Forward `MessageEnvelope.MessageId` to `ServiceBusMessage.MessageId` as today (already done) so duplicate detection works correctly.

## How partitioning is implemented

From [Partitioned queues and topics](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning):

> Service Bus uses multiple message brokers to process messages and multiple messaging stores to store messages. A conventional queue or topic is handled by a single message broker and one messaging store. Service Bus partitions enable queues and topics, or messaging entities, to be partitioned across multiple message brokers and messaging stores.

- Each partition is its own broker + its own message store ("fragment" in the architecture docs).
- A partition outage degrades capacity for that fragment but not the whole entity.
- The broker assigns partitions; the **partition ID is not exposed to the client**, unlike Event Hubs.
- On receive, Service Bus queries all partitions and returns the first message it gets — the SDK consumer is unaware that partitioning is in play.
- `Peek` returns the oldest message **per partition**, not globally; sequence numbers are per-partition.
- "There's no extra cost when sending a message to, or receiving a message from, a partitioned queue or topic" (per Microsoft docs).

## `EnablePartitioning` — when can it be set?

From [Enable partitioning in Basic / Standard](https://learn.microsoft.com/azure/service-bus-messaging/enable-partitions-basic-standard) and [Enable partitioning for Premium](https://learn.microsoft.com/azure/service-bus-messaging/enable-partitions-premium):

- **Set at create time only.** Cannot be changed on an existing namespace, queue, or topic. The error you get back is *Bad Request, sub code 40000: "Partitioning can't be changed for Queue/Topic"* ([Resource Manager exceptions](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-resource-manager-exceptions)).
- **Standard / Basic**: per-entity flag (`CreateQueueOptions.EnablePartitioning` / `CreateTopicOptions.EnablePartitioning`). When enabled, Service Bus **always creates exactly 16 partitions** of the entity. The namespace can mix partitioned and non-partitioned entities. Quota: max 100 partitioned entities per namespace.
- **Premium**: namespace-level flag set when the namespace is created. **All** queues and topics in that namespace are partitioned, and you choose the partition count up front. Cannot mix partitioned and non-partitioned entities in the same Premium namespace. The number of messaging units must be a multiple of the partition count and is split evenly across partitions.
- A flag literally named `EnablePartitioning` exists on `Azure.Messaging.ServiceBus.Administration.CreateQueueOptions` and `CreateTopicOptions`; default is `false`.

Other constraints:
- Partitioned entities cannot use **AutoDeleteOnIdle**.
- Partitioned entities **don't** support sending messages from different sessions in a single transaction.
- Partitioned namespaces (Premium) don't support JMS or migration from Standard.

## How `PartitionKey` routes messages

From [Use of partition keys](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning#use-of-partition-keys):

The broker resolves which fragment to write to by checking message properties **in this priority order**:

1. **`SessionId`** — if set, used as the partition key. All messages in a session land on the same broker (this is what makes session ordering work).
2. **`PartitionKey`** — if `SessionId` is not set, this property is used. If both are set they **must be identical** or the broker rejects the message with `InvalidOperationException`.
3. **`MessageId`** — only when **duplicate detection is enabled** on the entity and neither `SessionId` nor `PartitionKey` is set. Same `MessageId` => same fragment, which is what allows the dedup window to work correctly across partitions. Without duplicate detection, `MessageId` is **not** considered for partition selection.
4. None of the above — broker assigns round-robin across partitions; if a partition is unhealthy it picks another (availability over consistency).

Hash function over the key chooses the partition. The partition itself is not pickable; max length is **128 characters**. A key "pins" the message to a specific partition — if that partition's store is unavailable, the send **fails** instead of falling back, so the [official guidance](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning#not-using-a-partition-key) is *"don't supply a partition key unless it's required."*

## `ViaPartitionKey` / `TransactionPartitionKey`

The .NET SDK property is named [`ServiceBusMessage.TransactionPartitionKey`](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.transactionpartitionkey) (older docs and the AMQP wire annotation `x-opt-via-partition-key` use the term "via-partition-key"). It is **only used during cross-entity transactions** ("send via" — see [Transfers and Send Via](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-transactions#transfers-and-send-via)). When you receive from one entity and send to another inside the same transaction scope through a transfer queue, this key selects the **transfer queue's** partition so the transactional bundle stays atomic on a single broker.

For a normal send to a partitioned entity it is functionally equivalent to `PartitionKey`. **Mocha does not currently use cross-entity transactions and has no transfer queue concept**, so `TransactionPartitionKey` is not relevant today. (If/when we add `TransactionScope`-style cross-entity sends, this becomes mandatory.)

## Throughput and latency implications

**Pros (when enabled correctly):**
- Removes the single-broker throughput ceiling.
- Survives a single-fragment outage.
- For Premium, multiple lower-MU partitions outperform a single higher-MU partition ([Performance best practices](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-performance-improvements#partitioned-namespaces)).

**Cons / footguns:**
- **Low-volume entities perform worse**. Service Bus has to fan out receive requests across partitions; if all messages happen to be locked or cached in another front-end, the receive returns null. The official doc literally says *"don't use partitioning in these scenarios. Delete any existing partitioned entities and recreate them with partitioning disabled to improve performance."*
- **No global ordering**. Per-partition only. `Peek` doesn't return the oldest message globally.
- **Hot partitions hurt availability**. If a key value sees disproportionate traffic, that fragment can throttle while others sit idle.
- **Batching restriction**. All messages in a single `ServiceBusMessageBatch` (or single `SendMessagesAsync` call) **must share the same `PartitionKey`** (and `SessionId`). Different keys => `InvalidOperationException` from the SDK / broker. Source: [Troubleshooting guide — Can't send a batch with multiple partition keys](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-troubleshooting-guide#troubleshoot-sender-issues).
- **Partition pinning trades availability for consistency**. With a key, send fails if that partition is down; without a key, Service Bus will pick another partition.

## Tier compatibility

| | Basic | Standard | Premium |
| --- | --- | --- | --- |
| Where the flag lives | Per entity | Per entity | Per **namespace** |
| Partition count | 16 (fixed) | 16 (fixed) | Choose at namespace create (`1, 2, 4, …`) |
| Mix partitioned + non-partitioned in same namespace | n/a (no topics in Basic) | Yes | **No** — all entities partitioned or none |
| Mutable later | No | No | No |
| AMQP support | Yes (modern SDK) | Yes (modern SDK) | Yes |
| JMS | n/a | JMS 1.1 only | JMS 2.0; **not on partitioned namespaces** |

Note on Premium being "by default partitioned": this is **only true if you opt in at namespace creation**. Default for a new Premium namespace is non-partitioned. The phrasing in the question slightly overstates it.

## Deprecated behavior in Standard tier

There used to be a different partitioning implementation backed by Azure Storage (the legacy "WindowsAzure.ServiceBus" SDK era). The current Service Bus partitioning (16 brokers per entity) is the only supported model — there is no separate "Storage-backed" toggle in modern docs.

What **is** being retired (30 September 2026) is the older SDK family that used different APIs: `WindowsAzure.ServiceBus`, `Microsoft.Azure.ServiceBus`, and the SBMP wire protocol ([retirement notice](https://azure.microsoft.com/updates/retirement-notice-update-your-azure-service-bus-sdk-libraries-by-30-september-2026/)). Mocha already targets the modern `Azure.Messaging.ServiceBus` SDK, so this is not a Mocha concern. There is no separate "Storage-backed partitioning" deprecation to worry about — the term still occasionally appears in third-party blog posts but is not in the current Microsoft documentation.

## Interaction with sessions

`SessionId` and `PartitionKey` are unified at the broker:

- A message with `SessionId` set is automatically routed to the partition selected by hashing the session ID. Setting `PartitionKey` separately is allowed only if it equals `SessionId`; mismatched values get rejected.
- All messages in a session land on the **same broker**, which is what makes `RequiresSession` work in combination with partitioning — session state and ordering are guaranteed per-session even on a partitioned entity.
- Session-aware entities can therefore be partitioned without losing the per-session FIFO guarantee, only losing global FIFO (which sessions don't promise anyway).

## Interaction with duplicate detection

From [Duplicate detection — How it works](https://learn.microsoft.com/azure/service-bus-messaging/duplicate-detection#how-it-works):

> When **partitioning** is **enabled**, `MessageId+PartitionKey` is used to determine uniqueness. When sessions are enabled, partition key and session ID must be the same.
>
> When **partitioning** is **disabled** (default), only `MessageId` is used to determine uniqueness.

Implications:

- On a partitioned entity, the dedup window is **per-partition**. Two messages with the same `MessageId` that happen to land on different partitions (because they have different `PartitionKey`s) are **not** detected as duplicates.
- If neither `SessionId` nor `PartitionKey` is set on a partitioned, dedup-enabled entity, then `MessageId` itself becomes the partition key — guaranteeing duplicate copies hash to the same fragment, which is the whole reason the rule exists.
- Microsoft explicitly advises against combining batching + partitioning + duplicate detection because batches must share a partition key but you also want each message to be deduped individually, which conflicts.

Mocha already sets `ServiceBusMessage.MessageId` from `MessageEnvelope.MessageId` (`AzureServiceBusDispatchEndpoint.cs:116`), so dedup will function for the simple case (no explicit `PartitionKey`) provided the message envelope has a stable `MessageId`. The framework already generates one in the dispatch pipeline.

## What Mocha currently does

`grep -n EnablePartitioning|PartitionKey` across `src/Mocha/src/Mocha.Transport.AzureServiceBus`:

- **Topology**: `EnablePartitioning` is fully wired through configuration → topology → admin client:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusQueueConfiguration.cs:57` — `bool? EnablePartitioning`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusTopicConfiguration.cs:32` — same
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs:65` and `:172-175` — copied into `CreateQueueOptions.EnablePartitioning`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusTopic.cs:40` and `:123-125` — same for topic
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Descriptors/IAzureServiceBusQueueDescriptor.cs:65` — `WithEnablePartitioning(bool enablePartitioning = true)`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Descriptors/IAzureServiceBusTopicDescriptor.cs:36` — same
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusDefaultQueueOptions.cs:53` and `AzureServiceBusDefaultTopicOptions.cs:27` — defaults
  - Default in both default-options classes is `null` (i.e. SDK default = false). Topology XML doc on `IAzureServiceBusQueueDescriptor.WithEnablePartitioning` already states *"Must be set at creation time and cannot be altered later."*

- **Send path**: zero references to `PartitionKey`. `AzureServiceBusDispatchEndpoint.CreateMessage` (`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs:111-196`) sets `MessageId`, `CorrelationId`, `ContentType`, `Subject`, `ReplyTo`, `TimeToLive`, and copies user headers as `ApplicationProperties` — but never assigns `PartitionKey`, `SessionId`, or `TransactionPartitionKey`. So today, sending to a partitioned entity always falls back to either round-robin (if no dedup) or `MessageId`-as-partition-key (if dedup is on).

- **Receive path**: `AzureServiceBusMessageEnvelopeParser` does **not** read `PartitionKey` or `SessionId` off the incoming `ServiceBusReceivedMessage` (`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessageEnvelopeParser.cs:31-50`). For replies/forwarding the same partition key would not propagate.

- **Sessions**: `RequiresSession` is wired through topology (`AzureServiceBusQueue.cs:60`, `AzureServiceBusSubscription.cs:47`, etc.) but, identically to partitioning, no session-aware send/receive logic exists yet. The `ServiceBusProcessor` is created via `CreateProcessor`, not `CreateSessionProcessor`, so a queue with `RequiresSession=true` will not actually drain through the current receive endpoint.

## Recommendation

### `EnablePartitioning` as a topology option

**Default off; keep opt-in.** That matches:

1. The Azure SDK default (`CreateQueueOptions.EnablePartitioning = false`).
2. Microsoft's own guidance that low-volume entities perform **worse** with partitioning and should be recreated without it.
3. The fact that flipping it later is impossible — opt-in forces a deliberate decision the user can't take back.
4. Premium namespaces handle this at the namespace level, before Mocha is in the picture, so a per-entity default is irrelevant there anyway.

The current shape (`bool? EnablePartitioning` on configurations + `WithEnablePartitioning(bool = true)` on descriptors + a global default in `AzureServiceBusDefaultQueueOptions`) is correct and idiomatic. **No change needed there.** What I would add is a doc line on the descriptor explicitly noting the throughput trade-off (low-volume entities) and the immutability constraint — both already partly documented.

### How `PartitionKey` should be derived

Mirror the **already-existing** RabbitMQ routing-key pattern (`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQRoutingKeyExtensions.cs`, `RabbitMQRoutingKeyExtractor.cs`, `Middlewares/Dispatch/RabbitMQRoutingKeyMiddleware.cs`). It already solves exactly this problem for another transport, so the API is familiar to Mocha users:

1. **`UseAzureServiceBusPartitionKey<TMessage>(Func<TMessage, string?>)`** extension on `IMessageTypeDescriptor`. Stores an `AzureServiceBusPartitionKeyExtractor` in the message-type feature collection — same shape as `RabbitMQRoutingKeyExtractor`.
2. **A small dispatch middleware** (`AzureServiceBusPartitionKeyMiddleware`) that runs before serialization, extracts the key from the message instance via the feature, and writes it into the dispatch context (e.g. as a header `x-mocha-partition-key`, or onto a typed `AzureServiceBusDispatchFeature`).
3. **In `AzureServiceBusDispatchEndpoint.CreateMessage`**, copy that key onto `ServiceBusMessage.PartitionKey`. Skip if `SessionId` is set (the broker rejects mismatches; defer to session as the partition key — that mirrors what the broker itself does in priority order).

**Where the key value should come from** in priority:

1. Explicit per-message-type extractor (the `UseAzureServiceBusPartitionKey<T>` above) — owner of the message picks the routing dimension that makes sense (tenant ID, customer ID, aggregate ID).
2. **Do not** auto-derive a key from envelope metadata or message type. Picking a poor key (e.g. `MessageType` => everything of one type pins to one partition; `MessageId` => destroys batching) silently makes throughput worse than non-partitioned.
3. For session-aware entities, **`SessionId` is the partition key by definition** — once we add session support, the session-id producer drives partitioning; no separate key extractor needed.
4. If neither is configured, leave `PartitionKey` unset and let Service Bus do round-robin. With the existing `MessageId` propagation that's already in `CreateMessage`, duplicate detection still works correctly because the broker uses `MessageId` as the partition key in that mode.

This keeps Mocha's defaults safe (no surprise partition pinning), gives users a typed, discoverable opt-in, and reuses an established pattern from the RabbitMQ transport so the conventions match across transports.

### What this implies for the receive path

Add an **optional** `PartitionKey` propagation through the envelope so reply/forward scenarios inherit it. Two reasonable options:

- Add `MessageEnvelope.PartitionKey` (cleanest, but spreads ASB-specific naming into the core envelope — would need to be transport-agnostic; "PartitionKey" reads fine across Kafka/Event Hubs too).
- Or stash on a transport-specific `AzureServiceBusReceiveFeature` and have `AzureServiceBusDispatchEndpoint` look it up when crafting reply messages.

I'd lean on the feature-based approach for now (smaller blast radius, matches how the RabbitMQ transport stores its routing key in `RabbitMQMessageHeaders.RoutingKey` as a `ContextDataKey<string>`).

### What we explicitly do **not** need to do yet

- `TransactionPartitionKey` / "via-partition-key": only relevant once we add cross-entity transactions with a transfer queue. Not on the current roadmap.
- Hard-coding partition counts: Mocha's `WithEnablePartitioning(true)` correctly leaves the count to Service Bus (16 on Standard, namespace-level on Premium). No `WithPartitionCount(int)` is needed since the SDK does not expose one for entity-level partitioning.
- Validating `EnablePartitioning && AutoDeleteOnIdle` together: would be a nicety. Service Bus rejects the `Create` call anyway with a clear error, so optional.

# Research: `DefaultMessageTimeToLive` and envelope TTL mapping

## TL;DR

**The wiring is already in place and correct.** The Mocha envelope's expiration field is `DeliverBy` (not `ExpiresAt`/`Expiration`/`Ttl`), and the ASB transport already maps it both ways:

| Direction | Envelope                  | ASB                              | Code                                                          |
| --------- | ------------------------- | -------------------------------- | ------------------------------------------------------------- |
| Send      | `MessageEnvelope.DeliverBy` | `ServiceBusMessage.TimeToLive`     | `AzureServiceBusDispatchEndpoint.cs:123-130`                  |
| Receive   | `MessageEnvelope.DeliverBy` | `ServiceBusReceivedMessage.ExpiresAt` | `AzureServiceBusMessageEnvelopeParser.cs:45`                 |
| Topology  | `WithDefaultMessageTimeToLive(...)` | `Create{Queue,Topic,Subscription}Options.DefaultMessageTimeToLive` | `Topology/AzureServiceBus{Queue,Topic,Subscription}.cs` |

There is **no global `DefaultMessageTimeToLive` outside per-entity defaults** — it lives on `AzureServiceBusDefaultQueueOptions` and `AzureServiceBusDefaultTopicOptions`, which are applied per-entity. That's the right shape.

## 1. Envelope property name

**Property:** `MessageEnvelope.DeliverBy` (not `ExpiresAt`, `Expiration`, or `Ttl`).

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs:107`

  ```csharp
  /// Must be processed before this timestamp.
  /// Used for TTL / NServiceBus "TimeToBeReceived".
  public DateTimeOffset? DeliverBy { get; init; }
  ```

- Wire-format property name constant: `MessageEnvelope.Properties.DeliverBy = "deliverBy"` (line 179)
- Read/write paths in `MessageEnvelopeReader.cs` (lines 85, 87, 224, 289) and `DispatchSerializerMiddleware.cs:89`
- Public API: `IDispatchContext.DeliverBy`, `IMessageContext.DeliverBy`, `ConsumeContext<T>.DeliverBy`
- Set from user options: `DefaultMessageBus.cs:69,123,279,331,391` via `options.ExpirationTime` (`SendOptions`/`PublishOptions`)
- Receive-side guard: `ReceiveExpiryMiddleware.cs` drops messages where `DeliverBy < now` before deserialization and marks them as consumed (no retry).

The `ExpiresAt` name only exists as the corresponding ASB SDK property on `ServiceBusReceivedMessage`.

## 2. ASB transport TTL handling — what's already implemented

### 2a. Send path (envelope -> ServiceBusMessage.TimeToLive)

`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs:123-130`

```csharp
if (envelope.DeliverBy is { } deliverBy)
{
    var ttl = deliverBy - DateTimeOffset.UtcNow;
    if (ttl > TimeSpan.Zero)
    {
        message.TimeToLive = ttl;
    }
}
```

Notes:
- Converts the absolute `DeliverBy` instant into a relative `TimeSpan` for `ServiceBusMessage.TimeToLive`. Correct — ASB takes a duration, not an instant.
- Skips already-expired messages (`ttl <= 0`). Reasonable: ASB would reject `Zero`/negative and the broker is going to drop it anyway, so let `ReceiveExpiryMiddleware` deal with truly stale messages on receive.
- Uses wall-clock `DateTimeOffset.UtcNow` rather than `TimeProvider`. Minor: the rest of the framework prefers `TimeProvider` (see `ReceiveExpiryMiddleware`) — would make this testable and consistent.

### 2b. Receive path (ServiceBusReceivedMessage.ExpiresAt -> envelope.DeliverBy)

`/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessageEnvelopeParser.cs:45`

```csharp
DeliverBy = message.ExpiresAt != DateTimeOffset.MaxValue ? message.ExpiresAt : null,
```

Notes:
- `ServiceBusReceivedMessage.ExpiresAt` is `enqueued-time + effective-TTL`. If neither per-message nor entity-level TTL is set, the broker reports `DateTimeOffset.MaxValue`, which is correctly mapped to `null` here.
- This means an entity-level `DefaultMessageTimeToLive` will *also* surface on the envelope's `DeliverBy` even when the sender didn't set one. That feeds straight into `ReceiveExpiryMiddleware`'s pre-deserialization drop. Good.

### 2c. Topology / entity-level TTL

Per-entity `DefaultMessageTimeToLive` is plumbed through configuration -> topology -> `Create{Entity}Options`:

- Configuration class:
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusQueueConfiguration.cs:42`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusTopicConfiguration.cs:22`
  - `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/Configurations/AzureServiceBusSubscriptionConfiguration.cs:37`
- Descriptor fluent API: `WithDefaultMessageTimeToLive(TimeSpan)` on all three descriptors (`AzureServiceBusQueueDescriptor.cs:61`, `AzureServiceBusSubscriptionDescriptor.cs:43`, `AzureServiceBusTopicDescriptor.cs:33`).
- Applied on provision:
  - Queue: `AzureServiceBusQueue.cs:157-160`
    ```csharp
    if (DefaultMessageTimeToLive is not null)
    {
        options.DefaultMessageTimeToLive = DefaultMessageTimeToLive.Value;
    }
    ```
  - Topic: `AzureServiceBusTopic.cs:113-116`
  - Subscription: `AzureServiceBusSubscription.cs:147-150`
- Defaults (applied per-entity, no global override): `AzureServiceBusDefaultQueueOptions.DefaultMessageTimeToLive` (`Configurations/AzureServiceBusDefaultQueueOptions.cs:38, 80`) and `AzureServiceBusDefaultTopicOptions.DefaultMessageTimeToLive` (`Configurations/AzureServiceBusDefaultTopicOptions.cs:17, 55`). Both use `??=` so explicit per-entity values win.

### 2d. Dead-lettering on expiration

Already exposed end-to-end via `DeadLetteringOnMessageExpiration` (queue + subscription level — there's no topic-level setting in ASB). Same plumbing layout (configuration -> descriptor `With...` -> provisioned via `CreateQueueOptions`/`CreateSubscriptionOptions`).

## 3. ASB semantics confirmed via Microsoft docs

From [Message expiration (Time to Live)](https://learn.microsoft.com/azure/service-bus-messaging/message-expiration):

1. **Entity-level vs per-message TTL.** `DefaultMessageTimeToLive` is the default applied to messages that don't set their own TTL, **and it is the ceiling**. If a message-level `TimeToLive` exceeds the entity default, the broker **silently** clamps it down to the entity default. So `ServiceBusMessage.TimeToLive` set from `DeliverBy` may be silently shortened — the receive side will see an `ExpiresAt` earlier than the sender's `DeliverBy`. The current parser handles this correctly because it reads `ExpiresAt` (the broker-resolved value).

2. **Enforcement.** ASB treats TTL as authoritative metadata, but enforcement is **lazy and opportunistic**:
   - Past `expires-at-utc`, messages are no longer eligible for retrieval.
   - The broker may not promptly remove expired messages — they can briefly show in counts and `Peek`, but `Receive`/`PeekLock` won't return them.
   - TTL is **not enforced when no clients are listening** — expiry is computed/applied during receive activity.
   - Messages **already locked** by a receiver are unaffected; expiry kicks in only after lock release/abandon.

3. **What happens to expired messages.**
   - If `DeadLetteringOnMessageExpiration` is `true` on the queue/subscription -> moved to the entity's DLQ with `DeadLetterReason = "TTLExpiredException"` (per docs).
   - If `false` (the SDK default) -> **silently dropped**.
   - This is settable **only at queue/subscription creation time** for new entities (per JS docs note; the .NET admin API allows updates).

4. **Tier note.** Standard/Premium: max TTL = `TimeSpan.MaxValue` (effectively unbounded). Basic tier: capped at **14 days**.

5. **Topic vs subscription.** If a topic specifies a smaller TTL than a subscription, the topic TTL wins.

## 4. Recommendations

### Already correct — no change needed
- Send maps `envelope.DeliverBy -> ServiceBusMessage.TimeToLive` (correctly converting absolute -> relative).
- Receive maps `ServiceBusReceivedMessage.ExpiresAt -> envelope.DeliverBy` (correctly mapping `DateTimeOffset.MaxValue` to `null`).
- Per-entity `DefaultMessageTimeToLive` is exposed on queue, topic, and subscription descriptors and propagated to `Create*Options`.
- Dead-lettering on expiration exposed for queue and subscription (correctly omitted for topic — ASB doesn't support it there).
- `ReceiveExpiryMiddleware` already drops envelopes whose `DeliverBy` has passed before deserialization runs.

### Worth tightening (small)
- **Use `TimeProvider` in `AzureServiceBusDispatchEndpoint.CreateMessage`** instead of `DateTimeOffset.UtcNow`. This matches `ReceiveExpiryMiddleware` and lets tests verify TTL truncation deterministically. The dispatch endpoint can resolve `TimeProvider` via the existing services container or carry it on the transport.
- **Document the silent ceiling behavior** in `WithDefaultMessageTimeToLive` XML doc comments — users setting `entity TTL = 1h` and sending with `DeliverBy = now + 24h` will be surprised that ASB drops it to 1h with no error. (Currently the doc just says "default time-to-live applied to messages that do not specify their own", which under-sells the ceiling effect.)
- **Consider warning when `ttl <= 0`** in the dispatch endpoint. Today the message goes through *without* a TTL in that case, which means it falls back to entity TTL (could be `Max`/effectively unbounded). Probably fine because `ReceiveExpiryMiddleware` will drop it on the other side, but a debug-level log entry would aid diagnosis.

### Not needed
- No "global" `DefaultMessageTimeToLive` config knob. The per-entity defaults on `AzureServiceBusDefaultQueueOptions` / `AzureServiceBusDefaultTopicOptions` are the right level — TTL is fundamentally an entity property in ASB, and a per-message default would re-implement what the broker already does. Skip.
- No need to rename `DeliverBy` to `ExpiresAt`. The semantic is "must be delivered by this instant", which is precisely what ASB's `ExpiresAt` represents on the receive side. The naming difference is a Mocha-vs-ASB vocabulary split, not a bug.

## Key file locations

- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Transport/MessageEnvelope.cs` (lines 105-107, 178-179)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusDispatchEndpoint.cs` (lines 123-130)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/AzureServiceBusMessageEnvelopeParser.cs` (line 45)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusQueue.cs` (lines 50, 95, 157-160)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusTopic.cs` (lines 30, 71, 113-116)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusSubscription.cs` (lines 42, 75, 147-150)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusDefaultQueueOptions.cs` (lines 38, 80)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Configurations/AzureServiceBusDefaultTopicOptions.cs` (lines 17, 55)
- `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha/Middlewares/Receive/ReceiveExpiryMiddleware.cs`

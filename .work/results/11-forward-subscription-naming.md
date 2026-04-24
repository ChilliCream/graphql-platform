# Forward-subscription naming: stability + correctness

## TL;DR

1. The `"fwd-" + Destination.Name` truncated to 50 chars is **not collision-stable** — two long destination queue names that share a 46-char prefix produce the same subscription name and the second `CreateSubscriptionAsync` swallows the conflict via the `MessagingEntityAlreadyExists` catch, silently mis-wiring the topic chain.
2. The comment on the workaround is **technically wrong** about *why* the SDK rejects the call. The Azure SDK's `CreateSubscriptionOptions.ForwardTo` setter compares the **topic name**, not the subscription name, against `ForwardTo` — so prefixing the subscription name does nothing for that validation. The real collision in Mocha is `topic name == destination queue name` (e.g. `process-payment` topic + `process-payment` queue from a `ProcessPaymentHandler`/`ProcessPayment` pair). The current code "happens to work" for the test cases because `GetPublishEndpointName` adds a namespace prefix, but it will throw for the send-topic path the moment a user lands on a colliding pair.
3. Recommended fix: a deterministic 50-char-budget name composed of an 11-byte prefix containing both the role discriminator and an explicit `→` arrow, the destination's truncated label, and a 13-char base32 xxHash64 of the full `(source-topic, destination-queue)` tuple as the suffix. See "Naming algorithm" below.

The truncation collision is the immediate bug the user flagged; the topic-name validation issue is adjacent and worth fixing in the same change.

## Where this lives

- Code under review: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Topology/AzureServiceBusSubscription.cs`, lines 120-134.
- Mirror in the AppHost emulator config: `/Users/pascalsenn/kot/hc2/src/Mocha/examples/AzureServiceBusTransport/AzureServiceBusTransport.AppHost/AppHost.cs`, lines 56-69 (`Topic(...)` helper hardcodes `"fwd-" + sub`).
- Test mirror: `/Users/pascalsenn/kot/hc2/src/Mocha/test/Mocha.Transport.AzureServiceBus.Tests/Behaviors/AutoProvisionIntegrationTests.cs`, lines 225-233 (`ToSubscriptionName`).

The convention that emits these subscriptions: `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/Conventions/AzureServiceBusReceiveEndpointTopologyConvention.cs`. Each `(publishTopicName | sendTopicName) → configuration.QueueName` pair gets one subscription whose name is currently `"fwd-" + queueName[..46]`.

## Topology context (why subscriptions exist at all)

Mocha models Azure Service Bus pub/sub-into-queue as:

- One topic per message type (publish topic = `<namespace>.<message-name>`; send topic = `<message-name>`).
- One queue per receive endpoint (default = `<handler-name>`; service-prefixed if `IHostInfo.ServiceName` is set).
- One subscription per (topic, queue) pair, with `ForwardTo = queueName`. Messages land in the topic, the subscription's auto-forwarding moves them to the queue, the receive endpoint reads from the queue. The subscription itself is never read from.

So the subscription is purely a routing primitive; its name is internal. That's important: the name only has to be **stable, unique per (topic, queue) pair, and valid** — humans never need to type or grep it.

## Limits, exact rules, and SDK validation

Confirmed against Microsoft Learn and `Azure.Messaging.ServiceBus` 7.20.1 source:

- **Subscription name length**: max **50 characters** (Service Bus broker limit, common to all tiers). Source: Service Bus quotas page and `Microsoft.ServiceBus` ARM resource-name rules. Both the broker and the SDK enforce this.
- **Subscription name characters**: ARM rules say "Alphanumerics, periods, hyphens, and underscores. Start and end with alphanumeric." `EntityNameFormatter.CheckValidQueueName` is applied to forwarding targets too, but the subscription name itself goes through topic/subscription validation. Lowercase hex (`0-9a-f`) and base32 lowercase (RFC 4648 lowercased) are both safely inside this set; base36 with lowercase letters is also safe. Avoid `/`, which Service Bus does allow in queue/topic *paths* but not in subscription names per ARM.
- **Self-forwarding validation (`Azure.Messaging.ServiceBus` 7.20.1)**: `CreateSubscriptionOptions.ForwardTo` setter does:
  ```csharp
  if (_topicName.Equals(value, StringComparison.CurrentCultureIgnoreCase))
      throw new InvalidOperationException("Entity cannot have auto-forwarding policy to itself");
  ```
  The check is **topic-vs-forwardTo, case-insensitive**, and the comparison is plain `string.Equals` — there is no fully-qualified path normalization. Same setter shape on `SubscriptionProperties.ForwardTo` and on `CreateQueueOptions.ForwardTo` (which compares against the queue's `_name`). The broker side uses the same logical rule but does not add a separate "subscription path == forward path" check, because subscription paths (`topic/Subscriptions/sub`) and queue paths (`queue`) are structurally distinct, so they never collide as paths.
  - **Implication**: the `fwd-` prefix on the *subscription name* does not affect this validation. The `fwd-` prefix only happens to dodge the issue when one would otherwise have set `SubscriptionName == ForwardTo`, which the SDK does *not* check. The current Mocha code is not actually saved by the prefix — it's saved by the fact that today's tests don't drive a topic name equal to a queue name. As soon as a user gets `GetSendEndpointName(typeof(Foo)) == GetReceiveEndpointName(typeof(FooHandler))` (which the default convention will happily produce), the `CreateSubscriptionAsync` throws and is swallowed by the broad `catch (Exception) when (AutoProvision is null or true)` fallback — the subscription silently isn't created, the topic has no fan-out, and published messages disappear. Comment update + behavior fix needed regardless of the truncation question.
- **Auto-forwarding chain**: max **4 hops** before a message is dead-lettered (`Don't create a chain that exceeds four hops. Messages that exceed four hops are dead-lettered`). Mocha's chain length is 1 (topic → queue), so this is comfortable. Forwarding is also **billed per hop** (each forward counts as one operation), so each Mocha publish is +1 op vs sending direct to the queue — acceptable, but documentable.
- **Tier**: auto-forwarding is **not supported on the Basic tier**. Standard and Premium are fine. Mocha already uses topics, which Basic also doesn't support, so this isn't a new constraint.
- **Sessions**: `Autoforwarding isn't supported for session-enabled queues or subscriptions.` If a Mocha endpoint enables `RequiresSession`, the subscription provisioning has to fail loudly; today it would be silently swallowed — out of scope for this question, worth flagging.

## Truncation collision: concrete blast radius

`Destination.Name` (the receive endpoint queue) by default convention is `<service-name>.<handler-base-name>` in kebab-case, where `<handler-base-name>` strips `Handler`/`Consumer`/`Handler\`1`/`Consumer\`1` and runs through the kebab-case regex.

Real-world destination names are easily 30-80 characters. Examples:

- `payments.process-payment` (24) — fits unprefixed
- `notification-service.order-shipped-notification` (47) — `fwd-` brings it to 51, truncates to 50 by chopping the trailing `n` ⇒ `fwd-notification-service.order-shipped-notificatio`
- `notification-service.order-shipped-notification-mobile` ⇒ same truncation as above ⇒ **collision**, second create silently swallowed by the catch
- For long namespaces under DDD layouts (`<bounded-context>.<aggregate>.<command-or-event>`) the 46-char usable budget runs out frequently. Anything past `<context>.<message>` of combined length 47 has a real probability of clashing under the current scheme.

Because the `MessagingEntityAlreadyExists` catch is by design (concurrent provisioning across instances), the collision is invisible at runtime; the symptom is "messages published to topic X never arrive at queue Y" — the worst kind of bug for a transport.

## Naming algorithm proposal

Total budget: 50 chars (broker max). Allowed character set we'll use: lowercase ASCII letters + digits + `-`. We avoid `.` and `_` even though they're valid, because they aren't needed and a single charset keeps logging predictable.

Proposed layout (50 chars total):

```
| prefix (5) | source-label (15) | sep (1) | dest-label (15) | sep (1) | hash (13) |
  fwd--        <truncated topic>   -          <truncated queue>  -        <xxh64 b32>
```

Concretely:

- **Prefix `fwd--` (5 bytes)**: keeps the human-grep-able role discriminator. Two hyphens so it's visually distinct from a trailing kebab segment of the topic name. (You can drop to `fwd-` (4) if you want one more byte for hash; 5 is fine for clarity.)
- **Source-topic label (15 bytes)**: `Sanitize(Source.Name)` then truncated. Lossy by design — exists only to make the entity name skim-readable in the Azure portal.
- **Separator `-` (1 byte)**.
- **Destination-queue label (15 bytes)**: `Sanitize(Destination.Name)` then truncated. Same story.
- **Separator `-` (1 byte)**.
- **Hash (13 bytes)**: `XxHash64.HashToUInt64(utf8(Source.Name + "\0" + Destination.Name))` rendered in **base32 lowercase, RFC 4648, no padding**. 64 bits in base32 is `ceil(64/5) = 13` chars. Collision-resistant for any practical number of `(topic, queue)` pairs (xxHash64 birthday bound is ~2^32 ≈ 4 billion pairs before a 50% collision probability — Mocha will have a few hundred at most).

Encoding choice: lowercase base32 is denser than hex (13 chars carries 64 bits vs. 16 chars for hex) and contains no `=` padding, no `/`, no `+`. Base36 would be 13 chars too (`64 / log2(36) = 12.4` → 13) but requires arbitrary-precision conversion; base32 is a fixed shift loop. Hex (16 chars) costs 3 extra bytes that we can spend on the readable labels, so prefer base32 here.

Sanitization rule for the labels: lowercase, replace any character outside `[a-z0-9-]` with `-`, collapse runs of `-`, trim leading/trailing `-`, then `Substring(0, 15)` and trim trailing `-` again. The hash carries the uniqueness; the labels just have to be valid and pleasant.

Pseudocode:

```csharp
private static string GetSubscriptionName(string sourceTopic, string destinationQueue)
{
    Span<byte> hashBytes = stackalloc byte[8];
    var hash = new XxHash64();
    hash.Append(Encoding.UTF8.GetBytes(sourceTopic));
    hash.Append([0]);
    hash.Append(Encoding.UTF8.GetBytes(destinationQueue));
    hash.GetCurrentHash(hashBytes);

    var hashSegment = Base32Lower(hashBytes);                 // 13 chars
    var sourceLabel = SanitizeAndTruncate(sourceTopic, 15);   // ≤15 chars
    var destLabel   = SanitizeAndTruncate(destinationQueue, 15);

    // Total = 5 + ≤15 + 1 + ≤15 + 1 + 13 = ≤50
    return $"fwd--{sourceLabel}-{destLabel}-{hashSegment}";
}
```

Worst-case length math: `5 + 15 + 1 + 15 + 1 + 13 = 50` exactly. Best-case (very short labels): still safely under 50. If a label sanitizes to empty (e.g. someone provisions a queue named `___`), the algorithm produces `fwd---<dest>-<hash>` or `fwd--<src>--<hash>` — still valid (starts with `f`, ends with a base32 lowercase letter or digit, no double-`-` issue at start/end). Verify the start/end-with-alphanumeric ARM rule by checking the first character of the prefix (`f`) and the last character of the hash (always `[a-z0-9]` in base32 lowercase) — both alphanumeric, fine.

Tier-up if you want pure determinism without truncation: drop the labels entirely and use `fwd--<26-char-base32-of-128-bit-hash>` for ~28 chars total. Cleaner but less debuggable when looking at the entity in the portal. Recommendation: keep the labels; the hash is what guarantees uniqueness.

## Adjacent fixes to consider in the same change

These aren't "the question," but the investigation surfaced them and they belong in the same PR if you touch this code:

1. **Comment is misleading**: the SDK doesn't reject "ForwardTo == subscription name" — it rejects "ForwardTo == topic name". Update the comment so the next person doesn't add a workaround for the wrong validation.
2. **Real `topic name == queue name` case is unhandled**: when the convention emits a send topic and a queue with identical kebab strings (totally possible for `ProcessPaymentHandler` + `ProcessPayment`), the SDK throws `InvalidOperationException` *before* the HTTP call. That's not a `ServiceBusException`, so the current `catch (Exception) when (AutoProvision is null or true)` swallows it silently. Either: (a) detect and rename the topic at convention time, (b) detect and use a different ForwardTo intermediate, or (c) at minimum let the exception propagate when `AutoProvision == true` (don't silently lose the topology).
3. **AppHost mirror**: `AzureServiceBusTransport.AppHost/AppHost.cs` line 63 hardcodes the same `"fwd-" + sub` pattern. If the runtime algorithm changes, the AppHost generator needs to call the same shared helper or it'll provision divergent subscriptions for the emulator.
4. **Test mirror**: `AutoProvisionIntegrationTests.ToSubscriptionName` duplicates the truncation logic. Move the algorithm into a public/internal helper and have both the runtime and the test call it — eliminates drift.

## Sources

Microsoft Learn:
- Service Bus quotas (subscription max name length = 50, max message-entity path = 260): https://learn.microsoft.com/azure/service-bus-messaging/service-bus-quotas
- ARM resource-name rules (`namespaces / topics / subscriptions`: 1-50, alphanumerics + `.` + `-` + `_`, start and end alphanumeric): https://learn.microsoft.com/azure/azure-resource-manager/management/resource-name-rules#microsoftservicebus
- Auto-forwarding concept page (4-hop chain limit, billed per hop, no sessions, requires Standard or Premium): https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding
- Enable auto-forwarding (.NET API: `CreateSubscriptionOptions.ForwardTo`): https://learn.microsoft.com/azure/service-bus-messaging/enable-auto-forward

Azure SDK for .NET (`Azure.Messaging.ServiceBus` 7.20.1) — exact validation:
- `CreateSubscriptionOptions.ForwardTo` setter compares against `_topicName` (case-insensitive): https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/src/Administration/CreateSubscriptionOptions.cs
- `SubscriptionProperties.ForwardTo` (post-create update) — same comparison shape: https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.subscriptionproperties.forwardto
- `CreateQueueOptions.ForwardTo` setter compares against the queue's own `_name`: https://github.com/Azure/azure-sdk-for-net/blob/main/sdk/servicebus/Azure.Messaging.ServiceBus/src/Administration/CreateQueueOptions.cs
- Property docs (50-char limit, restricted chars `@?#*/\\` for subscription name): https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.subscriptionproperties.subscriptionname

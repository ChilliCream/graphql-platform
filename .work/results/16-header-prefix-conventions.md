# Header Prefix Conventions for the Mocha Azure Service Bus Transport

## TL;DR / Recommendation

- The `x-mocha-` prefix uses the deprecated HTTP `X-` convention (RFC 6648, 2012) and does **not** match conventions used by Azure Service Bus itself (`x-opt-`), nor by any major .NET messaging library (MassTransit `MT-`, NServiceBus `NServiceBus.`, CloudEvents `cloudEvents_`).
- The filter on the receive side is **correct in intent**: it strips framework-internal application properties out of `MessageEnvelope.Headers` so handlers do not see them as user headers, and so a round-tripped message does not duplicate envelope fields back into headers.
- Recommendation: **drop the `x-` prefix and switch to `mocha.` (or `mocha-`)**. Centralize the prefix in one constant on `AzureServiceBusMessageHeaders` (today the literal `"x-mocha-"` is duplicated across two files and is not derivable from the constants), document it as a public-facing convention, and use that constant everywhere on both send and receive.

---

## 1. The code in question

Two files in `src/Mocha/src/Mocha.Transport.AzureServiceBus/`:

### `AzureServiceBusMessageHeaders.cs`
Defines the eight framework-internal application-property keys, each hard-coded with a `"x-mocha-"` prefix string literal — there is no `Prefix` constant:

```
ConversationId           = "x-mocha-conversation-id"
CausationId              = "x-mocha-causation-id"
SourceAddress            = "x-mocha-source-address"
DestinationAddress       = "x-mocha-destination-address"
FaultAddress             = "x-mocha-fault-address"
MessageType              = "x-mocha-message-type"
EnclosedMessageTypes     = "x-mocha-enclosed-message-types"
SentAt                   = "x-mocha-sent-at"
```

### `AzureServiceBusMessageEnvelopeParser.cs`
`BuildHeaders(IDictionary<string, object?> props)` walks the AMQP `application-properties` map twice. The first pass counts non-prefixed keys to size the result; the second pass copies them. In both passes the literal `"x-mocha-"` is used:

```csharp
if (!key.StartsWith("x-mocha-", StringComparison.Ordinal))
{
    userKeyCount++;
}
...
if (key.StartsWith("x-mocha-", StringComparison.Ordinal))
{
    continue;
}
result.Set(key, value);
```

### What the filter is for

`AzureServiceBusDispatchEndpoint.CreateMessage` writes envelope fields (`ConversationId`, `CausationId`, `SourceAddress`, `DestinationAddress`, `FaultAddress`, `MessageType`, `EnclosedMessageTypes`, `SentAt`) into `ApplicationProperties` under the framework keys, **and then writes the user's `Headers` into the same dictionary**:

```csharp
foreach (var header in envelope.Headers)
{
    ...
    props[header.Key] = header.Value;
}
```

So on the wire the AMQP `application-properties` map contains:

- ASB-native fields (`MessageId`, `CorrelationId`, `ContentType`, `Subject`, `ReplyTo`) — already extracted natively in `Parse`.
- Mocha framework metadata under `x-mocha-…`.
- User-defined headers under whatever keys the user chose.

`BuildHeaders` filters `x-mocha-…` so that:

1. Framework metadata that is already mapped to envelope fields (like `ConversationId`) is not duplicated as a "header".
2. A handler that inspects `envelope.Headers` only sees what the producer put there, not what the transport added.
3. A receive → re-publish round trip does not double-write envelope fields into headers and then into `application-properties` again.

This is the correct behavior. The only critique is that the filter relies on a *string literal*, not on a constant defined by `AzureServiceBusMessageHeaders`, so the prefix is duplicated and silently couples the parser to the headers class.

---

## 2. Historical context: the `X-` prefix and RFC 6648

Quoting [RFC 6648 — Deprecating the "X-" Prefix and Similar Constructs in Application Protocols](https://www.rfc-editor.org/rfc/rfc6648.html) (Saint-Andre, Crocker, Nottingham; June 2012):

- "Creators of new parameters … **SHOULD NOT prefix their parameter names with 'X-' or similar constructs.**"
- "Creators of new parameters SHOULD assume that all parameters they create might become standardized, public, commonly deployed, or usable across multiple implementations."
- "They SHOULD employ meaningful parameter names that they have reason to believe are currently unused."
- When meaningful segregation is necessary, "a parameter name could incorporate the organization's name or primary domain name", e.g., `ExampleInc-foo` or `com.example.foo`.

The motivation is concrete: every time an experimental `X-` parameter became broadly used, the eventual standardization required dual support for the unprefixed and the `X-`-prefixed name forever. The `X-` distinction therefore became meaningless in practice.

RFC 6648 is about HTTP/email/MIME parameter names, not AMQP, but the underlying argument transfers cleanly: a custom prefix is fine, but `X-` specifically signals "experimental / non-standard" — a signal that has been formally retired.

### AMQP/JMS conventions

- AMQP 1.0 itself does not impose a naming convention on user-defined `application-properties`; it is a free-form `map<symbol, primitive>`.
- JMS 2.0 message-property identifiers cannot contain `:`; they must be valid Java identifier-style names. This is what drove CloudEvents away from `cloudEvents:` and toward `cloudEvents_` in the AMQP binding (see §4 below).

---

## 3. What Azure Service Bus itself does

Looking at the Microsoft AMQP guide ([AMQP 1.0 in Azure Service Bus and Event Hubs protocol guide](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-protocol-guide)):

- ASB **broker properties** (the named fields on `ServiceBusMessage` — `MessageId`, `CorrelationId`, `ContentType`, `Subject`, `ReplyTo`, `SessionId`, `TimeToLive`, etc.) are mapped onto the **standard AMQP `properties` and `header` sections**, not into application properties. They are **not** prefixed.
- ASB-specific metadata that has no AMQP standard slot is carried in the AMQP **`message-annotations`** map, where Microsoft uses an **`x-opt-`** prefix:
  - `x-opt-scheduled-enqueue-time` → `ScheduledEnqueueTime`
  - `x-opt-partition-key` → `PartitionKey`
  - `x-opt-via-partition-key` → `TransactionPartitionKey`
  - `x-opt-enqueued-time` → `EnqueuedTime`
  - `x-opt-sequence-number` → `SequenceNumber`
  - `x-opt-locked-until` → `LockedUntil`
  - `x-opt-deadletter-source` → `DeadLetterSource`
- Microsoft management-protocol operations use a domain-name style: `com.microsoft:cancel-scheduled-message`, `com.microsoft:server-timeout`. Note this also matches RFC 6648's "use a domain name" guidance.
- Microsoft documentation explicitly states: "Any property that application needs to define should be mapped to AMQP's `application-properties` map." There is **no Microsoft-recommended prefix for custom application properties** — the namespace is yours.

So `x-opt-` is Microsoft's convention for **annotations** (broker-controlled metadata in `message-annotations`), not for application properties. We are writing to `application-properties`, which is the right channel, and Microsoft does not impose a prefix there.

### Reserved property name to be aware of

`scheduled-enqueue-time-utc` is **not** an `application-property`; the canonical wire form is `x-opt-scheduled-enqueue-time` in `message-annotations`. The .NET SDK maps it to `ServiceBusMessage.ScheduledEnqueueTime`. Our `AzureServiceBusDispatchEndpoint` already does the right thing by calling `sender.ScheduleMessageAsync(...)` rather than putting anything into application-properties for scheduling. So there is no name clash.

---

## 4. How other libraries name their framework headers

| Library | Prefix | Style | Notes |
|---|---|---|---|
| Mocha (current) | `x-mocha-` | kebab-case, deprecated `x-` | Hard-coded literal in two files |
| MassTransit | `MT-` | kebab-case | Used for ASB application properties (e.g., `MT-MessageType`, `MT-Fault-Message`); see [Discussion #2514](https://github.com/MassTransit/MassTransit/discussions/2514) |
| NServiceBus | `NServiceBus.` | dotted PascalCase | `NServiceBus.MessageId`, `NServiceBus.CorrelationId`, `NServiceBus.ConversationId`, `NServiceBus.EnclosedMessageTypes`, `NServiceBus.TimeSent`, `NServiceBus.OriginatingEndpoint`, `NServiceBus.Version`; see [Message Headers](https://docs.particular.net/nservicebus/messaging/headers) |
| CloudEvents (Kafka binding) | `ce_` | snake_case | For Kafka message headers; see [Kafka binding](https://github.com/cloudevents/spec/blob/v1.0.1/kafka-protocol-binding.md) |
| CloudEvents (AMQP binding) | `cloudEvents_` (preferred) or `cloudEvents:` | mixed | "The '_' separator character SHOULD be preferred in the interest of compatibility with JMS 2.0 clients and JMS message selectors where the ':' separator is not permitted." See [AMQP binding v1.0.2](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/bindings/amqp-protocol-binding.md). |
| Wolverine | None standard for ASB; uses an envelope mapper per transport. Diagnostic headers like `exception-type` for Kafka DLQ. |
| Brighter | No documented project-wide prefix; carries `Id`, `TimeStamp`, `Topic`, `MessageType` on its own message structure rather than via prefixed user properties. |

Observations:

- **Nobody else uses `x-`** for application-level framework headers, with the obvious exception that ASB's *broker* annotations use `x-opt-`. Reusing `x-` therefore actively invites confusion with broker-controlled annotations.
- The two long-standing .NET messaging libraries (MassTransit, NServiceBus) both use a clearly branded prefix without `x-`.
- The most rigorous spec in the field (CloudEvents) deliberately picked `_` over `:` for AMQP/JMS compatibility — a useful warning that punctuation choice has knock-on effects.

---

## 5. Recommendation

### What to change

1. **Drop the `x-` prefix**. RFC 6648 deprecated it 14 years ago, ASB itself uses `x-opt-` for a different concept (annotations), and no peer library uses it.
2. **Pick `mocha.` or `mocha-`**.
   - `mocha.` (dot-separated) matches NServiceBus's style and is the closest thing to a domain-name namespace, which is exactly what RFC 6648 suggests as an alternative to `X-`.
   - `mocha-` (hyphen-separated) is closer to MassTransit and to the existing kebab-case body.
   - Either is fine. Dot is slightly more "namespace-like" and reads better in trace tooling.
3. **Centralize the prefix in one place**. Today `"x-mocha-"` is a string literal duplicated in `AzureServiceBusMessageEnvelopeParser`. Define a single constant on `AzureServiceBusMessageHeaders`:

   ```csharp
   internal static class AzureServiceBusMessageHeaders
   {
       public const string Prefix = "mocha.";

       public const string ConversationId       = Prefix + "conversation-id";
       public const string CausationId          = Prefix + "causation-id";
       public const string SourceAddress        = Prefix + "source-address";
       public const string DestinationAddress   = Prefix + "destination-address";
       public const string FaultAddress         = Prefix + "fault-address";
       public const string MessageType          = Prefix + "message-type";
       public const string EnclosedMessageTypes = Prefix + "enclosed-message-types";
       public const string SentAt               = Prefix + "sent-at";
   }
   ```

   And in the parser:

   ```csharp
   if (key.StartsWith(AzureServiceBusMessageHeaders.Prefix, StringComparison.Ordinal))
   {
       continue;
   }
   ```

4. **Document the prefix as a public-facing convention** in the transport README / XML doc on `AzureServiceBusMessageHeaders`. State two things explicitly:
   - "Application property keys beginning with `mocha.` are reserved for the framework. User-supplied headers using this prefix will be silently dropped on receive."
   - "Avoid using application property keys beginning with `x-opt-` — those are reserved by Azure Service Bus for broker annotations." (Defensive.)

5. **Optional but useful**: also reject (or warn on) user-supplied envelope `Headers` whose keys start with `mocha.` on the send side, in `CreateMessage`. Today the user could shadow a framework field by setting a header with the same key, and `props[header.Key] = header.Value;` would clobber the value the transport already wrote.

### Why filter on the prefix at all (the receive-side justification, in writing)

Worth committing this rationale somewhere visible because today it is implicit in the code:

> Framework metadata is round-tripped through ASB application properties so that values like `ConversationId`, `CausationId`, and `EnclosedMessageTypes` can be reconstructed on the receiving side. These same values appear as first-class fields on `MessageEnvelope`, so they must not also appear inside `MessageEnvelope.Headers` — otherwise a handler would observe each value twice (once as `envelope.ConversationId`, once as `envelope.Headers["mocha.conversation-id"]`), and a forward-and-resend (e.g., a saga relaying through a reply endpoint) would duplicate them in the next message's application properties on every hop.

### Migration concern

Renaming `x-mocha-` → `mocha.` is a wire-format change. Any in-flight messages produced by the old code and consumed by the new code will:

- Lose `ConversationId`, `CausationId`, `SourceAddress`, `DestinationAddress`, `FaultAddress`, `EnclosedMessageTypes`, and `SentAt` because the new parser will not look them up under `x-mocha-…`.
- Have those same keys leak into `envelope.Headers` (since they no longer match the new prefix filter).

Mitigations, in order of preference:

1. **Cut over cleanly while there are no production deployments.** This is the simplest fix and fits the project state.
2. If there are deployments: have the receive parser look up *both* prefixes for one release, log a deprecation warning when a `x-mocha-…` key is observed, and remove the legacy lookup in the next release.

The first option is almost certainly what we want here — `Mocha.Transport.AzureServiceBus` is new (recent commits in the branch) and there is no installed-base contract to preserve.

---

## Sources

- [RFC 6648 — Deprecating the "X-" Prefix and Similar Constructs in Application Protocols](https://www.rfc-editor.org/rfc/rfc6648.html)
- [AMQP 1.0 in Azure Service Bus and Event Hubs protocol guide](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-amqp-protocol-guide)
- [Messages, payloads, and serialization (Azure Service Bus)](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messages-payloads)
- [Message sequencing and timestamps — Scheduled messages](https://learn.microsoft.com/azure/service-bus-messaging/message-sequencing#scheduled-messages)
- [ServiceBusMessage.ScheduledEnqueueTime Property (.NET)](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusmessage.scheduledenqueuetime)
- [NServiceBus — Message Headers](https://docs.particular.net/nservicebus/messaging/headers)
- [MassTransit — Discussion #2514: Configuring MassTransit to set Custom Properties on Azure Service Bus](https://github.com/MassTransit/MassTransit/discussions/2514)
- [CloudEvents AMQP Protocol Binding v1.0.2](https://github.com/cloudevents/spec/blob/v1.0.2/cloudevents/bindings/amqp-protocol-binding.md)
- [CloudEvents Kafka Protocol Binding v1.0.1](https://github.com/cloudevents/spec/blob/v1.0.1/kafka-protocol-binding.md)

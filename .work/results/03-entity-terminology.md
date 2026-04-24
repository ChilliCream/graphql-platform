# Entity Terminology in Azure Service Bus

## TL;DR

**"Entity" is official Azure Service Bus terminology.** Microsoft uses it consistently across Microsoft Learn docs, the .NET SDK (`Azure.Messaging.ServiceBus`), the REST API, the AMQP wire protocol, and the management plane. Mocha should keep it as the umbrella term, while preferring the concrete term (queue / topic / subscription) at call sites where the kind is statically known.

## What "entity" means in ASB

A *messaging entity* is the umbrella term for any addressable, persistent messaging resource hosted inside a Service Bus namespace. The official set is:

- **Queue** — point-to-point
- **Topic** — pub/sub source
- **Subscription** — virtual queue attached to a topic (addressed as `<topic>/Subscriptions/<sub>`)
- **Rule** — subordinate of a subscription (the REST overview also calls these entities, addressed as `<topic>/Subscriptions/<sub>/Rules/<rule>`)

Source: ["Service Bus queues, topics, and subscriptions"](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-queues-topics-subscriptions) opens with: *"The messaging entities that form the core of the messaging capabilities in Service Bus are queues, topics and subscriptions."*

The [REST API overview](https://learn.microsoft.com/rest/api/servicebus/overview#entity-descriptions) reinforces this: queues/topics/subscriptions/rules are all "entities" with "entity descriptions" and an "entity path" that is the URL segment under the namespace host.

The [FAQ](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-faq) also uses "partitioned entity" as a first-class concept ("a partitioned queue or topic"). Docs on [auto-delete on idle](https://learn.microsoft.com/azure/service-bus-messaging/message-expiration#temporary-entities) and [auto-forwarding](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding) repeatedly speak of "source entity", "destination entity", "temporary entities", etc.

## Where the term appears in `Azure.Messaging.ServiceBus`

This is not a Mocha invention — the SDK surface uses it everywhere:

- `ServiceBusException.EntityPath` — *"The name of the Service Bus entity, if available; otherwise, null."* ([docs](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusexception.entitypath))
- `ServiceBusException(string, ServiceBusFailureReason, string entityPath, Exception)` — constructor parameter literally named `entityPath` ([docs](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusexception.-ctor))
- `ProcessErrorEventArgs.EntityPath` — *"Gets the entity path associated with the error event."* ([docs](https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.processerroreventargs.entitypath))
- `ServiceBusFailureReason` enum values: `MessagingEntityNotFound`, `MessagingEntityAlreadyExists`, `MessagingEntityDisabled` ([docs](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest#servicebusexception))
- Older legacy package `Microsoft.Azure.ServiceBus` shipped a public `MessagingEntityNotFoundException` type ([docs](https://learn.microsoft.com/dotnet/api/microsoft.azure.servicebus.messagingentitynotfoundexception))
- Forwarding properties (`ForwardTo`, `ForwardDeadLetteredMessagesTo`) on queue/subscription properties take the destination *entity name* — Microsoft's own auto-forwarding doc consistently calls them "source entity" / "destination entity"

The term traces back to the AMQP wire layer — sender/receiver `Source` / `Target` addresses are entity paths in ASB's AMQP dialect. So `EntityPath` is not a marketing word; it is what the protocol actually addresses.

## Comparison to other brokers

| Broker | Resource types | Umbrella term |
| --- | --- | --- |
| **Azure Service Bus** | Queue, topic, subscription, rule | **Entity** (official, used in docs + SDK + REST + AMQP) |
| RabbitMQ | Exchange, queue, binding | None — each is named directly. "Resources" used loosely. |
| Apache Kafka | Topic, partition, consumer group | None — "topic" is its own umbrella for the addressable thing. |
| Amazon SQS | Queue | N/A (single concept) |
| Amazon SNS | Topic, subscription | None universal. |
| Google Pub/Sub | Topic, subscription | None universal. |

ASB is unusual in that it has a real, consistently used umbrella term backed by the SDK and protocol. RabbitMQ and Kafka deliberately don't have one, which is why their wrappers usually pick per-call-site terminology.

## Recommendation for Mocha ASB transport

**Keep "entity" as the umbrella term, but only where the kind is genuinely unknown or both kinds are valid.** Specifically:

1. **Keep "entity" / `entityPath` (good — matches the SDK):**
   - `AzureServiceBusClientManager.GetSender(string entityPath)` — a sender can target either a queue or a topic; `entityPath` is the right name and matches SDK convention.
   - The scheduling token format `asb:{entityPath}:{sequenceNumber}` and `TryParseToken(... out string entityPath ...)` — same reason; the token is kind-agnostic.
   - Receive/processor error log messages keying off `args.EntityPath` — you are surfacing the SDK's own field, so use its name.
   - Catch handlers for `ServiceBusFailureReason.MessagingEntityAlreadyExists` and the comment "Best-effort provisioning — the entity may already exist…" — fine, since the failure reason name is `MessagingEntity*`.

2. **Prefer the concrete term where the kind is known (current code is mostly fine, a couple of tightenings worth doing):**
   - `IAzureServiceBusQueueDescriptor.WithForwardTo(string entityName)` — the *parameter name* is correct (the destination can be a queue or topic), but the XML doc currently says "the destination entity name (queue or topic)" which is the right gloss. Keep as is.
   - `IAzureServiceBusSubscriptionDescriptor.WithForwardTo(string entityName)` — same reasoning, keep.
   - Doc comments such as "Gets the entity to which messages received on this queue are auto-forwarded" on `AzureServiceBusQueue.ForwardTo` are fine — the destination is genuinely either queue or topic, so "entity" is accurate. (If you wanted to be slightly clearer you could say "destination queue or topic", but "entity" is not wrong.)
   - `AzureServiceBusMessagingTransport`'s local variable `var entities = new List<TopologyEntityDescription>()` — this is `TopologyEntityDescription` from Mocha core, not ASB; the name comes from the framework's topology abstraction, leave alone.

3. **Avoid inventing a Mocha-specific umbrella term.** Anything other than "entity" (e.g., "resource", "destination", "endpoint") would diverge from the SDK and confuse anyone reading ASB stack traces / docs alongside Mocha code.

## Conclusion

Mocha is using the term correctly. "Entity" is the right umbrella in:

- public types and parameter names that span queue + topic (`entityPath`, `entityName`)
- catch clauses on `MessagingEntity*` failure reasons
- log messages forwarding the SDK's own `EntityPath`

When the kind is fixed and known, the existing code already uses the concrete term (`Queue.Name`, `Topic.Name`, `subscription`), which is the correct pattern. No renames needed.

## Sources (Microsoft Learn)

- https://learn.microsoft.com/azure/service-bus-messaging/service-bus-queues-topics-subscriptions
- https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-overview
- https://learn.microsoft.com/azure/service-bus-messaging/service-bus-faq
- https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-exceptions-latest
- https://learn.microsoft.com/azure/service-bus-messaging/message-expiration
- https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding
- https://learn.microsoft.com/rest/api/servicebus/overview#entity-descriptions
- https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusexception.entitypath
- https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.servicebusexception.-ctor
- https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.processerroreventargs.entitypath

# Routing Keys: RabbitMQ vs Azure Service Bus

## TL;DR

**Azure Service Bus has no "routing key" primitive.** RabbitMQ routes by matching a string the *publisher* writes (`basic_publish(routing_key)`) against patterns the *subscriber* declared (`queue.bind(routing_key)`). ASB has no equivalent header that the broker uses for routing decisions. Instead, ASB pushes routing into **subscription rules**: every message published to a topic is evaluated against every subscription's rules, and only matches are copied into the subscription's "virtual queue."

The closest functional equivalents to RabbitMQ routing keys are:

| RabbitMQ                            | Azure Service Bus                                                |
| ----------------------------------- | ---------------------------------------------------------------- |
| Direct exchange + routing key       | Topic + `CorrelationFilter` on a single property                 |
| Topic exchange + wildcard pattern   | Topic + `SqlRuleFilter` with `LIKE` (`%` and `_` wildcards)      |
| Headers exchange + arguments        | Topic + `CorrelationFilter` with multiple `Properties` entries   |
| Fanout exchange                     | Topic + `TrueRuleFilter` (default rule)                          |

The mechanical difference: in RabbitMQ the routing key is a **single string sent with each message** that the broker tokenizes (`order.eu.high`); in ASB you set **named system properties** (`Subject`, `CorrelationId`, `To`) and/or **application properties** that filters evaluate. There is no `routing_key` field on `ServiceBusMessage`.

## How Mocha currently maps to ASB

The current ASB transport at `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.AzureServiceBus/` already populates the natural filter targets but does **not** expose any subscription-rule API. From `AzureServiceBusDispatchEndpoint.cs:111-196`:

```csharp
var message = new ServiceBusMessage(envelope.Body)
{
    MessageId = envelope.MessageId,
    CorrelationId = envelope.CorrelationId,    // -> usable by CorrelationFilter / sys.CorrelationId
    ContentType = envelope.ContentType,         // -> usable by CorrelationFilter / sys.ContentType
    Subject = envelope.MessageType,             // -> usable by CorrelationFilter / sys.Label
    ReplyTo = envelope.ResponseAddress          // -> usable by CorrelationFilter / sys.ReplyTo
};
// ...envelope.Headers and the x-mocha-* fields go into ApplicationProperties
```

And from `AzureServiceBusMessageEnvelopeParser.cs:31-50`, on receive these come back via the same fields. So the envelope-to-message contract already provides the data filters need. What's missing is:

1. **No routing-key concept on `MessageEnvelope`.** Search for `RoutingKey` or `PartitionKey` in `src/Mocha/src/Mocha/Transport/MessageEnvelope.cs` returns nothing — the abstraction is RabbitMQ-specific in the RabbitMQ transport, never lifted to the core. `RabbitMQRoutingKeyExtractor` (a `MessageType` feature) and `RabbitMQRoutingKeyMiddleware` (a dispatch middleware that copies the extracted key into the dispatch headers) are the entire surface, see `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/RabbitMQRoutingKeyExtractor.cs` and `/Users/pascalsenn/kot/hc2/src/Mocha/src/Mocha.Transport.RabbitMQ/Middlewares/Dispatch/RabbitMQRoutingKeyMiddleware.cs`.

2. **No subscription rules at all.** `AzureServiceBusSubscription.ProvisionAsync` (Topology/AzureServiceBusSubscription.cs:104-183) creates the subscription with `CreateSubscriptionOptions` but never attaches a `CreateRuleOptions`. Result: every subscription has the implicit `$Default` rule (`TrueRuleFilter`) and receives every message published to its source topic. `AzureServiceBusSubscriptionConfiguration` and `IAzureServiceBusSubscriptionDescriptor` have no fluent API for filters. The convention in `AzureServiceBusReceiveEndpointTopologyConvention.cs:96-118` provisions one subscription per `(publish-topic, queue)` and `(send-topic, queue)` pair — purely structural, no per-message-type filtering.

So Mocha on ASB today implements the **broadcast** pattern only. The **partitioning** and **routing** patterns from the ASB filter docs are not reachable through the descriptor API.

## ASB filter primitives in depth

Source: <https://learn.microsoft.com/azure/service-bus-messaging/topic-filters>

### 1. CorrelationFilter — the cheap one

Equality-only match against system or user properties. Combined as a logical AND when multiple properties are set. Microsoft is explicit:

> Applications should choose correlation filters over SQL-like filters because they're much more efficient in processing and have less impact on throughput.

System properties usable in a `CorrelationRuleFilter`:

- `CorrelationId`
- `MessageId`
- `Subject` (the new SDK's name for the legacy `Label`)
- `To`
- `ReplyTo`
- `ReplyToSessionId`
- `SessionId`
- `ContentType`

Plus an arbitrary number of user-defined properties via the `Properties` dictionary.

```csharp
// Equality filter on the native CorrelationId (CorrelationFilter convenience ctor)
new CorrelationRuleFilter("high");

// Multi-property AND
var filter = new CorrelationRuleFilter
{
    Subject = "red",          // matches sys.Label = 'red'
    CorrelationId = "high",
    To = "warehouse-eu"
};
filter.Properties["region"] = "eu";       // matches user.region = 'eu'
filter.Properties["priority"] = "high";

// Equivalent SQL: sys.Label='red' AND sys.CorrelationId='high'
//                 AND sys.To='warehouse-eu' AND region='eu' AND priority='high'
```

### 2. SqlRuleFilter — the expressive one

Subset of SQL-92 evaluated against properties (never the body). Supports `=`, `<>`, `<`, `<=`, `>`, `>=`, `AND`, `OR`, `NOT`, `LIKE`, `IN`, `IS NULL`, `EXISTS`, arithmetic, `@parameter` substitution. System properties prefixed with `sys.`, user properties prefixed with `user.` (or unprefixed by default).

```csharp
// LIKE pattern over Subject — closest analogue to a RabbitMQ topic exchange
new SqlRuleFilter("sys.Label LIKE 'order.eu.%'");

// IN with user properties
new SqlRuleFilter("StoreId IN ('Store1','Store2','Store3')");

// Mixed system/user with arithmetic
new SqlRuleFilter("user.color = 'red' AND user.quantity >= 10 AND sys.CorrelationId LIKE 'high-%'");

// Parameterised
var f = new SqlRuleFilter("DateTimeMp < @cutoff");
f.Parameters["@cutoff"] = DateTimeOffset.UtcNow.AddMinutes(-5);
```

### 3. TrueRuleFilter / FalseRuleFilter

Subclass `SqlRuleFilter` with constant `1=1` / `1=0`. `TrueRuleFilter` is the default `$Default` rule on every freshly-created subscription. `FalseRuleFilter` is occasionally useful as a placeholder while you reconfigure rules.

```csharp
new TrueRuleFilter();   // accept everything (default behaviour)
new FalseRuleFilter();  // accept nothing
```

### 4. SqlRuleAction (annotations)

Optional companion to a SQL filter that mutates the message *copy* delivered to that subscription:

```csharp
new CreateRuleOptions("RedOrders")
{
    Filter = new SqlRuleFilter("user.color='red'"),
    Action = new SqlRuleAction("SET quantity = quantity / 2; REMOVE priority; SET sys.CorrelationId = 'low';")
};
```

Important: rules with actions produce one delivered copy *per matching rule*, so they break the "rules without actions OR-combine into one delivery" semantic. Document this loudly if you expose it.

## When to pick which filter

| Scenario                                                                 | Filter                                            |
| ------------------------------------------------------------------------ | ------------------------------------------------- |
| Match exact value of one or more well-known properties                   | `CorrelationFilter` — pick this whenever possible |
| Match a string pattern (wildcard) — RabbitMQ topic-style routing         | `SqlRuleFilter` with `LIKE`                       |
| Range comparisons, numeric conditions, `IN`, `IS NULL`, `EXISTS`         | `SqlRuleFilter`                                   |
| Combine OR semantics across heterogeneous conditions                     | `SqlRuleFilter` (correlation is AND-only)         |
| Mutate the message before it lands in the subscription                   | `SqlRuleFilter` + `SqlRuleAction`                 |
| Receive everything (broadcast)                                           | `TrueRuleFilter` (the default — don't override)   |

A useful heuristic: **if you would write a RabbitMQ direct exchange, use `CorrelationFilter` on `Subject` (the message-type slot). If you would write a RabbitMQ topic exchange with `*`/`#`, use `SqlRuleFilter` with `LIKE '...%'`.**

## Mapping the Mocha "routing key" concept to ASB

The Mocha core envelope has no `RoutingKey` field. The RabbitMQ transport invented one (a per-message-type extractor) because RabbitMQ's wire protocol carries it as a separate field. ASB has no such field, so a literal port doesn't make sense.

What Mocha *should* expose for ASB is:

| Mocha concept                                                                              | ASB primitive                                                                |
| ------------------------------------------------------------------------------------------ | ---------------------------------------------------------------------------- |
| `envelope.MessageType` (already mapped to `Subject`)                                       | Filter on `sys.Label` (the discriminator most users will reach for)          |
| `envelope.CorrelationId`                                                                   | Filter on `sys.CorrelationId` — used today as a generic correlator          |
| User-supplied per-message extractor (analogous to `UseRabbitMQRoutingKey<T>`)              | Set a user property that the subscription rule references                    |
| Static topology binding key (the RabbitMQ `routing_key` argument on `queue.bind`)          | Subscription rule attached at provisioning time                              |

### Recommendation

**Expose ASB filtering at two layers, mirroring how ASB itself splits things.**

1. **Topology layer (the primary mechanism).** Add a fluent API to `IAzureServiceBusSubscriptionDescriptor` that lets the user attach one or more rules. This is where the analogue to RabbitMQ's "binding key" lives — declarative, provisioned at startup, evaluated by the broker.

   ```csharp
   .AddSubscription("orders", "warehouse-eu")
       .WithRule("eu-only", new CorrelationFilter
       {
           Subject = "OrderPlaced",
           Properties = { ["region"] = "eu" }
       });

   .AddSubscription("orders", "warehouse-bulk")
       .WithRule("large-orders", "user.quantity >= 100");   // shorthand -> SqlRuleFilter
   ```

   Implementation notes:
   - Extend `AzureServiceBusSubscriptionConfiguration` with `IList<RuleConfiguration> Rules`.
   - In `AzureServiceBusSubscription.ProvisionAsync`, after `CreateSubscriptionAsync`, if the user supplied custom rules, **delete `$Default`** then call `CreateRuleAsync` for each. ASB rejects creating a rule that already exists, so the loop should mirror the existing `MessagingEntityAlreadyExists` swallow.
   - Default behaviour (no rules) keeps `$Default` and remains broadcast — preserves current semantics.

2. **Message metadata layer (the convenience).** Provide an extractor analogous to `UseRabbitMQRoutingKey<T>` that lets a user populate any of the filterable system properties or arbitrary user properties from a message instance. Don't invent a new envelope field — just write through to existing slots.

   ```csharp
   .AddMessage<OrderPlaced>(d => d
       .UseAzureServiceBusSubject(o => o.OrderType)        // -> sys.Label / Subject
       .UseAzureServiceBusCorrelationId(o => o.CustomerId) // -> sys.CorrelationId
       .UseAzureServiceBusProperty("region", o => o.Region)); // -> user.region
   ```

   Implementation: a small dispatch middleware (mirror of `RabbitMQRoutingKeyMiddleware`) that runs the extractors and sets the corresponding header keys before `AzureServiceBusDispatchEndpoint.CreateMessage` reads them. The `Subject` slot in particular is currently hard-coded to `envelope.MessageType` — the extractor should override it when set.

The split matters: rules are **broker-side** (one rule, many messages), extractors are **client-side** (one message, many properties). Both are required to express anything beyond broadcast, and they compose: an extractor sets `region=eu` on the message, the subscription rule on `warehouse-eu` selects messages where `region='eu'`.

### What about `To` for explicit point-to-point?

ASB's `To` system property is the most literal "routing key" — Microsoft's own samples (`sys.To IN ('Store5','Store6','Store7')`) treat it as an addressing slot. The Mocha envelope has `DestinationAddress` which today goes into the `x-mocha-destination-address` user property. If you want the cheapest possible exact-match filter on destination, mirror `DestinationAddress` into `ServiceBusMessage.To` as well (or move it there entirely) so users can write `new CorrelationFilter { To = "warehouse-eu" }` instead of `new SqlRuleFilter("user.\"x-mocha-destination-address\" = '...'")`. Worth flagging as a follow-up.

## Performance notes (worth surfacing in docs)

From the Microsoft topic-filters page:

- **Filter on properties only — never the body.** Mocha already serialises everything onto application properties or system slots, so this is fine, but it's a hard constraint to call out.
- **Correlation filters are cheaper than SQL filters.** Document the recommendation in the descriptor XML docs so users default to `CorrelationFilter` and only reach for `SqlRuleFilter` when they need `LIKE`, `IN`, `OR`, ranges, or arithmetic.
- **Rule count matters.** Each subscription supports up to 2,000 rules but every additional rule is extra evaluation work per published message. The convention-based topology already creates one subscription per receive-endpoint queue per topic; adding rules on top of that is the right axis. Don't generate one subscription per filter.
- **Default rule.** A freshly-created subscription has `$Default` (a `TrueRuleFilter`). If you add a custom rule without removing `$Default`, the subscription effectively still receives everything OR-combined with your rule.

## Reference URLs

- Topic filters and actions: <https://learn.microsoft.com/azure/service-bus-messaging/topic-filters>
- Set subscription filters (worked examples): <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-filter-examples>
- SQL filter syntax (full grammar): <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-sql-filter>
- SQL rule action syntax: <https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messaging-sql-rule-action>
- `CorrelationRuleFilter` API: <https://learn.microsoft.com/dotnet/api/azure.messaging.servicebus.administration.correlationrulefilter>

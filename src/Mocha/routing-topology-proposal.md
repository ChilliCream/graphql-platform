# Routing Topology and Endpoint Shape: Design Proposal

Status: draft for review. This supersedes the interim `ConsumerBindingMode`-gated
convention suppression currently in the working tree. That interim stays as the shipped
bug fix; this document describes the target design it should evolve into.

## 1. Problem

The reported symptom: a service that declares its own RabbitMQ topology (custom
`events.exchange`, routing keys, explicit queue bindings) and turns on
`BindHandlersExplicitly()` still emits a second, parallel convention topology
(`contracts.order-placed` to `order-placed` to the queue) for the same message types.
The producer and consumer end up pointed at different exchanges.

Four root causes surfaced during analysis:

1. One enum (`ConsumerBindingMode`, set by `BindHandlersExplicitly()`) steers three
   subsystems at once (endpoint discovery, outbound-route fabrication, convention
   topology generation), while a fourth (producer destination naming in
   `RabbitMQMessagingTransport.CreateEndpointConfiguration`) ignores it entirely. That
   asymmetry is why the producer targets a convention exchange the consumer side
   suppressed.

2. Topology-shape generation and endpoint identity are coupled in one class. The receive
   topology convention both materializes the endpoint's queue and builds the message-type
   exchange chains. Removing or replacing the convention strands the queue (the
   "Queue not found" failure that made `RemoveConvention` a footgun).

3. Convention generation is all-or-nothing. Real configuration is mixed: conventions for
   the types you did not think about, hand-wired topology for the ones you did. A
   transport-wide boolean cannot express that.

4. The producer destination and the consume-side topology are computed by two independent
   code paths that happen to call the same naming methods, so they can drift.

## 2. How the field solves it

- MassTransit: `ConfigureConsumeTopology` is a flag per receive endpoint and per message
  type. Explicit `Bind(...)` and convention binds coexist in one specification list, and
  same-name entities are idempotent. Producer and consumer both dereference one shared
  `IMessageTopology` (the consume side even reuses the publish exchange object), so they
  cannot disagree.
- Wolverine: one naming function names both the publish exchange and the listener binding.
  Conventional subscriptions are tagged `IsFromConvention` so explicit rules win.
- NServiceBus: a single pluggable `IRoutingTopology` owns publish, subscribe, send, and
  declare for the whole transport. Provisioning (`EnableInstallers`) is a separate gate.

The common thread: producer and consumer resolve destinations from one source, the axes
are separate knobs, and conventions defer to explicit configuration.

## 3. Principles

1. Separate the axes: consumer binding, topology shape, provisioning, naming.
2. Conventions gap-fill. Explicit configuration always wins.
3. One source of truth for "message type to destination", consulted by both producer and
   consumer, so the asymmetry is impossible by construction.
4. Entities merge by identity with `declared` beating `convention`.
5. The endpoint owns its identity (input queue, error, skip, reply). Topology binds to the
   queue, it never creates it.

## 4. The four concerns

| Concern | Owns | Cardinality | Varies |
| --- | --- | --- | --- |
| Endpoint shape | input queue, error/skip/reply, prefetch, concurrency | per endpoint | stable across routing |
| Routing topology | type to entity, producer destination, type to queue binds | per message type | the pluggable part |
| Provisioning | declaring entities on the broker | per entity | `AutoProvision`, already separate |
| Naming and message-type classification | name formatting, command vs event | global | orthogonal |

Today the convention registry mixes the first two. The proposal pulls them apart.

## 5. Design

### 5.1 The endpoint owns its queue

Move input-queue creation (and error, skip, reply satellites) out of the receive topology
convention and onto the receive endpoint. The queue exists because the endpoint exists.
The routing topology takes the endpoint's queue name as input and binds to it. This
removes the load-bearing coupling and makes the topology strategy safely swappable.

### 5.2 Routing topology as the single, gap-filling source of truth

A per-transport routing topology answers all directional questions from one place:

```csharp
public interface IRabbitMQRoutingTopology
{
    // producer: where does a published or sent message of this type go?
    RabbitMQEndpointAddress ResolveDestination(MessageType type, OutboundRouteKind kind);

    // declare the type-level entities (exchanges), not the endpoint queue
    void DeclarePublish(RabbitMQTopologyBuilder topology, MessageType type);

    // bind the type to a consuming queue that already exists
    void BindConsume(RabbitMQTopologyBuilder topology, MessageType type, RabbitMQQueue endpointQueue);
}
```

Precedence is fixed:

- If the user configured an explicit destination (`AddMessage<T>().Publish/Send(ToExchange(...))`),
  `ResolveDestination` returns it and `DeclarePublish` does nothing for that type. No
  parallel exchange.
- If the user declared an entity (`DeclareExchange`, `DeclareQueue`, `DeclareBinding`),
  that entity is authoritative. The topology only adds what is missing, and where both
  touch the same identity the declared value wins. This is the `MergeFrom` precedence
  already in the working tree (`declared` over `convention`), generalized from duplicate
  binds to the whole model.

Because both the producer path (`CreateEndpointConfiguration`) and the consume path call
the same object, they cannot drift.

### 5.3 Per-scope opt-out for consume topology

The framework cannot reliably infer that a custom exchange plus routing key already covers
a message type (wildcards, multiple keys, fan-in). So the consume-side suppression is
explicit, per endpoint and per message type:

```csharp
t.Endpoint("orderservice.events.queue")
 .Handler<OrderPlacedHandler>()
 .Handler<OrderCancelledHandler>()
 .ConfigureConsumeTopology(false);   // I own the binds on this queue
```

This is the granular replacement for the transport-wide `BindHandlersExplicitly()`. We
deliberately do not infer overlap by routing-key matching: it is order dependent, fragile
under wildcards, and the kind of implicit behavior this whole effort is removing.

### 5.4 Pluggable shape (secondary axis)

The conventional shape (one exchange per type plus the publish-to-send hierarchy) is one
implementation. Direct (a single exchange with routing keys) and custom are others.
Selected with `UseRoutingTopology(...)`. This axis matters less for the reported problem
and can land after 5.1 to 5.3.

### 5.5 Error and skip as middleware

Per the existing TODO in `RabbitMQDefaultReceiveEndpointConvention`, the error and skip
queues become an endpoint interceptor plus middleware rather than topology. Reliability
infrastructure then stays identical regardless of routing choice, and becomes its own
independently pluggable seam.

## 6. User-facing API

```csharp
builder.Services.AddMessageBus().AddRabbitMQ(t =>
{
    // shape strategy (optional, defaults to Conventional)
    t.UseRoutingTopology(RabbitMQRoutingTopology.Conventional);

    // explicit topology is authoritative; conventions fill gaps
    t.DeclareExchange("events.exchange").Type(RabbitMQExchangeType.Topic).Durable();
    t.DeclareQueue("orderservice.events.queue").Durable();
    t.DeclareBinding("events.exchange", "orderservice.events.queue").RoutingKey("event.order.#");

    // I own the consume binds on this queue, do not generate convention chains for it
    t.Endpoint("orderservice.events.queue")
     .Handler<OrderPlacedHandler>()
     .Handler<OrderCancelledHandler>()
     .ConfigureConsumeTopology(false);
})
.AddMessage<OrderPlaced>(d => d
    .Publish(r => r.ToExchange("events.exchange"))     // explicit producer destination wins
    .UseRabbitMQRoutingKey(_ => "event.order.placed"));
```

## 7. Relationship to `ConsumerBindingMode` and the current diff

- The current diff gates all convention generation on `ConsumerBindingMode.Explicit`. That
  is the coarse, all-or-nothing interim, and it ships as the bug fix.
- This design replaces that gate with granular gap-filling plus per-scope opt-out.
- `BindHandlersExplicitly()` and `ConsumerBindingMode` are deprecated in favor of the two
  concerns they currently conflate: explicit endpoint declaration (consumer binding) and
  `ConfigureConsumeTopology(false)` (topology suppression).
- The diff's `declared` vs `convention` provenance and the `MergeFrom` precedence are the
  foundation this builds on, not throwaway work.

## 8. Cross-transport

The RabbitMQ and Postgres receive topology conventions are structurally identical (publish
entity, send entity, bind or subscription to the queue), differing only in primitive names
(exchange and bind vs topic and subscription). The routing-topology algorithm is therefore
transport agnostic, parameterized by a small set of entity factories. InMemory is
degenerate (no broker, binds are in process).

Decision required: a shared routing-topology algorithm with per-transport entity
primitives, or a per-transport interface. Recommendation: shared algorithm, per-transport
primitives, since the algorithm is provably identical across the two broker transports
today.

## 9. Phasing

- Phase 0 (done, in the working tree): the bug fix. Ship as is.
- Phase 1: endpoint owns its queue, relocate error and skip. Fixes the coupling, no public
  API change. Independently valuable.
- Phase 2: the routing-topology seam, gap-filling precedence, and
  `ConfigureConsumeTopology` opt-out. Deprecate `ConsumerBindingMode`. This is the core and
  carries the public API change.
- Phase 3 (optional): pluggable shape (direct, custom) and error/skip as middleware.

## 10. Non-goals

- No destination throw and no `ExplicitRouteEnforcementFeature`. Producers that publish an
  unconfigured type fail loudly at the broker under `AutoProvision(false)`, which is the
  honest consequence of that contract. Reply is dynamic and was never in scope.
- No inference of "this custom binding covers type T". Explicit opt-out only.

## 11. Open questions and risks

- Per-transport versus shared routing-topology abstraction (the Phase 2 crux).
- Validate the seam against sagas, request and reply, and batch consumers before
  committing across transports. A RabbitMQ-only prototype should derisk this first.
- Deprecation path for `BindHandlersExplicitly()`: keep it as an alias that maps to the new
  opt-outs, or hard break while still prerelease.
- Reply addressing is already correct (resolved by address through the reply dispatch
  endpoint) and must remain untouched by this work.

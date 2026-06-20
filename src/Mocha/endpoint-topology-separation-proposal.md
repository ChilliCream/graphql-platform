# Endpoint / Topology Separation: `Queue` as a Flat Composition Descriptor

## Problem statement

The current PR (`pse/rework-rabbitmq-explicit-binding`, commit `c2fd5e7145`) introduced a "unified queue endpoint" that conflates two concerns that must stay strictly separate. `Declare*` is **infrastructure**: it declares broker entities and their physical shape (exchanges, queues, bindings, durability, quorum, arguments, provisioning). The receive endpoint is **routing**: which handlers and consumers run, which message types are received, auto-bind policy, concurrency, receive middleware. Today the receive descriptor (`IRabbitMQReceiveEndpointDescriptor`) also carries infrastructure (`Queue(name)`, `ErrorQueue`/`SkippedQueue`), a new `IRabbitMQQueueEndpointDescriptor` piles even more infrastructure onto routing (`Durable`/`Quorum`/`WithArgument`/`AutoProvision`, all duplicating `DeclareQueue`), and the transport descriptor exposes both `DeclareQueue(name)` (infra) and a "unified" `Queue(name)` that merges an endpoint's identity with its queue declaration by string-equality on the queue name.

The user wants to keep `Queue` as the front door, but `Queue` must be **its own descriptor that configures BOTH the queue infrastructure AND the receive endpoint**, on one flat surface, by **composing** two independently-typed descriptors rather than inheriting from either.

> This document supersedes the earlier `ConsumeFrom` draft. Per the user's decision, the design is no longer "split `Queue` into a standalone `ConsumeFrom` link"; instead `Queue` stays the terse front door and becomes a flat composition descriptor over a pure infra descriptor and a pure routing descriptor. Only the prior draft's analysis (the conflation map, the silent-data-loss bug, the cross-transport facts, and the migration-churn reality) is carried forward; its entire `ConsumeFrom` design is replaced.

## The conflation today

Grounded in the code at commit `c2fd5e7145`:

- **`IRabbitMQReceiveEndpointDescriptor` (routing) carries infrastructure members.** `Queue(string name)` (`:84`) sets the backing queue identity, and `ErrorQueue(string)`/`DisableErrorQueue()`/`SkippedQueue(string)`/`DisableSkippedQueue()` declare satellite queues. File: `src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQReceiveEndpointDescriptor.cs`.
- **`IRabbitMQQueueEndpointDescriptor : IRabbitMQReceiveEndpointDescriptor` duplicates `DeclareQueue`.** `Durable(bool)`, `Quorum()`, `WithArgument(string, object)`, `AutoProvision(bool)` are pure queue shape on a routing-derived surface (this `: IRabbitMQReceiveEndpointDescriptor` inheritance is the structural mistake). `Queue(string)` is marked obsolete-error. File: `src/Mocha.Transport.RabbitMQ/Descriptors/IRabbitMQQueueEndpointDescriptor.cs`. Its impl `RabbitMQQueueEndpointDescriptor` wraps a `RabbitMQReceiveEndpointDescriptor _inner` and writes infra into `_inner.Configuration.QueueDurable` (`:179`), `_inner.Configuration.QueueArguments` (`:191`), `_inner.Configuration.QueueAutoProvision` (`:200`), the wrong home.
- **The transport descriptor has two entry points for one entity.** `t.DeclareQueue(name)` (infra) at `RabbitMQMessagingTransportDescriptor.cs:201` and `t.Queue(name)` at `:221`. `Queue(name)` caches a `RabbitMQQueueEndpointDescriptor` in `_queueEndpoints` (dict at `:15-16`), then searches `_receiveEndpoints` by `QueueName.EqualsOrdinal(name)` (`:230-231`); `Endpoint(name)` matches by `Name OR QueueName` (`:162-163`). Two surfaces both write the queue name and merge by string-equality, which is the drift vector.
- **`RabbitMQReceiveEndpointConfiguration` is the dumpsite.** It mixes routing (inherited) with infrastructure: `QueueName` (`:11`), `MaxPrefetch` (`:17`), `ErrorQueue`/`SkippedQueue` satellite configs (`:22`/`:27`), `QueueDurable` (`:32`), `QueueAutoProvision` (`:37`), `QueueArguments` (`:42`). File: `src/Mocha.Transport.RabbitMQ/Configurations/RabbitMQReceiveEndpointConfiguration.cs`.
- **There is a confirmed silent-data-loss bug.** `RabbitMQReceiveEndpoint.OnDiscoverTopology` (`:49-56`) builds the `RabbitMQQueueConfiguration` from `Name`, `AutoDelete`, `AutoProvision`, `Provenance` only. `QueueDurable` and `QueueArguments`, settable via the endpoint/unified surface, are never passed to `AddQueue`, so durability/args set through the endpoint are silently dropped. File: `src/Mocha.Transport.RabbitMQ/RabbitMQReceiveEndpoint.cs`.
- **The infra descriptor is already pure.** `IRabbitMQQueueDescriptor` (returned by `DeclareQueue`) carries only `Name`/`Durable`/`Exclusive`/`AutoDelete`/`WithArgument`/`AutoProvision`, no routing. File: `src/Mocha.Transport.RabbitMQ/Topology/Descriptors/IRabbitMQQueueDescriptor.cs`. Its lowering target `RabbitMQQueueConfiguration` is created with `Provenance = Declared` (`RabbitMQQueueDescriptor.cs:17`).

## Principles

1. **`Declare*` = infrastructure, exclusively.** Exchanges, queues, bindings, and all physical shape (durable, quorum, arguments, exclusive, auto-delete, provisioning) live only on `IRabbitMQQueueDescriptor`/`IRabbitMQExchangeDescriptor`/`IRabbitMQBindingDescriptor`. No consumer/handler/`Receives` is ever added to them.
2. **The receive endpoint = routing, exclusively.** Handlers, consumers, received types, auto-bind policy, per-type binds, explicit `BindFrom`, `Kind`, `MaxConcurrency`, `MaxPrefetch` (consumer-channel QoS, not broker shape), `FaultEndpoint`/`SkippedEndpoint` (which endpoint receives faults), receive middleware. No queue name setter, no satellite-queue shape, no broker shape.
3. **`Queue` is its own composition descriptor.** `t.Queue(name)` returns a new flat type that is **not** a subtype of the receive-endpoint descriptor and **not** a subtype of an infra `Declare*` descriptor. It presents a flat, terse surface (infra and routing methods directly on it) but internally **composes** two separate, independently-typed descriptors and delegates each flat method to exactly one of them. Composition, not inheritance.
4. **One identity per name, no string-match drift.** `Queue(name)`, `Endpoint(name)`, and `DeclareQueue(name)` converge on the same two store-owned objects, each deduplicated by `Configuration.Name` only. The `QueueName.EqualsOrdinal(name)` match arm of `Endpoint` and the `_queueEndpoints` cache are deleted. There is no second writer of the queue name, so nothing can drift.
5. **Conventions gap-fill, declared wins.** Merge-by-identity precedence `convention < endpoint < declared` is preserved exactly (`RabbitMQMessagingTopology.AddQueue` + `RabbitMQQueue.MergeFrom`). The 90% case stays a one-liner.

## The Queue composition descriptor

### Name and collision resolution (Q1)

`IRabbitMQQueueDescriptor` is already the **infra** descriptor returned by `t.DeclareQueue(name)`. It is **kept exactly as-is**, not renamed, not widened, not shared. The new flat composition type returned by `t.Queue(name)` is named `IRabbitMQQueueBuilder`.

The `-Builder` suffix is deliberate. In this codebase a `*Descriptor` produces exactly one `*Configuration` via `IMessagingDescriptor<TConfiguration>`. The flat `Queue` surface produces **two** configurations (a `RabbitMQQueueConfiguration` and a `RabbitMQReceiveEndpointConfiguration`), so it is categorically not a single-configuration descriptor and must **not** implement `IMessagingDescriptor<T>`. Keeping it out of the descriptor type family is what makes Principle 3 (not a subtype of either descriptor) enforced by the compiler rather than by convention. The suffix is the lower-priority signal; the load-bearing guarantee is "does not implement `IMessagingDescriptor<T>` and does not derive from either descriptor."

The PR's `IRabbitMQQueueEndpointDescriptor` / `RabbitMQQueueEndpointDescriptor` are **deleted**. The `-EndpointDescriptor` name was the lie ("it is an endpoint"; it is a builder over an endpoint and a queue).

| Door | Returns | Concern | Family |
| --- | --- | --- | --- |
| `t.DeclareQueue(name)` | `IRabbitMQQueueDescriptor` (unchanged) | infra only | `IMessagingDescriptor<RabbitMQQueueConfiguration>` |
| `t.Endpoint(name)` | `IRabbitMQReceiveEndpointDescriptor` (slimmed) | routing only | `IMessagingDescriptor<RabbitMQReceiveEndpointConfiguration>` |
| `t.Queue(name)` | **`IRabbitMQQueueBuilder` (new)** | flat infra + routing | **none, a builder, not a descriptor** |

### The flat surface

Every member returns `IRabbitMQQueueBuilder`, so the infra and routing groups interleave on one chain and the builder never leaks either composed type.

```csharp
namespace Mocha.Transport.RabbitMQ;

/// <summary>
/// Flat builder returned by <c>t.Queue(name)</c>. Configures both the queue's broker shape and the
/// endpoint that consumes it. Composes (does not inherit) a queue infrastructure descriptor and a
/// receive endpoint descriptor; each flat method delegates to exactly one of the two.
/// </summary>
public interface IRabbitMQQueueBuilder
{
    // ---- infrastructure group: delegates to the queue descriptor (the queue topology entity) ----
    IRabbitMQQueueBuilder Durable(bool durable = true);
    IRabbitMQQueueBuilder Quorum();
    IRabbitMQQueueBuilder Exclusive(bool exclusive = true);
    IRabbitMQQueueBuilder AutoDelete(bool autoDelete = true);
    IRabbitMQQueueBuilder WithArgument(string key, object value);
    IRabbitMQQueueBuilder AutoProvision(bool autoProvision = true);

    // ---- routing group: delegates to the receive endpoint descriptor ----
    IRabbitMQQueueBuilder Handler<THandler>() where THandler : class, IHandler;
    IRabbitMQQueueBuilder Handler(Type handlerType);
    IRabbitMQQueueBuilder Consumer<TConsumer>() where TConsumer : class, IConsumer;
    IRabbitMQQueueBuilder Consumer(Type consumerType);
    IRabbitMQQueueBuilder Receives<TMessage>();
    IRabbitMQQueueBuilder Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);
    IRabbitMQQueueBuilder Receives(Type messageType);
    IRabbitMQQueueBuilder AutoBind(bool enabled);
    IRabbitMQQueueBuilder BindFrom(Uri source, string? routingKey = null);
    IRabbitMQQueueBuilder Kind(ReceiveEndpointKind kind);
    IRabbitMQQueueBuilder MaxConcurrency(int maxConcurrency);
    IRabbitMQQueueBuilder MaxPrefetch(ushort maxPrefetch);       // consumer QoS, on the receive config
    IRabbitMQQueueBuilder FaultEndpoint(string name);            // routing target for faults
    IRabbitMQQueueBuilder SkippedEndpoint(string name);          // routing target for skips
    IRabbitMQQueueBuilder UseReceive(ReceiveMiddlewareConfiguration configuration,
        string? before = null, string? after = null);

}
```

What is intentionally **absent** from the routing group: `Queue(string)` (you do not rename the queue you are building) and the verbatim satellite-queue setters `ErrorQueue(string)`/`SkippedQueue(string)` (those name and shape a satellite queue, infra). `FaultEndpoint`/`SkippedEndpoint` remain because they pick which endpoint receives faults/skips, routing.

### The class skeleton (composition by reference)

The builder holds the two composed descriptors as its only fields and owns no configuration of its own.

```csharp
namespace Mocha.Transport.RabbitMQ;

internal sealed class RabbitMQQueueBuilder : IRabbitMQQueueBuilder
{
    private readonly IRabbitMQQueueDescriptor _queue;             // infra   == t.DeclareQueue(name)
    private readonly IRabbitMQReceiveEndpointDescriptor _endpoint; // routing == t.Endpoint(name)

    internal RabbitMQQueueBuilder(
        IRabbitMQQueueDescriptor queue,
        IRabbitMQReceiveEndpointDescriptor endpoint)
    {
        _queue = queue;
        _endpoint = endpoint;
    }

    // infra group -> _queue (writes RabbitMQQueueConfiguration, the SAME entity DeclareQueue writes)
    public IRabbitMQQueueBuilder Durable(bool d = true)          { _queue.Durable(d); return this; }
    public IRabbitMQQueueBuilder Quorum()                        { _queue.WithArgument("x-queue-type", RabbitMQQueueType.Quorum); return this; }
    public IRabbitMQQueueBuilder Exclusive(bool e = true)        { _queue.Exclusive(e); return this; }
    public IRabbitMQQueueBuilder AutoDelete(bool a = true)       { _queue.AutoDelete(a); return this; }
    public IRabbitMQQueueBuilder WithArgument(string k, object v){ _queue.WithArgument(k, v); return this; }
    public IRabbitMQQueueBuilder AutoProvision(bool a = true)    { _queue.AutoProvision(a); return this; }

    // routing group -> _endpoint (writes RabbitMQReceiveEndpointConfiguration, the SAME config Endpoint writes)
    public IRabbitMQQueueBuilder Consumer<TC>() where TC : class, IConsumer { _endpoint.Consumer<TC>(); return this; }
    public IRabbitMQQueueBuilder Handler<TH>()  where TH : class, IHandler  { _endpoint.Handler<TH>();  return this; }
    public IRabbitMQQueueBuilder Receives<TM>()                             { _endpoint.Receives<TM>(); return this; }
    public IRabbitMQQueueBuilder AutoBind(bool e)                           { _endpoint.AutoBind(e);    return this; }
    public IRabbitMQQueueBuilder BindFrom(Uri s, string? rk = null)         { _endpoint.BindFrom(s, rk); return this; }
    public IRabbitMQQueueBuilder Kind(ReceiveEndpointKind k)                { _endpoint.Kind(k);        return this; }
    public IRabbitMQQueueBuilder MaxConcurrency(int n)                      { _endpoint.MaxConcurrency(n); return this; }
    public IRabbitMQQueueBuilder MaxPrefetch(ushort n)                      { _endpoint.MaxPrefetch(n); return this; }
    public IRabbitMQQueueBuilder FaultEndpoint(string n)                    { _endpoint.FaultEndpoint(n); return this; }
    public IRabbitMQQueueBuilder SkippedEndpoint(string n)                  { _endpoint.SkippedEndpoint(n); return this; }
    // ... remaining routing overloads identical ...

}
```

### Exact delegation per member group

| Flat method group | Delegates to | Writes |
| --- | --- | --- |
| `Durable` `Exclusive` `AutoDelete` `WithArgument` `AutoProvision` `Quorum` | `_queue` (`IRabbitMQQueueDescriptor`) | `RabbitMQQueueConfiguration` (the topology queue entity) |
| `Handler` `Consumer` `Receives` `AutoBind` `BindFrom` `Kind` `MaxConcurrency` `MaxPrefetch` `FaultEndpoint` `SkippedEndpoint` `UseReceive` | `_endpoint` (`IRabbitMQReceiveEndpointDescriptor`) | `RabbitMQReceiveEndpointConfiguration` (routing only) |

`Quorum()` is on the infra path and writes `x-queue-type=quorum` into the **queue config's** `Arguments` (where `DeclareQueue().WithArgument` writes), not into the PR's `_inner.Configuration.QueueArguments` on the receive config. That single redirection fixes the silent-data-loss bug structurally. (Honesty note: `Quorum()` is the one infra method that is not a pure pass-through to an identically named member, it composes a single `_queue.WithArgument` call. It still touches only `_queue`, so the purity invariant "infra-group methods write only `_queue`" holds. Optionally `Quorum()` can be added to `IRabbitMQQueueDescriptor` so `DeclareQueue` and the builder share it; either way is acceptable.)

## Relationship to Endpoint and DeclareQueue

**All three survive**, with distinct roles:

- `t.DeclareQueue(name)` declares a queue with no consumer: producer-only destination, satellite-queue shape, a queue another endpoint binds to.
- `t.Endpoint(name)` is pure routing for an endpoint whose queue is default-shaped or declared elsewhere, including the rare case where the endpoint name differs from the queue name.
- `t.Queue(name)` is the flat front door for "shape this queue and consume it here", the 90% case.

They converge on **one identity per name with no string-match**, because `Queue(name)` is implemented in terms of the other two lookups, not as a third parallel store:

```csharp
public IRabbitMQQueueBuilder Queue(string name)
    => new RabbitMQQueueBuilder(DeclareQueue(name), Endpoint(name));
```

- `DeclareQueue(name)` already dedups by `Configuration.Name.EqualsOrdinal(name)` over `_queues` (`:203`).
- `Endpoint(name)` dedups over `_receiveEndpoints` by `Configuration.Name` **after we delete its second arm**. Today `Endpoint` matches `Name OR QueueName` (`:162-163`); the `|| QueueName.EqualsOrdinal(name)` arm is the drift vector and is **deleted**.

The single-identity rule, stated precisely (this replaces `QueueName.EqualsOrdinal`):

> An endpoint is identified solely by `Configuration.Name`. A queue entity is identified solely by `Configuration.Name`. The endpoint produced by `Endpoint(n)` consumes the queue named `n` by default. `QueueName` defaults to the endpoint name **at descriptor construction** (`RabbitMQReceiveEndpointDescriptor.cs:15`, `new RabbitMQReceiveEndpointConfiguration { Name = name, QueueName = name }`) and is never reassigned through the public surface, because the only public reassignment path (`Queue(string)` on the routing descriptor) is deleted. There is no `QueueName`-based lookup anywhere in the descriptor layer.

Convergence consequences:

- `t.Queue("orders").Consumer<C>()` then `t.DeclareQueue("orders").Quorum()`: `DeclareQueue("orders")` returns the **same** `_queues` entry the builder already grabbed; `Quorum()` and the consumer accumulate on one queue and one endpoint.
- `t.Endpoint("orders").Consumer<C>()` then `t.Queue("orders").Durable()`: `Queue` calls `Endpoint("orders")`, which finds the **same** existing endpoint by `Name`; `Durable()` goes to `DeclareQueue("orders")`. One endpoint, one queue.
- The `_queueEndpoints` dict (`:15-16`) is **deleted**; there is no separate cache to drift from the real stores.

The builder constructor is `internal` and is only ever invoked from `t.Queue(name)` inside the transport descriptor, so the two composed descriptors are always the store-owned instances.

### The differing-name case (endpoint name != queue name)

A small but real set of tests and the framework reply path use an endpoint whose logical name differs from its physical queue (for example `t.Endpoint("primary").Queue("orders-primary")`, and the reply endpoint constructed with `Name="Replies"`, `QueueName=instanceEndpointName`). This is **routing identity** (which queue this endpoint consumes), not queue shape, so it is legitimate to keep, but it must have exactly one home.

Resolution: `QueueName` stays a separate field on `RabbitMQReceiveEndpointConfiguration` (it is **not** collapsed into `Name`), so identity-by-`Name` still kills the drift vector while the differing-name capability survives. The user-facing door for it is the slimmed routing descriptor obtained via `t.Endpoint(name)`, which retains a single narrow consume-from-name override (kept as `ConsumeFrom(string queueName)` on `IRabbitMQReceiveEndpointDescriptor`, routing-only: it sets `QueueName`, never shape). The flat `Queue(name)` front door is the same-name 90% door and does not expose this override. The framework reply path sets `QueueName` directly at the config layer (`RabbitMQMessagingTransport.CreateEndpointConfiguration`), bypassing the descriptor surface entirely, so it is unaffected by deleting the public `Queue(string)` setter.

This is the one capability the prior `ConsumeFrom` draft and the three candidate designs all wrestled with; see Open risks #1 for the residual naming question on the override verb.

## Purity proof

### Receive endpoint, after deletion: pure routing

`IRabbitMQReceiveEndpointDescriptor` loses `Queue(string)` (`:84`), `ErrorQueue(string)` (`:45`), `DisableErrorQueue()` (`:52`), `SkippedQueue(string)` (`:60`), `DisableSkippedQueue()` (`:67`). It keeps handlers/consumers/`Receives`/`AutoBind`/`BindFrom`/`Kind`/`MaxConcurrency`/`MaxPrefetch`/`FaultEndpoint`/`SkippedEndpoint`/`UseReceive`, plus the narrow `ConsumeFrom(string)` queue-name override (routing identity, see above).

```csharp
public interface IRabbitMQReceiveEndpointDescriptor
    : IReceiveEndpointDescriptor<RabbitMQReceiveEndpointConfiguration>
{
    new IRabbitMQReceiveEndpointDescriptor Handler<THandler>() where THandler : class, IHandler;
    new IRabbitMQReceiveEndpointDescriptor Consumer<TConsumer>() where TConsumer : class, IConsumer;
    new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>();
    new IRabbitMQReceiveEndpointDescriptor Receives<TMessage>(Action<IReceiveTypeBindDescriptor> configure);
    new IRabbitMQReceiveEndpointDescriptor AutoBind(bool enabled);
    new IRabbitMQReceiveEndpointDescriptor BindFrom(Uri source, string? routingKey = null);
    new IRabbitMQReceiveEndpointDescriptor Kind(ReceiveEndpointKind kind);
    new IRabbitMQReceiveEndpointDescriptor MaxConcurrency(int maxConcurrency);
    IRabbitMQReceiveEndpointDescriptor MaxPrefetch(ushort maxPrefetch);      // consumer QoS, kept
    new IRabbitMQReceiveEndpointDescriptor FaultEndpoint(string name);       // routing target, kept
    new IRabbitMQReceiveEndpointDescriptor SkippedEndpoint(string name);     // routing target, kept
    IRabbitMQReceiveEndpointDescriptor ConsumeFrom(string queueName);        // routing identity (QueueName), no shape
    new IRabbitMQReceiveEndpointDescriptor UseReceive(/* ... */);
    // NO Durable/Quorum/WithArgument/AutoProvision/Exclusive/AutoDelete. NO ErrorQueue/SkippedQueue shape.
}
```

It carries **no** queue shape. `t.Endpoint("orders").Consumer<C>()` compiles and works standalone (consumes the queue named `orders`, gap-filled if undeclared).

### Infra queue descriptor: already pure, unchanged

`IRabbitMQQueueDescriptor` carries only `Name`/`Durable`/`Exclusive`/`AutoDelete`/`WithArgument`/`AutoProvision`. No `Consumer`, no `Handler`, no `Receives`. `t.DeclareQueue("orders").Quorum().Durable()` compiles and works standalone (a declared queue, no consumer).

### The builder flattens both but composes, not merges

Proof by reachability. `RabbitMQQueueBuilder` has exactly two fields, `_queue : IRabbitMQQueueDescriptor` and `_endpoint : IRabbitMQReceiveEndpointDescriptor`. The infra group can only reach `_queue` (no routing method exists on `IRabbitMQQueueDescriptor` to call); the routing group can only reach `_endpoint` (no shape method exists on `IRabbitMQReceiveEndpointDescriptor` to call). Neither composed descriptor gains a member of the other's concern. Re-conflation is impossible because the type that would have to absorb both concerns does not exist, only two pure descriptors and a stateless delegator over them.

This is the categorical difference from the PR's `RabbitMQQueueEndpointDescriptor`, which **is-a** receive endpoint and must store infra somewhere reachable, namely `_inner.Configuration.QueueDurable`/`QueueArguments` (the wrong second home that `OnDiscoverTopology` drops). The builder **has-a** infra descriptor and writes `RabbitMQQueueConfiguration`, the home the topology already reads.

## Conceptual model

| Concern | Owning type | Members | Cardinality |
| --- | --- | --- | --- |
| Queue infrastructure | `IRabbitMQQueueDescriptor` via `t.DeclareQueue(name)` | `Durable`, `Quorum`, `WithArgument`, `AutoProvision`, `Exclusive`, `AutoDelete` | 1 per queue name; merge-by-identity |
| Exchange infrastructure | `IRabbitMQExchangeDescriptor` via `t.DeclareExchange(name)` | `Type`, `Durable`, `WithArgument`, `AutoProvision` | 1 per exchange name |
| Binding infrastructure | `IRabbitMQBindingDescriptor` via `t.DeclareBinding(exchange, queue)` | `RoutingKey`, `WithArgument`, `AutoProvision` | N per call; dedup by (source, key, args, dest) |
| Routing / consumption | `IRabbitMQReceiveEndpointDescriptor` via `t.Endpoint(name)` | `Handler<T>`, `Consumer<T>`, `Receives<T>`, `AutoBind`, `BindFrom`, `Kind`, `MaxConcurrency`, `MaxPrefetch`, `FaultEndpoint`, `SkippedEndpoint`, `ConsumeFrom`, `UseReceive` | 1 per endpoint name |
| **Flat front door (NEW)** | **`IRabbitMQQueueBuilder` via `t.Queue(name)`** | infra group + routing group (flat) | stateless facade; 0 own configs |
| Consumer QoS (runtime, not topology) | `RabbitMQReceiveEndpointConfiguration` (slimmed) | `MaxPrefetch` (RMQ), `MaxBatchSize` (PG) | 1 per endpoint |
| Name minting / drift guard | `RabbitMQDestinationResolver` (unchanged) | `ResolveDestination`, `ResolvePublishDestination`, `TryResolveSourceExchange` | singleton per transport |

The `IRabbitMQQueueEndpointDescriptor`/`IPostgresQueueEndpointDescriptor`/`IInMemoryQueueEndpointDescriptor` tier is deleted; `t.Queue(name)` now returns the builder.

## Fluent API

### (a) Common-case one-liner, RabbitMQ

Default-shaped queue, one consumer; the queue is gap-filled at discovery (`Provenance=Endpoint`).

```csharp
t.Queue("orders").Consumer<OrderConsumer>();
// still valid, unchanged:
t.Endpoint("orders").Consumer<OrderConsumer>();
t.Consumer<OrderConsumer>();   // name derived from the consumer
```

### (b) Durable/quorum queue + consumer + MaxPrefetch, RabbitMQ

The user's chosen shape, infra and routing interleaved on one flat chain.

```csharp
t.Queue("orders")
    .Durable().Quorum()
    .WithArgument("x-max-length", 50_000)
    .Consumer<OrderConsumer>()
    .MaxPrefetch(64);
```

### (c) Custom exchange + explicit binding + suppressed convention binds across two queues, RabbitMQ

Exchange and bindings are pure infra via `DeclareExchange`/`DeclareBinding`; per-queue convention suppression is `AutoBind(false)` on the builder's routing group; per-type suppression is `Receives<T>(r => r.AutoBind(false))`.

```csharp
t.DeclareExchange("orders.fanout").Type(RabbitMQExchangeType.Fanout).Durable();
t.DeclareBinding("orders.fanout", "orders.audit");
t.DeclareBinding("orders.fanout", "orders.ship");

t.Queue("orders.audit")
    .Durable()
    .AutoBind(false)                  // this endpoint owns its binds
    .Consumer<AuditConsumer>();

t.Queue("orders.ship")
    .Durable()
    .Consumer<ShipConsumer>()
    .Receives<OrderShipped>(r => r.AutoBind(false));   // suppress just this type
```

### (b) Postgres variant: declared queue case

Postgres queue infra is `AutoProvision` only (no `Durable`/`Quorum`), so those are absent from `IPostgresQueueBuilder`; the consumer QoS analog is `MaxBatchSize`.

```csharp
t.Queue("orders")
    .AutoProvision()
    .Consumer<OrderConsumer>()
    .MaxBatchSize(200);
```

### (b) InMemory variant: declared queue case

InMemory's `IInMemoryQueueDescriptor` exposes only `Name`, so `IInMemoryQueueBuilder` has an **empty infra group**, no `Durable`/`Quorum`/`WithArgument` to call (degeneracy via absent members, not present no-ops). The routing group is fully real.

```csharp
t.Queue("orders")
    .Consumer<OrderConsumer>()
    .MaxConcurrency(8);
```

## Satellites, default queue, single-source-of-truth

### Satellites (error / skip / reply)

Split cleanly by the routing-vs-infra boundary, no satellite-queue-shape member on either the routing descriptor or the builder's routing group:

- **Which endpoint receives faults/skips is routing.** It stays as `FaultEndpoint(name)`/`SkippedEndpoint(name)` on the receive endpoint (and on the builder's routing group). These write base `ReceiveEndpointConfiguration` `Uri?` routing addresses (`FaultEndpoint`/`SkippedEndpoint` are addresses, not queue names), which is genuinely routing.
- **The satellite queue's existence/name/shape is infra.** It is declared with `t.DeclareQueue(satelliteName).Durable()` like any queue. When undeclared, the convention still synthesizes a `Provenance=Endpoint` satellite queue by convention name, inheriting `AutoProvision` from the parent queue entity. A declared satellite queue wins the merge by identity.
- **Deleted:** the verbatim-rename setters `ErrorQueue(string)`/`SkippedQueue(string)` and `Disable*` are removed from the routing descriptor (R3), and `RabbitMQReceiveEndpointConfiguration.ErrorQueue`/`SkippedQueue` (the `RabbitMQSatelliteConfiguration` objects, `:22`/`:27`) are removed. A non-convention satellite name is expressed by naming the routing target (`FaultEndpoint("LEGACY.Orders.Error")`) plus `DeclareQueue("LEGACY.Orders.Error")` for its shape. Disabling faults is `DisableFault()` on the core routing surface (one place), not a queue-shape setter.
- **Satellite name derivation moves into the convention.** The default name derivation (`GetReceiveEndpointName(queueName, kind)`) relocates into `RabbitMQDefaultReceiveEndpointConvention`, keyed off the routing endpoint name rather than the deleted config object. It synthesizes the satellite queue entity in `topology.Queues` at that point (`Provenance=Endpoint`, `AutoProvision` inherited).
- **Runtime auto-provision recovery is re-pointed at the topology.** `RabbitMQMessagingTransport.TryGetSatelliteAutoProvision` parses the satellite queue name out of the `FaultEndpoint`/`SkippedEndpoint` Uri (it already Uri-matches at `:529-534`) and reads `AutoProvision` from the synthesized `topology.Queues` entry, not from the deleted config object.
- **Reply is untouched.** Reply addressing stays address-routed; `Kind(Reply)` and the auto-delete reply queue are unchanged. The framework reply endpoint (`Name != QueueName`) is constructed directly at the config layer, bypassing the descriptor surface.

### Default queue synthesis and ordering

The existing mechanism is unchanged. `RabbitMQReceiveEndpoint.OnDiscoverTopology` gap-fills a `Provenance=Endpoint` queue named `configuration.QueueName` when no declared queue exists; declared always wins the `MergeFrom`. The builder does not special-case this: `t.Queue("orders")` eagerly creates the `_queues` entry via `DeclareQueue("orders")`, so the gap-fill merges onto it; `t.Endpoint("orders")` alone lets the gap-fill create it. Same entity either way.

Build order in `CreateConfiguration`: (1) lower `_queues` to the topology (declared shape, `Provenance=Declared`); (2) endpoints discover and gap-fill/merge by name; (3) the topology convention binds exchanges into the now-existing queues. Queues exist before bindings reference them, matching current ordering.

The silent-data-loss bug disappears by construction: `QueueDurable`/`QueueAutoProvision`/`QueueArguments` (`:32`/`:37`/`:42`) are deleted from the receive config, so `OnDiscoverTopology` has nothing to drop. Durability and arguments live only on the `RabbitMQQueueConfiguration` that `DeclareQueue`/the builder's infra group writes and that the topology already reads.

### Single-source-of-truth (producer vs consumer)

`RabbitMQDestinationResolver` stays the sole minter of "message type -> exchange/destination" for producers and the consume convention. The consumer's input queue is the endpoint's `QueueName` (defaulting to `Name`), written once at construction and never reassigned through the public surface (the `Queue(string)` setter is deleted). `OnComplete` still looks up `topology.Queues` by that name (`:126-130`, mechanism unchanged). Because `Queue("orders")` and `DeclareQueue("orders")` are reference-identical entities, a producer targeting `queue:orders` and this consumer converge on one entity name with no copy. `MaxPrefetch` stays on the receive config (consumer QoS), never touching `_queue`.

## What changes in the current PR

| Action | Target (file : member) | Detail |
| --- | --- | --- |
| DELETE | `Descriptors/IRabbitMQQueueEndpointDescriptor.cs` (whole file) | The `: IRabbitMQReceiveEndpointDescriptor` facade with infra piled on. Replaced by `IRabbitMQQueueBuilder`. |
| DELETE | `Descriptors/RabbitMQQueueEndpointDescriptor.cs` (whole file) | The adapter writing infra to `_inner.Configuration.QueueDurable`/`QueueArguments` (`:179`/`:191`/`:200`), the wrong-home bug source. |
| DELETE | `RabbitMQMessagingTransportDescriptor.cs` : `_queueEndpoints` dict (`:15-16`), `Queue(string)` body (`:221-242`) incl. the `QueueName.EqualsOrdinal` backing search (`:230-231`), `Queue(string, Action)` body (`:245`) re-pointed to the builder | Adapter cache and string-match writer removed. |
| DELETE | `RabbitMQMessagingTransportDescriptor.cs:163` : the `\|\| ...QueueName.EqualsOrdinal(name)` arm of `Endpoint(name)` | Endpoints identified by `Name` only, kills the drift vector. |
| DELETE | `RabbitMQMessagingTransportDescriptor.cs` : `IsEntityOnly`/`LowerEntityOnlyQueue` (`:300-367`) and the `SatelliteRequiresConsumingEndpoint` guard (`:316-323`) | A dispatch-only queue is now plain `DeclareQueue` + `DeclareBinding`. The "satellite on a non-consumed queue" state is now unrepresentable (satellites attach to `FaultEndpoint`/`SkippedEndpoint` on a routing endpoint; a bare `DeclareQueue` exposes no satellite surface), so the guard becomes impossible-by-construction. |
| DELETE | `IRabbitMQReceiveEndpointDescriptor.cs` : `Queue(string)` (`:84`), `ErrorQueue` (`:45`), `DisableErrorQueue` (`:52`), `SkippedQueue` (`:60`), `DisableSkippedQueue` (`:67`) | Infra/satellite-queue members off the routing surface (R3). `MaxPrefetch` KEPT. |
| DELETE | `RabbitMQReceiveEndpointDescriptor.cs` : `Queue(string)` (`:135-145`), `PinQueueIdentity()` (`:28`), `IsQueueIdentityPinned` (`:22`), `_queueIdentityPinned` (`:10`), `ErrorQueue`/`DisableErrorQueue`/`SkippedQueue`/`DisableSkippedQueue` (`:156-185`) | Endpoint no longer sets queue name (except via the kept `ConsumeFrom` override) or satellite-queue config; identity-pin machinery is moot. |
| DELETE | `RabbitMQReceiveEndpointConfiguration.cs` : `QueueDurable` (`:32`), `QueueAutoProvision` (`:37`), `QueueArguments` (`:42`), `ErrorQueue` (`:22`), `SkippedQueue` (`:27`) | Shape and satellite-queue config leave the receive config; fixes silent-data-loss by removing the second home. |
| DELETE | `ThrowHelper.cs` : `QueueIdentityPinned` | No identity is pinned anymore. |
| KEEP | `RabbitMQReceiveEndpointDescriptor.cs:15` construction-time `QueueName = name` | The positional default; drift-free since the only other writer (`Queue(string)`) is deleted. `ValidateOneEndpointPerQueue` (`:369-390`) keeps working because `QueueName` is non-null at `CreateConfiguration` time via this set. |
| KEEP / RENAME-AS-ROUTING | `IRabbitMQReceiveEndpointDescriptor` : narrow `ConsumeFrom(string queueName)` override | Sets `QueueName` only (routing identity), preserves endpoint-name != queue-name without a shape setter. |
| ADD | `Descriptors/IRabbitMQQueueBuilder.cs` + `RabbitMQQueueBuilder.cs` | The flat composition builder (two fields: `IRabbitMQQueueDescriptor` + `IRabbitMQReceiveEndpointDescriptor`). |
| ADD | `RabbitMQMessagingTransportDescriptor.cs` : `Queue(name) => new RabbitMQQueueBuilder(DeclareQueue(name), Endpoint(name))` | The convergent front door; no new store. |
| MOVE | satellite name derivation `GetReceiveEndpointName(queueName, kind)` | From the deleted config object into `RabbitMQDefaultReceiveEndpointConvention`, keyed off the routing endpoint name; synthesizes the satellite `topology.Queues` entry. |
| MOVE | `RabbitMQMessagingTransport.TryGetSatelliteAutoProvision` (`:529-536`) | Reads `AutoProvision` from `topology.Queues` keyed by the queue name parsed out of the `FaultEndpoint`/`SkippedEndpoint` Uri, not from the deleted satellite config. |
| KEEP | `RabbitMQReceiveEndpoint.OnDiscoverTopology` (`:49-56`) gap-fill, `OnComplete` (`:126-130`) `Source` lookup-by-name | Mechanism unchanged; nothing left to drop after the config-field deletions. |
| KEEP | `RabbitMQDestinationResolver`, `MergeFrom`/provenance, `RabbitMQMessagingTopology.AddQueue`, `BindFrom`/`AutoBind`, reply addressing, sagas/request-reply | Load-bearing, untouched. |
| MIRROR | Postgres: delete `IPostgresQueueEndpointDescriptor`/`PostgresQueueEndpointDescriptor`, `_queueEndpoints` (`PostgresMessagingTransportDescriptor.cs:237-266`), the `QueueName.EqualsOrdinal` arm (`:165-178`), the satellite-queue setters and config fields; add `IPostgresQueueBuilder` = `DeclareQueue(name)` + `Endpoint(name)`. InMemory: same, add routing-dominant `IInMemoryQueueBuilder` with an empty infra group. | Same composition pattern across transports. |

## Reconciliation with routing-topology-proposal.md 5.1

Section 5.1 states "the endpoint OWNS its queue" and "the queue exists because the endpoint exists." Section 5.2's topology convention is documented "binds to the queue but never creates it."

This design **keeps 5.1's stance more literally** than the rejected `ConsumeFrom` split, because the queue still comes into being through the endpoint door (`t.Queue`/`t.Endpoint`), not through a separate link object. The refinement: "owns" means **owns the consume relationship and the default queue name**, not **owns the shape exclusively**. Shape is co-authored, the builder's infra group and `DeclareQueue` write the **same** `RabbitMQQueueConfiguration` via the same `_queues` entry, and `MergeFrom` precedence (`declared > endpoint > convention`) resolves any overlap deterministically. "The queue exists because the endpoint exists" weakens to "a default queue is gap-filled (`Provenance=Endpoint`) when a consuming endpoint references an undeclared name", which is exactly the existing precedence.

Action: update `routing-topology-proposal.md` 5.1, 3.5, and the `RabbitMQReceiveEndpointTopologyConvention` doc comment in the same change to: "the endpoint references a separately-declared (or convention-synthesized) queue; the topology convention binds to it and never creates it; `t.Queue(name)` is the flat front door that configures both sides of that reference at once, co-authoring the queue shape through merge-by-identity." No doc inversion is required, only this one-line clarification that shape is co-authored.

## Cross-transport + InMemory degeneracy

The builder is transport-specific by design (its infra group mirrors that transport's `DeclareQueue` surface), so each transport's `Queue(name)` composes that transport's own two descriptors:

- **RabbitMQ:** `new RabbitMQQueueBuilder(DeclareQueue(name), Endpoint(name))`; infra group = `Durable`/`Quorum`/`Exclusive`/`AutoDelete`/`WithArgument`/`AutoProvision`.
- **Postgres:** `new PostgresQueueBuilder(DeclareQueue(name), Endpoint(name))`; infra group = `AutoProvision` (the only Postgres queue shape); routing QoS = `MaxBatchSize`. Same `DeclareQueue`/`Endpoint` join by `Name`.
- **InMemory degeneracy:** `IInMemoryQueueDescriptor` exposes only `Name`, so `IInMemoryQueueBuilder` has an **empty infra group**. It exposes only the routing group. There is no no-op infra method to mislead the user: methods that would no-op simply do not exist on the InMemory builder. The `DeclareQueue(name)` call inside `Queue(name)` still runs (a tolerated hint that records the channel-key identity and feeds gap-fill). This is the cleanest InMemory answer, degeneracy via absent members. Add a test that `Durable`/`Quorum`/`WithArgument` are NOT present on `IInMemoryQueueBuilder`, so a future "consistency" refactor cannot reintroduce no-op infra.

## Open risks / questions

Honest residuals the adversarial review flagged. None is fatal; each has a stated mitigation.

1. **The differing-name override verb name.** Keeping `ConsumeFrom(string queueName)` on the routing descriptor preserves endpoint-name != queue-name (tests and the reply path need it), but `ConsumeFrom` is the same word the rejected draft used for a whole feature, which may confuse readers. Mitigation: run the `dotnet-naming-review` skill on candidates (`ConsumeFrom`, `FromQueue`, `QueueName`) before committing; this is routing identity, so the verb should read as "consume from queue X", and it is build-validated to not introduce a second drift writer (it sets `QueueName`, and identity-dedup remains `Name`-only).
2. **`t.Queue` vs `t.DeclareQueue` naming footgun.** Bare noun-as-method `Queue(name)` has no contract-distinguishing verb against `DeclareQueue(name)`. The user likes `Queue` as the front door, so a rename is off-limits. Mitigation: XML docs on both methods stating the split explicitly ("`Queue` shapes AND consumes; `DeclareQueue` shapes only, for non-consumed queues"), and keep `ValidateOneEndpointPerQueue` so the double-consume case is a hard error. Document that the collision is deliberate.
3. **Migration churn is large, not "mechanical."** Roughly 95 test files reference `.Queue(`, ~15 use the deleted `ErrorQueue`/`SkippedQueue` endpoint satellite setters, ~27 queue snapshots, across about six named unified-queue/front-door suites (`RabbitMQQueueFrontDoorTests`, `RabbitMQUnifiedQueueTests`, `UnifiedQueueBehaviorTests`, `RabbitMQQueueFrontDoorLoweringTests`, `InMemoryUnifiedQueueTests`, plus Postgres) times three transports. Old delegate-style queue call sites must be rewritten to fluent `t.Queue(name)...` chains. The non-mechanical rewrites: the `Endpoint(name).Queue(differingName)` sites (migrate to `Endpoint(name).ConsumeFrom(differingName)`), the satellite-setter sites (migrate to `FaultEndpoint` + `DeclareQueue`), and the behavioral lowering suites whose throw-message text changes. Budget these as compiler-guided breaks; stage snapshot regeneration from `__mismatch__/` per the project's snapshot workflow. The branch is unreleased, so the churn is acceptable, but it must be budgeted, not hand-waved.
4. **Producer-only / dispatch-target queue.** A consumer-less queue (`t.Queue("audit").BindFrom(...)` with no consumer in the PR) must not eagerly materialize a phantom receive endpoint. Mitigation: the producer-only and bound-but-not-consumed path is `t.DeclareQueue(name)` (+ `DeclareBinding`); the flat `Queue(name)` front door is for consumed queues. Confirm the build prunes an endpoint with zero consumers and zero received types from `ReceiveEndpoints` (the existing entity-only predicate's intent), so a stray `Queue(name)` with no routing verb does not produce an empty receive endpoint. Rewrite `RabbitMQQueueFrontDoorLoweringTests` and its mirrors against this decision.

## Phasing

1. **Add the builder behind the existing surface.** Introduce `IRabbitMQQueueBuilder`/`RabbitMQQueueBuilder` and re-point `t.Queue(name)` at `new RabbitMQQueueBuilder(DeclareQueue(name), Endpoint(name))`. Delete the `_queueEndpoints` cache and the `QueueName.EqualsOrdinal` arms. Nothing else moves yet; the receive config still carries the shape fields. Tests still green except the deleted-cache call sites.
2. **Slim the receive descriptor and config.** Delete `Queue(string)` (keep the narrow `ConsumeFrom` override), the satellite-queue setters, and the `QueueDurable`/`QueueArguments`/`QueueAutoProvision`/`ErrorQueue`/`SkippedQueue` config fields. This is the step that fixes the silent-data-loss bug by construction.
3. **Move satellites.** Relocate satellite name derivation into the convention keyed off the routing endpoint name; re-point `TryGetSatelliteAutoProvision` at `topology.Queues` via the `FaultEndpoint`/`SkippedEndpoint` Uri. Migrate satellite tests.
4. **Delete the conflated tier and entity-only partitioning.** Remove `IRabbitMQQueueEndpointDescriptor` + impl, `IsEntityOnly`/`LowerEntityOnlyQueue` + the `SatelliteRequiresConsumingEndpoint` guard, and `PinQueueIdentity`/`ThrowHelper.QueueIdentityPinned`. Add the producer-only-queue prune. Rewrite/delete the unified-queue suites and regenerate snapshots from `__mismatch__/`.
5. **Reconcile the proposal doc.** Update `routing-topology-proposal.md` 5.1/3.5 and the topology convention doc comment to the "references, co-authors shape" story.
6. **Mirror to Postgres + InMemory** at each step so the three transports never diverge mid-phase; assert the InMemory builder has no infra group.

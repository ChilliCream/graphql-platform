# Plan: `Queue` as a lazy flat composition builder (no `ConsumeFrom`)

Understanding of the rework, shaped as a plan so the mental model can be checked before any code is written. Incorporates two decisions: drop the endpoint-name-vs-queue-name override, and create the receive endpoint lazily.

- Make `t.Queue(name)` a flat builder that *composes* a queue and an endpoint instead of *being* an endpoint
  - The PR made `IRabbitMQQueueEndpointDescriptor` inherit the receive endpoint and pile queue shape on top, which is the mixing you object to
    - Inheriting from the endpoint forces queue-shape methods onto the routing type and stores that shape on the receive config, the wrong home, which is the mixing of queue setup and routing you object to
  - Replace it with `IRabbitMQQueueBuilder`, a type that is neither a `*Descriptor` nor a receive endpoint
    - Its infra methods (`Durable`, `Quorum`, `WithArgument`, `AutoProvision`) delegate to the queue descriptor and its routing methods (`Consumer`, `Handler`, `Receives`, `MaxConcurrency`, `MaxPrefetch`) delegate to the endpoint descriptor, so the flat surface owns no state of its own
  - `t.Queue(name)` builds over the same two stores the other doors use
    - It resolves the queue through `DeclareQueue(name)` and the endpoint through `Endpoint(name)`, both keyed by name, so `Queue`, `DeclareQueue`, and `Endpoint` always land on one queue and one endpoint

- Create the receive endpoint lazily, only when a routing method is called
  - Today `Queue(name)` and `Endpoint(name)` eagerly add to `_receiveEndpoints` (`RabbitMQMessagingTransportDescriptor.cs:221-242`, `:160-173`), so an infra-only or producer-only `Queue` makes a phantom endpoint
    - That phantom then has to be detected and pruned at build time, which is the machinery we want gone
  - The builder holds the transport descriptor and the name, and only calls `Endpoint(name)` the first time a routing method runs
    - `t.Queue("audit").Durable()` touches only the queue topology entity and never registers an endpoint, so a producer-only queue simply has no consumer side
  - The queue entity is also resolved on first infra call, not forced into existence by a bare `Queue(name)`
    - `t.Queue("orders").Consumer<C>()` with no shape leaves the queue to be gap-filled at discovery exactly as `t.Endpoint("orders").Consumer<C>()` does today, so the two doors behave identically

- Strip the receive endpoint descriptor and its config down to pure routing
  - The receive descriptor currently carries `Queue(string)` and the satellite-queue setters, and the receive config carries `QueueDurable`/`QueueArguments`/`QueueAutoProvision`, which is the shape living in the wrong place
    - `RabbitMQReceiveEndpoint.OnDiscoverTopology` never forwards `QueueDurable`/`QueueArguments` to `AddQueue`, so shape set through the endpoint is silently dropped (the confirmed bug)
  - Delete `Queue(string)`, `ErrorQueue`/`SkippedQueue`/`Disable*` from `IRabbitMQReceiveEndpointDescriptor`, and delete `QueueDurable`/`QueueArguments`/`QueueAutoProvision`/`ErrorQueue`/`SkippedQueue` from `RabbitMQReceiveEndpointConfiguration`
    - With no second home for shape, the only place durability and arguments can be written is the queue topology entity that the topology already reads, so the silent-drop bug cannot happen
  - Keep `MaxPrefetch`, `MaxConcurrency`, `FaultEndpoint`, `SkippedEndpoint`, `AutoBind`, `BindFrom`, `Kind`, `UseReceive` on the endpoint
    - These are consumption and routing concerns, not broker shape, so they stay on the routing surface

- Drop the endpoint-name-vs-queue-name override entirely (no `ConsumeFrom`)
  - An endpoint can currently consume a queue whose name differs from the endpoint name via `Endpoint("ep").Queue("q")`, used cosmetically in ~50 tests and one Postgres example, and you do not want this
    - The only production user of differing names is the reply path, and it sets `QueueName` directly in `RabbitMQMessagingTransport.CreateEndpointConfiguration` (`:549-560`), bypassing the descriptor, so it is unaffected
  - Remove every public way to set the queue name on an endpoint and let endpoint identity be its queue name
    - `QueueName` stays a field on the config defaulted to `Name` at construction and is never reassigned through the public surface, so there is no override verb to add and nothing named `ConsumeFrom`
  - Migrate the call sites that used differing names to matching names
    - `Endpoint("ep").Queue("q")` becomes `Endpoint("q")` (or `Queue("q")`), losing only the cosmetic separate label, and the reply path keeps working because it never used the public verb

- Kill the string-match drift and the phantom-endpoint prune, now made impossible
  - The drift came from two surfaces both writing the queue name and matching by string equality (`Endpoint` matching `Name OR QueueName` at `:162-163`, the `_queueEndpoints` cache at `:15-16`)
    - With the public queue-name setter gone, no second writer of `QueueName` exists, so identity can be `Name` only and nothing can diverge
  - Delete the `|| QueueName.EqualsOrdinal(name)` arm of `Endpoint`, the `_queueEndpoints` cache, and the `PinQueueIdentity`/`IsQueueIdentityPinned` machinery
    - Identity becomes a single `Configuration.Name` lookup, so `Queue("x")` and `Endpoint("x")` and `DeclareQueue("x")` provably resolve to the same instances
  - Delete `IsEntityOnly` and `LowerEntityOnlyQueue` (`:300-367`) and the `SatelliteRequiresConsumingEndpoint` guard
    - Lazy creation means an endpoint cannot exist without a routing method having been called, so there is no empty endpoint left to detect or prune

- Re-home satellites as routing addresses plus declared queue shape
  - Error and skip queues are currently named and shaped on the routing surface, which mixes infra into routing
    - Their existence and shape are infrastructure, while the choice of where faults go is routing, and these were tangled together
  - Keep `FaultEndpoint(name)`/`SkippedEndpoint(name)` as routing addresses on the endpoint and declare the satellite queue's shape with `DeclareQueue(name)`
    - When the satellite queue is not declared, the convention synthesizes it by derived name with `Provenance=Endpoint`, and a declared one wins the merge, so shape has one home and routing has another
  - Move the satellite name derivation into `RabbitMQDefaultReceiveEndpointConvention` and read auto-provision from the topology entry
    - `TryGetSatelliteAutoProvision` reads the synthesized queue entity instead of the deleted satellite config object, so runtime provisioning still works

- Mirror the same shape across RabbitMQ, Postgres, and InMemory
  - All three transports carry the identical conflation today (`QueueEndpointDescriptor`, eager endpoints, entity-only prune, the string-match arm)
    - Leaving any one on the old shape would let the three diverge mid-rework
  - Add a per-transport `IXxxQueueBuilder` composing that transport's own queue and endpoint descriptors, and delete the same members everywhere
    - Postgres infra is `AutoProvision` only and its QoS is `MaxBatchSize`, and InMemory has no broker shape so its builder exposes only the routing group, which keeps degeneracy honest by absent members rather than no-op methods

## Self-check (least sure)

- **`BindFrom`-only endpoints.** I am assuming `BindFrom` is a routing method that lazily creates an endpoint, and that a binding into a queue with no consumer is rare enough to express as `DeclareBinding` (infra) instead. If `BindFrom` must remain a way to create a consumer-less endpoint, a minimal entity-only lowering survives and `IsEntityOnly` is not fully dead. This is the one place the prune might not vanish completely.
- **Dropping differing names is acceptable cosmetic loss.** I am assuming nothing depends on an endpoint having a label distinct from its queue name beyond the reply path (which is config-layer and safe), so renaming the ~50 test call sites and the Postgres example to matching names is purely mechanical and loses no behavior.

Stopping here for confirmation before writing any code.

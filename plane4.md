# Plan: Queue as a flat composition descriptor

## Understanding

The PR introduced `IRabbitMQQueueEndpointDescriptor : IRabbitMQReceiveEndpointDescriptor` with infra (`Durable`/`Quorum`/`WithArgument`/`AutoProvision`) piled on top of a routing type. That inheritance is the structural mistake. The fix: `Queue` stays the front door but becomes its own type that internally **composes** a separate infra descriptor and a separate receive endpoint descriptor, delegating each flat method to exactly one of them.

Two user corrections to the prior proposal:
- No `ConsumeFrom` on the receive endpoint. The only place `Name != QueueName` is the reply endpoint, and that sets the config directly at the transport level (`RabbitMQMessagingTransport.CreateReceiveEndpointConfiguration`), never through the descriptor. So there is no public need for a queue-name override on the routing surface.
- Producer-only queues: instead of eagerly creating an endpoint in `Queue(name)` and then pruning it, create the endpoint lazily (only when a routing method is called on the builder). A `Queue("audit").Durable()` that never calls `Consumer`/`Handler`/`Receives` simply never materializes an endpoint.

---

## The plan

- Introduce `IRabbitMQQueueBuilder` as its own type that composes the two concerns via delegation
  - The PR's mistake was `IRabbitMQQueueEndpointDescriptor : IRabbitMQReceiveEndpointDescriptor`, which meant infra had to live on a routing type
    - A standalone builder type that holds two separate descriptors by reference cannot re-conflate, because infra methods can only call `_queue` and routing methods can only call `_endpoint`
  - Name it `IRabbitMQQueueBuilder` (not `*Descriptor`) because it does not implement `IMessagingDescriptor<T>` and produces two configs, not one
    - This avoids the naming collision with `IRabbitMQQueueDescriptor` (the existing infra type from `DeclareQueue`) by being in a different naming family entirely
  - The infra descriptor (`IRabbitMQQueueDescriptor`) is always created eagerly because `Queue` is fundamentally a queue
    - It delegates to the same `_queues` store entry that `DeclareQueue(name)` returns, so shape set on either surface lands on the same `RabbitMQQueueConfiguration`
  - The receive endpoint descriptor is created lazily, only when a routing method (`Consumer`/`Handler`/`Receives`/`AutoBind`/`BindFrom`/`MaxPrefetch`/etc.) is first called
    - This means `t.Queue("audit").Durable()` with no consumer never creates a phantom receive endpoint, no pruning needed
    - When the endpoint is created, it uses the same `Endpoint(name)` path and enters the same `_receiveEndpoints` list, so all existing wiring (conventions, topology discovery, validation) applies unchanged

- Delete the conflated tier (`IRabbitMQQueueEndpointDescriptor` and `RabbitMQQueueEndpointDescriptor`)
  - These types inherit from the receive endpoint descriptor and bolt infra on top, which is the structural defect
    - `RabbitMQQueueEndpointDescriptor` writes infra into `_inner.Configuration.QueueDurable` / `QueueArguments` / `QueueAutoProvision`, a second home that `OnDiscoverTopology` never reads, causing the confirmed silent-data-loss bug
  - The `_queueEndpoints` dictionary and `PinQueueIdentity` machinery in the transport descriptor exist only to support this adapter and are deleted with it

- Remove infra members from the receive endpoint descriptor (`IRabbitMQReceiveEndpointDescriptor`)
  - Delete `Queue(string name)` (line 84 of the interface, lines 135-145 of the impl)
    - This was the setter that let an endpoint rename its queue, creating the second write path for `QueueName`
  - Delete `ErrorQueue(string)` / `DisableErrorQueue()` / `SkippedQueue(string)` / `DisableSkippedQueue()` (lines 45-67 of the interface, lines 156-185 of the impl)
    - These configure the satellite queue's name and existence, which is queue infrastructure, not routing
    - `FaultEndpoint`/`SkippedEndpoint` stay because those set a routing address (where faults go), not queue shape
  - Keep `MaxPrefetch` because it is consumer-channel QoS (how many messages the broker delivers to this consumer), not queue shape
  - Do not add `ConsumeFrom` because there is no public-API need for endpoint-name != queue-name; the reply path sets it at the config level directly

- Remove the second-home infra fields from `RabbitMQReceiveEndpointConfiguration`
  - Delete `QueueDurable` (line 32), `QueueAutoProvision` (line 37), `QueueArguments` (line 42)
    - These exist because the old `RabbitMQQueueEndpointDescriptor` needed somewhere on the receive config to store infra; the builder writes to `RabbitMQQueueConfiguration` instead (the topology's real home), so these become unreachable
    - Deleting them is what fixes the silent-data-loss bug by construction: there is no second home left to drop
  - Delete `ErrorQueue` (line 22) and `SkippedQueue` (line 27) satellite configs
    - These `RabbitMQSatelliteConfiguration` objects stored per-endpoint satellite queue name/disabled/auto-provision; they move to the builder's infra delegation (satellite queues become regular `DeclareQueue` calls)

- Kill the string-match drift vector in `Endpoint(name)`
  - Delete the `|| e.Extend().Configuration.QueueName.EqualsOrdinal(name)` arm at `RabbitMQMessagingTransportDescriptor.cs:163`
    - Today `Endpoint("foo")` matches by Name OR QueueName, so two different identities (`Name="bar"`, `QueueName="foo"`) can be found by `Endpoint("foo")`, creating ambiguity
    - After this, `Endpoint(name)` matches by `Name` only, which is the same key `DeclareQueue(name)` uses for its `_queues` store, so both stores are keyed by the same single identity
  - This also removes `PinQueueIdentity` / `IsQueueIdentityPinned` / `_queueIdentityPinned` from the receive endpoint descriptor, since the only reason to pin was to prevent `Queue(string)` from renaming after the unified front door set it

- Rewrite `t.Queue(name)` to return the builder
  - `Queue(name)` becomes `new RabbitMQQueueBuilder(DeclareQueue(name), name, this)` where the transport ref is held for lazy endpoint creation
    - The infra descriptor is always the same `_queues` store entry (eagerly created via `DeclareQueue(name)`)
    - The receive endpoint is created lazily via `Endpoint(name)` on first routing-method call, entering the same `_receiveEndpoints` list
  - `Queue(name, Action<IRabbitMQQueueBuilder>)` calls the above and applies the delegate, returning the transport descriptor for chaining
  - Delete the old `Queue(string)` body (lines 221-242) and the `_queueEndpoints` dictionary (lines 15-16)
  - Delete `IsEntityOnly` / `LowerEntityOnlyQueue` / `SatelliteRequiresConsumingEndpoint` (lines 300-367) because lazy endpoint creation makes the entity-only partitioning unnecessary
    - A `Queue("audit").Durable()` that never calls a routing method simply never creates an endpoint, so there is nothing to partition

- Handle satellites under the new model
  - Satellite queue naming and shape leave the receive endpoint config and become regular topology queue declarations
    - The convention (`RabbitMQDefaultReceiveEndpointConvention`) currently reads `configuration.ErrorQueue.QueueName` and `configuration.ErrorQueue.IsDisabled` to decide whether to synthesize; these move to read from the topology or from a lightweight flag on the builder
  - The builder exposes `ErrorQueue(string)` / `DisableErrorQueue()` / `SkippedQueue(string)` / `DisableSkippedQueue()` on its own surface (they are about the queue, not pure routing, not pure infra; the builder is the right home since it composes both)
    - Internally, `ErrorQueue(name)` calls `DeclareQueue(name)` on the transport for the satellite's shape and records the routing address on the endpoint's `FaultEndpoint`
  - `TryGetSatelliteAutoProvision` in `RabbitMQMessagingTransport` re-points to read from `topology.Queues` by the satellite queue name, not from the deleted config object

- Mirror to Postgres and InMemory
  - Postgres: same pattern, `IPostgresQueueBuilder` composing `IPostgresQueueDescriptor` + `IPostgresReceiveEndpointDescriptor`; infra group is just `AutoProvision` (no `Durable`/`Quorum`); routing QoS is `MaxBatchSize`
  - InMemory: `IInMemoryQueueBuilder` has an empty infra group (no broker shape exists to configure); it composes `IInMemoryQueueDescriptor` (identity-only) + `IInMemoryReceiveEndpointDescriptor`
    - Degeneracy via absent members, not present no-ops: `Durable`/`Quorum`/`WithArgument` simply do not exist on the InMemory builder

- Fix `OnDiscoverTopology` gap-fill to merge correctly with the builder's declared queue
  - Today `OnDiscoverTopology` calls `topology.AddQueue` with `Provenance = Endpoint` for the input queue; `AddQueue` merges by name via `MergeFrom`
    - When the builder already created a `Provenance = Declared` entry via `DeclareQueue(name)`, the merge keeps the declared shape (durable/args/auto-provision) because `MergeFrom` never downgrades provenance
    - This is already correct and needs no change; the bug was that infra written to `QueueDurable`/`QueueArguments` on the receive config was never passed to `AddQueue` at all, and that second home is now deleted

- Update `routing-topology-proposal.md` section 5.1
  - "The endpoint owns its queue" refines to "the endpoint owns the consume relationship and the default queue name; shape is co-authored through the topology's merge-by-identity"

---

## Least certain

1. **Satellite routing.** I assumed the builder is the right home for `ErrorQueue`/`DisableErrorQueue` because it is about "this queue's error satellite" (touching both infra and routing). If you want satellites to be pure `DeclareQueue` + `FaultEndpoint` with no sugar, the builder surface is simpler but the user writes two statements instead of one. I went with the sugar.

2. **Lazy endpoint and `ValidateOneEndpointPerQueue`.** The validation runs in `CreateConfiguration` over `_receiveEndpoints`. A lazy-created endpoint enters `_receiveEndpoints` when the first routing method is called, so validation still catches duplicates at build time. But if a user calls `t.Queue("x").Consumer<A>()` and then `t.Endpoint("x").Consumer<B>()`, the lazy creation on the first call adds the endpoint, and the second call finds the same entry by name. I believe this is correct (same endpoint, two consumers). But I have not verified that the lazy creation interleaving is safe in all orderings.

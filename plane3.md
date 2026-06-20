# Plan: `Queue` as a lazy flat builder over two pure descriptors

How I understand the problem and why this fix is correct. Each level only adds detail to the item above it.

- Replace the inherited queue-endpoint type with a `Queue` builder that wraps two existing descriptors.
  - The PR's `IRabbitMQQueueEndpointDescriptor` derives from `IRabbitMQReceiveEndpointDescriptor`, so a queue is forced to also be a receive endpoint.
    - That inheritance is why queue shape like `Durable`, `Quorum`, and arguments becomes reachable on the routing surface and has to be stored on the receive config.
  - The new `IRabbitMQQueueBuilder` instead holds the infra descriptor from `DeclareQueue` and the routing descriptor from `Endpoint` as two fields and forwards each flat method to one of them.
    - Because the builder only forwards and stores no config of its own, neither concern can leak into the other, while the call site stays one terse chain.
  - `t.Queue(name)` returns this builder; `IRabbitMQQueueEndpointDescriptor` and its implementation are deleted.
    - The deleted type was the only thing claiming a queue "is a" receive endpoint, so removing it is what ends the conflation.

- Create each wrapped descriptor lazily, only the first time its own method group is used.
  - A bare `t.Queue("audit").Durable()` with no consumer should declare a queue but must not register a receive endpoint that has nothing to consume.
    - Eagerly calling `Endpoint(name)` inside `Queue(name)` is what would leave a phantom empty endpoint for a queue nobody reads.
  - The builder calls `Endpoint(name)` only when a routing method runs, and `DeclareQueue(name)` only when an infrastructure method runs.
    - So the existence of a receive endpoint is decided by whether any routing method was used, which removes the phantom endpoint by construction instead of pruning it later.
  - A pure-consume `t.Queue("orders").Consumer<C>()` therefore never calls `DeclareQueue`, and its queue is still gap-filled at discovery with `Provenance=Endpoint`.
    - Keeping the queue side lazy too preserves the existing "the endpoint gap-fills its own queue" provenance, instead of stamping the queue as explicitly declared when the user only asked to consume it.

- Remove every public way to set an endpoint's queue name, so the name is the only identity.
  - Endpoint-name-different-from-queue-name is not a needed public feature, and the PR's `Queue(string)` setter is a second writer of the queue name.
    - Two writers of the queue name are exactly what let `Queue(...)` and `Endpoint(...)` point at different entities and drift apart.
  - Delete the queue-name setter from `IRabbitMQReceiveEndpointDescriptor`, the `QueueName.EqualsOrdinal` match arm of `Endpoint(name)`, and the `_queueEndpoints` cache on the transport descriptor.
    - With no public setter and all lookups keyed only on `Configuration.Name`, `Queue("x")`, `Endpoint("x")`, and `DeclareQueue("x")` always resolve to the same endpoint and the same queue.
  - Keep the `QueueName` field on the config and let only the internal reply path set it directly.
    - The reply endpoint still needs a queue name that differs from its logical name, and it assigns that at the config layer, so dropping the public verb does not break reply.

- Strip queue shape out of `RabbitMQReceiveEndpointConfiguration`.
  - The PR stores `QueueDurable`, `QueueArguments`, and `QueueAutoProvision` on the receive config, but `OnDiscoverTopology` never passes them to the queue it builds.
    - This is the confirmed data-loss bug: durability or arguments set through the old unified surface never reach the broker.
  - Delete those three fields so the only home for queue shape is the `RabbitMQQueueConfiguration` that `DeclareQueue` and the builder's infra methods write.
    - With no second home to read from there is nothing left for `OnDiscoverTopology` to drop, and the topology already reads the real queue configuration.

- Split error and skip queues into a routing target and a declared queue shape.
  - The PR sets satellite queue names and shape through `ErrorQueue` and `SkippedQueue` on the routing surface, which mixes infrastructure back in.
    - Naming and shaping an error queue is infrastructure, while choosing where faults are routed is routing, and the PR conflates the two.
  - Keep `FaultEndpoint` and `SkippedEndpoint` as the routing address on the endpoint, and declare any custom satellite queue through `DeclareQueue`.
    - The convention still synthesizes a default satellite queue when none is declared, so the common case needs no extra calls.
  - Move the default satellite name derivation into `RabbitMQDefaultReceiveEndpointConvention` and read its auto-provision flag from the topology.
    - Keying the satellite off the routing endpoint name instead of the deleted config object keeps provisioning working after the fields are gone.

- Apply the same builder split to Postgres and InMemory.
  - The PR duplicated the conflated queue-endpoint type in all three transports, so the fix has to land in all three to keep them consistent.
    - Leaving one transport on the old shape would make the three diverge in the middle of the change.
  - Give Postgres a builder whose infra group is just `AutoProvision`, and give InMemory a builder with no infra methods at all.
    - InMemory queues only have a name, so omitting infra methods entirely avoids offering no-op shape calls that would mislead the reader.

## Self-check (least sure)

- I am least sure that endpoint-name-different-from-queue-name is truly unused outside the internal reply path. I assumed the prior analysis is right that reply sets `QueueName` at the config layer and that no saga, temporary, or request-client path needs a differing name through the public descriptor; if one does, deleting the setter would break it.
- I am also unsure whether the queue side should be lazy or always declared eagerly by `t.Queue(name)`. You only asked about the endpoint being lazy; I assumed making the queue lazy as well, so a pure-consume queue keeps `Provenance=Endpoint` gap-fill rather than being marked as explicitly declared.

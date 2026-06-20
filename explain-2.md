# Understand First

Direction taken from your two calls: there is no `ConsumeFrom` anywhere (the
endpoint name and its queue name are always the same), and the endpoint is built
lazily so a produce-only `Queue` never creates one.

- Make `t.Queue(name)` return a stateless `IRabbitMQQueueBuilder` that forwards to two separate descriptors.
  - Today `IRabbitMQQueueEndpointDescriptor` inherits the receive endpoint and piles queue shape onto it, so infrastructure and routing share one type.
    - The builder holds one queue descriptor and one endpoint descriptor and sends each method to exactly one of them, so neither type ever gains the other's members.

- Build the queue descriptor and the endpoint descriptor lazily, each only when its own group of methods is first called.
  - A `Queue` used only as a producer target should not create a consuming endpoint, and a plain consumer should not mark its queue as explicitly declared.
    - Infra methods like `Durable` and `WithArgument` call `DeclareQueue(name)` on first use and routing methods like `Consumer` and `Receives` call `Endpoint(name)` on first use, so using only one group creates only one object and no phantom endpoint appears.

- Remove the public queue-name override `Queue(string)` and its `PinQueueIdentity` guard from the receive endpoint.
  - The endpoint's name and its queue name are the same thing, so a public setter is a second writer that can drift from the endpoint's identity.
    - `QueueName` is set once to `Name` when the descriptor is constructed (`RabbitMQReceiveEndpointDescriptor.cs:15`) and is never reassigned through the public API, while the reply path keeps setting it directly in `RabbitMQMessagingTransport.CreateEndpointConfiguration` (`:555`/`:565`).

- Delete the queue-shape fields `QueueDurable`, `QueueAutoProvision`, and `QueueArguments` from `RabbitMQReceiveEndpointConfiguration`.
  - These fields are a second home for queue shape that `RabbitMQReceiveEndpoint.OnDiscoverTopology` never reads, so shape set through them is silently dropped (the data-loss bug).
    - Shape then lives only on the `RabbitMQQueueConfiguration` that `DeclareQueue` and the builder's infra group write, which discovery already reads, so there is nothing left to drop.

- Move the error and skip satellite queues off the receive endpoint onto declared queues plus a fault routing address.
  - Naming and shaping an error queue is infrastructure, but today `ErrorQueue(string)`/`SkippedQueue(string)` and their `RabbitMQSatelliteConfiguration` objects sit on the routing surface.
    - Which endpoint receives faults stays as the `FaultEndpoint`/`SkippedEndpoint` addresses, the satellite queue's shape becomes a normal `DeclareQueue`, and `RabbitMQDefaultReceiveEndpointConvention` (`:67`) still fills in the satellite name when none is declared.

- Delete the `Name`-or-`QueueName` endpoint match and the entity-only-queue partitioning in `RabbitMQMessagingTransportDescriptor`.
  - Endpoints are matched by `Name OR QueueName` (`:162-163`), the string-equality path that lets two surfaces register the same queue twice, and entity-only queues exist only to model queues that have no consumer.
    - Identity becomes `Name` only so there is exactly one endpoint per name, and a non-consumed queue is just a queue with no endpoint, which lazy creation produces without a special partition.

- Apply the same builder-plus-lazy shape to Postgres and InMemory.
  - All three transports carry the same conflation and must not diverge after the change.
    - Each transport's `Queue(name)` composes its own queue and endpoint descriptors, and the InMemory builder exposes an empty infrastructure group because its queue descriptor has only a name.

## Self-check

- Least sure: what triggers the lazy endpoint. I assumed any routing-group method
  creates it, which means a tuning-only call like `MaxConcurrency` with no consumer
  would still make an endpoint; if you want that avoided, the trigger should be only
  the consumer-defining methods (`Consumer`/`Handler`/`Receives`), or we keep the
  existing zero-consumer endpoint prune as a backstop. Tell me which.
- Assumption: dropping `Queue(string)` removes every user-facing way to have an
  endpoint name differ from its queue name. I checked that the reply path sets
  `QueueName` itself at the config layer, so reply still works, but I am assuming no
  test or app code depends on a public differing-name door that must keep working.

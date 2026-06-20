# Queue Composition Descriptor: Implementation Plan

Refines `endpoint-topology-separation-proposal.md` with two decisions:

- **Delete the differently-named-queue option.** `Queue(name)` and `Endpoint(name)` always use one name for both the endpoint and its queue. The reply path still sets `RabbitMQReceiveEndpointConfiguration.QueueName` directly in `RabbitMQMessagingTransport.CreateEndpointConfiguration`, so nothing real is lost.
- **Create the endpoint lazily.** `Queue(name)` calls `DeclareQueue(name)` now and `Endpoint(name)` only on the first consume call, so a queue nobody consumes registers no receive endpoint.

## Plan (by concept)

- **1. `Queue(name)` returns a `RabbitMQQueueBuilder` that wraps the descriptors from `DeclareQueue(name)` and `Endpoint(name)` and adds no config of its own.**
    - `Durable()`, `Quorum()`, `WithArgument()`, and `AutoProvision()` on the builder call the same methods on the `IRabbitMQQueueDescriptor` that `DeclareQueue(name)` returns.
        - Reusing that one descriptor makes `Queue` and `DeclareQueue` write the same `RabbitMQQueueConfiguration`, not two.
    - `Consumer<T>()`, `Handler<T>()`, and `Receives<T>()` on the builder call the same methods on the `IRabbitMQReceiveEndpointDescriptor` that `Endpoint(name)` returns.
        - Reusing that one descriptor makes `Queue` and `Endpoint` register the same receive endpoint, not two.
    - The builder stores only those two descriptor references and no `RabbitMQQueueConfiguration` or `RabbitMQReceiveEndpointConfiguration` of its own.
        - With no config of its own, the builder cannot hold a value that contradicts the real queue or endpoint.

- **2. An endpoint and a queue are each found only by their own `Name`, and we delete the methods that let the name change or double-match.**
    - `Endpoint(name)` matches existing endpoints by `Configuration.Name` only, after we delete its `|| QueueName.EqualsOrdinal(name)` arm.
        - That arm matched an endpoint by either its own name or its queue name, which split or merged endpoints by accident.
    - `RabbitMQReceiveEndpointConfiguration.QueueName` is set once in the descriptor constructor, and we delete the public `Queue(string)` setter that could reassign it.
        - With no setter to reassign `QueueName`, a producer and a consumer cannot point at different queues.
    - We delete the differently-named-queue option from the public surface entirely.
        - The reply path still sets `QueueName` in `RabbitMQMessagingTransport.CreateEndpointConfiguration`, below the descriptor surface, so the deletion changes nothing there.

- **3. We delete the duplicate queue-property fields from `RabbitMQReceiveEndpointConfiguration`, which also fixes the data-loss bug.**
    - `QueueDurable`, `QueueAutoProvision`, and `QueueArguments` duplicate the durability and arguments that `RabbitMQQueueConfiguration` already stores.
        - Two homes for the same values let them disagree.
    - `RabbitMQReceiveEndpoint.OnDiscoverTopology` never copies those fields into the `RabbitMQQueueConfiguration` it builds, so today their values never reach the broker.
        - Deleting the fields removes that drop; `Durable()` and `WithArgument()` then write only `RabbitMQQueueConfiguration`, which `OnDiscoverTopology` already reads.
    - `IRabbitMQReceiveEndpointDescriptor` keeps `Consumer<T>()`, `Handler<T>()`, `Receives<T>()`, `AutoBind()`, `MaxConcurrency()`, and `MaxPrefetch()`.
        - `MaxPrefetch()` stays because it sets the consumer's channel prefetch, not the queue's durability or arguments.

- **4. `Queue(name)` calls `DeclareQueue(name)` immediately but calls `Endpoint(name)` only on the first consume method.**
    - The builder creates its `IRabbitMQReceiveEndpointDescriptor` on the first call to `Consumer<T>()`, `Handler<T>()`, or `Receives<T>()`.
        - Before that first call, the builder holds only the `IRabbitMQQueueDescriptor`, so `Queue(name)` is a declared queue and nothing more.
    - A `Queue(name)` that only calls `Durable()`/`WithArgument()`, or nothing, never calls `Endpoint(name)` and registers no receive endpoint.
        - Never creating the endpoint avoids the empty receive endpoint that a create-then-remove approach would leave behind.

- **5. `FaultEndpoint(name)` and `SkippedEndpoint(name)` set the failure address on the endpoint, while the error queue's properties move to `DeclareQueue`.**
    - `FaultEndpoint(name)` and `SkippedEndpoint(name)` write the fault and skip `Uri` on `RabbitMQReceiveEndpointConfiguration`, choosing which endpoint receives failures.
        - These set an address, not a queue's durability or arguments, so they belong on the endpoint.
    - A custom error queue's durability and arguments are set with `DeclareQueue(errorName).Durable()` like any other queue.
        - We delete the `ErrorQueue(string)` and `SkippedQueue(string)` setters from `IRabbitMQReceiveEndpointDescriptor`, which mixed the address and the queue's properties.
    - When nobody declares an error queue, `RabbitMQDefaultReceiveEndpointConvention` still synthesizes one by convention name.
        - The default keeps a plain setup working without a `DeclareQueue` line.

- **6. Each transport's `Queue(name)` composes that transport's own `DeclareQueue(name)` and `Endpoint(name)`, and InMemory's builder has no infra methods.**
    - RabbitMQ's `IRabbitMQQueueBuilder` exposes `Durable()`, `Quorum()`, `WithArgument()`, and `AutoProvision()`; Postgres's exposes only `AutoProvision()`.
        - The methods differ because `IRabbitMQQueueDescriptor` and `IPostgresQueueDescriptor` expose different properties.
    - InMemory's `IInMemoryQueueDescriptor` exposes only `Name`, so `IInMemoryQueueBuilder` has no `Durable`/`Quorum`/`WithArgument` methods.
        - We leave those methods off rather than adding ones that do nothing, so nobody calls `Durable()` expecting it to matter in memory.
    - A test asserts `IInMemoryQueueBuilder` has no infra methods.
        - The test stops a later refactor from adding do-nothing methods back.

- **7. We land the change in compiling slices, with the field deletion isolated to the slice that fixes the bug.**
    - First slice: add `IRabbitMQQueueBuilder` and point `Queue(name)` at `new RabbitMQQueueBuilder(DeclareQueue(name), Endpoint(name))`, while `RabbitMQReceiveEndpointConfiguration` still has the old fields.
        - Adding the builder before deleting anything keeps every test compiling at this point.
    - Second slice: delete `QueueDurable`, `QueueAutoProvision`, and `QueueArguments`, which is the slice that fixes the data-loss bug.
        - Isolating the deletion lets us run the durability test against just that change.
    - Remaining slices delete the `Queue(string)` setter, the `ErrorQueue`/`SkippedQueue` setters, and the `QueueName.EqualsOrdinal` arm, then regenerate snapshots from `__mismatch__/`.
        - The compiler lists every call site to fix, so the test churn is mechanical.

## Open question

Should `AutoBind()` and `BindFrom()` also trigger the lazy `Endpoint(name)` call, or only `Consumer<T>()`/`Handler<T>()`/`Receives<T>()`? I lean toward all routing methods triggering it, since `BindFrom()` into a queue you never consume is usually a mistake; the alternative leaves `BindFrom()` on the producer side and creates no endpoint.

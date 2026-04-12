---
title: "Transports"
description: "Understand how transports move messages in Mocha, how the transport abstraction works, and how to choose between InMemory and RabbitMQ."
---

# Transports

A transport is the infrastructure layer that connects Mocha to a message broker. It manages connections, provisions topology (exchanges, queues, bindings), and handles the low-level details of dispatching and receiving messages. You write handlers and publish messages. The transport handles the rest.

The transport abstraction means your handlers, patterns, and pipeline are identical regardless of which broker you use. Only the infrastructure changes. Swap `.AddInMemory()` for `.AddRabbitMQ()` and your application code stays unchanged. This portability is the core value of the [Message Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageChannel.html) pattern: the sender and receiver are decoupled from the physical infrastructure that carries the message.

Mocha ships with two transports:

| Transport    | Package                    | Use case                                                     |
| ------------ | -------------------------- | ------------------------------------------------------------ |
| **InMemory** | `Mocha.Transport.InMemory` | Development, testing, single-process scenarios               |
| **RabbitMQ** | `Mocha.Transport.RabbitMQ` | Production, distributed systems, multi-service architectures |

# Add a transport

Every transport implements the same `MessagingTransport` base class. The transport is always the last call in the builder chain:

```csharp
// InMemory - zero configuration
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddInMemory();
```

```csharp
// RabbitMQ - production-ready
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ();
```

Each `Add{Transport}()` method registers a transport instance, applies default conventions, and wires up the middleware pipelines.

# Choose a transport

Use this decision matrix to pick the right transport. Both columns include trade-offs - choose the one whose trade-offs you can accept:

| Criterion          | InMemory                          | RabbitMQ                                    |
| ------------------ | --------------------------------- | ------------------------------------------- |
| Setup effort       | None - zero dependencies          | Requires a running broker                   |
| Message durability | **Messages lost on process exit** | Messages survive broker restarts            |
| Multi-process      | **Single process only**           | Multiple services, multiple instances       |
| Request/reply      | Supported                         | Supported                                   |
| Operational cost   | None                              | Broker infrastructure, monitoring, upgrades |
| Network latency    | None - in-process                 | Real network round-trip                     |

**InMemory limitations:** Because all messages live in process memory, the InMemory transport cannot model multi-service fan-out, cannot survive process restarts, and does not exercise RabbitMQ-specific behavior like connection recovery, acknowledgement semantics, or topology conflicts.

**RabbitMQ operational cost:** RabbitMQ requires expertise to operate in production - cluster management, disk and memory alarms, queue type selection, and monitoring. Use a managed broker (CloudAMQP, Amazon MQ) if you want to reduce operational burden.

# Scope and middleware

Mocha uses a three-level feature scope: **bus**, **transport**, and **endpoint**. Features set at the bus level apply to all transports. Features set at the transport level override bus-level defaults for that transport. Features set at the endpoint level override both.

```text
Bus scope (global defaults)
  └── Transport scope (transport-specific overrides)
       └── Endpoint scope (endpoint-specific overrides)
```

To add middleware at the transport level:

```csharp
builder.Services
    .AddMessageBus()
    .AddRabbitMQ(transport =>
    {
        // Add dispatch middleware scoped to this transport
        transport.UseDispatch(myDispatchMiddleware);

        // Add receive middleware scoped to this transport
        transport.UseReceive(myReceiveMiddleware);

        // Insert middleware relative to existing ones
        transport.UseReceive(myMiddleware, after: "ConcurrencyLimiter");
        transport.UseDispatch(myMiddleware, before: "Serialization");
    });
```

This scoping model lets you run different middleware configurations per transport without affecting other transports in a multi-transport setup.

# Control handler binding

By default, transports bind handlers to endpoints implicitly using naming conventions:

```csharp
// Implicit binding (default) - handlers are auto-discovered
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.BindHandlersImplicitly(); // This is the default
    });
```

To take full control over which handlers go to which endpoints:

```csharp
// Explicit binding - you declare every endpoint
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.BindHandlersExplicitly();

        transport.Endpoint("order-events")
            .Handler<OrderPlacedEventHandler>();
    });
```

Explicit binding is useful when you need multiple handlers on the same queue, custom queue names, or fine-grained control over endpoint topology.

# Claim handlers for a transport

When you need to configure a handler's endpoint without switching to fully explicit binding, use `transport.Handler<T>()`. This claims the handler for the transport and returns a descriptor that lets you configure the endpoint through `ConfigureEndpoint()`:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.Handler<OrderPlacedEventHandler>()
            .ConfigureEndpoint(e => e.MaxPrefetch(50).MaxConcurrency(10));
    });
```

The handler still gets a convention-named endpoint - `Handler<T>()` does not change the name. It gives you a handle to configure that endpoint without needing `BindHandlersExplicitly()` or knowing the endpoint name.

For raw `IConsumer` types, the equivalent is `transport.Consumer<T>()`:

```csharp
transport.Consumer<OrderAuditConsumer>()
    .ConfigureEndpoint(e => e.MaxConcurrency(3));
```

`Handler<T>()` and `Consumer<T>()` are the primary tool for multi-transport routing. When a handler is claimed by a transport, it is bound to that transport regardless of which transport is marked as the default.

# Use multiple transports

You can register multiple transports and route specific handlers to specific transports. Mark one transport as the default with `.IsDefaultTransport()`. Any handler not explicitly claimed by another transport is bound to the default:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddEventHandler<AuditHandler>()
    .AddRabbitMQ(r => r.IsDefaultTransport())       // default for unclaimed handlers
    .AddInMemory(m => m.Handler<AuditHandler>());   // AuditHandler claimed by InMemory
// OrderPlacedEventHandler → RabbitMQ (default, implicit)
// AuditHandler → InMemory (claimed)
```

You can also use the older `Endpoint("name").Handler<T>()` pattern with explicit binding:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddEventHandler<ClickStreamHandler>()
    // Default transport for most messages
    .AddRabbitMQ()
    // High-throughput transport for click-stream data
    .AddInMemory(transport =>
    {
        transport.BindHandlersExplicitly();

        transport.Endpoint("click-stream")
            .Handler<ClickStreamHandler>();
    });
```

Each transport manages its own connections, topology, and middleware pipeline independently. A handler bound to one transport does not consume from another transport's endpoints.

# Next steps

- [InMemory Transport](/docs/mocha/v16/transports/in-memory) - Set up the InMemory transport for development and testing.
- [RabbitMQ Transport](/docs/mocha/v16/transports/rabbitmq) - Configure the RabbitMQ transport for production deployments.

> **Runnable example:** [MultiTransport](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/Transports/MultiTransport)

---
title: "Transports"
description: "Understand how transports move messages in Mocha, how the transport abstraction works, and how to choose between InMemory, PostgreSQL, and RabbitMQ."
---

A transport is the infrastructure layer that connects Mocha to a message broker. It manages connections, provisions topology (exchanges, queues, bindings), and handles the low-level details of dispatching and receiving messages. You write handlers and publish messages. The transport handles the rest.

The transport abstraction means your handlers, patterns, and pipeline are identical regardless of which broker you use. Only the infrastructure changes. Swap `.AddInMemory()` for `.AddPostgres()` or `.AddRabbitMQ()` and your application code stays unchanged. This portability is the core value of the [Message Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/MessageChannel.html) pattern: the sender and receiver are decoupled from the physical infrastructure that carries the message.

Mocha ships with three transports:

| Transport      | Package                    | Use case                                                     |
| -------------- | -------------------------- | ------------------------------------------------------------ |
| **InMemory**   | `Mocha.Transport.InMemory` | Development, testing, single-process scenarios               |
| **PostgreSQL** | `Mocha.Transport.Postgres` | Database-backed messaging when you already operate Postgres  |
| **RabbitMQ**   | `Mocha.Transport.RabbitMQ` | Production, distributed systems, multi-service architectures |

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

```csharp
// PostgreSQL - database-backed transport
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres("Host=localhost;Database=mocha_messaging;Username=postgres;Password=postgres");
```

Each `Add{Transport}()` method registers a transport instance, applies default conventions, and wires up the middleware pipelines.

# Choose a transport

Use this decision matrix to pick the right transport. Each column includes trade-offs - choose the one whose trade-offs you can accept:

| Criterion          | InMemory                                  | PostgreSQL                                  | RabbitMQ                                    |
| ------------------ | ----------------------------------------- | ------------------------------------------- | ------------------------------------------- |
| Setup effort       | None - zero dependencies                  | Requires a PostgreSQL database              | Requires a running broker                   |
| Message durability | **Messages lost on process exit**         | Messages are stored in database tables      | Messages survive broker restarts            |
| Multi-process      | **Single process only**                   | Multiple services sharing the same database | Multiple services, multiple instances       |
| Request/reply      | Supported                                 | Supported                                   | Supported                                   |
| Native scheduling  | None - requires `UsePostgresScheduling()` | Yes, built-in and cancellable               | None - requires `UsePostgresScheduling()`   |
| Operational cost   | None                                      | Database capacity, migrations, monitoring   | Broker infrastructure, monitoring, upgrades |
| Network latency    | None - in-process                         | Database round trip                         | Broker round trip                           |

**InMemory limitations:** Because all messages live in process memory, the InMemory transport cannot model multi-service fan-out, cannot survive process restarts, and does not exercise RabbitMQ-specific behavior like connection recovery, acknowledgement semantics, or topology conflicts.

**PostgreSQL trade-offs:** PostgreSQL is a good fit when you already operate PostgreSQL and want database-backed messaging without another broker. It favors operational simplicity and transactional consistency over dedicated broker throughput.

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

# Customize queues and binding

Use `transport.Queue("name")` when you need to customize receive topology. The queue builder is the primary surface for custom queue names, multiple handlers on one queue, queue-level settings, and handler assignment.

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.BindExplicitly();

        transport.Queue("order-processing")
            .BindImplicitly()
            .Handler<OrderPlacedEventHandler>();
    });
```

Calling `Queue("name")` without a handler, consumer, or `Receives<T>()` declares only the queue. As soon as you add a handler, consumer, or received message type, Mocha materializes a receive endpoint for that queue.

# Control implicit and explicit binding

By default, transports bind handlers implicitly using naming conventions:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.BindImplicitly(); // This is the default
    });
```

With implicit transport binding, registered handlers are auto-discovered, assigned to convention-named queues, and connected to convention-derived exchange, topic, or subscription bindings.

Use `BindExplicitly()` at the transport scope when the queues you configure should be the complete receive topology:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddRabbitMQ(transport =>
    {
        transport.BindExplicitly();

        transport.Queue("order-events")
            .BindImplicitly()
            .Handler<OrderPlacedEventHandler>();
    });
```

Use `BindImplicitly()` on the queue when you want a custom queue name but still want Mocha to generate the source bindings for that queue's handlers. Use `BindExplicitly()` on the queue when you provide those source bindings yourself, for example with `BindFrom(...)` or transport-specific topology declarations.

# Claim handlers for a transport

When you need to keep the convention-derived queue name and only tune a single handler endpoint, use `transport.Handler<T>()` at the end of the transport configuration. This claims the handler for the transport and returns a descriptor that lets you configure the endpoint through `ConfigureEndpoint()`:

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

The handler still gets a convention-named endpoint - `Handler<T>()` does not change the name. It gives you a handle to configure that endpoint without needing `BindExplicitly()` or knowing the endpoint name.

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

You can also use the queue descriptor with explicit binding:

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
        transport.BindExplicitly();

        transport.Queue("click-stream")
            .BindImplicitly()
            .Handler<ClickStreamHandler>();
    });
```

Each transport manages its own connections, topology, and middleware pipeline independently. A handler bound to one transport does not consume from another transport's endpoints.

# Next steps

- [InMemory Transport](./in-memory.md) - Set up the InMemory transport for development and testing.
- [PostgreSQL Transport](./postgres.md) - Configure database-backed messaging with PostgreSQL.
- [RabbitMQ Transport](./rabbitmq.md) - Configure the RabbitMQ transport for production deployments.

> **Runnable example:** [MultiTransport](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/Transports/MultiTransport)

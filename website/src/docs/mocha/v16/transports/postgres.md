---
title: "PostgreSQL Transport"
description: "Configure the PostgreSQL transport in Mocha for database-backed messaging with automatic topology provisioning, LISTEN/NOTIFY signaling, and schema multi-tenancy."
---

# PostgreSQL transport

> **Experimental:** The PostgreSQL transport is currently in preview and its API may change in future releases.

The PostgreSQL transport uses your existing database as a message broker. It stores messages, topics, queues, and subscriptions as rows in PostgreSQL tables, delivers messages using `SELECT ... FOR UPDATE SKIP LOCKED`, and signals consumers in real time with `LISTEN/NOTIFY`. When you already run PostgreSQL and want messaging without deploying a separate broker, this is the transport to use.

**When to choose PostgreSQL over a dedicated broker:**

- You already operate PostgreSQL and want to avoid additional infrastructure.
- You need ACID guarantees between your domain writes and message dispatch (outbox pattern).
- Your message volume fits within PostgreSQL's throughput (tens of thousands of messages per second).
- You value operational simplicity over maximum throughput.

**Trade-offs:** A dedicated message broker like RabbitMQ provides higher throughput, built-in clustering, and protocol-level flow control. The PostgreSQL transport trades peak throughput for operational simplicity and transactional consistency with your application data.

# Set up the PostgreSQL transport

By the end of this section, you will have a Mocha bus connected to PostgreSQL with automatic topology provisioning and schema migration.

## Install the package

```bash
dotnet add package Mocha.Transport.Postgres
```

## Register with a connection string

The simplest setup passes a connection string directly:

```csharp
using Mocha;
using Mocha.Transport.Postgres;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres("Host=localhost;Database=mocha_messaging;Username=postgres;Password=postgres");

var app = builder.Build();
app.Run();
```

`.AddPostgres(connectionString)` creates an `NpgsqlDataSource` from the connection string, runs schema migrations on first use, and provisions topics, queues, and subscriptions for your registered handlers.

## Register with .NET Aspire

When using .NET Aspire, define a PostgreSQL resource in your AppHost and reference it from each service:

```csharp
// AppHost
var postgres = builder.AddPostgres("postgres").WithPgAdmin();
var messagingDb = postgres.AddDatabase("messaging-db");

builder
    .AddProject<Projects.OrderService>("order-service")
    .WithReference(messagingDb)
    .WaitFor(messagingDb);
```

In each service, read the connection string from Aspire-injected configuration:

```csharp
using Mocha;
using Mocha.Transport.Postgres;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

var connectionString = builder.Configuration.GetConnectionString("messaging-db")!;

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres(t => t.ConnectionString(connectionString));

var app = builder.Build();
app.Run();
```

The Aspire component handles health checks, dashboard integration, and ensures the database is ready before the service starts.

## Register with advanced configuration

For full control over transport settings, use the configuration delegate:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.ConfigureDefaults(defaults =>
        {
            defaults.Endpoint.MaxBatchSize = 20;
            defaults.Endpoint.MaxConcurrency = 8;
        });
    });
```

## Verify it works

Add an endpoint that publishes through the bus and verify the handler executes:

```csharp
app.MapPost("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlacedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-1",
        TotalAmount = 99.99m
    }, CancellationToken.None);

    return Results.Ok();
});
```

Send a POST request to `/orders` and check your application logs. You should see the handler process the event. You can also inspect the `mocha_message`, `mocha_topic`, and `mocha_queue` tables directly to see the auto-provisioned topology.

# How connections work

The transport creates a single `NpgsqlDataSource` from your connection string, which manages an internal connection pool. All message operations (publish, send, read, delete) open a connection from this pool and return it when done.

Two connection-string settings are applied automatically:

| Setting     | Value   | Reason                                                                  |
| ----------- | ------- | ----------------------------------------------------------------------- |
| `Enlist`    | `false` | Prevents messages from enlisting in ambient `TransactionScope`          |
| `KeepAlive` | `30`    | Sends TCP keepalive packets every 30 seconds to detect dead connections |

A separate long-lived connection is opened for `LISTEN/NOTIFY` signaling. This connection subscribes to the notification channel and dispatches queue-change signals to receive endpoints for low-latency message pickup.

The transport checks database connectivity with a lightweight `SELECT 1` query with a 5-second timeout before polling. If the health check fails, the receive endpoint backs off with exponential delay.

# How topology works

The PostgreSQL transport stores topology as database rows instead of broker-side resources. Topics, queues, and subscriptions map to rows in the `mocha_topic`, `mocha_queue`, and `mocha_queue_subscription` tables.

**Events (publish/subscribe):** Each event type gets a topic row. Each service that subscribes creates a queue row and a subscription row linking the topic to the queue. Publishing inserts one message per subscribed queue using a single `INSERT...SELECT`, then calls `pg_notify` to signal each queue's consumers.

**Commands (send):** Each command type gets a queue row. Sending inserts the message directly into the queue and calls `pg_notify`.

**Request/reply:** The transport creates a temporary reply queue per service instance. The reply address is embedded in the request message headers so the responder knows where to send the reply.

## Default topology for event handlers

When you register an event handler with `AddEventHandler<T>()`, the transport creates:

1. A **topic** named after the message type (e.g., `order-placed-event`)
2. A **queue** named after the service and message type (e.g., `billing-service.order-placed-event`)
3. A **subscription** linking the topic to the queue

Publishing fans out to all subscribed queues in a single SQL statement. Each subscriber processes its own copy independently.

## Default topology for send handlers

When you register a request handler for send (fire-and-forget), the transport creates a single queue. Only one handler processes each message - this is the point-to-point guarantee.

# Configure transport-level defaults

You can set defaults that apply to all auto-provisioned queues, topics, and endpoints. This is useful when you want consistent settings across all resources without configuring each one individually.

Use `ConfigureDefaults` to set queue, topic, and endpoint defaults:

```csharp
builder.Services
    .AddMessageBus()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.ConfigureDefaults(defaults =>
        {
            // All queues will be auto-provisioned with auto-delete disabled
            defaults.Queue.AutoProvision = true;
            defaults.Queue.AutoDelete = false;

            // All topics will be auto-provisioned
            defaults.Topic.AutoProvision = true;

            // All endpoints will fetch 20 messages per batch
            // and process up to 8 concurrently
            defaults.Endpoint.MaxBatchSize = 20;
            defaults.Endpoint.MaxConcurrency = 8;
        });
    });
```

Available queue defaults:

| Property        | Type    | Description                                                      |
| --------------- | ------- | ---------------------------------------------------------------- |
| `AutoProvision` | `bool?` | Whether queues are auto-provisioned at startup (default: `true`) |
| `AutoDelete`    | `bool?` | Whether queues are auto-deleted when unused (default: `false`)   |

Available topic defaults:

| Property        | Type    | Description                                                      |
| --------------- | ------- | ---------------------------------------------------------------- |
| `AutoProvision` | `bool?` | Whether topics are auto-provisioned at startup (default: `true`) |

Available endpoint defaults:

| Property         | Type   | Description                                                                    |
| ---------------- | ------ | ------------------------------------------------------------------------------ |
| `MaxBatchSize`   | `int?` | Maximum messages fetched per poll cycle (default: `10`)                        |
| `MaxConcurrency` | `int?` | Maximum messages processed in parallel (default: `Environment.ProcessorCount`) |

Defaults never override explicitly configured values. If you configure an endpoint with a specific `MaxBatchSize`, that setting takes precedence over the transport default.

# Schema and multi-tenancy

The transport stores all data in the `public` schema with a `mocha_` table prefix by default. Table and channel naming is controlled by the `PostgresSchemaOptions` class, which computes fully qualified names from two properties:

| Property      | Default    | Description                           |
| ------------- | ---------- | ------------------------------------- |
| `Schema`      | `"public"` | The PostgreSQL schema name            |
| `TablePrefix` | `"mocha_"` | The prefix applied to all table names |

With the defaults, the transport creates the following tables:

| Table                             | Purpose                             |
| --------------------------------- | ----------------------------------- |
| `public.mocha_topic`              | Topic definitions                   |
| `public.mocha_queue`              | Queue definitions                   |
| `public.mocha_queue_subscription` | Topic-to-queue subscriptions        |
| `public.mocha_message`            | Message storage                     |
| `public.mocha_consumers`          | Consumer registration and heartbeat |
| `public.mocha_migrations`         | Migration tracking                  |

The LISTEN/NOTIFY channel is derived from the table prefix: `mocha_queue_changed`.

Changing `Schema` and `TablePrefix` shifts all table names accordingly. For example, with `Schema = "tenant_a"` and `TablePrefix = "bus_"`, the tables become `tenant_a.bus_topic`, `tenant_a.bus_queue`, and so on, and the notification channel becomes `bus_queue_changed`. This enables multi-tenant deployments and coexistence with other applications in the same database.

## Schema migration

The transport runs migrations automatically on first use. Migrations are protected by a PostgreSQL advisory lock (`pg_advisory_xact_lock`) to prevent concurrent migration attempts from multiple service instances starting simultaneously.

Each migration is tracked in the migrations table and is idempotent - running the same migration twice has no effect. The migration creates the schema if it does not exist, then applies each pending migration in order within a single transaction.

:::warning
**Advisory lock scope.** The advisory lock ID is fixed. If you run multiple independent Mocha transports in the same PostgreSQL cluster with different table prefixes, they share the same advisory lock. This is safe - it serializes migrations but does not block normal message operations.
:::

# Declare custom topology

Mocha auto-provisions topology by default. To declare additional topics, queues, or subscriptions:

```csharp
builder.Services
    .AddMessageBus()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);

        // Declare a topic
        transport.DeclareTopic("order-events");

        // Declare a queue
        transport.DeclareQueue("billing-orders");

        // Declare a subscription linking the topic to the queue
        transport.DeclareSubscription("order-events", "billing-orders");
    });
```

All explicitly declared topology is provisioned when the transport starts, before receive endpoints begin consuming.

To configure explicit endpoint-to-queue binding with handler assignment:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<ProcessOrderCommandHandler>()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);
        transport.BindHandlersExplicitly();

        // Declare the queue
        transport.DeclareQueue("process-order");

        // Bind a receive endpoint to the queue with a handler
        transport.Endpoint("process-order-ep")
            .Queue("process-order")
            .Handler<ProcessOrderCommandHandler>();

        // Configure a dispatch endpoint for sending to the queue
        transport.DispatchEndpoint("send-demo")
            .ToQueue("process-order")
            .Send<ProcessOrderCommand>();
    });
```

# Control auto-provisioning

By default, the transport auto-provisions all topology resources (topics, queues, subscriptions) in the database at startup. In environments where database schema is managed externally - for example by Flyway, Liquibase, or a CI/CD pipeline - you can disable auto-provisioning so the transport expects resources to already exist.

## Disable globally

Turn off auto-provisioning for the entire transport:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.ConfigureDefaults(defaults =>
        {
            defaults.Queue.AutoProvision = false;
            defaults.Topic.AutoProvision = false;
        });
    });
```

With auto-provisioning disabled, the transport will not insert any topic, queue, or subscription rows. All rows must already exist in the database before the transport starts.

## Common patterns

**Fully managed infrastructure:** Disable auto-provisioning globally and pre-populate the topology tables through your database migration pipeline.

**Selective provisioning:** Disable globally but let the transport provision specific resources it owns.

**Opt-out individual resources:** Keep auto-provisioning enabled but skip specific resources that are managed elsewhere.

The effective auto-provision value for each resource follows a cascading pattern:

| Resource setting | Transport default | Result          |
| ---------------- | ----------------- | --------------- |
| `true`           | any               | Provisioned     |
| `false`          | any               | Not provisioned |
| not set          | `true` (default)  | Provisioned     |
| not set          | `false`           | Not provisioned |

When a resource does not specify `AutoProvision`, it inherits the transport-level default. When the transport does not specify `AutoProvision`, it defaults to `true`.

# Batching and concurrency

Customize batch size, concurrency, and handler assignments on receive endpoints:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddPostgres(transport =>
    {
        transport.ConnectionString(connectionString);
        transport.BindHandlersExplicitly();

        transport.Endpoint("order-processing")
            .Queue("orders.processing")
            .MaxBatchSize(20)
            .MaxConcurrency(10)
            .Handler<OrderPlacedEventHandler>();
    });
```

**MaxBatchSize** controls how many messages the endpoint reads from the database in a single `SELECT ... FOR UPDATE SKIP LOCKED` query. Default: `10`. Higher values reduce round trips but lock more rows simultaneously.

**MaxConcurrency** controls how many messages the endpoint processes in parallel using `Parallel.ForEachAsync`. Default: `Environment.ProcessorCount`. Set this based on your handler's throughput characteristics.

A good starting point: set `MaxBatchSize` equal to or slightly higher than `MaxConcurrency`. For I/O-bound handlers (database queries, HTTP calls), increase `MaxConcurrency` beyond the processor count. For CPU-bound handlers, keep `MaxConcurrency` close to `Environment.ProcessorCount`.

# Message delivery

The transport uses a hybrid polling and notification model for message delivery.

## Polling with LISTEN/NOTIFY

Receive endpoints do not busy-poll the database. Instead, they wait on an `AsyncAutoResetEvent` signal:

1. When a message is published or sent, the transport calls `pg_notify('mocha_queue_changed', queue_name)`.
2. A long-lived LISTEN connection receives the notification and sets the signal for the matching receive endpoint.
3. The endpoint wakes up and reads available messages.

If the endpoint drains all messages (empty read), it goes back to waiting on the signal. If it reads a full batch, it immediately reads again until the queue is drained.

## Concurrent consumers with SKIP LOCKED

Multiple consumers can process messages from the same queue concurrently. The transport uses `SELECT ... FOR UPDATE SKIP LOCKED` to lock messages for processing without blocking other consumers. Each consumer gets a unique `consumer_id` (a GUID), and locked messages are assigned to that consumer.

## Retry backoff

When a message processing attempt fails, the message is released back to the queue with its `delivery_count` incremented. Redelivery is delayed using exponential backoff computed in SQL:

```text
delay = 2^min(delivery_count, 10) seconds
```

This means: 2s, 4s, 8s, 16s, ... up to a maximum of 1024 seconds (~17 minutes). Messages that exceed `max_delivery_count` (default: 10) are moved to a fault queue.

## Scheduled messages

Messages with a `scheduled_time` in the future are not eligible for delivery until that time arrives. The transport queries for the next scheduled time after draining all available messages and sets a delayed trigger to wake the polling loop at the right moment.

## Message expiration

Messages with an `expiration_time` are automatically skipped during reads when the expiration has passed. A background cleanup task deletes expired messages every 60 seconds.

# Background maintenance tasks

The transport runs several background tasks to maintain system health:

| Task                     | Interval | Description                                                                                                 |
| ------------------------ | -------- | ----------------------------------------------------------------------------------------------------------- |
| Consumer heartbeat       | 10s      | Updates the consumer's `updated_at` timestamp to indicate liveness                                          |
| Expired consumer cleanup | 60s      | Removes consumers with no heartbeat for 2 minutes, cascading deletes to their temporary queues and messages |
| Message cleanup          | 60s      | Deletes expired messages that have not been picked up                                                       |
| Queue monitoring         | 5min     | Logs queue statistics (message count, scheduled count, age)                                                 |
| Topic monitoring         | 5min     | Logs topic statistics (subscription count)                                                                  |
| Queue overflow cleanup   | 5min     | Enforces per-queue message limits (default: 100,000) by deleting oldest messages                            |

All background tasks use exponential backoff on failure and shut down gracefully when the transport stops.

# Auto-provisioned resource naming

| Resource          | Naming convention                                             | Created when                       |
| ----------------- | ------------------------------------------------------------- | ---------------------------------- |
| Topic             | Message type name (e.g., `order-placed-event`)                | First publish or subscribe         |
| Queue (subscribe) | Service and message type (e.g., `billing.order-placed-event`) | Handler is bound to the transport  |
| Queue (send)      | Message type name (e.g., `process-order-command`)             | First send or handler registration |
| Reply queue       | Instance-specific name                                        | Transport starts                   |
| Subscription      | Topic-to-queue link                                           | Endpoint discovery phase           |

All auto-provisioned resources are inserted as rows in the corresponding topology tables at transport startup.

# Next steps

- [Transports Overview](/docs/mocha/v16/transports) - Understand the transport abstraction and lifecycle.
- [Handlers and Consumers](/docs/mocha/v16/handlers-and-consumers) - Learn about handler types and consumer configuration.
- [Reliability](/docs/mocha/v16/reliability) - Configure dead-letter routing, outbox, inbox, and fault handling.
- [Middleware and Pipelines](/docs/mocha/v16/middleware-and-pipelines) - Customize the receive and dispatch pipelines.
- [Routing and Endpoints](/docs/mocha/v16/routing-and-endpoints) - Understand naming conventions and endpoint model.

> **Runnable example:** [PostgresTransport](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/PostgresTransport)
>
> **Multi-service demo:** The PostgresTransport example includes three services (OrderService, ShippingService, NotificationService) orchestrated with .NET Aspire, demonstrating publish/subscribe, send, request/reply, and batch processing over a shared PostgreSQL database.

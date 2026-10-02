---
title: "NATS Transport"
description: "Configure the NATS JetStream transport in Mocha, including shared stream ownership, consumer provisioning, subject naming, request/reply over core NATS, and scheduled messages."
---

The NATS transport connects Mocha to a NATS server running JetStream. It provisions streams and durable consumers, acknowledges messages, supports request/reply over core NATS, and delegates scheduled delivery to the broker.

# Set up the NATS transport

## Install the package

```bash
dotnet add package Mocha.Transport.Nats
```

## Register with .NET Aspire

The Aspire NATS component registers the connection:

```bash
dotnet add package Aspire.NATS.Net
```

```csharp
using Mocha;
using Mocha.Transport.Nats;

var builder = WebApplication.CreateBuilder(args);

// Aspire registers INatsConnection from the "nats" connection resource
builder.AddNatsClient("nats");

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddNats(nats => nats.StreamName("order-service"));

var app = builder.Build();
app.Run();
```

`.AddNats()` picks up the `INatsConnection` from dependency injection and uses it for both publishing and consuming. NATS.Net owns reconnection, so the transport has no connection manager of its own.

## Register with a manual connection

```csharp
using Mocha;
using Mocha.Transport.Nats;
using NATS.Client.Core;

builder.Services.AddSingleton<INatsConnection>(_ => new NatsConnection(new NatsOpts
{
    Url = "nats://localhost:4222",

    // NATS.Net drops messages when a subscriber falls behind. Request/reply responses arrive on a
    // core subscription, so leaving the default in place can silently lose them.
    SubPendingChannelFullMode = BoundedChannelFullMode.Wait
}));

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddNats(nats => nats.StreamName("order-service"));
```

The transport logs a warning at start-up if the connection is left on the dropping default.

## Use a custom connection provider

Supply your own provider when the connection is not resolvable from dependency injection, or when the host and port reported in endpoint addresses need to differ from the connection string:

```csharp
.AddNats(nats => nats.ConnectionProvider(services =>
    new NatsConnectionProvider(services.GetRequiredKeyedService<INatsConnection>("primary"))))
```

Only the first server in a multi-server connection string is used for endpoint addresses, because the transport base address has to be a single stable value.

## Verify it works

The transport provisions its stream and one durable consumer per handler before any endpoint starts, so a clean start-up means the topology is in place. Publish an event once the bus is running:

```csharp
await bus.PublishAsync(new OrderPlaced(orderId, "Mechanical Keyboard"));
```

# How Mocha concepts map onto JetStream

| Mocha                          | JetStream                                      |
| ------------------------------ | ---------------------------------------------- |
| Exchange, fan-out              | Stream `Subjects` with `*` and `>` wildcards   |
| Queue                          | Durable pull consumer                          |
| Binding, routing key list      | `ConsumerConfig.FilterSubjects`                |
| Competing consumers            | Several instances sharing one durable consumer |
| Concurrency                    | Parallel handling bounded by `MaxConcurrency`  |
| Prefetch                       | Local buffer, also bounded by `MaxConcurrency` |
| Back pressure across instances | `MaxAckPending`                                |
| Ack, nack                      | `AckAsync`, `NakAsync`                         |
| Retry backoff                  | `MaxDeliver` and `ConsumerConfig.Backoff`      |
| Reply endpoints                | Core NATS request/reply, not JetStream         |

`Send` and `Publish` converge on a single subject, and subscribers select what they receive through consumer filter subjects.

# Naming

Streams are containers; all routing happens by subject, and the stream name never appears on the publish path.

| Concept          | Derived from                                 | Example                          |
| ---------------- | -------------------------------------------- | -------------------------------- |
| Stream           | `StreamName`, else the host's service name   | `ORDER_SERVICE`                  |
| Subject          | The message type's namespace and name        | `contracts.orders.order-created` |
| Durable consumer | The host's service name and the handler name | `order-service_order-created`    |

Endpoint names contain dots, which are the natural subject separator. Dots are illegal in stream and consumer names, along with `*`, `>`, whitespace and path separators, so derived names replace them with underscores. NATS uses these names as storage directory names and recommends keeping them under 32 characters; the transport logs a warning at start-up when a derived name is longer.

## Where fault subjects live

A failing endpoint forwards to a subject derived from the endpoint name with `_error` appended, and a skipped message to one with `_skipped`. Both are scoped by the **host service name**, whether the endpoint took its name from a handler or you named it yourself:

| Endpoint                                   | Fault subject                        |
| ------------------------------------------ | ------------------------------------ |
| `OrderCreatedHandler`, named by convention | `order-service.order-created_error`  |
| `Endpoint("order-commands")`               | `order-service.order-commands_error` |
| `Endpoint("contracts.orders.commands")`    | `contracts.orders.commands_error`    |

Stream subjects have to be disjoint across the whole server. An unscoped `order-commands_error` would sit at the root of the subject space, where every service naming an endpoint the same way claims the same subject, and whichever provisions second fails to start. A name you namespaced yourself is left as it is.

Fault subjects are therefore in the service's namespace rather than the message type's, so a declaration covering only the contracts namespace does not capture them.

## What names the stream, and what names the durables

`nats.StreamName("order-service")` names the convention stream, and nothing else. Durable consumer names are scoped by the **host** service name, which comes from the messaging host builder, else the `SERVICE_NAME` environment variable, else `OTEL_SERVICE_NAME`, else the entry assembly name.

Both default to the host service name. Name the stream explicitly when it should not be named after the service, such as a stream several services co-own:

```csharp
.AddNats(nats => nats.StreamName("orders"))
```

Two services that end up with the same host service name derive the same durable name for a handler of the same type, share one durable, and compete for messages instead of each receiving a copy. Set it with the `SERVICE_NAME` environment variable, or in code:

```csharp
bus.ConfigureMessageBus(mocha => mocha.Host(host => host.ServiceName("order-service")));
```

# Streams are shared

Subjects are derived from the message type, so every service that touches a message type derives the same subject. JetStream requires a stream's subjects to be disjoint from every other stream's, which means one subject belongs to exactly one stream no matter how many services publish to or consume from it.

The transport therefore claims only what nothing else has claimed:

1. For each subject the service publishes, it asks the server whether a stream already captures it.
2. Subjects that are already captured are bound to the owning stream, and nothing is declared for them.
3. Only the remaining subjects go into this service's own convention stream.

The first service to start creates the stream; later services bind to it. Two services subscribing to the same event both start cleanly, and each gets its own durable consumer on the shared stream. If two services race, the loser yields and binds to the winner's stream.

Because a convention stream is shared, this also means:

- Its **retention, storage, limits and replicas are set by whichever service created it**. A service adding subjects to an existing stream never rewrites those, and the server rejects an attempt to change storage outright.
- Its **subject list only ever grows**. A service that rolls out publishing fewer subjects will not strip the ones its peers still publish to.

> [!WARNING]
> Deriving stream names from the service name means `order-service` and `order.service` both produce `ORDER_SERVICE`. Two services whose names differ only by a separator will share a stream by accident rather than by subject ownership.

## Declaring a stream under the derived name

Declaring a stream named the same as the one the service name derives is the common case, and the two are folded together: the subjects the convention stream would have claimed are added to the declaration, while the retention, storage and limits you declared are kept. Handlers you did not put on a named endpoint still get their subjects captured.

A declared stream is never silently discarded. If any of its subjects are already owned by another stream, start-up fails naming both the subject and the owning stream, because JetStream requires stream subjects to be disjoint:

```text
Stream 'ORDER_SERVICE' cannot be provisioned because its subjects overlap a stream
that already exists: 'orders_error' is already captured by stream 'ORDER_FAULTS'.
```

Resolve it by removing the overlapping subject from the declaration, deleting the stream that owns it, or dropping the declaration and letting the transport bind to the existing stream.

# Handling a family of messages on one endpoint

A handler bound to an interface or base type does not receive its implementations by default. A publish resolves its subject from the **concrete runtime type**, so `PublishAsync<IOrderCommand>(command)` and `PublishAsync(command)` behave identically: both go to the concrete type's subject. The generic argument does not select the subject.

To funnel a family onto one endpoint, filter the subjects it publishes to. When the family shares a namespace, which it does whenever the messages live together, one wildcard covers all of it:

```csharp
nats.Endpoint("order-commands")
    .Handler<OrderCommandHandler>()          // IEventHandler<IOrderCommand>
    .Subject("contracts.orders.>")
    // Ordered delivery comes from the single durable; ordered handling needs this.
    .MaxConcurrency(1);
```

A wildcard stays correct as the family grows. Naming each concrete subject works too, and is what to use when the family does not map onto a namespace:

```csharp
    .Subject("contracts.orders.cancel-order")
    .Subject("contracts.orders.hold-order")
```

Implementations are not discovered automatically: message types are completed after topology is discovered, so their base types are not yet known when subject filters are built. The interface's own subject is filtered as well, and is dropped when a wildcard already covers it, since JetStream rejects overlapping filter subjects.

Whatever an endpoint filters is also captured by a stream, so no stream declaration is needed for this. The handler receives each message typed as the interface: the envelope carries its enclosed types, and the receive pipeline selects the handler from those. Because one durable on one stream delivers in order, this is also the only arrangement that orders a whole family relative to itself.

# Which stream does a consumer read from?

A JetStream consumer must be created on the stream that captures its subject, and that stream may belong to another service entirely.

The transport resolves this at start-up by asking the server which stream captures each subscribed subject, so an endpoint declares what it consumes rather than where it lives. Resolution fails at start-up when:

- No stream captures the subject, because the publishing service has not been deployed or its stream was never provisioned.
- Several streams capture it, so the choice would be arbitrary.
- One consumer's subjects are spread across different streams, which a single consumer cannot read.

Declaring the stream removes the start-up ordering dependency entirely, because whichever service starts first provisions it:

```csharp
.AddNats(nats => nats
    .StreamName("shipping-service")
    .DeclareStream("ORDER_SERVICE")
        .Subject("contracts.orders.>"));
```

A declared stream and the convention stream coexist. Declaring the publisher's stream does not stop this service getting a stream for the subjects it publishes itself.

# Publishing to an uncaptured subject

A JetStream publish waits for an acknowledgement from the stream capturing the subject. If no stream captures it, the call does not fail immediately, it times out. The transport therefore verifies subject coverage while starting, and fails naming the subject that no stream captures.

# Where a consumer starts reading

A consumer created by the transport starts at the end of the stream, so it receives what is published after it exists and nothing from before. A stream retains messages independently of any consumer and is shared between services, so starting at the beginning would hand a service subscribing to an existing subject for the first time every message still retained on it.

This applies only when the consumer is created. One that already exists resumes from its own position, so a service that was down still receives what arrived while it was away.

To replay from the start of the stream:

```csharp
nats.Endpoint("order-audit")
    .Handler<OrderAuditHandler>()
    .DeliverFrom(ConsumerConfigDeliverPolicy.All);
```

Because it only takes effect at creation, pointing an existing durable at a different position means deleting it first.

# Own the topology yourself

`BindExplicitly` stops the transport deriving topology, for a cluster whose streams are managed by its operators:

```csharp
.AddNats(nats => nats
    .StreamName("order-service")
    .BindExplicitly()
    .DeclareStream("ORDER_SERVICE")
        .Subject("contracts.orders.>")
        // Fault subjects sit under the host service name, so one wildcard covers them.
        .Subject("order-service.>"))
```

Under explicit binding no convention stream is created, and an endpoint filters only the subjects it names with `Subject` rather than any derived from its handlers. Whatever the service publishes to still has to be captured by a stream you declare, including the fault and skipped subjects, or start-up fails naming the subject that has no stream.

A single endpoint can override the transport:

```csharp
nats.Endpoint("order-processing")
    .Handler<OrderPlacedHandler>()
    .Subject("contracts.orders.>")
    .BindExplicitly();
```

This is distinct from `AutoProvision(false)`, which keeps convention topology but creates none of it, expecting the streams and consumers to exist already. Explicit binding changes what topology the transport derives; auto-provisioning changes whether it creates it.

# Control auto-provisioning

Auto-provisioning is on by default. Turning it off leaves retention and limits to whoever manages the cluster, rather than to whichever instance starts first:

```csharp
.AddNats(nats => nats.StreamName("order-service").AutoProvision(false))
```

With auto-provisioning off, the transport creates nothing and binds to the streams that already exist. Subject verification still runs, so a missing stream is a start-up failure rather than a publish timeout.

Auto-provisioning can also be overridden per resource:

```csharp
.AddNats(nats => nats
    .StreamName("order-service")
    .AutoProvision(false)
    .DeclareStream("ORDER_SERVICE")
        .Subject("contracts.orders.>")
        .AutoProvision(true))
```

# Configure endpoints

An endpoint is the receive side: a durable consumer and the subjects it filters. Naming it explicitly sets the durable name instead of deriving it from the handler type:

```csharp
.AddNats(nats => nats
    .StreamName("order-service")
    .Endpoint("order-processing")
    .Handler<OrderPlacedEventHandler>()
    .MaxConcurrency(10))
```

| Method                                            | Effect                                                               |
| ------------------------------------------------- | -------------------------------------------------------------------- |
| `Handler<T>` / `Consumer<T>`                      | Places a handler on this endpoint                                    |
| `Receives<T>`                                     | Binds a message type without naming a handler                        |
| `Subject`                                         | Adds a subject filter beyond those derived from handlers             |
| `ConsumerName`                                    | Sets the durable name, which defaults to the sanitized endpoint name |
| `FromStream`                                      | Reads from a named stream instead of resolving one at start-up       |
| `MaxConcurrency`                                  | Bounds parallel handling and the local buffer                        |
| `FaultEndpoint` / `SkippedEndpoint`               | Replaces the derived address failed or skipped messages go to        |
| `DisableFaultEndpoint` / `DisableSkippedEndpoint` | Stops forwarding them at all                                         |

## Scoping an endpoint the framework names

Endpoint names are host-scoped only for routes a handler subscribes to. A route the framework registers for one of its own message types, such as the saga timeout, is named after that message type with no service prefix. Every service hosting a saga therefore derives the same `saga-timed-out` endpoint, which means the same durable name and the same fault subjects.

Declaring the endpoint under the name the framework derives configures that endpoint rather than adding a second one, so both can be scoped:

```csharp
nats.Endpoint("saga-timed-out")
    .ConsumerName($"{serviceName}_saga-timed-out")
    .FaultEndpoint(new Uri($"nats:s/{serviceName}.saga-timed-out_error"))
    .SkippedEndpoint(new Uri($"nats:s/{serviceName}.saga-timed-out_skipped"));
```

> [!WARNING]
> Scoping the durable gives each service its own consumer on the shared timeout subject, so each receives every service's timeouts and ignores those whose saga it does not hold. Leaving the durable shared instead means each timeout reaches only one service, chosen arbitrarily. Neither is a substitute for the endpoint being scoped where it is named.

# Declare topology resources

`DeclareStream` and `DeclareConsumer` configure JetStream resources directly, for settings the endpoint API does not expose:

```csharp
.AddNats(nats =>
{
    nats.StreamName("order-service")
        .Endpoint("order-processing")
        .Handler<OrderPlacedEventHandler>();

    nats.DeclareStream("ORDER_SERVICE")
        .Subject("contracts.orders.>")
        .Retention(StreamConfigRetention.Interest)
        .MaxAge(TimeSpan.FromDays(7))
        .MaxMessages(1_000_000)
        .Replicas(3);

    nats.DeclareConsumer("order-processing")
        .AckWait(TimeSpan.FromSeconds(30))
        .MaxDeliver(5)
        .Backoff(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5));
})
```

Naming a consumer that an endpoint already derives folds the two together: the filter subjects the endpoint contributes are kept, and the settings declared here win. Use it to set `AckWait`, `MaxDeliver`, `Backoff` and `AckProgressEvery` on a convention endpoint.

# Set defaults for convention topology

`ConfigureDefaults` sets what the conventions apply to the streams and consumers they create. Settings on an individual stream, consumer or endpoint win over them:

```csharp
.AddNats(nats => nats
    .StreamName("order-service")
    .ConfigureDefaults(defaults =>
    {
        defaults.Stream.Retention = StreamConfigRetention.Interest;
        defaults.Stream.MaxAge = TimeSpan.FromDays(7);
        defaults.Stream.NumReplicas = 3;
        defaults.Consumer.AckWait = TimeSpan.FromSeconds(45);
        defaults.Consumer.MaxDeliver = 5;
    }))
```

This is how the convention stream gets retention, storage, limits and replicas without being declared under its derived name. Defaults are applied when a resource is created, so a stream this service binds to rather than creates keeps the settings of whichever service created it.

# Concurrency

`MaxConcurrency` bounds how many messages one instance handles at a time, and how many it buffers locally. It is not mapped onto `MaxAckPending`, the server-side ceiling shared by every instance reading the durable: lowering that to one instance's concurrency would starve the others.

A pulled message counts as delivered the moment it reaches the local buffer, so its acknowledgement deadline is already running while it waits for a free handler. Buffering far more than can be handled concurrently would expire the tail of the buffer and have it redelivered before it was ever handled.

# Deduplication

JetStream can deduplicate at the broker, and the transport leaves it off. The failure mode is quiet: a stream discards a repeated identifier and acknowledges the publish as though it had been stored, so a deliberate republish of the same message vanishes with no error, and a `SkipInbox` on the receiving side never gets to see it.

Deduplicate with the inbox instead. It is transport independent, and it is scoped per consumer rather than per subject, so one subscriber having already processed a message does not suppress it for the others.

To opt into the broker's own deduplication:

```csharp
.AddNats(nats => nats
    .StreamName("order-service")
    .EnablePublishDeduplication())
```

That sends a `Nats-Msg-Id` with every publish, qualified by destination subject. The qualification is required because deduplication is scoped to the stream rather than the subject: without it, republishing a message inside the same stream, which is what dead-lettering does, is discarded as a duplicate.

The window comes from the stream, and it cannot be turned off from the client once the header is being sent: a zero `DuplicateWindow` is omitted from the request and the server applies its own default.

```csharp
nats.DeclareStream("ORDER_SERVICE").DeduplicateWithin(TimeSpan.FromMinutes(2));
```

# Long-running handlers

A handler that runs longer than the consumer's `AckWait` is redelivered while it is still working. JetStream can extend the deadline while the handler runs:

```csharp
nats.Endpoint("order-processing")
    .Handler<OrderProcessingHandler>()
    .AckWait(TimeSpan.FromSeconds(30))
    .AckProgressEvery(TimeSpan.FromSeconds(10));
```

It is off by default, and costs a background task per in-flight message.

`AckWait`, `MaxDeliver`, `Backoff`, `MaxAckPending` and `DeliverFrom` are available on the endpoint and on `DeclareConsumer`. Prefer the endpoint: declaring a consumer means naming the durable, and a name that does not match the one the endpoint derives silently creates a second consumer instead of configuring the first.

# Scheduled messages

JetStream holds scheduled messages itself, so the transport does not run a scheduler:

```csharp
.AddNats(nats => nats.StreamName("order-service").EnableScheduling());
```

Enabling scheduling turns on per-message TTL and message schedules on the stream, and captures a scheduling namespace alongside each subject. A separate subject is required because the server refuses a schedule whose target is the subject it was published to.

Each scheduled message gets its own subject inside that namespace, so the stream captures it as a filter such as `contracts.orders.order-created._schedule.>`. A subject holds at most one schedule, so messages sharing a scheduling subject would replace one another and only the last would ever be delivered.

`MessageEnvelope.ScheduledTime` maps to a message schedule, requiring server 2.12, and `DeliverBy` maps to a per-message TTL, requiring server 2.11. Dispatching either to a server too old to support it fails with an explicit error rather than silently.

## Cancelling a scheduled message

`SchedulePublishAsync` returns a token, and cancelling with it removes the message while it is still waiting:

```csharp
var scheduled = await bus.SchedulePublishAsync(new OrderExpired(orderId), deadline, ct);

if (scheduled.Token is { } token)
{
    await bus.CancelScheduledMessageAsync(token, ct);
}
```

Cancellation reports `false` once the message has been released to its target, and for a token that was already cancelled. In both cases there is no schedule left to withdraw.

# Failure handling

A handler that fails is negatively acknowledged and redelivered according to the consumer's `MaxDeliver` and `Backoff`. Once Mocha's resilience policy gives up, the message is republished to the endpoint's error subject, which the transport verifies is captured by a stream at start-up.

If a consume loop fails outright, for example because its consumer was deleted on the server, the transport logs the failure and restarts the loop rather than leaving the endpoint stopped.

# Shutdown

Stopping drains rather than aborting: no new messages are pulled, but everything already buffered is handled and acknowledged, bounded by the host shutdown timeout. Handlers only observe cancellation once that timeout expires, and their messages are then released for redelivery.

# Next steps

- [Transports Overview](./index.md) - Understand the transport abstraction and lifecycle.
- [Handlers and Consumers](../handlers-and-consumers.md) - Learn about handler types and consumer configuration.
- [Reliability](../reliability.md) - Configure dead-letter routing, outbox, inbox, and fault handling.

> **Runnable example:** [Nats](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/Transports/Nats)

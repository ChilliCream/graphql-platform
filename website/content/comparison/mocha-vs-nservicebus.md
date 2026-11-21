---
title: "Mocha vs. NServiceBus"
metaTitle: "Mocha vs NServiceBus: An MIT-Licensed NServiceBus Alternative"
description: "Compare Mocha and NServiceBus for messaging in .NET: licensing and pricing, routing, request/reply, retries, the outbox, sagas, and Native AOT."
featuredImage: "mocha-vs-nservicebus.png"
date: "2026-10-29"
tags: ["mocha", "comparison", "nservicebus"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "NServiceBus"
    url: "https://particular.net/nservicebus"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license file, no endpoint count, and no limit on messages."
  - question: "How much does NServiceBus cost?"
    answer: "NServiceBus is free outside production. In production, the Community tier is free for up to 3 logical endpoints and 10,000 messages a day. Paid tiers cost $0.75 to $3.00 per logical endpoint per day, and the tier depends on both the number of endpoints and the daily message throughput across the organization. Organizations whose income, expenses, and assets are each under $1 million can apply to the Small Business Program for a free license."
  - question: "Does Mocha have sagas, an outbox, and retries?"
    answer: "Yes. Mocha has state machine sagas with timeouts, a transactional outbox and inbox, scheduled messages, and exception policies that retry, redeliver, or dead-letter a failed message by exception type."
  - question: "Can a Mocha service await the response to a request?"
    answer: "Yes. A request declares its response type, its handler returns the response, and the caller awaits it with RequestAsync. No extra package or configuration is needed."
  - question: "Does Mocha support Native AOT?"
    answer: "Yes. A source generator registers your handlers at build time, and for a service published with Native AOT it generates serialization code from the JsonSerializerContext you reference in the MessagingModule attribute."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license, with no license file and no endpoint count. Build your first service with the quick start, or move from NServiceBus step by step with the migration guide. Moving a large system?"
  primary:
    label: "Get started with Mocha"
    href: "/docs/mocha/quick-start"
  secondary:
    label: "Migrate from NServiceBus"
    href: "/docs/mocha/migration/coming-from-nservicebus"
  contact: "Talk to us about your migration"
---

Mocha does the messaging you build with NServiceBus, free for every company under the MIT license: no price per endpoint, no throughput tiers, and no license file. One `IMessageBus` publishes, sends, and requests, a command finds its handler's queue by convention instead of a routing table, a request awaits its typed response, and sagas are state machines. A source generator registers your handlers at build time, ready for Native AOT.

# Free for every company, at any scale

Mocha is MIT licensed, like Hot Chocolate. Any company can use it in production for free. There is no paid edition, no license file, and no limit on endpoints or messages, and the source code is public.

NServiceBus is [dual licensed](https://github.com/Particular/NServiceBus/blob/master/LICENSE.md) under the copyleft Reciprocal Public License 1.5 or Particular's commercial license agreement. It is free in development, test, and other non-production environments. In production, [Particular charges](https://particular.net/pricing) per logical endpoint per day. The Community tier is free for up to 3 endpoints and 10,000 messages a day. The paid tiers cost $0.75, $1.25, $2.00, or $3.00 per endpoint per day, from $273.75 to $1,095 per endpoint per year, and your tier is the higher of what your number of endpoints and your daily message throughput across the organization require. Organizations whose income, expenses, and assets are each under $1 million can apply to the [Small Business Program](https://particular.net/pricing/small-business-program) for a free license, and smaller discounts apply up to $5 million.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

```csharp filename="NServiceBus 10.2"
var endpoint =
    new EndpointConfiguration("Ordering");
endpoint.UseSerialization<SystemJsonSerializer>();
endpoint.EnableInstallers();

var routing = endpoint.UseTransport(
    new RabbitMQTransport(
        RoutingTopology.Conventional(
            QueueType.Quorum),
        connectionString));
routing.RouteToEndpoint(
    typeof(ReserveStock), "Inventory");

builder.Services
    .AddNServiceBusEndpoint(endpoint);
```

</CodeComparison>

The Mocha registration is complete. `AddOrdering()` is a method the source generator writes for the handlers in `MyApp.Ordering` when you compile, and the RabbitMQ connection comes from dependency injection, for example from Aspire's `AddRabbitMQClient`. Exchanges and queues are created at startup from naming conventions.

# One bus, no routing table

Everything goes through one `IMessageBus`. An event reaches every service that handles it, and a command is sent to the queue of its handler, named after the message type, so there is no route to configure. Messages are plain records, with no marker interface.

NServiceBus sends a command to the endpoint its routing configuration names, by message type, namespace, or assembly. Messages are marked as commands or events, with the `ICommand` and `IEvent` interfaces or with conventions.

<MochaVisual
  name="fanout"
  caption="A published event reaches every subscriber, and each one processes it from its own queue at its own pace."
/>

<CodeComparison>

```csharp filename="Mocha"
public record OrderPlaced(Guid OrderId);
public record ReserveStock(Guid OrderId);

await bus.PublishAsync(
    new OrderPlaced(orderId), ct);

await bus.SendAsync(
    new ReserveStock(orderId), ct);
```

```csharp filename="NServiceBus"
public record OrderPlaced(Guid OrderId) : IEvent;
public record ReserveStock(Guid OrderId)
    : ICommand;

// At startup, a route for every command
routing.RouteToEndpoint(
    typeof(ReserveStock), "Inventory");

await session.Publish(new OrderPlaced(orderId));

await session.Send(new ReserveStock(orderId));
```

</CodeComparison>

# Requests return their response

A Mocha request declares the response it expects, its handler returns that response, and the caller awaits it. There is no reply call to forget, and the compiler checks that the response type matches.

<MochaVisual
  name="request-reply"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

An NServiceBus handler answers with `context.Reply`, and the reply arrives at another handler in the requesting endpoint. To await the reply where you sent the request, you add the [`NServiceBus.Callbacks`](https://docs.particular.net/nservicebus/messaging/callbacks) package, enable callbacks, and give every endpoint instance a unique address.

<CodeComparison>

```csharp filename="Mocha"
public record CheckOrderStatus(Guid OrderId)
    : IEventRequest<OrderStatus>;

public class CheckOrderStatusHandler(
    OrderStore orders)
    : IEventRequestHandler<
        CheckOrderStatus, OrderStatus>
{
    public async ValueTask<OrderStatus> HandleAsync(
        CheckOrderStatus request,
        CancellationToken ct)
        => await orders.GetStatusAsync(
            request.OrderId);
}

// Caller
var status = await bus.RequestAsync(
    new CheckOrderStatus(orderId), ct);
```

```csharp filename="NServiceBus"
public record CheckOrderStatus(Guid OrderId)
    : IMessage;
public record OrderStatus(string State)
    : IMessage;

public class CheckOrderStatusHandler(
    OrderStore orders)
    : IHandleMessages<CheckOrderStatus>
{
    public async Task Handle(
        CheckOrderStatus message,
        IMessageHandlerContext context)
    {
        var status = await orders.GetStatusAsync(
            message.OrderId);
        await context.Reply(status);
    }
}

// Caller, with NServiceBus.Callbacks
endpoint.EnableCallbacks();
endpoint.MakeInstanceUniquelyAddressable("1");
routing.RouteToEndpoint(
    typeof(CheckOrderStatus), "Catalog");

var status = await session
    .Request<OrderStatus>(
        new CheckOrderStatus(orderId));
```

</CodeComparison>

# Exception policies in one place

Mocha decides what happens to a failed message by exception type, in one place: retry it, redeliver it later, dead-letter it, or discard it. Policies apply to the whole bus, a transport, or a single receive endpoint.

NServiceBus configures recoverability per endpoint: a number of immediate retries, delayed retries that wait a fixed increment longer each time, a list of exceptions that skip them, and a custom policy function for anything else. By default it retries a message 5 times, then delays it 3 times by 10, 20, and 30 seconds, up to 24 attempts before the message goes to the `error` queue.

<MochaVisual
  name="retry"
  caption="The default policy retries a failing message three times in process, then redelivers it after 5, 15, and 30 minutes. A validation error goes straight to the error endpoint."
/>

<CodeComparison>

```csharp filename="Mocha"
.AddResilience(policy =>
{
    // Permanent errors skip the retries
    policy.On<ValidationException>().DeadLetter();

    // Everything else: retry, redeliver after
    // 5, 15, and 30 minutes, then dead-letter
    policy.Default()
        .Retry()
        .ThenRedeliver()
        .ThenDeadLetter();
})
```

```csharp filename="NServiceBus"
var recoverability = endpoint.Recoverability();

// Permanent errors skip the retries
recoverability.AddUnrecoverableException<
    ValidationException>();

recoverability.Immediate(i =>
    i.NumberOfRetries(3));

// Redeliver after 5, 10, and 15 minutes
recoverability.Delayed(d =>
{
    d.NumberOfRetries(3);
    d.TimeIncrease(TimeSpan.FromMinutes(5));
});
```

</CodeComparison>

# An outbox on your own DbContext

Mocha writes outgoing messages to the database in the same transaction as your changes and delivers them in the background, and its inbox drops messages a handler has already processed. The outbox lives in your Entity Framework Core `DbContext`, so a handler adds its entities, publishes, and saves as usual.

The NServiceBus outbox stores messages through the endpoint's persistence. A handler writes its own data through the persistence's synchronized storage session to share the outbox transaction.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

<CodeComparison>

```csharp filename="Mocha"
// Registration
.AddEntityFramework<AppDbContext>(p =>
{
    p.UsePostgresOutbox();
    p.UseTransaction();
    p.UsePostgresInbox();
})

// In a handler
db.Orders.Add(order);
await bus.PublishAsync(
    new OrderPlaced(order.Id), ct);
await db.SaveChangesAsync(ct);
```

```csharp filename="NServiceBus"
// Registration
var persistence = endpoint
    .UsePersistence<SqlPersistence>();
var dialect = persistence
    .SqlDialect<SqlDialect.PostgreSql>();
dialect.JsonBParameterModifier(p =>
    ((NpgsqlParameter)p).NpgsqlDbType =
        NpgsqlDbType.Jsonb);
persistence.ConnectionBuilder(() =>
    new NpgsqlConnection(connectionString));
endpoint.EnableOutbox();

// In a handler
var storage = context.SynchronizedStorageSession
    .SqlPersistenceSession();
// ...write the order with storage.Connection
// and storage.Transaction
await context.Publish(new OrderPlaced(order.Id));
```

</CodeComparison>

# Sagas as state machines

A Mocha saga is a state machine. States are strings, events are type arguments, and each transition reads top to bottom. The saga is correlated by its ID: the state factory below sets it to the order ID, and later events return it from `ICorrelatable.CorrelationId`.

An NServiceBus saga handles each message in its own method and keeps its state in the properties of its saga data, with a mapping that tells it how to find the saga for each message type.

<MochaVisual
  name="saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

<CodeComparison>

```csharp filename="Mocha"
public sealed class OrderSaga : Saga<OrderState>
{
    private const string Submitted = nameof(Submitted);
    private const string Accepted = nameof(Accepted);

    protected override void Configure(
        ISagaDescriptor<OrderState> saga)
    {
        saga.Initially()
            .OnEvent<OrderSubmitted>()
            .StateFactory(e =>
                new OrderState { Id = e.OrderId })
            .TransitionTo(Submitted);

        saga.During(Submitted)
            .OnEvent<OrderAccepted>()
            .TransitionTo(Accepted);

        saga.Finally(Accepted);
    }
}
```

```csharp filename="NServiceBus"
public class OrderSaga
    : Saga<OrderSagaData>,
      IAmStartedByMessages<OrderSubmitted>,
      IHandleMessages<OrderAccepted>
{
    protected override void ConfigureHowToFindSaga(
        SagaPropertyMapper<OrderSagaData> mapper)
        => mapper.MapSaga(s => s.OrderId)
            .ToMessage<OrderSubmitted>(m => m.OrderId)
            .ToMessage<OrderAccepted>(m => m.OrderId);

    public Task Handle(OrderSubmitted message,
        IMessageHandlerContext context)
        => Task.CompletedTask;

    public Task Handle(OrderAccepted message,
        IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}

public class OrderSagaData : ContainSagaData
{
    public Guid OrderId { get; set; }
}
```

</CodeComparison>

Mocha keeps saga state in PostgreSQL through Entity Framework Core, or in memory for tests. Its sagas can also start from a request and answer it when they finish, send commands, publish events, and time out.

# Ready for Native AOT

The `Mocha.Analyzers` source generator registers your handlers when you compile, and two handlers for the same command or request are a build error. For a service published with Native AOT, it generates the serialization code from a `JsonSerializerContext` and reports a build error when a message type is missing from it.

In July 2026, Particular wrote that NServiceBus [is not ready for trimming or AOT](https://particular.net/blog/preparing-for-trimming-and-aot) yet.

```csharp filename="Mocha"
[assembly: MessagingModule("Ordering",
    JsonContext = typeof(OrderingJsonContext))]

[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(ReserveStock))]
public partial class OrderingJsonContext
    : JsonSerializerContext;
```

# Moving from NServiceBus

Mocha and NServiceBus use different message formats, so a Mocha service cannot read what an NServiceBus endpoint sends. Plan the move by message flow: move the services that exchange a message together.

1. Add the `Mocha`, `Mocha.Analyzers`, and transport packages.
2. Replace the endpoint configuration and `AddNServiceBusEndpoint` with `AddMessageBus()`, the generated registration method, and the transport, and remove the command routes.
3. Turn `IHandleMessages<T>` handlers into event, command, and request handlers, and replies into request handlers that return their response.
4. Replace `IMessageSession` and `IMessageHandlerContext` calls with `IMessageBus`.
5. Move recoverability, the outbox, and sagas to their Mocha counterparts.

The [NServiceBus migration guide](../docs/mocha/migration/coming-from-nservicebus.md) walks through every step with code.

# Mocha and NServiceBus at a glance

<!-- The FeatureStatus links are placeholders until the issues and pull requests exist. -->

## Licensing

|                     | Mocha       | NServiceBus                                 |
| ------------------- | ----------- | ------------------------------------------- |
| License             | MIT         | RPL-1.5 or commercial                       |
| Price in production | Free        | $0.75 to $3.00 per endpoint per day         |
| Free commercial use | Any company | Up to 3 endpoints and 10,000 messages a day |
| License file        | Not needed  | Required in production, not enforced        |

## Transports

|                   | Mocha                                                                                                  | NServiceBus |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| RabbitMQ          | Yes                                                                                                    | Yes         |
| Azure Service Bus | Yes                                                                                                    | Yes         |
| PostgreSQL        | Preview                                                                                                | Yes         |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Amazon SQS        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| In-memory         | Yes                                                                                                    | Yes         |

## Messaging

|                    | Mocha          | NServiceBus                |
| ------------------ | -------------- | -------------------------- |
| Publish and send   | Yes            | Yes                        |
| Command routing    | By convention  | Configured per endpoint    |
| Awaitable requests | Yes            | With the callbacks package |
| Batch consumers    | Yes            | No                         |
| Scheduled messages | Yes            | Yes                        |
| Sagas              | State machines | Handlers with saga data    |

## Reliability

|                                        | Mocha | NServiceBus |
| -------------------------------------- | ----- | ----------- |
| Retry and redelivery policies          | Yes   | Yes         |
| Circuit breaker and concurrency limits | Yes   | Yes         |
| Transactional outbox and inbox         | Yes   | Yes         |

## Storage

Databases for the outbox, inbox, and saga state.

|            | Mocha                                                                                                  | NServiceBus |
| ---------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| PostgreSQL | Yes                                                                                                    | Yes         |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |

## Development

|                      | Mocha                                                                                                  | NServiceBus                                        |
| -------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------- |
| Handler registration | Source generator at build time                                                                         | Assembly scanning, or a source generator from 10.2 |
| Native AOT           | Yes                                                                                                    | No                                                 |
| OpenTelemetry        | Yes                                                                                                    | Yes                                                |
| Test harness         | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                                |
| Target frameworks    | .NET 8 to 11                                                                                           | .NET 10 (v10), .NET 8 (v9)                         |

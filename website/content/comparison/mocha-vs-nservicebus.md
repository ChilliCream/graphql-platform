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

Building distributed applications in .NET usually means making a few important decisions early on. How do services communicate? Where do messages go? What happens when something fails? And how do you keep everything reliable as your application grows? For many .NET teams, NServiceBus has been a familiar answer to these questions. It provides a mature messaging model with commands, events, sagas, retries, and transactional messaging.

But it also comes with licensing costs, explicit routing configuration, and an architecture that wasn't originally designed around modern .NET features such as Native AOT.

**Mocha takes a different approach.**

Mocha brings the messaging patterns you know from NServiceBus into a modern, convention-based .NET messaging framework. You get commands, events, request-response messaging, sagas, retries, and transactional outboxes, all built around a single `IMessageBus`. There are no routing tables to maintain, handlers are registered automatically at compile time, and Native AOT is supported.

And perhaps most importantly, **Mocha is completely free under the MIT license, for every company, at any scale.** Let's look at what that means in practice and how Mocha compares to NServiceBus.

# Messaging Shouldn't Get More Expensive as You Grow

One of the biggest differences between Mocha and NServiceBus has nothing to do with code. It's licensing. Mocha is open source under the MIT license, just like Hot Chocolate. You can use it commercially, deploy as many services as you need, and process as many messages as your infrastructure can handle. There are no license files, endpoint limits, throughput tiers, or paid editions.

NServiceBus follows a different model. It's [dual licensed](https://github.com/Particular/NServiceBus/blob/master/LICENSE.md) under the Reciprocal Public License 1.5 and Particular's commercial license. Development and testing are free, and the Community tier covers production environments with up to three endpoints and 10,000 messages per day. Beyond that, [commercial pricing](https://particular.net/pricing) ranges from $0.75 to $3.00 per logical endpoint per day.

That works out to roughly $274 to $1,095 per endpoint per year. The pricing tier depends on both the number of endpoints and the total daily message throughput across your organization. Whichever requires the higher tier determines your pricing.

Particular also offers a [Small Business Program](https://particular.net/pricing/small-business-program), which provides free licenses to qualifying organizations with income, expenses, and assets below $1 million, along with smaller discounts for organizations up to $5 million. For teams already invested in NServiceBus, that may be a reasonable trade-off.

But when you're building a new system, especially one with many small services, licensing can become a significant architectural consideration.

**With Mocha, the number of services you deploy is an engineering decision, not a licensing decision.**

And that simplicity carries over into the code.

## Getting Started Without the Ceremony

Let's say we're building an ordering service that communicates through RabbitMQ. With Mocha, the registration looks like this:

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

That's the complete Mocha registration. The interesting part is `AddOrdering()`. You don't write that method yourself. Mocha's source generator discovers the handlers in your `MyApp.Ordering` assembly and generates the registration code when you compile. RabbitMQ connections come from dependency injection, so you can use integrations such as Aspire's `AddRabbitMQClient`. Mocha also creates the necessary exchanges and queues at startup, using naming conventions.

There's no need to manually register handlers or maintain a list of command routes. You describe what your application does, and Mocha takes care of connecting the pieces.

# Commands and Events, Without the Routing Table

Most messaging systems distinguish between two fundamental operations. You publish an event when something has happened and other services might be interested. You send a command when you want a particular service to do something. Both patterns are available in NServiceBus and Mocha, but the way you work with them is slightly different. In Mocha, everything goes through a single interface: `IMessageBus`.

<MochaVisual
  name="fanout"
  caption="A published event reaches every subscriber, and each one processes it from its own queue at its own pace."
/>

Imagine our ordering service has just received an order. We want to notify interested services that the order was placed, and we also need the inventory service to reserve stock. With Mocha, that looks like this:

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

The APIs are intentionally familiar. The difference is what happens around them. In NServiceBus, commands need routing configuration that tells the framework which endpoint handles a particular message. Routes can be configured by message type, namespace, or assembly. Mocha uses conventions instead. When you send `ReserveStock`, Mocha determines its destination from the message type. The command finds the queue of its handler without requiring an explicit route.

Events work similarly. When you publish `OrderPlaced`, every service with a handler for that event receives its own copy and processes it independently. Mocha also keeps message contracts simple. You don't need to implement `ICommand`, `IEvent`, or another marker interface. Ordinary C# records are enough. This makes it easy to share contracts between services without introducing unnecessary framework dependencies.

And as your application grows, you can add handlers and services without continuously revisiting a central routing configuration.

# Request-Response Messaging That Feels Like Calling a Method

Messaging is great for decoupling services, but not every interaction is fire-and-forget. Sometimes you need an answer. Perhaps the ordering service needs to know whether an order exists, what its current status is, or whether a product is available. In those situations, request-response messaging is a natural fit. Mocha makes this a first-class part of the messaging model.

<MochaVisual
  name="order-status"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

A request declares the type of response it expects. Its handler returns that response directly. And the caller simply awaits the result.

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

Notice how closely the Mocha handler resembles an ordinary asynchronous method. It receives a request and returns a value. There's no separate reply operation to remember, and the response type is checked by the compiler. NServiceBus supports request-response messaging too, but its default model works differently. A handler sends its response through `context.Reply`, and another handler in the requesting endpoint processes that reply.

If you want to await the response directly at the call site, you need the additional [`NServiceBus.Callbacks`](https://docs.particular.net/nservicebus/messaging/callbacks) package. You also need to enable callbacks and configure a unique address for each endpoint instance. Mocha brings that experience into the core API.

**You still get asynchronous communication between services, but writing the code feels much closer to ordinary C#.**

# When Messages Fail, You Decide What Happens

In a distributed system, messages will fail. A database might be temporarily unavailable. An external API might time out. A service might restart halfway through processing. These are normal conditions that a messaging framework needs to handle. But not every failure should be treated the same way.

A network timeout might be worth retrying. A validation error probably isn't. And if a dependency is unavailable for several minutes, immediately retrying the same operation over and over won't help. Mocha handles these situations through resilience policies.

<MochaVisual
  name="retry"
  caption="The default policy retries a failing message three times in process, then redelivers it after 5, 15, and 30 minutes. A validation error goes straight to the error endpoint."
/>

You define what should happen when a particular exception occurs. For example, validation errors can go directly to the dead-letter endpoint, while other failures get retries followed by delayed redelivery.

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
        .ThenRedeliver();
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

Both frameworks provide immediate retries, delayed retries, and ways to handle exceptions that shouldn't be retried. NServiceBus configures recoverability per endpoint. By default, it performs five immediate retries, followed by three delayed retries at 10, 20, and 30 seconds. Including the initial processing attempt and retries across those stages, that can mean up to 24 processing attempts before a message reaches the `error` queue.

Mocha approaches the same problem through composable exception policies. You can define policies for the entire message bus, a particular transport, or an individual receive endpoint. The benefit is that your failure-handling rules live together and read almost like a description of the behavior you want. A validation error? Dead-letter it. A transient failure? Retry. Still failing? Redeliver later. Nothing worked? Move the message to the error endpoint.

That makes resilience easier to reason about, particularly when different types of failures require different strategies.

# Reliable Messaging That Works With Your DbContext

One of the trickiest problems in messaging appears when database changes and outgoing messages need to happen together. Imagine an order handler that does two things:

1. Saves a new order to PostgreSQL.
2. Publishes an `OrderPlaced` event.

What happens if the database transaction succeeds, but publishing the event fails? The order exists, but the rest of the system never hears about it. And if you publish first, you have the opposite problem: other services might receive an event for an order that was never committed. This is exactly what the transactional outbox pattern solves. Instead of publishing the message immediately, the framework stores it in the same database transaction as your business data.

A background process then delivers it to the message broker.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

Both Mocha and NServiceBus support this pattern. The difference is how naturally it fits into your application code. Mocha integrates directly with Entity Framework Core.

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

With Mocha, you keep working with your existing `DbContext`. You add entities, publish messages, and call `SaveChangesAsync`. The outbox makes sure the outgoing messages are persisted together with your database changes, then delivers them in the background. Mocha also provides an inbox. This handles the other side of reliable messaging: duplicate delivery.

Message brokers can deliver the same message more than once, particularly when a consumer processes a message but fails before acknowledging it. The inbox records which messages have already been processed so they can be skipped when delivered again. NServiceBus also provides a transactional outbox and deduplication. Its SQL persistence integration uses a synchronized storage session to coordinate your business data with the outbox transaction.

Both approaches solve the same fundamental reliability problem. Mocha's advantage is how closely the integration follows the Entity Framework Core programming model developers already use.

**Your business logic stays focused on business logic. The messaging infrastructure handles delivery.**

# Sagas That Look Like State Machines

Some business processes can't be completed by a single handler. Consider an order that moves through several stages. First, the customer submits it. Then inventory is reserved. Payment is processed. Finally, the order is accepted or rejected. Each step might happen in a different service, and the process could take seconds, minutes, or even days. You need somewhere to remember the current state and decide what should happen when the next message arrives.

That's what sagas are for. Both Mocha and NServiceBus support sagas, but they model them differently.

**In Mocha, a saga is explicitly a state machine.**

<MochaVisual
  name="order-saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

You define the states, the events that trigger transitions, and where the saga should go next.

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

The Mocha version describes the process from top to bottom. When an `OrderSubmitted` event arrives, create the saga state and transition to `Submitted`. While in that state, an `OrderAccepted` event transitions the saga to `Accepted`. And `Accepted` is a final state. You can read the code and follow the lifecycle.

NServiceBus uses a handler-oriented approach. A saga implements interfaces for the messages it handles, stores its state in a saga data class, and uses message-to-saga mappings to correlate incoming messages. That model is flexible and well established. Mocha's state-machine approach makes the transitions more explicit, which can be particularly useful when a workflow has many states and possible paths.

## Correlation and Persistence

Of course, defining transitions is only half the problem. A saga also needs to know which instance an incoming message belongs to. Mocha correlates sagas by their ID. In our example, the initial state factory uses the order ID as the saga ID. Subsequent events can implement `ICorrelatable` to provide that same ID through `CorrelationId`. Mocha persists saga state in PostgreSQL through Entity Framework Core. For tests, you can use in-memory persistence.

Sagas can also send commands, publish events, schedule timeouts, and participate in request-response workflows. A saga can even begin with a request and return the response when the workflow finishes. There is one important behavior to be aware of: when a correlated event arrives before its saga instance exists, Mocha ignores it.

If an initial transition publishes a message that can trigger a correlated response, you should commit that outgoing message together with the saga state through the outbox. This ensures the saga exists before the response can arrive.

# Built for Modern .NET, Including Native AOT

.NET has changed considerably over the past few years. Source generators, trimming, and Native AOT have opened up new ways to build applications with faster startup times and smaller deployment footprints. Messaging frameworks need to work with those changes. Mocha was designed with them in mind. Instead of discovering and registering handlers through runtime assembly scanning, Mocha uses the `Mocha.Analyzers` source generator.

When you compile your application, it discovers your handlers and generates the registration code. That has a few useful consequences. You don't need to maintain handler registration manually, and errors can be caught earlier. For example, if two handlers claim the same command or request, Mocha reports a build error rather than leaving you to discover the problem at runtime. For Native AOT applications, Mocha also generates serialization code using a `JsonSerializerContext`.

```csharp filename="Mocha"
[assembly: MessagingModule("Ordering",
    JsonContext = typeof(OrderingJsonContext))]

[JsonSerializable(typeof(OrderPlaced))]
[JsonSerializable(typeof(ReserveStock))]
public partial class OrderingJsonContext
    : JsonSerializerContext;
```

This explicitly describes the message types your application needs to serialize. If a message type is missing from the context, Mocha reports a build error. That's particularly valuable with Native AOT, where reflection-based serialization can be problematic. NServiceBus has been moving in this direction too. Version 10.2 introduced source-generator-based handler registration as an alternative to assembly scanning.

However, as Particular explained in its [July 2026 article on trimming and AOT](https://particular.net/blog/preparing-for-trimming-and-aot), NServiceBus isn't ready for trimming or Native AOT yet. For teams building new services around modern .NET deployment models, this is an important distinction. Mocha doesn't treat Native AOT as something to add later. It's part of the development model.

# What About Existing NServiceBus Applications?

If you're already using NServiceBus, moving to Mocha doesn't mean throwing away the messaging patterns your application is built around. Commands are still commands. Events are still events. Sagas still coordinate long-running processes, and the outbox still protects reliable message delivery. But there are some important differences to account for. The biggest one is message compatibility.

**Mocha and NServiceBus use different message formats, so they cannot directly consume each other's messages.**

You shouldn't expect to replace one endpoint at a time and have it immediately communicate with unchanged endpoints using the other framework. Instead, plan the migration around message flows. Move services that exchange messages together, so that the services on each side of a communication flow use compatible formats. Once you've identified a suitable boundary, the code migration is relatively straightforward. The main changes are:

1. **Install Mocha.** Add the `Mocha`, `Mocha.Analyzers`, and appropriate transport packages.
2. **Replace endpoint configuration.** Use `AddMessageBus()`, your generated module registration method, and the transport integration instead of `AddNServiceBusEndpoint`. Explicit command routes can be removed.
3. **Migrate handlers.** Replace `IHandleMessages<T>` with the appropriate Mocha event, command, or request handlers. Request handlers return their responses directly.
4. **Switch to `IMessageBus`.** Replace messaging operations through `IMessageSession` and `IMessageHandlerContext` with the corresponding Mocha APIs.
5. **Move infrastructure configuration.** Configure Mocha's resilience policies, outbox, inbox, and state-machine sagas.

For a complete walkthrough, see the [NServiceBus migration guide](../docs/mocha/migration/coming-from-nservicebus.md).

<MigrationGuideCard href="/docs/mocha/migration/coming-from-nservicebus" from="NServiceBus" />

# Feature comparison

So far, we've looked at the programming model. But choosing a messaging framework involves more than deciding which API you prefer. Transport support, persistence options, observability, and testing are all important, particularly for applications that already have infrastructure in place. Here's how the two frameworks compare.

## Licensing

| Feature             | Mocha       | NServiceBus                                 |
| ------------------- | ----------- | ------------------------------------------- |
| License             | MIT         | RPL-1.5 or commercial                       |
| Price in production | Free        | $0.75 to $3.00 per endpoint per day         |
| Free commercial use | Any company | Up to 3 endpoints and 10,000 messages a day |
| License file        | Not needed  | Required in production, not enforced        |

Mocha's MIT license makes the cost predictable: the framework itself is free, regardless of how your architecture evolves. NServiceBus offers free options for smaller deployments and qualifying businesses, with commercial pricing for larger production environments.

## Transports

Both frameworks support RabbitMQ and Azure Service Bus, two common choices for messaging infrastructure in .NET applications. Mocha also supports in-memory messaging and has a PostgreSQL transport in preview. NServiceBus currently has broader transport coverage, including SQL Server and Amazon SQS.

| Feature           | Mocha                                                                                                  | NServiceBus |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| RabbitMQ          | Yes                                                                                                    | Yes         |
| Azure Service Bus | Yes                                                                                                    | Yes         |
| PostgreSQL        | Preview                                                                                                | Yes         |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Amazon SQS        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| In-memory         | Yes                                                                                                    | Yes         |

If your application depends on a transport Mocha doesn't support yet, that's an important consideration before migrating.

## Messaging

The core messaging capabilities are available in both frameworks. The biggest differences are in how you configure routing, work with request-response messaging, and define sagas. Mocha also supports batch consumers, allowing messages to be processed in batches where that fits your workload.

| Feature            | Mocha          | NServiceBus                |
| ------------------ | -------------- | -------------------------- |
| Publish and send   | Yes            | Yes                        |
| Command routing    | By convention  | Configured per endpoint    |
| Awaitable requests | Yes            | With the callbacks package |
| Batch consumers    | Yes            | No                         |
| Scheduled messages | Yes            | Yes                        |
| Sagas              | State machines | Handlers with saga data    |

## Reliability

Reliable message delivery is essential, and neither framework expects you to build those mechanisms yourself. Both provide retries, delayed redelivery, concurrency controls, and transactional messaging.

| Feature                                | Mocha | NServiceBus |
| -------------------------------------- | ----- | ----------- |
| Retry and redelivery policies          | Yes   | Yes         |
| Circuit breaker and concurrency limits | Yes   | Yes         |
| Transactional outbox and inbox         | Yes   | Yes         |

The difference is primarily in configuration and integration. Mocha emphasizes exception-based resilience policies and direct integration with Entity Framework Core. NServiceBus provides its established recoverability and persistence infrastructure.

## Persistence

When choosing a messaging framework, you'll also want to consider where the outbox, inbox, and saga state are stored. Mocha currently focuses on PostgreSQL for durable storage. NServiceBus supports a wider selection of persistence technologies, including SQL Server, MySQL, and MongoDB.

| Feature    | Mocha                                                                                                  | NServiceBus |
| ---------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| PostgreSQL | Yes                                                                                                    | Yes         |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |

For applications already using PostgreSQL and Entity Framework Core, Mocha provides a particularly natural fit. For teams depending on other databases, NServiceBus currently offers more options.

## Developer Experience

This is where some of Mocha's architectural choices become especially visible. Handler registration happens at build time, Native AOT is supported, and the framework targets modern .NET versions. Both frameworks integrate with OpenTelemetry, so you can observe messaging activity alongside the rest of your distributed application. NServiceBus has an established testing infrastructure, while Mocha's dedicated test harness is currently in review.

| Feature              | Mocha                                                                                                  | NServiceBus                                        |
| -------------------- | ------------------------------------------------------------------------------------------------------ | -------------------------------------------------- |
| Handler registration | Source generator at build time                                                                         | Assembly scanning, or a source generator from 10.2 |
| Native AOT           | Yes                                                                                                    | No                                                 |
| OpenTelemetry        | Yes                                                                                                    | Yes                                                |
| Test harness         | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                                |
| Target frameworks    | .NET 8 to 11                                                                                           | .NET 10 (v10), .NET 8 (v9)                         |

# A Different Approach to .NET Messaging

NServiceBus has been part of the .NET messaging ecosystem for a long time. It offers a mature platform with broad transport and persistence support, established tooling, and a programming model that many teams already know. Mocha isn't trying to reinvent the messaging patterns that make systems like NServiceBus useful. Commands, events, sagas, retries, and outboxes are proven building blocks. They're useful because they solve real problems in distributed applications.

What Mocha changes is how much work you need to do around those building blocks. You don't configure routes for every command. You don't manually register handlers. You don't need a separate callbacks package to await a response. You can use your existing Entity Framework Core `DbContext` with the transactional outbox, and you can define sagas as explicit state machines.

Source generation moves more work to compile time, while Native AOT support makes Mocha suitable for modern .NET deployment scenarios. And the MIT license means you can adopt those capabilities without worrying about endpoint counts, message throughput, or licensing tiers. There are still reasons to choose NServiceBus, particularly if you depend on its broader infrastructure support or existing ecosystem.

But if you're starting a new distributed application with .NET, or looking to simplify an existing messaging architecture, Mocha offers an alternative worth exploring.

**Reliable messaging shouldn't require a complicated setup, and scaling your application shouldn't require scaling your licensing costs.**

That's the idea behind Mocha. And because it's open source, you can try it, inspect how it works, and build on it without committing to a commercial license. **Ready to give it a try?** Start with the [Mocha documentation](../docs/mocha) or explore the [NServiceBus migration guide](../docs/mocha/migration/coming-from-nservicebus.md) to see how your existing messaging patterns translate.

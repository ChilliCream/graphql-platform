---
title: "Mocha vs. MassTransit"
metaTitle: "Mocha vs MassTransit: An MIT-Licensed MassTransit Alternative"
description: "Compare Mocha and MassTransit for messaging in .NET: licensing, transports, sagas, the outbox, retries, and what a migration takes."
featuredImage: "mocha-vs-masstransit.png"
date: "2026-10-28"
tags: ["mocha", "comparison", "masstransit"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "MassTransit"
    url: "https://masstransit.massient.com/"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Is MassTransit still free?"
    answer: "MassTransit 8 is licensed under Apache 2.0 and still gets releases. MassTransit 9 is commercial and costs $4,000 per year for one product line or $12,000 per year for a whole organization. Organizations with less than $1 million in annual revenue may qualify for a free license without commercial support."
  - question: "Does Mocha need a license key?"
    answer: "No. Mocha is MIT licensed and runs without a license key on developer machines, build agents, and in production."
  - question: "Does Mocha have a transactional outbox and sagas?"
    answer: "Yes. Mocha has a transactional outbox and inbox, scheduled messages, retries and redelivery through exception policies, and state machine sagas."
  - question: "Does Mocha have an in-process mediator like MassTransit Mediator?"
    answer: "Yes. The Mocha mediator dispatches commands, queries, and notifications inside a service, with handlers registered by the same source generator as the message bus."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license, with no license key on any machine. Start with the quick start, or move from MassTransit step by step with the migration guide. Moving a large system?"
  primary:
    label: "Get started with Mocha"
    href: "/docs/mocha/quick-start"
  secondary:
    label: "Migrate from MassTransit"
    href: "/docs/mocha/migration/coming-from-masstransit"
  contact: "Talk to us about your migration"
---

Building message-driven applications in .NET shouldn't require mountains of configuration, complicated abstractions, or a commercial license. That's why we built **Mocha**, an open-source messaging framework for .NET that makes working with events, commands, and requests feel natural.

If you've used MassTransit before, much of Mocha will look familiar. You can publish events, send commands, implement request/reply patterns, build sagas, and rely on retries, outboxes, and inboxes to keep your messages flowing reliably. The difference is in how much work you have to do to get there.

Mocha gives you a single `IMessageBus` for all your messaging, handlers that work directly with your message types, and retry policies that read like the behavior you actually want. A source generator takes care of registering your handlers at build time, so there's less configuration to write and fewer surprises at runtime. And perhaps most importantly, **Mocha is completely free and MIT licensed.** No license keys, no revenue limits, and no commercial edition.

Let's take a look at how it compares to MassTransit.

# Open source should mean open to everyone

Before we get into the code, there's an important difference worth talking about: licensing. With the release of MassTransit 9 in January 2026, MassTransit moved to a commercial licensing model under Massient. For companies that don't qualify for a free license, that means paying [$4,000 per year for a single product line or $12,000 per year for an organization-wide license](https://massient.com/#pricing).

Organizations generating less than $1 million in annual revenue may qualify for a free license, although commercial support isn't included. MassTransit 9 also requires a [license key on every machine](https://masstransit.massient.com/configuration/license/) running the framework. That includes production servers, developer machines, and CI build agents.

MassTransit 8 remains available under Apache 2.0 and continues to receive updates. When version 9 was announced in April 2025, the original announcement stated that version 8 would receive patches through at least the end of 2026. That announcement is [now only available through the Wayback Machine](https://web.archive.org/web/20251212070758/https://masstransit.io/introduction/v9-announcement), and there is currently no published end date.

**Mocha takes a different approach.**

Like Hot Chocolate, Mocha is MIT licensed. You can use it in a hobby project, a startup, or an enterprise application without paying anything. There are no license keys to configure, no revenue thresholds to worry about, and no features locked behind a commercial edition. The source code is public, too. That simplicity extends to getting started.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

```csharp filename="MassTransit 9" {6,7}
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.SetLicense(builder
            .Configuration["MassTransit:License"]);
        cfg.ConfigureEndpoints(context);
    });
});
```

</CodeComparison>

That's all the configuration Mocha needs. You might be wondering where `AddOrdering()` comes from. It's generated automatically at build time by Mocha's source generator, based on the handlers in your `MyApp.Ordering` namespace. There's no assembly scanning at startup, and you don't need to register every handler yourself.

Mocha also creates exchanges and queues using naming conventions when your application starts. The RabbitMQ connection comes from dependency injection, so it works naturally with tools like Aspire's `AddRabbitMQClient`.

And because Mocha knows about your handlers at compile time, it can catch configuration mistakes early. Accidentally register two handlers for the same command or request? You'll get a build error instead of discovering the problem when your application is running.

# Handlers that focus on the message

One of the first things you'll notice when moving from MassTransit to Mocha is how handlers are written. In MassTransit, a consumer receives a `ConsumeContext<T>`. The actual message lives inside that context, alongside metadata such as headers and cancellation tokens. That flexibility is useful, but most of the time, your handler just wants to process a message. Mocha makes that the default.

<CodeComparison>

```csharp filename="Mocha" {3,5,6}
public class OrderPlacedHandler(
    IInvoiceService invoices)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message, CancellationToken ct)
        => await invoices.CreateAsync(
            message.OrderId, ct);
}
```

```csharp filename="MassTransit" {3,5,6}
public class OrderPlacedConsumer(
    IInvoiceService invoices)
    : IConsumer<OrderPlaced>
{
    public async Task Consume(
        ConsumeContext<OrderPlaced> context)
        => await invoices.CreateAsync(
            context.Message.OrderId,
            context.CancellationToken);
}
```

</CodeComparison>

It's a small difference, but one you'll appreciate when you're writing dozens of handlers. Your handler receives the message directly, along with a cancellation token. There's no context object to navigate just to access the data you need. Mocha also makes the purpose of a handler explicit through its interface:

- `IEventHandler<T>` handles published events.
- `IEventRequestHandler<T>` handles commands.
- `IEventRequestHandler<TRequest, TResponse>` handles requests that expect a response.

This makes it easier to understand what a handler does just by looking at its declaration. Of course, sometimes you do need access to message headers, delivery information, or other metadata. For those cases, Mocha provides `IConsumer<T>`, which receives an `IConsumeContext<T>`. You still have access to the underlying messaging context when you need it. You just don't have to work with it everywhere.

# Request/reply without the extra ceremony

Request/reply is another area where Mocha makes things a little more straightforward. Imagine an ordering service that needs to ask the catalog service for the current status of an order.

<MochaVisual
  name="order-status"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

With Mocha, the request itself declares what kind of response it expects. The handler then returns that response directly.

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
```

```csharp filename="MassTransit"
public record CheckOrderStatus(Guid OrderId);

public class CheckOrderStatusConsumer(
    OrderStore orders)
    : IConsumer<CheckOrderStatus>
{
    public async Task Consume(
        ConsumeContext<CheckOrderStatus> context)
    {
        var status = await orders.GetStatusAsync(
            context.Message.OrderId);
        await context.RespondAsync(status);
    }
}
```

</CodeComparison>

Notice what's missing from the Mocha version? There's no `RespondAsync` call. The handler simply returns an `OrderStatus`, just like any other asynchronous method. Because the request implements `IEventRequest<OrderStatus>`, the compiler also knows which response type to expect. That means you can't accidentally return the wrong type, and you don't have to remember to explicitly send a response. It's a familiar C# programming model applied to distributed messaging.

# One message bus for everything

Publishing an event, sending a command, and making a request are three different messaging patterns. But do they really need three different APIs? In Mocha, they all go through a single interface: `IMessageBus`. Let's say we're building an ordering system. When an order is placed, we want to publish an event so other services can react to it. We also need to send a command to reserve inventory, and occasionally ask another service for an order's current status.

A published event can have multiple subscribers, each processing the event independently.

<MochaVisual
  name="fanout"
  caption="A published event reaches every subscriber, and each one processes it from its own queue at its own pace."
/>

Here's what those three operations look like side by side.

<CodeComparison>

```csharp filename="Mocha"
await bus.PublishAsync(
    new OrderPlaced(orderId), ct);

await bus.SendAsync(
    new ReserveStock(orderId), ct);

var status = await bus.RequestAsync(
    new CheckOrderStatus(orderId), ct);
```

```csharp filename="MassTransit"
await publishEndpoint.Publish(
    new OrderPlaced(orderId), ct);

var endpoint = await sendEndpointProvider
    .GetSendEndpoint(new Uri("queue:reserve-stock"));
await endpoint.Send(new ReserveStock(orderId), ct);

var response = await client
    .GetResponse<OrderStatus>(
        new CheckOrderStatus(orderId), ct);
var status = response.Message;
```

</CodeComparison>

With Mocha, you inject `IMessageBus` and use the operation you need. There's no separate publish endpoint, send endpoint provider, or request client to manage.

**You also don't need to know the destination queue when sending a command.**

Mocha uses the message type to determine the queue of its handler. That removes the need to manually look up send endpoints or scatter queue names throughout your application. And when you make a request, `RequestAsync` returns the response message directly. The result is an API that feels consistent whether you're publishing an event, sending a command, or waiting for a reply.

# Retry policies that describe what should happen

Messages fail. Networks become unavailable, databases time out, and sometimes a downstream service simply isn't ready. A good messaging framework needs to handle those failures reliably. Both Mocha and MassTransit support retries and delayed redelivery, but they approach configuration differently. MassTransit exposes these behaviors through middleware, allowing you to configure immediate retries and delayed redelivery on receive endpoints.

Mocha uses **exception-based resilience policies**. Instead of thinking about which middleware to attach, you describe what should happen when a particular exception occurs.

<MochaVisual
  name="retry"
  caption="The default policy retries a failing message three times in process, then redelivers it after 5, 15, and 30 minutes. A validation error goes straight to the error endpoint."
/>

For example, a validation error probably won't disappear if we retry the same message. A temporary database outage, on the other hand, might resolve itself after a few seconds or minutes. Mocha lets us express that distinction directly.

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

```csharp filename="MassTransit"
x.AddConfigureEndpointsCallback(
    (context, name, cfg) =>
    {
        cfg.UseDelayedRedelivery(r => r.Intervals(
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(15),
            TimeSpan.FromMinutes(30)));
        cfg.UseMessageRetry(r => r.Immediate(3));
    });
```

</CodeComparison>

The Mocha configuration reads almost like a description of the desired behavior. If a `ValidationException` occurs, send the message straight to the error endpoint. For other exceptions, retry the message three times in process. If those attempts fail, redeliver it after 5, 15, and 30 minutes before finally moving it to the error endpoint. You can configure these policies globally for the entire bus, for a specific transport, or for an individual receive endpoint.

And if you're familiar with MassTransit's error handling, the destination names will feel familiar: Mocha uses `_error` for failed messages and `_skipped` for messages that couldn't be handled. The goal isn't to hide failure handling. It's to make the intended behavior easier to express and understand.

# Reliable messaging with an outbox and inbox

One of the trickiest problems in message-driven applications is keeping database changes and outgoing messages consistent. Imagine creating an order in your database and then publishing an `OrderPlaced` event. What happens if the database transaction succeeds, but publishing the message fails? Your order exists, but the rest of the system never hears about it.

The transactional outbox solves this by writing outgoing messages to the database as part of the same transaction as your application changes. A background process then delivers those messages to the broker. And because message brokers can deliver messages more than once, an inbox helps prevent your handlers from processing the same message repeatedly.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

Mocha supports both patterns through Entity Framework Core and PostgreSQL.

<CodeComparison>

```csharp filename="Mocha"
.AddEntityFramework<AppDbContext>(p =>
{
    p.UsePostgresOutbox();
    p.UseTransaction();
    p.UsePostgresInbox();
})
```

```csharp filename="MassTransit"
x.AddEntityFrameworkOutbox<AppDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox();
});
```

</CodeComparison>

With Mocha, you configure the outbox, transaction handling, and inbox together. Your application can then persist its business changes and outgoing messages in one transaction, while the messaging infrastructure handles delivery in the background. This is particularly useful when building workflows that span multiple services, where losing a message can leave your system in an inconsistent state.

# Sagas without all the ceremony

As applications grow, some workflows become too complicated for a single message handler. Consider an order that needs to be submitted, accepted, paid for, and shipped. Each step might involve a different service, and the entire process could take minutes, hours, or even days. That's where sagas come in. A saga keeps track of a long-running workflow and reacts to messages as the workflow progresses.

<MochaVisual
  name="order-saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

MassTransit provides a mature state machine implementation for building these workflows. Mocha supports state machine sagas too, but with an API designed to reduce the amount of setup. You don't need to declare separate `State` and `Event` properties. States are represented by strings, while events are expressed through generic type arguments. Let's look at a simple order saga that starts when an order is submitted and finishes when the order is accepted.

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

```csharp filename="MassTransit"
public class OrderStateMachine
    : MassTransitStateMachine<OrderState>
{
    public State Submitted
        { get; private set; } = null!;
    public Event<OrderSubmitted> Submit
        { get; private set; } = null!;
    public Event<OrderAccepted> Accept
        { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => Submit, x => x.CorrelateById(
            m => m.Message.OrderId));
        Event(() => Accept, x => x.CorrelateById(
            m => m.Message.OrderId));

        Initially(
            When(Submit).TransitionTo(Submitted));
        During(Submitted,
            When(Accept).Finalize());
    }
}
```

</CodeComparison>

In Mocha, the workflow reads from top to bottom. When an `OrderSubmitted` event arrives, create the saga state and transition to `Submitted`. While in `Submitted`, wait for `OrderAccepted`. Once that event arrives, transition to `Accepted` and finish the saga. Correlation is handled through the saga ID. The initial state factory assigns the order ID, and subsequent events provide that ID through `ICorrelatable.CorrelationId`.

There's no need to repeat correlation expressions for every event. Of course, real-world sagas usually do more than transition between two states. Mocha sagas can also send commands, publish events, handle timeouts, and participate in request/reply workflows. A saga can start from a request and send its response when the workflow finishes. Saga state can be persisted in PostgreSQL using Entity Framework Core, or kept in memory for tests.

# What about moving from MassTransit?

If you already have an application running on MassTransit, switching messaging frameworks is understandably a bigger decision than choosing one for a new project. The good news is that many of the concepts you're already familiar with carry over. Events are still events. Commands are still commands. You still have request/reply, sagas, retries, and transactional messaging. What changes is how you express those concepts in code.

There is, however, one important compatibility consideration.

**Mocha and MassTransit use different message formats.**

That means a Mocha service cannot directly consume messages published by a MassTransit service. You shouldn't simply replace MassTransit in one service and expect the rest of your system to continue communicating with it. Instead, we recommend migrating one message flow at a time. For example, if your ordering service publishes an event that your invoicing service consumes, migrate both sides of that flow together.

Alternatively, you can register both Mocha and MassTransit in the same application while gradually moving individual flows. A typical migration looks like this:

1. Add the `Mocha`, `Mocha.Analyzers`, and appropriate transport packages.
2. Replace your `AddMassTransit` configuration with `AddMessageBus()`, the generated handler registration method, and your transport.
3. Convert MassTransit consumers into Mocha event, command, or request handlers.
4. Replace publish endpoints, send endpoints, and request clients with `IMessageBus`.
5. Move your retry policies, outbox configuration, and sagas to their Mocha equivalents.

We've also put together a [step-by-step migration guide](../docs/mocha/migration/coming-from-masstransit.md) with code examples to help you through the process.

<MigrationGuideCard href="/docs/mocha/migration/coming-from-masstransit" from="MassTransit" />

# Feature comparison

So far, we've focused on the developer experience. But when choosing a messaging framework, the API is only part of the story. Transport support, reliability features, persistence options, and tooling matter just as much. MassTransit has been around for a long time and supports a broad range of transports and databases. Mocha already covers many common messaging scenarios, but some integrations are still being developed. Here's how the two compare today.

## Licensing

| Feature             | Mocha       | MassTransit                          |
| ------------------- | ----------- | ------------------------------------ |
| License             | MIT         | Commercial (v9), Apache 2.0 (v8)     |
| Price               | Free        | $4,000 or $12,000 per year (v9)      |
| Free commercial use | Any company | May qualify under $1M annual revenue |
| License key         | Not needed  | Required on every machine (v9)       |
| Public source code  | Yes         | v8 only                              |

## Transports

Mocha currently supports RabbitMQ and Azure Service Bus, along with an in-memory transport for development and testing. PostgreSQL transport support is available in preview. Support for additional transports is planned.

| Feature              | Mocha                                                                                                  | MassTransit |
| -------------------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| RabbitMQ             | Yes                                                                                                    | Yes         |
| Azure Service Bus    | Yes                                                                                                    | Yes         |
| PostgreSQL           | Preview                                                                                                | Yes         |
| SQL Server           | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Amazon SQS/SNS       | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Kafka and Event Hubs | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | As riders   |
| In-memory            | Yes                                                                                                    | Yes         |

## Messaging

Both frameworks support the core messaging patterns needed to build event-driven and distributed applications.

| Feature             | Mocha | MassTransit |
| ------------------- | ----- | ----------- |
| Publish and send    | Yes   | Yes         |
| Request/reply       | Yes   | Yes         |
| Batch consumers     | Yes   | Yes         |
| Scheduled messages  | Yes   | Yes         |
| State machine sagas | Yes   | Yes         |
| In-process mediator | Yes   | Yes         |

## Reliability

Reliable messaging is a first-class concern in both frameworks. Mocha includes retry and redelivery policies, circuit breakers, concurrency limits, and transactional outbox and inbox support.

| Feature                                | Mocha | MassTransit |
| -------------------------------------- | ----- | ----------- |
| Retry and redelivery policies          | Yes   | Yes         |
| Circuit breaker and concurrency limits | Yes   | Yes         |
| Transactional outbox and inbox         | Yes   | Yes         |

## Storage

For persisting outbox messages, inbox state, scheduled messages, and sagas, Mocha currently focuses on PostgreSQL. MassTransit supports a wider range of persistence technologies, while additional database integrations are planned for Mocha.

| Feature    | Mocha                                                                                                  | MassTransit |
| ---------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| PostgreSQL | Yes                                                                                                    | Yes         |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |

## Development experience

One of Mocha's biggest differences is its use of source generation. Rather than relying on assembly scanning at runtime, Mocha discovers and registers handlers during compilation. Both frameworks support OpenTelemetry, and a dedicated Mocha test harness is currently in review.

| Feature              | Mocha                                                                                                  | MassTransit                                           |
| -------------------- | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------- |
| Handler registration | Source generator at build time                                                                         | Assembly scanning or explicit                         |
| OpenTelemetry        | Yes                                                                                                    | Yes                                                   |
| Test harness         | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                                   |
| Target frameworks    | .NET 8 to 11                                                                                           | .NET 8 to 10, .NET Standard 2.0, .NET Framework 4.7.2 |

# A different approach to messaging in .NET

MassTransit has played an important role in the .NET ecosystem, and there's a reason so many teams use it. It's a mature framework with extensive transport support, a broad feature set, and years of production use behind it. Mocha isn't trying to pretend those advantages don't exist. What we're offering is a different approach. We want messaging in .NET to feel closer to the C# code you're already comfortable writing.

Handlers should receive messages directly. Requests should return responses. Publishing, sending, and requesting should work through one consistent API. Retry policies should describe the behavior you want, and the compiler should catch mistakes before your application starts. We also believe that the infrastructure you build your applications on should remain accessible, regardless of your company's size or revenue.

That's why **Mocha is MIT licensed, completely free to use in production, and developed in the open.** If you're starting a new event-driven application, or reconsidering your messaging stack for an existing one, we'd love for you to give Mocha a try. And if you're coming from MassTransit, our [migration guide](../docs/mocha/migration/coming-from-masstransit.md) is a good place to start.

**Less configuration, less ceremony, and more time spent building the application itself.**

That's what we're building Mocha for.

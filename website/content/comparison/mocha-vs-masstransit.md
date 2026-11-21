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

Mocha does the messaging you build with MassTransit in less code. One IMessageBus publishes, sends, and requests, handlers receive the message instead of a context, retries are exception policies, and a source generator registers your handlers at build time. Free for every company under the MIT license, with no license key on any machine.

# Free for every company, no key on any machine

Mocha is MIT licensed, like Hot Chocolate. Any company can use it in production for free. There is no paid edition, no license key, and no revenue limit, and the source code is public.

MassTransit 9, released in January 2026, is a commercial product from Massient. Its source code is available to licensed customers, not publicly. A [license for one product line](https://massient.com/#pricing) costs $4,000 per year, and one for the whole organization costs $12,000 per year. Organizations with less than $1 million in annual revenue may qualify for a free license without commercial support. The [license key has to be available on every machine](https://masstransit.massient.com/configuration/license/) that runs MassTransit, including developer machines and build agents. MassTransit 8 stays under the [Apache 2.0 license](https://github.com/MassTransit/MassTransit) and still gets releases. When version 9 was announced in April 2025, the announcement said version 8 would be patched through at least the end of 2026. The announcement is no longer online ([archived copy](https://web.archive.org/web/20251212070758/https://masstransit.io/introduction/v9-announcement)), and no current page gives an end date.

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

The Mocha registration is complete. `AddOrdering()` is a method the source generator writes for the handlers in `MyApp.Ordering` when you compile, and the RabbitMQ connection comes from dependency injection, for example from Aspire's `AddRabbitMQClient`. Exchanges and queues are created at startup from naming conventions. Two handlers for the same command or request are a build error instead of a surprise at runtime.

# Handlers get the message, not a context

A MassTransit consumer receives a `ConsumeContext<T>` and reads the message from it. Most Mocha handlers receive the message itself, and the interface says what kind of message it is: `IEventHandler<T>` for events, `IEventRequestHandler<T>` for commands, and `IEventRequestHandler<TRequest, TResponse>` for requests that get a response.

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

When a handler needs headers or message metadata, Mocha's `IConsumer<T>` receives an `IConsumeContext<T>`, so nothing is lost.

# Requests return their response

A Mocha request declares the response it expects, and its handler returns that response. There is no `RespondAsync` call to forget, and the compiler checks that the response type matches.

<MochaVisual
  name="request-reply"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

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

# One bus for publish, send, and request

Everything goes through one `IMessageBus`. A command is sent to the queue of its handler, named after the message type, so there is no send endpoint to look up first, and a request returns the response message directly.

<MochaVisual
  name="fanout"
  caption="A published event reaches every subscriber, and each one processes it from its own queue at its own pace."
/>

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

# Exception policies instead of retry middleware

Mocha decides what happens to a failed message by exception type, in one place: retry it, redeliver it later, dead-letter it, or discard it. Policies apply to the whole bus, a transport, or a single receive endpoint. Failed messages end up in an `_error` endpoint and unhandled ones in a `_skipped` endpoint, the same suffixes MassTransit uses.

MassTransit configures retries and redelivery as separate middleware on each endpoint.

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

# Outbox and inbox

Mocha writes outgoing messages to the database in the same transaction as your changes and delivers them in the background, and its inbox drops messages a handler has already processed. Both use Entity Framework Core with PostgreSQL.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

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

# Sagas without the ceremony

A Mocha saga needs no `State` and `Event` properties. States are strings, events are type arguments, and each transition reads top to bottom. The saga is correlated by its ID: the state factory below sets it to the order ID, and later events return it from `ICorrelatable.CorrelationId`.

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

Saga state lives in PostgreSQL through Entity Framework Core, or in memory for tests. Sagas can also start from a request and answer it when they finish, send commands, publish events, and time out.

# Moving from MassTransit

Mocha and MassTransit use different message formats, so a Mocha service cannot read what a MassTransit service publishes. Plan the move by message flow: move the services that exchange a message together, or keep both libraries registered in a service until all of its flows have moved.

1. Add the `Mocha`, `Mocha.Analyzers`, and transport packages.
2. Replace `AddMassTransit` with `AddMessageBus()`, the generated registration method, and the transport.
3. Turn consumers into event, command, and request handlers.
4. Replace `Publish`, `Send`, and request clients with `IMessageBus`.
5. Move retries, the outbox, and sagas to their Mocha counterparts.

The [MassTransit migration guide](../docs/mocha/migration/coming-from-masstransit.md) walks through every step with code.

# Mocha and MassTransit at a glance

<!-- The FeatureStatus links are placeholders until the issues and pull requests exist. -->

## Licensing

|                     | Mocha       | MassTransit                          |
| ------------------- | ----------- | ------------------------------------ |
| License             | MIT         | Commercial (v9), Apache 2.0 (v8)     |
| Price               | Free        | $4,000 or $12,000 per year (v9)      |
| Free commercial use | Any company | May qualify under $1M annual revenue |
| License key         | Not needed  | Required on every machine (v9)       |
| Public source code  | Yes         | v8 only                              |

## Transports

|                      | Mocha                                                                                                  | MassTransit |
| -------------------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| RabbitMQ             | Yes                                                                                                    | Yes         |
| Azure Service Bus    | Yes                                                                                                    | Yes         |
| PostgreSQL           | Preview                                                                                                | Yes         |
| SQL Server           | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Amazon SQS/SNS       | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| Kafka and Event Hubs | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | As riders   |
| In-memory            | Yes                                                                                                    | Yes         |

## Messaging

|                     | Mocha | MassTransit |
| ------------------- | ----- | ----------- |
| Publish and send    | Yes   | Yes         |
| Request/reply       | Yes   | Yes         |
| Batch consumers     | Yes   | Yes         |
| Scheduled messages  | Yes   | Yes         |
| State machine sagas | Yes   | Yes         |
| In-process mediator | Yes   | Yes         |

## Reliability

|                                        | Mocha | MassTransit |
| -------------------------------------- | ----- | ----------- |
| Retry and redelivery policies          | Yes   | Yes         |
| Circuit breaker and concurrency limits | Yes   | Yes         |
| Transactional outbox and inbox         | Yes   | Yes         |

## Storage

Databases for the outbox, inbox, scheduled messages, and saga state.

|            | Mocha                                                                                                  | MassTransit |
| ---------- | ------------------------------------------------------------------------------------------------------ | ----------- |
| PostgreSQL | Yes                                                                                                    | Yes         |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes         |

## Development

|                      | Mocha                                                                                                  | MassTransit                                           |
| -------------------- | ------------------------------------------------------------------------------------------------------ | ----------------------------------------------------- |
| Handler registration | Source generator at build time                                                                         | Assembly scanning or explicit                         |
| OpenTelemetry        | Yes                                                                                                    | Yes                                                   |
| Test harness         | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                                   |
| Target frameworks    | .NET 8 to 11                                                                                           | .NET 8 to 10, .NET Standard 2.0, .NET Framework 4.7.2 |

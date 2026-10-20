---
title: "Mocha vs MassTransit"
metaTitle: "Mocha vs MassTransit: An MIT-Licensed MassTransit Alternative"
description: "Compare Mocha and MassTransit for messaging in .NET: licensing, transports, sagas, the outbox, retries, and what a migration takes."
lead: "Mocha does the messaging you build with MassTransit in less code. One IMessageBus publishes, sends, and requests, handlers receive the message instead of a context, retries are exception policies, and a source generator registers your handlers at build time. Free for every company under the MIT license, with no license key on any machine."
product: "mocha"
competitor: "MassTransit"
lastChecked: "2026-10-20"
versions:
  product: "16.6"
  competitor: "9.2"
migrationGuide: "/docs/mocha/migration/coming-from-masstransit"
features:
  - title: "Licensing"
    rows:
      - label: "License"
        product: "MIT"
        competitor: "Commercial (v9), Apache 2.0 (v8)"
      - label: "Price"
        product: "Free"
        competitor: "$4,000 or $12,000 per year (v9)"
      - label: "Free commercial use"
        product: "Any company"
        competitor: "May qualify under $1M annual revenue"
      - label: "License key"
        product: "Not needed"
        competitor: "Required on every machine (v9)"
      - label: "Public source code"
        product: true
        competitor: "v8 only"
  - title: "Transports"
    rows:
      - label: "RabbitMQ"
        product: true
        competitor: true
      - label: "Azure Service Bus"
        product: true
        competitor: true
      - label: "PostgreSQL"
        product: "Preview"
        competitor: true
      - label: "SQL Server"
        product: false
        competitor: true
      - label: "Amazon SQS/SNS"
        product: false
        competitor: true
      - label: "ActiveMQ"
        product: false
        competitor: true
      - label: "Kafka and Event Hubs"
        product: false
        competitor: "As riders"
      - label: "In-memory"
        product: true
        competitor: true
  - title: "Messaging"
    rows:
      - label: "Publish and send"
        product: true
        competitor: true
      - label: "Request/reply"
        product: true
        competitor: true
      - label: "Batch consumers"
        product: true
        competitor: true
      - label: "Scheduled messages"
        product: true
        competitor: true
      - label: "Routing slips"
        product: false
        competitor: true
      - label: "Job consumers"
        product: false
        competitor: true
      - label: "In-process mediator"
        product: true
        competitor: true
  - title: "Reliability"
    rows:
      - label: "Retry and redelivery policies"
        product: true
        competitor: true
      - label: "Circuit breaker and concurrency limits"
        product: true
        competitor: true
      - label: "Rate limiting"
        product: false
        competitor: true
      - label: "Transactional outbox and inbox"
        product: "PostgreSQL"
        competitor: "PostgreSQL, SQL Server, MySQL, MongoDB"
  - title: "Sagas"
    rows:
      - label: "State machine sagas"
        product: true
        competitor: true
      - label: "Saga storage"
        product: "PostgreSQL, in-memory"
        competitor: "Entity Framework, MongoDB, Redis, Marten, and more"
      - label: "Correlation"
        product: "By saga ID"
        competitor: "By any message property"
  - title: "Development"
    rows:
      - label: "Handler registration"
        product: "Source generator at build time"
        competitor: "Assembly scanning or explicit"
      - label: "OpenTelemetry"
        product: true
        competitor: true
      - label: "Test harness"
        product: false
        competitor: true
      - label: "Target frameworks"
        product: ".NET 8 to 11"
        competitor: ".NET 8 to 10, .NET Standard 2.0, .NET Framework 4.7.2"
sources:
  - title: "Massient pricing and FAQ"
    url: "https://massient.com/"
  - title: "MassTransit Commercial Software License Agreement"
    url: "https://massient.com/license"
  - title: "MassTransit license key configuration"
    url: "https://masstransit.massient.com/configuration/license/"
  - title: "MassTransit v9 announcement, April 2025 (archived copy)"
    url: "https://web.archive.org/web/20251212070758/https://masstransit.io/introduction/v9-announcement"
  - title: "MassTransit v8 repository and license"
    url: "https://github.com/MassTransit/MassTransit"
  - title: "MassTransit 9.2.3 on NuGet"
    url: "https://www.nuget.org/packages/MassTransit/9.2.3"
  - title: "MassTransit 8.5.11 on NuGet"
    url: "https://www.nuget.org/packages/MassTransit/8.5.11"
  - title: "MassTransit transports"
    url: "https://masstransit.massient.com/concepts/transports/"
  - title: "MassTransit consumers"
    url: "https://masstransit.massient.com/concepts/consumers/"
  - title: "MassTransit middleware"
    url: "https://masstransit.massient.com/concepts/middleware/"
  - title: "MassTransit transactional outbox"
    url: "https://masstransit.massient.com/configuration/middleware/outbox/"
  - title: "MassTransit saga state machines"
    url: "https://masstransit.massient.com/concepts/saga-state-machines/"
  - title: "MassTransit Entity Framework saga repository"
    url: "https://masstransit.massient.com/configuration/saga-repositories/entity-framework/"
  - title: "MassTransit routing slips"
    url: "https://masstransit.massient.com/concepts/routing-slips/"
  - title: "MassTransit job consumers"
    url: "https://masstransit.massient.com/concepts/job-consumers/"
  - title: "MassTransit mediator"
    url: "https://masstransit.massient.com/configuration/mediator/"
  - title: "MassTransit test harness"
    url: "https://masstransit.massient.com/guides/unit-testing/"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Is MassTransit still free?"
    answer: "MassTransit 8 is licensed under Apache 2.0 and still gets releases. MassTransit 9 is commercial and costs $4,000 per year for one product line or $12,000 per year for a whole organization. Organizations with less than $1 million in annual revenue may qualify for a free license without commercial support."
  - question: "Can Mocha and MassTransit services exchange messages?"
    answer: "No. They use different message formats and different exchange and queue names. Move the services that exchange a message together, or move one message flow at a time and keep both libraries registered until it is done."
  - question: "Does Mocha support Amazon SQS, Kafka, or SQL Server?"
    answer: "Not yet. Transports build on a public base class, so you can write your own. You can also request a transport on GitHub, or get in touch and the ChilliCream team builds it for you."
  - question: "Can I keep my outbox and sagas in SQL Server or MongoDB?"
    answer: "Not with the stores that ship today, which use PostgreSQL. The outbox, inbox, and saga store are public interfaces, so you can implement them for your database, or have the ChilliCream team build them for you."
  - question: "Does Mocha have an in-process mediator like MassTransit Mediator?"
    answer: "Yes. The Mocha mediator dispatches commands, queries, and notifications inside a service, with handlers registered by the same source generator as the message bus."
gaps:
  - "Amazon SQS/SNS, ActiveMQ, SQL Server, Kafka, and Event Hubs transports."
  - "Outbox, inbox, and saga stores outside PostgreSQL, such as SQL Server, MySQL, MongoDB, or Redis."
  - "Routing slips, job consumers, rate limiting, and Quartz.NET or Hangfire scheduling."
  - "A dedicated test harness. Tests run the bus on the in-memory transport."
  - ".NET Framework and .NET Standard. Mocha targets .NET 8 and later."
---

# Free for every company, no key on any machine

Mocha is MIT licensed, like Hot Chocolate. Any company can use it in production for free. There is no paid edition, no license key, and no revenue limit, and the source code is public.

MassTransit 9, released in January 2026, is a commercial product from Massient. Its source code is available to licensed customers, not publicly. A license for one product line costs $4,000 per year, and one for the whole organization costs $12,000 per year. Organizations with less than $1 million in annual revenue may qualify for a free license without commercial support. The license key has to be available on every machine that runs MassTransit, including developer machines and build agents. MassTransit 8 stays under the Apache 2.0 license and still gets releases. When version 9 was announced in April 2025, the announcement said version 8 would be patched through at least the end of 2026. That page is no longer online, and no current page gives an end date.

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

Mocha decides what happens to a failed message by exception type, in one place: retry it, redeliver it later, dead-letter it, or discard it. Policies apply to the whole bus, a transport, or a single handler. Failed messages end up in an `_error` endpoint and unhandled ones in a `_skipped` endpoint,, the same suffixes MassTransit uses.

MassTransit configures retries and redelivery as separate middleware on each endpoint.

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

---
title: "Mocha vs. Brighter"
metaTitle: "Mocha vs Brighter: .NET Messaging Without the Ceremony"
description: "Compare Mocha and Brighter (Paramore.Brighter) for messaging in .NET: setup, handlers, request/reply, failed messages, the outbox, and sagas."
featuredImage: "mocha-vs-brighter.png"
date: "2026-10-29"
tags: ["mocha", "comparison", "brighter"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "Brighter"
    url: "https://github.com/BrighterCommand/Brighter"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "What happens to a message when a Mocha handler throws?"
    answer: "Mocha moves it to the endpoint's error queue with the exception type, message, and stack trace in its headers. With AddResilience, it first retries the message three times in process and then redelivers it after 5, 15, and 30 minutes."
  - question: "Does Mocha support sagas?"
    answer: "Yes. Mocha has state machine sagas that start from an event or a request, send commands, publish events, time out, and keep their state in PostgreSQL through Entity Framework Core."
  - question: "Can a Mocha request wait for a response from another service?"
    answer: "Yes. A request declares its response type, its handler returns the response, and the caller awaits it with RequestAsync without blocking a thread."
  - question: "Does Mocha support Native AOT?"
    answer: "Yes. A source generator registers your handlers at build time, so Mocha needs no assembly scanning or reflection to find them, and it reports a build error when a module that is published for Native AOT has no JsonSerializerContext."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license. Build your first service with the quick start, or move from Brighter step by step with the migration guide. Moving a large system?"
  primary:
    label: "Get started with Mocha"
    href: "/docs/mocha/quick-start"
  secondary:
    label: "Migrate from Brighter"
    href: "/docs/mocha/migration/coming-from-brighter"
  contact: "Talk to us about your migration"
---

Mocha does the messaging you build with Brighter in less code. Exchanges and queues come from naming conventions instead of a publication and a subscription for every message, a handler returns its result, a request awaits its response, and a failed message ends up in an error queue instead of being discarded. Sagas are built in, and a source generator registers your handlers at build time, ready for Native AOT.

# Conventions instead of a registry

Mocha creates exchanges and queues at startup from the message types and their handlers. An event goes to every service that handles it, and a command goes to the queue of its handler, named after the message type. The registration below is complete: `AddOrdering()` is written by the source generator when you compile, and the RabbitMQ connection comes from dependency injection, for example from Aspire's `AddRabbitMQClient`.

[Brighter](https://github.com/BrighterCommand/Brighter) routes by name. Every message type that leaves the service needs a publication with a routing key in a producer registry, and every message type a service receives needs a subscription, plus a hosted service that runs the message pumps.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

```csharp filename="Brighter"
var producers = new RmqProducerRegistryFactory(
    connection,
    [
        new RmqPublication<OrderPlaced>
        {
            Topic = new RoutingKey("order.placed"),
            MakeChannels = OnMissingChannel.Create
        },
        new RmqPublication<ReserveStock>
        {
            Topic = new RoutingKey("reserve.stock"),
            MakeChannels = OnMissingChannel.Create
        }
    ]).Create();

builder.Services
    .AddBrighter()
    .AddProducers(o =>
        o.ProducerRegistry = producers)
    .AutoFromAssemblies();
```

</CodeComparison>

# Plain messages, plain handlers

A Mocha message is a plain record with no base class. Its handler receives the message and returns a `ValueTask`, and the interface says what kind of message it handles.

A Brighter handler derives from `RequestHandlerAsync<T>`, returns the message it received, and has to call `base.HandleAsync` so the rest of the pipeline runs. Messages derive from `Command` or `Event`, and `Send` and `Post` return nothing to the caller: Brighter separates commands from queries by design.

<CodeComparison>

```csharp filename="Mocha"
public record OrderPlaced(Guid OrderId);

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

```csharp filename="Brighter"
public class OrderPlaced(Guid orderId)
    : Event(Id.Random())
{
    public Guid OrderId { get; } = orderId;
}

public class OrderPlacedHandler(
    IInvoiceService invoices)
    : RequestHandlerAsync<OrderPlaced>
{
    public override async Task<OrderPlaced>
        HandleAsync(OrderPlaced @event,
            CancellationToken ct = default)
    {
        await invoices.CreateAsync(
            @event.OrderId, ct);
        return await base.HandleAsync(@event, ct);
    }
}
```

</CodeComparison>

# Requests you can await

A Mocha request declares the response it expects, its handler returns that response, and the caller awaits it like any other asynchronous call. The compiler checks that the response type matches.

<MochaVisual
  name="request-reply"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

Brighter's request/reply over a broker is `Call`, which is synchronous: it blocks the calling thread until the reply arrives or the timeout expires, 500 milliseconds by default, and returns `null` on a timeout. The request and the reply derive from Brighter's `Request` and `Reply` types, and the request goes straight to the broker, not through the outbox.

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

```csharp filename="Brighter"
public class CheckOrderStatus()
    : Request(new ReplyAddress("", ""))
{
    public Guid OrderId { get; set; }
}

public class OrderStatus()
    : Reply(new ReplyAddress("", ""))
{
    public string State { get; set; } = "";
}

// Caller: blocks until the reply arrives
var status = commandProcessor
    .Call<CheckOrderStatus, OrderStatus>(
        new CheckOrderStatus { OrderId = orderId },
        timeOut: TimeSpan.FromSeconds(5));
```

</CodeComparison>

# Failed messages are never dropped

Without any configuration, Mocha moves a message whose handler throws to the endpoint's error queue, with the exception type, message, and stack trace in its headers. One call adds exception policies that decide by exception type what happens next: retry it, redeliver it later, dead-letter it, or discard it. Policies apply to the whole bus, a transport, or a single receive endpoint.

Brighter [acknowledges every message](https://brightercommand.gitbook.io/paramore-brighter-documentation/using-an-external-bus/handlerfailure), whether the handler succeeds or throws. To keep a failed message, you add attributes to each handler: a Polly resilience pipeline for retries, registered by name, and a backstop that requeues the message. The number of requeues and the dead-letter queue for a message that still fails are set on each subscription.

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

```csharp filename="Brighter"
// Retries, registered by name
pipelines.TryAddBuilder("Retry", (b, _) =>
    b.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
    }));

// On every handler: requeue after five
// minutes once the retries have failed
[DeferMessageOnErrorAsync(step: 0,
    delayMilliseconds: 300_000)]
[UseResiliencePipelineAsync("Retry", step: 1)]
public override async Task<PlaceOrder>
    HandleAsync(PlaceOrder command,
        CancellationToken ct = default)

// On every subscription: three requeues,
// then the dead-letter queue
new RmqSubscription<PlaceOrder>(
    new SubscriptionName("place-order"),
    new ChannelName("place.order"),
    new RoutingKey("place.order"),
    requeueCount: 4,
    deadLetterChannelName:
        new ChannelName("place.order.dlq"),
    deadLetterRoutingKey:
        new RoutingKey("place.order.dlq"),
    messagePumpType: MessagePumpType.Proactor,
    makeChannels: OnMissingChannel.Create);
```

</CodeComparison>

# An outbox that needs no extra calls

With the Mocha outbox, a handler publishes and sends through `IMessageBus` as usual. The messages are written to the database in the same transaction as your changes and delivered in the background, and the inbox drops messages a handler has already processed. Both use Entity Framework Core with PostgreSQL.

In Brighter, a handler deposits each outgoing message with `DepositPostAsync` inside a transaction it gets from a transaction provider, commits, and then clears the outbox. An outbox sweeper sends what is left.

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

```csharp filename="Brighter"
// In a handler
var tx = await transactionProvider
    .GetTransactionAsync(ct);
// ...write the order with tx
var id = await commandProcessor.DepositPostAsync(
    new OrderPlaced(order.Id),
    transactionProvider,
    cancellationToken: ct);
await transactionProvider.CommitAsync(ct);
await commandProcessor.ClearOutboxAsync(
    [id], cancellationToken: ct);
```

</CodeComparison>

# Sagas are built in

Mocha has state machine sagas, and Brighter has none. States are strings, events are type arguments, and each transition reads top to bottom. A saga can start from a request and answer it when it finishes, send commands, publish events, and time out, and its state lives in PostgreSQL through Entity Framework Core.

<MochaVisual
  name="saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

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

# Registered at build time, ready for Native AOT

The `Mocha.Analyzers` source generator finds your handlers when you compile and writes the registration method, so there is no assembly scanning at startup. Two handlers for the same command or request are a build error, and a service published with Native AOT gets its serialization code generated from a `JsonSerializerContext`.

Brighter's `AutoFromAssemblies()` scans the assemblies loaded into the application domain at startup, and the command processor reads each handler's pipeline attributes through reflection. Brighter [does not support Native AOT](https://github.com/BrighterCommand/Brighter/issues/3743).

# Moving from Brighter

Mocha and Brighter use different message formats, so plan the move by message flow: move the services that exchange a message together.

1. Add the `Mocha.Mediator`, `Mocha`, `Mocha.Analyzers`, and transport packages.
2. Replace `AddBrighter`, the producer registry, and the subscriptions with `AddMediator()`, `AddMessageBus()`, the generated registration methods, and the transport.
3. Move handlers you call in the process to the Mocha mediator, and handlers of broker messages to event, command, and request handlers.
4. Replace `PostAsync` and `DepositPostAsync` with `IMessageBus`, and `Call` with `RequestAsync`.
5. Replace resilience pipelines and requeue attributes with exception policies.

The [Brighter migration guide](../docs/mocha/migration/coming-from-brighter.md) walks through every step with code.

# Mocha and Brighter at a glance

<!-- The FeatureStatus links are placeholders until the issues and pull requests exist. -->

## Licensing

|                     | Mocha       | Brighter    |
| ------------------- | ----------- | ----------- |
| License             | MIT         | MIT         |
| Free commercial use | Any company | Any company |

## Transports

|                   | Mocha                                                                                                  | Brighter |
| ----------------- | ------------------------------------------------------------------------------------------------------ | -------- |
| RabbitMQ          | Yes                                                                                                    | Yes      |
| Azure Service Bus | Yes                                                                                                    | Yes      |
| PostgreSQL        | Preview                                                                                                | Yes      |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |
| Amazon SQS/SNS    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |
| Kafka             | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |
| In-memory         | Yes                                                                                                    | Yes      |

## Messaging

|                     | Mocha | Brighter         |
| ------------------- | ----- | ---------------- |
| Publish and send    | Yes   | Yes              |
| Request/reply       | Yes   | Synchronous only |
| Batch consumers     | Yes   | No               |
| Scheduled messages  | Yes   | Yes              |
| State machine sagas | Yes   | No               |
| In-process mediator | Yes   | Yes              |

## Reliability

|                                 | Mocha                    | Brighter                 |
| ------------------------------- | ------------------------ | ------------------------ |
| Failed message without a policy | Moved to the error queue | Acknowledged and dropped |
| Retry and redelivery policies   | Yes                      | Yes                      |
| Circuit breaker                 | Yes                      | Yes                      |
| Transactional outbox and inbox  | Yes                      | Yes                      |

## Storage

Databases for the outbox and inbox.

|            | Mocha                                                                                                  | Brighter |
| ---------- | ------------------------------------------------------------------------------------------------------ | -------- |
| PostgreSQL | Yes                                                                                                    | Yes      |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes      |

## Development

|                      | Mocha                          | Brighter                                |
| -------------------- | ------------------------------ | --------------------------------------- |
| Handler registration | Source generator at build time | Assembly scanning or manual, at startup |
| Native AOT           | Yes                            | No                                      |
| OpenTelemetry        | Yes                            | Yes                                     |
| Target frameworks    | .NET 8 to 11                   | .NET 8 to 10, .NET Standard 2.0         |

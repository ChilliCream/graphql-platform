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

Building a message-driven application should be about the messages your services exchange, not the infrastructure required to move them around. [Brighter](https://github.com/BrighterCommand/Brighter) has been part of the .NET messaging ecosystem for years. It brings command processing, messaging, resilience, and the outbox pattern together in a framework built around explicit configuration. Mocha takes a different approach.

Instead of configuring publications, subscriptions, and routing keys for every message, Mocha uses conventions to figure out where messages belong. Handlers work with plain C# types, requests return strongly typed responses, and failed messages are preserved automatically. It also brings a few things that aren't available in Brighter today, including asynchronous request/reply, state machine sagas, and Native AOT support.

Let's look at what those differences mean when you're actually building an application.

# Messaging without all the configuration

One of the first things you'll notice when moving from Brighter to Mocha is how little configuration you need to get started. In Brighter, message routing is explicit. Every message you publish needs an entry in a producer registry, complete with its routing key. On the receiving side, you configure subscriptions and the message pumps that process incoming messages.

That gives you control over your messaging topology, but it also means maintaining configuration as your application grows. Mocha approaches this differently.

**Your messages and their handlers already describe how your application communicates. Mocha uses that information to create the messaging topology for you.**

When your application starts, Mocha discovers the registered message types and handlers and creates the necessary exchanges and queues. Events are delivered to every service that handles them. Commands are routed to the queue belonging to their handler, using the message type as the naming convention. Here's what the setup looks like.

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

That's the complete Mocha registration. The `AddOrdering()` method is generated at compile time, so there's no assembly scanning required when your application starts. The RabbitMQ connection comes from dependency injection, which also makes it easy to integrate with tools like .NET Aspire and its `AddRabbitMQClient` extension. As you add more messages and handlers, you don't need to keep updating a separate registry of routing information.

**You define what your application handles. Mocha takes care of connecting the pieces.**

# Handlers that look like ordinary C\#

Message handlers are where most of your application logic lives, so it's worth keeping them simple. Mocha doesn't require messages to inherit from framework-specific base classes. A message can be an ordinary C# record, and a handler is just a class implementing the appropriate interface. For example, imagine an ordering service publishes an `OrderPlaced` event and an invoicing service needs to react to it. With Mocha, the event and its handler look like this:

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

Both handlers accomplish the same thing: they create an invoice when an order is placed. The difference is how much framework machinery appears in your application code. Brighter requires messages to derive from `Command` or `Event`. Handlers inherit from `RequestHandlerAsync<T>`, return the message they received, and call `base.HandleAsync` to continue the processing pipeline. In Mocha, the handler simply returns a `ValueTask`.

There's no base class to inherit from, no message to return, and no pipeline continuation to remember. This becomes particularly useful as an application grows. When you have dozens or hundreds of handlers, keeping each one focused on its actual responsibility makes the codebase easier to navigate and maintain.

# Request/reply that feels like an async method call

Not every interaction between services is a fire-and-forget message. Sometimes one service needs information from another before it can continue. Imagine an ordering service that needs to check the status of an order stored by another service. With Mocha, you can model that interaction as a strongly typed request. The request declares the response type it expects, the handler returns that response, and the caller awaits the result.

<MochaVisual
  name="order-status"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

Here's what that looks like in practice.

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

The Mocha version behaves much like any other asynchronous operation in .NET. You send a request, await its response, and continue working with a strongly typed result. The compiler knows which response belongs to which request. Brighter also supports request/reply over a message broker, but its `Call` API is synchronous.

That means the calling thread blocks while waiting for the reply. If no response arrives before the timeout, which defaults to 500 milliseconds, `Call` returns `null`. Brighter also requires requests and replies to derive from its `Request` and `Reply` types. These calls go directly to the broker rather than through the outbox. **Mocha makes request/reply a natural part of asynchronous C# programming**, without requiring a separate synchronous communication model.

For applications that frequently coordinate work across services, that can make a meaningful difference.

# When something fails, the message is still there

Failures are inevitable in a distributed system. A database might be temporarily unavailable. A downstream service might be overloaded. A message might contain invalid data. What matters is what happens next. Mocha starts with a simple principle: **a message shouldn't disappear just because its handler throws an exception.** By default, when a handler fails, Mocha moves the message to the receive endpoint's error queue.

The original message is preserved along with diagnostic information, including the exception type, message, and stack trace. You can inspect the failure, understand what happened, and decide how to recover. For more control, Mocha provides exception-based resilience policies.

<MochaVisual
  name="retry"
  caption="The default policy retries a failing message three times in process, then redelivers it after 5, 15, and 30 minutes. A validation error goes straight to the error endpoint."
/>

For example, you might want to retry temporary failures but immediately dead-letter messages that fail validation.

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

There's an important difference in the default behavior. Brighter [acknowledges messages even when their handlers throw](https://brightercommand.gitbook.io/paramore-brighter-documentation/using-an-external-bus/handlerfailure). Without additional failure-handling configuration, those messages are effectively lost. To preserve and retry them, you configure a Polly resilience pipeline, reference it through handler attributes, and add a backstop that requeues failed messages.

Requeue limits and dead-letter destinations are then configured on the individual subscriptions. Mocha brings these decisions together in one policy. You can apply policies across the entire message bus, to a specific transport, or to individual receive endpoints. That means you can establish sensible defaults for your application while still handling exceptional cases differently.

For example, a `ValidationException` can immediately move a message to the error queue, while a temporary database failure can trigger retries followed by delayed redelivery. And if you haven't configured anything yet, the failed message is still preserved.

# A transactional outbox without changing how you publish

Publishing a message after updating a database sounds straightforward. But what happens if the database transaction succeeds and the message broker is unavailable? Your order might be saved without the corresponding `OrderPlaced` event ever reaching another service. The transactional outbox pattern solves this by storing outgoing messages in the same database transaction as the application changes. Both Mocha and Brighter support this pattern, but they expose it differently.

With Mocha, **you continue using `IMessageBus` exactly as you normally would.** Once the outbox is configured, Mocha takes care of storing outgoing messages transactionally and delivering them in the background.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

Here's an example using Entity Framework Core and PostgreSQL.

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

Notice what's missing from the Mocha handler. There's no explicit outbox deposit, no separate transaction provider, and no call to clear the outbox after committing. The handler adds an order, publishes an event, and saves its changes. Mocha writes the outgoing message as part of the database transaction and delivers it asynchronously. The inbox provides the other half of the reliability story.

Message brokers can deliver the same message more than once. With the inbox enabled, Mocha tracks processed messages so duplicates can be skipped. Together, the outbox and inbox help keep database changes and message processing consistent, without requiring every handler to manage those mechanics.

Brighter offers both patterns as well, but its outbox API requires handlers to explicitly deposit messages, participate in a transaction, and clear deposited messages after the commit. An outbox sweeper takes care of messages that remain. Mocha keeps those implementation details out of your business logic.

# Long-running workflows with built-in sagas

Some business processes can't be completed in a single operation. Consider an order workflow. An order is submitted, inventory is reserved, payment is processed, and the order is eventually accepted or rejected. Those steps might involve different services, and they might happen seconds or minutes apart. You need somewhere to remember the workflow's current state and decide what should happen when the next message arrives. That's what a saga is for.

**Mocha includes state machine sagas as a built-in messaging capability.**

A saga defines its states, the events it responds to, and the transitions between them.

<MochaVisual
  name="order-saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

For example, here's a simple order saga.

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

The workflow is expressed directly in code. When an `OrderSubmitted` event arrives, Mocha creates a saga instance and moves it into the `Submitted` state. When the corresponding `OrderAccepted` event arrives, the saga transitions to `Accepted` and completes. Of course, real workflows are usually more involved. Mocha sagas can also send commands, publish events, handle timeouts, and coordinate requests that expect a response when the workflow finishes.

Saga state can be persisted in PostgreSQL using Entity Framework Core, allowing workflows to survive application restarts. There's also an important detail when coordinating messages across services: Mocha ignores correlated events if their saga instance doesn't exist yet. When an initial transition needs to trigger work that can produce a reply, you should publish that message from the initial transition and commit it together with the saga state through the outbox.

This ensures that the workflow's state is persisted before the corresponding reply can be processed. Brighter doesn't currently provide a built-in saga implementation, so applications that need this kind of orchestration must introduce another solution. With Mocha, sagas use the same messaging infrastructure as the rest of your application.

# Source-generated registration and Native AOT

There's another difference between Mocha and Brighter that becomes especially interesting when you look at application startup and deployment. Brighter discovers handlers through assembly scanning. Its `AutoFromAssemblies()` method inspects assemblies loaded into the application domain at startup, and the command processor uses reflection to inspect handler pipeline attributes. Mocha moves handler discovery to compile time.

The `Mocha.Analyzers` source generator finds your handlers during compilation and generates the registration code for them. That's where the `AddOrdering()` method from the first example comes from. Instead of searching for handlers when the application starts, Mocha already knows which handlers exist. This also allows the compiler to catch certain configuration problems early.

For example, registering two handlers for the same command or request produces a build error rather than leaving the problem to surface at runtime. The same approach supports Native AOT. When publishing a Mocha application with Native AOT, serialization metadata can be generated using a `JsonSerializerContext`, avoiding the need for reflection-based serialization.

Brighter's reflection-based architecture means it [doesn't currently support Native AOT](https://github.com/BrighterCommand/Brighter/issues/3743). For applications targeting smaller deployments, faster startup, or ahead-of-time compilation, this is an important distinction.

# Moving from Brighter to Mocha

If you're already using Brighter, migrating to Mocha is more than replacing a few method calls. The two frameworks use different message formats, so existing producers and consumers won't automatically understand each other's messages. For that reason, it's best to approach migration one message flow at a time. Move the services that exchange a particular message together, verify that the new flow works, and then continue with the next part of the application.

The main changes are:

1. **Install Mocha.** Add `Mocha.Mediator`, `Mocha`, `Mocha.Analyzers`, and the transport packages your application needs.
2. **Replace the messaging configuration.** Move from `AddBrighter`, producer registries, and subscriptions to `AddMediator()`, `AddMessageBus()`, generated registration methods, and your transport configuration.
3. **Update your handlers.** Move in-process handlers to the Mocha mediator and broker-facing handlers to the appropriate event, command, or request interfaces.
4. **Switch to the Mocha messaging APIs.** Replace `PostAsync` and `DepositPostAsync` with `IMessageBus`, and synchronous `Call` operations with `RequestAsync`.
5. **Bring over your resilience behavior.** Replace handler-level resilience attributes and requeue configuration with Mocha's exception policies.

You don't need to redesign your entire application at once. Start with a small message flow, migrate its producers and consumers, and build from there. For a more detailed walkthrough, including code examples for each step, see the [Brighter migration guide](../docs/mocha/migration/coming-from-brighter.md).

<MigrationGuideCard href="/docs/mocha/migration/coming-from-brighter" from="Brighter" />

# Feature comparison

The examples above show how the two frameworks differ in everyday use. But there's more to choosing a messaging framework than the amount of code you write. Transport support, persistence options, deployment requirements, and available messaging patterns all matter. Here's how Mocha and Brighter compare across those areas.

## Licensing

Both projects use the MIT license and can be used commercially without licensing fees.

| Feature             | Mocha       | Brighter    |
| ------------------- | ----------- | ----------- |
| License             | MIT         | MIT         |
| Free commercial use | Any company | Any company |

## Transports

Both frameworks support RabbitMQ and Azure Service Bus. Brighter currently offers a broader selection of transport integrations, including SQL Server, Amazon SQS/SNS, and Kafka. Mocha supports RabbitMQ, Azure Service Bus, and in-memory messaging, with PostgreSQL transport support in preview and additional transports planned.

| Feature           | Mocha                                                                                                  | Brighter |
| ----------------- | ------------------------------------------------------------------------------------------------------ | -------- |
| RabbitMQ          | Yes                                                                                                    | Yes      |
| Azure Service Bus | Yes                                                                                                    | Yes      |
| PostgreSQL        | Preview                                                                                                | Yes      |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11031" /> | Yes      |
| Amazon SQS/SNS    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11032" /> | Yes      |
| Kafka             | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11033" /> | Yes      |
| In-memory         | Yes                                                                                                    | Yes      |

## Messaging capabilities

Both frameworks cover the fundamentals of publishing events, sending commands, scheduling messages, and handling messages in-process. Mocha adds asynchronous request/reply, batch consumers, and built-in state machine sagas.

| Feature             | Mocha | Brighter         |
| ------------------- | ----- | ---------------- |
| Publish and send    | Yes   | Yes              |
| Request/reply       | Yes   | Synchronous only |
| Batch consumers     | Yes   | No               |
| Scheduled messages  | Yes   | Yes              |
| State machine sagas | Yes   | No               |
| In-process mediator | Yes   | Yes              |

## Reliability

Both frameworks offer retries, redelivery, circuit breakers, and transactional messaging patterns. The biggest difference is what happens when a handler fails without an explicit failure policy. Mocha preserves the failed message in an error queue by default. Brighter acknowledges it, so additional configuration is needed to prevent the message from being lost.

| Feature                         | Mocha                    | Brighter                 |
| ------------------------------- | ------------------------ | ------------------------ |
| Failed message without a policy | Moved to the error queue | Acknowledged and dropped |
| Retry and redelivery policies   | Yes                      | Yes                      |
| Circuit breaker                 | Yes                      | Yes                      |
| Transactional outbox and inbox  | Yes                      | Yes                      |

## Outbox and inbox storage

Both frameworks support PostgreSQL for transactional messaging. Brighter currently offers more database options, including SQL Server, MySQL, and MongoDB. Mocha's Entity Framework Core integration currently focuses on PostgreSQL, with additional storage providers planned.

| Feature    | Mocha                                                                                                  | Brighter |
| ---------- | ------------------------------------------------------------------------------------------------------ | -------- |
| PostgreSQL | Yes                                                                                                    | Yes      |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11035" /> | Yes      |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11036" /> | Yes      |
| MongoDB    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/11037" /> | Yes      |

## Development and deployment

Mocha uses source generation for handler registration and supports Native AOT. Brighter uses runtime assembly scanning or manual registration and supports a broader range of target frameworks, including .NET Standard 2.0. Both provide OpenTelemetry integration.

| Feature              | Mocha                          | Brighter                                |
| -------------------- | ------------------------------ | --------------------------------------- |
| Handler registration | Source generator at build time | Assembly scanning or manual, at startup |
| Native AOT           | Yes                            | No                                      |
| OpenTelemetry        | Yes                            | Yes                                     |
| Target frameworks    | .NET 8 to 11                   | .NET 8 to 10, .NET Standard 2.0         |

# Choosing between Mocha and Brighter

Brighter and Mocha solve many of the same problems, but they make different trade-offs. Brighter gives you explicit control over how messages are published, routed, and consumed. It also has a broader selection of transport and storage integrations today. If your application depends on those integrations, or you've already invested heavily in Brighter's configuration model, those are good reasons to continue using it.

Mocha is built around a different idea: **most applications shouldn't need to describe the same messaging infrastructure over and over again.** Your message types already describe what is being exchanged. Your handlers already describe who receives it. And your application code shouldn't need to change just because you enable an outbox or introduce a retry policy.

That's why Mocha uses conventions for routing, source generation for registration, and plain C# types for messages and handlers. It's also why features such as asynchronous request/reply, state machine sagas, resilience policies, and transactional messaging are designed to work together rather than requiring separate programming models. The result is less infrastructure code to write and maintain, with more of your application focused on the work it's actually supposed to do.

If you're starting a new .NET messaging application, or considering simplifying an existing Brighter setup, Mocha is worth exploring. And if you're ready to migrate, the [Brighter migration guide](../docs/mocha/migration/coming-from-brighter.md) will help you get started.

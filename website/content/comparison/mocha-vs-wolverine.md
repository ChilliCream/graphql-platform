---
title: "Mocha vs. Wolverine"
metaTitle: "Mocha vs Wolverine: Build-Time Messaging for .NET"
description: "Compare Mocha and Wolverine (WolverineFx) for messaging in .NET: handler discovery, code generation, Native AOT, request/reply, the outbox, and sagas."
featuredImage: "mocha-vs-wolverine.png"
date: "2026-10-29"
tags: ["mocha", "comparison", "wolverine"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "Wolverine"
    url: "https://wolverinefx.net/"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Does Mocha compile code at runtime?"
    answer: "No. The Mocha.Analyzers source generator writes the registration and dispatch code when you compile, so there is no Roslyn compiler at runtime and no generated code to check in or regenerate."
  - question: "Does Mocha support Native AOT?"
    answer: "Yes. Handlers are registered at build time, and for a service published with Native AOT the source generator writes the serialization code from the JsonSerializerContext you reference in the MessagingModule attribute. There is no separate code generation step."
  - question: "How does Mocha find my handlers?"
    answer: "A handler implements an interface such as IEventHandler<T> or IEventRequestHandler<TRequest, TResponse>, and the source generator registers it when you compile. Two handlers for the same command or request are a build error."
  - question: "Does Mocha have sagas and a transactional outbox?"
    answer: "Yes. Mocha has state machine sagas with timeouts, and a transactional outbox and inbox that live in your Entity Framework Core DbContext."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license. Build your first service with the quick start, or move from Wolverine step by step with the migration guide. Moving a large system?"
  primary:
    label: "Get started with Mocha"
    href: "/docs/mocha/quick-start"
  secondary:
    label: "Migrate from Wolverine"
    href: "/docs/mocha/migration/coming-from-wolverine"
  contact: "Talk to us about your migration"
---

Building a message-driven application in .NET comes with plenty of decisions. How do you discover handlers? When does code generation happen? How do you keep database changes and outgoing messages consistent? And how much of your messaging infrastructure can the compiler actually verify?

[Wolverine](https://wolverinefx.net/) has established itself as a powerful option for building message-driven .NET applications. It combines messaging, durable execution, and workflow orchestration with a convention-based programming model. **Mocha takes a different approach.** It puts more responsibility on the compiler, uses explicit contracts for handlers and requests, and generates the code your application needs during the build rather than at runtime.

Both frameworks are MIT licensed. Both support familiar messaging patterns, including publish/subscribe, request/reply, transactional outboxes, and sagas. And both use code generation to avoid unnecessary runtime overhead. The interesting differences are in how they get there. Let's look at what those differences mean when you're building an application.

# Handlers: Conventions vs. Contracts

Every messaging framework needs a way to connect incoming messages to the code that handles them. Wolverine makes this straightforward through conventions. Create a public class whose name ends in `Handler` or `Consumer`, add a public method called `Handle` or `Consume`, and Wolverine discovers it automatically. You can also use marker interfaces or attributes when you don't want to follow the naming conventions. Mocha approaches handler discovery differently.

Instead of relying on names, **handlers explicitly declare which messages they handle through interfaces.** There are three main interfaces:

- `IEventHandler<T>` handles published events.
- `IEventRequestHandler<T>` handles commands.
- `IEventRequestHandler<TRequest, TResponse>` handles requests that expect a response.

Here's what an event handler looks like in both frameworks.

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

```csharp filename="Wolverine" {1,4,5}
public class OrderPlacedHandler(
    IInvoiceService invoices)
{
    public async Task Handle(
        OrderPlaced message, CancellationToken ct)
        => await invoices.CreateAsync(
            message.OrderId, ct);
}
```

</CodeComparison>

At first glance, there's hardly any difference. Both handlers receive an `OrderPlaced` event and create an invoice. The difference becomes more interesting when something goes wrong.

In Wolverine, handler discovery depends on conventions being followed correctly. If a class or method doesn't match the expected pattern, Wolverine won't recognize it as a handler. Because those conventions aren't checked by the C# compiler, the problem may only become apparent when a message isn't handled. With Mocha, the interface is a contract. The compiler knows that `OrderPlacedHandler` must implement `HandleAsync` with the correct message type and return type.

Change the method signature incorrectly, and the project won't compile. Mocha's source generator also uses these interfaces to discover and register handlers automatically. You don't need to maintain a separate list of registrations. Neither approach is inherently complicated. Wolverine favors minimal ceremony and flexibility, while Mocha favors explicit contracts and compile-time validation.

For teams that prefer catching messaging mistakes before an application starts, that distinction matters.

# Code Generation: Build Time vs. Runtime

Both Mocha and Wolverine generate code to execute handlers efficiently. The difference is **when that code gets generated and compiled.** Mocha uses a Roslyn source generator, provided by `Mocha.Analyzers`, that runs as part of the normal .NET build. When you compile your application, Mocha discovers your handlers, validates their contracts, and generates the registration code needed to connect everything.

For example, if your handlers live in `MyApp.Ordering`, the generator creates an `AddOrdering()` registration method. Your application setup becomes:

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMessageBus()
    .AddOrdering() // written at build time
    .AddRabbitMQ();
```

```csharp filename="Wolverine 6"
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq()
        .AutoProvision()
        .UseConventionalRouting();

    // Production: load the files that
    // `dotnet run -- codegen write` wrote
    opts.CodeGeneration.TypeLoadMode =
        TypeLoadMode.Static;
});
```

</CodeComparison>

Once Mocha's build finishes, the generated code is already part of your assembly. There's no runtime compiler to load, no generated source files to check into version control, and no additional code-generation command to run before deployment. The generator also validates certain messaging constraints. For example, registering two handlers for the same command or request produces a build error.

## How Wolverine handles generated code

Wolverine also generates optimized handler execution code, but its default workflow is different. By default, Wolverine compiles generated handler wrappers at runtime using Roslyn, when they're first needed. Starting with Wolverine 6.0, runtime compilation requires the separate `WolverineFx.RuntimeCompilation` package. This approach works, but it introduces additional memory usage and startup costs because the application needs access to the compiler.

Wolverine's own [code generation documentation](https://wolverinefx.net/guide/codegen) recommends a different setup for production: generate the code ahead of time using `codegen write`, then configure `TypeLoadMode.Static` to load those generated types. That avoids runtime compilation, but introduces another step into the development and deployment workflow. When handlers change, the generated files need to be updated, either manually or through CI.

## What about Native AOT?

This difference also affects Native AOT deployments. Mocha's generated handler code is compiled directly into the application. For Native AOT, it also generates serialization support based on a `JsonSerializerContext`. Wolverine supports Native AOT as well, but requires its static code-generation mode because runtime compilation isn't available in an AOT application.

**Both frameworks can produce applications without runtime code generation.** Mocha simply makes that the default development experience. If you're building applications where startup time, deployment simplicity, or Native AOT compatibility matters, removing the separate code-generation workflow can be useful.

# Request/Reply: Let the Compiler Check the Response

Request/reply messaging is another area where the two frameworks make different trade-offs. Imagine an ordering service that needs to check the status of an order. It sends a `CheckOrderStatus` request and expects an `OrderStatus` response. Both Mocha and Wolverine support this pattern, including communication between services over a message broker. The difference is how they represent the relationship between the request and its response.

<MochaVisual
  name="order-status"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

## Mocha connects the request to its response type

In Mocha, the request itself declares what kind of response it expects.

```csharp
public record CheckOrderStatus(Guid OrderId)
    : IEventRequest<OrderStatus>;
```

That single declaration establishes a contract: `CheckOrderStatus` produces an `OrderStatus`. The handler implements the corresponding interface, and the caller can send the request without specifying the response type again.

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

```csharp filename="Wolverine"
public record CheckOrderStatus(Guid OrderId);

public class CheckOrderStatusHandler(
    OrderStore orders)
{
    public Task<OrderStatus> Handle(
        CheckOrderStatus request)
        => orders.GetStatusAsync(
            request.OrderId);
}

// Caller: the response type is not checked
var status = await bus.InvokeAsync<OrderStatus>(
    new CheckOrderStatus(orderId), ct);
```

</CodeComparison>

In Wolverine, the caller specifies the expected response type through `InvokeAsync<T>`. That's flexible, but it also means the request type doesn't define its response contract. For example, a caller could accidentally write:

```csharp
var status = await bus.InvokeAsync<Customer>(
    new CheckOrderStatus(orderId), ct);
```

The compiler has no way to know that `CheckOrderStatus` is supposed to return an `OrderStatus`. In Mocha, that mismatch is caught at compile time because the response type is part of the request's interface. The practical benefit becomes more noticeable as an application grows.

When requests are shared between multiple services or used across many call sites, the response contract stays attached to the request itself. If that contract changes, the compiler can help identify the code that needs updating.

## Remote requests still need care

Compile-time safety doesn't eliminate the challenges of distributed communication. A remote request can still time out, its destination might be unavailable, or the network might fail. Wolverine uses a default timeout of five seconds for remote invocations, and its maintainers recommend using remote request/reply cautiously.

The same architectural consideration applies to Mocha: request/reply is useful when a caller genuinely needs an answer, but it also introduces temporal coupling between services. Mocha's contribution here is type safety, not a promise that distributed requests can't fail.

# Reliable Messaging: Keeping the Database and Broker in Sync

Consider a common scenario in an ordering system. A customer places an order. Your application needs to save the order to PostgreSQL and publish an `OrderPlaced` event so other services can react. Doing those operations independently creates a problem. What happens if the database transaction succeeds, but publishing the event fails? The order exists, but the rest of the system never hears about it.

Or what happens if the event gets published, but the database transaction rolls back? Other services might start processing an order that doesn't exist. This is the problem the transactional outbox pattern solves. Instead of publishing immediately, the application stores outgoing messages in the same database transaction as its business changes. A background process then delivers those messages to the broker.

An inbox provides the complementary protection on the receiving side by recognizing messages that have already been processed.

<MochaVisual
  name="outbox"
  caption="The outbox commits outgoing messages with your database changes, and the inbox skips a message the broker delivers twice."
/>

Both Mocha and Wolverine support transactional outboxes and inboxes. The difference is primarily in how they're configured.

## Mocha keeps persistence configuration together

Mocha integrates its outbox and inbox directly with your Entity Framework Core `DbContext`. You can enable both, along with transactional handling, in one registration.

<CodeComparison>

```csharp filename="Mocha"
.AddEntityFramework<AppDbContext>(p =>
{
    p.UsePostgresOutbox();
    p.UseTransaction();
    p.UsePostgresInbox();
})
```

```csharp filename="Wolverine"
opts.Services
    .AddDbContextWithWolverineIntegration<
        AppDbContext>(x => x.UseNpgsql(connection));
opts.PersistMessagesWithPostgresql(
    connection, "wolverine");
opts.UseEntityFrameworkCoreTransactions();
opts.Policies.AutoApplyTransactions();
opts.Policies.UseDurableLocalQueues();
opts.Policies
    .UseDurableOutboxOnAllSendingEndpoints();
opts.Policies.UseDurableInboxOnAllListeners();
```

</CodeComparison>

Wolverine offers similar capabilities through its Entity Framework Core integration and durable messaging policies. It separates configuration for database integration, transactions, local queues, sending endpoints, and listeners. That gives you control over how durability is applied throughout the application, but it also requires more configuration. Mocha groups the outbox, inbox, and transaction configuration around the `DbContext`.

For applications that use Entity Framework Core as their persistence layer, this provides a compact way to enable reliable messaging without managing several separate policies. Of course, an outbox doesn't magically guarantee that a message will only ever be delivered once. Brokers can redeliver messages, and failures can happen during processing. That's why the inbox matters: it helps prevent the same message from being processed repeatedly.

Together, these patterns make it much easier to keep application state and messaging behavior consistent when failures occur.

# Sagas: Making Long-Running Workflows Explicit

Not every business process fits into a single transaction. An order might need to be submitted, validated, accepted, paid for, and eventually shipped. Those steps can involve several services, multiple messages, and failures that happen minutes or hours apart. Sagas help coordinate workflows like these by keeping track of their progress across messages. Both Mocha and Wolverine support sagas, but they model them differently.

**Mocha represents a saga as an explicit state machine. Wolverine represents it as a class whose methods react to messages.**

Let's look at a simple order workflow. An order starts when an `OrderSubmitted` event arrives. Later, an `OrderAccepted` event moves the workflow forward.

<MochaVisual
  name="order-saga"
  caption="A saga moves through its states as messages arrive and keeps its state in between."
/>

## Explicit states and transitions in Mocha

In Mocha, you define the states a saga can enter and the events that trigger transitions between them. The workflow is described in one place.

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

```csharp filename="Wolverine"
public record OrderSubmitted(Guid OrderId);
public record OrderAccepted(
    [property: SagaIdentity] Guid OrderId);

public class OrderSaga : Saga
{
    public Guid Id { get; set; }

    public static OrderSaga Start(
        OrderSubmitted submitted)
        => new() { Id = submitted.OrderId };

    public void Handle(OrderAccepted accepted)
        => MarkCompleted();
}
```

</CodeComparison>

Both examples describe the same basic workflow, but they communicate it differently. In Mocha, you can read the state machine from top to bottom. Initially, the saga waits for `OrderSubmitted`. When that event arrives, it creates the saga state and transitions to `Submitted`. While in `Submitted`, it accepts `OrderAccepted` and transitions to `Accepted`. The `Accepted` state is final.

As workflows become more complex, this structure makes it easier to see which events are valid in which states.

## Wolverine uses methods and conventions

Wolverine takes a more code-oriented approach. A static `Start` method creates a saga instance, `Handle` methods process subsequent messages, and `MarkCompleted()` indicates that the workflow has finished. Instead of defining named states and transitions, the saga's current state is represented by its properties and the logic inside its methods. This can be appealing for straightforward workflows because it looks like regular C# application code.

Mocha's state-machine approach becomes particularly interesting when workflows have multiple stages, branching transitions, or events that should only be handled in certain states. Rather than distributing those rules across methods, the transitions become part of the saga definition itself.

## How messages find their saga

A saga also needs to know which running workflow an incoming message belongs to. Mocha correlates messages using the saga ID. In the example, the state factory initializes that ID from `OrderSubmitted.OrderId`. Subsequent events implement `ICorrelatable` and return the same identifier through `CorrelationId`.

Wolverine uses naming conventions to locate the saga identity. It recognizes members such as `OrderSagaId`, `SagaId`, or `Id`. You can also explicitly identify the correlation property with `[SagaIdentity]`. Both approaches support correlating messages with persisted saga instances. Mocha makes the correlation contract explicit, while Wolverine supports discovery through conventions.

## More than just state transitions

Mocha sagas can also send commands, publish events, schedule timeouts, and coordinate request/reply workflows. A saga can even begin with a request and provide the response once the workflow finishes. Saga state can be persisted in PostgreSQL through Entity Framework Core, or stored in memory for tests. There's one behavior worth understanding when designing a Mocha saga: **if a correlated event arrives before its saga instance exists, Mocha ignores it.**

For workflows where the initial event triggers additional messages, you should publish those messages as part of the initial transition and commit them together with the saga state through the outbox. That way, downstream processing doesn't begin before the saga has been persisted.

# Moving an Existing Wolverine Application to Mocha

If you're already using Wolverine, you don't need to rethink your entire messaging architecture to adopt Mocha. Both frameworks support the same fundamental patterns, so many of the concepts carry over directly. Handlers still process messages. Requests still return responses. Outboxes still protect database changes and outgoing messages. Sagas still coordinate long-running workflows. What changes is how those concepts are expressed and configured.

One important consideration is that **Mocha and Wolverine use different message formats.** That means migration should be planned around message flows rather than individual handler classes. If two services exchange messages, it's generally better to migrate them together instead of assuming their existing transport messages will be interchangeable. A typical migration looks like this:

1. **Install Mocha.** Add the `Mocha`, `Mocha.Analyzers`, and relevant transport packages to your application.
2. **Replace the bus configuration.** Swap `UseWolverine` for `AddMessageBus()`, the generated registration method, and your transport configuration.
3. **Update your handlers.** Implement the appropriate Mocha handler interfaces and rename `Handle` methods to `HandleAsync`.
4. **Migrate request/reply calls.** Move in-process `InvokeAsync` calls to the Mocha mediator, and replace remote `InvokeAsync<T>` calls with `RequestAsync`.
5. **Move reliability and workflow configuration.** Translate your retry policies, outbox and inbox setup, and sagas to their Mocha equivalents.

You can approach this incrementally, moving related services together while keeping the rest of your system on Wolverine. For a more detailed walkthrough, including code examples, see the [migration guide from Wolverine to Mocha](../docs/mocha/migration/coming-from-wolverine.md).

<MigrationGuideCard href="/docs/mocha/migration/coming-from-wolverine" from="Wolverine" />

# Feature comparison

So far, we've focused on how the two frameworks approach messaging. But implementation style is only part of the decision. Transport support, storage options, operational features, and framework compatibility also matter. Here's how Mocha and Wolverine compare across those areas.

## Transports

Both frameworks support RabbitMQ, Azure Service Bus, and in-memory messaging. Wolverine currently has broader transport coverage, particularly if you're working with SQL Server, Amazon SQS/SNS, or Kafka.

| Feature           | Mocha                                                                                                  | Wolverine |
| ----------------- | ------------------------------------------------------------------------------------------------------ | --------- |
| RabbitMQ          | Yes                                                                                                    | Yes       |
| Azure Service Bus | Yes                                                                                                    | Yes       |
| PostgreSQL        | Preview                                                                                                | Yes       |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| Amazon SQS/SNS    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| Kafka             | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| In-memory         | Yes                                                                                                    | Yes       |

If your application depends on a transport that Mocha doesn't support yet, that's an important consideration before migrating.

## Messaging

Both frameworks cover the core messaging patterns you'd expect, including scheduled messages, batch consumers, request/reply, and in-process messaging. The main differences are compile-time response validation and the saga programming model.

| Feature                        | Mocha          | Wolverine             |
| ------------------------------ | -------------- | --------------------- |
| Publish and send               | Yes            | Yes                   |
| Request/reply                  | Yes            | Yes                   |
| Compile-time checked responses | Yes            | No                    |
| Batch consumers                | Yes            | Yes                   |
| Scheduled messages             | Yes            | Yes                   |
| Sagas                          | State machines | Classes by convention |
| In-process mediator            | Yes            | Yes                   |

## Reliability

Reliable messaging is a priority for both frameworks. Retries, redelivery, transactional outboxes, inboxes, circuit breakers, and concurrency controls are supported on both sides.

| Feature                                | Mocha | Wolverine |
| -------------------------------------- | ----- | --------- |
| Retry and redelivery policies          | Yes   | Yes       |
| Circuit breaker and concurrency limits | Yes   | Yes       |
| Transactional outbox and inbox         | Yes   | Yes       |

The distinction here is less about available capabilities and more about how they're configured and integrated into your application.

## Storage

The following databases can be used to persist outbox and inbox messages, scheduled messages, and saga state. Wolverine currently supports more database engines, while Mocha's persistence integration is focused on PostgreSQL.

| Feature    | Mocha                                                                                                  | Wolverine |
| ---------- | ------------------------------------------------------------------------------------------------------ | --------- |
| PostgreSQL | Yes                                                                                                    | Yes       |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |

## Development Experience

This is where the frameworks differ most noticeably. Mocha emphasizes compile-time contracts, build-time source generation, and a workflow that doesn't require runtime compilation. Wolverine emphasizes conventions and flexibility, with the option to pre-generate its execution code for production.

| Feature           | Mocha                                                                                                  | Wolverine                             |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------- |
| License           | MIT                                                                                                    | MIT                                   |
| Handler discovery | Interfaces, checked at build time                                                                      | Naming conventions or markers         |
| Handler code      | Generated at build time                                                                                | Compiled at runtime, or pre-generated |
| Native AOT        | Yes                                                                                                    | With pre-generated code               |
| OpenTelemetry     | Yes                                                                                                    | Yes                                   |
| Test harness      | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                   |
| Target frameworks | .NET 8 to 11                                                                                           | .NET 9 and 10                         |

# Which One Should You Choose?

Wolverine and Mocha solve many of the same problems, but they make different choices about how developers should interact with a messaging framework.

**Wolverine is a strong choice when you value convention-based development and a broad integration ecosystem.**

Its handler model requires little ceremony, it supports a wide range of transports and databases, and its durable messaging and workflow capabilities are well established. Its runtime code-generation model also offers flexibility during development, with the option to switch to pre-generated code for production.

**Mocha is designed for teams that prefer explicit contracts and want the compiler involved in more of their messaging infrastructure.**

Handlers declare their message types through interfaces. Requests declare their response types. Sagas describe their states and transitions explicitly. And the code needed to execute handlers is generated during the normal build. That means fewer opportunities for convention-related mistakes, no runtime compiler dependency, and no separate code-generation workflow for production or Native AOT. There are trade-offs, of course.

Mocha's explicit interfaces and state machines introduce more structure, which won't appeal to everyone. And Wolverine currently offers broader transport and storage support. But if you're building a .NET application around RabbitMQ, Azure Service Bus, or PostgreSQL, and you want messaging contracts that the compiler can verify, Mocha offers an interesting alternative. Ultimately, the choice comes down to where you want complexity to live.

Wolverine gives you flexibility through conventions and runtime discovery. Mocha moves more of that responsibility into explicit contracts and build-time generation.

**With Mocha, the goal is simple: define your messages and workflows in C#, let the compiler check what it can, and ship an application that's already wired up when it starts.**

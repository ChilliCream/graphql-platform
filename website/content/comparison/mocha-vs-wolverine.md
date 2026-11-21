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

Mocha and Wolverine are both MIT licensed, and both generate the code that runs your handlers. Mocha does it when you compile: handlers say what they handle through their interface, mistakes are build errors, and there is no runtime compiler, no generated code to check in, and no extra step for Native AOT. Requests are typed end to end, and sagas are explicit state machines.

# Handlers say what they handle

A Mocha handler implements an interface that names its message and what kind of message it is: `IEventHandler<T>` for events, `IEventRequestHandler<T>` for commands, and `IEventRequestHandler<TRequest, TResponse>` for requests that get a response. The compiler checks the signature, and the source generator registers the handler.

[Wolverine](https://wolverinefx.net/) finds handlers by naming conventions: public classes whose name ends in `Handler` or `Consumer`, with public methods named `Handle`, `Consume`, or one of their variants, whose first parameter is the message. A marker interface or attribute can stand in for the class name. Nothing checks the conventions when you compile, so a class or method that misses them is not a handler, and you find out when the message is not handled.

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

# Generated when you compile, not when you start

The `Mocha.Analyzers` source generator writes the registration method, here `AddOrdering()` for `MyApp.Ordering`, as part of every build. Nothing is compiled at runtime, there are no generated files to keep in sync, and two handlers for the same command or request are a build error. For a service published with Native AOT, it also writes the serialization code from a `JsonSerializerContext`.

Wolverine generates wrapper code for each handler as well, but by default it compiles that code with Roslyn at runtime, the first time a handler is needed. Since Wolverine 6.0, this needs the `WolverineFx.RuntimeCompilation` package, which brings in the Roslyn compiler. Its [documentation](https://wolverinefx.net/guide/codegen) warns about the memory and cold start cost and recommends running `codegen write` at build time, checking in the generated files, and loading them with `TypeLoadMode.Static` in production. When a handler changes, the files have to be generated again, by hand or in CI. Native AOT works only in this static mode.

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

# Requests are typed end to end

A Mocha request declares the response it expects, its handler returns that response, and the caller awaits it. The compiler checks that the handler and the caller agree on the response type.

<MochaVisual
  name="request-reply"
  caption="The ordering service sends a typed request and awaits the response from the catalog service."
/>

Wolverine sends a request with `InvokeAsync<T>`, and the caller names the response type at the call site. Nothing ties the request to its response at compile time. Over a broker, the call waits 5 seconds by default, and the Wolverine team recommends using remote invocations cautiously.

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

# An outbox in your DbContext

Mocha writes outgoing messages to the database in the same transaction as your changes and delivers them in the background, and its inbox drops messages a handler has already processed. Both live in your Entity Framework Core `DbContext` and take one registration.

Wolverine adds its message storage to your `DbContext` through its own integration, enables transactions for handlers, and enrolls endpoints into durable messaging with separate policies for local queues, sending endpoints, and listeners.

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

# Sagas as state machines

A Mocha saga is a state machine. States are strings, events are type arguments, and each transition reads top to bottom. The saga is correlated by its ID: the state factory below sets it to the order ID, and later events return it from `ICorrelatable.CorrelationId`.

A Wolverine saga is a class whose methods are found by convention: a `Start` method creates it, `Handle` methods apply later messages, and `MarkCompleted()` ends it. Its states are whatever its properties say, and a message finds its saga through a member named by convention, such as `OrderSagaId`, `SagaId`, or `Id`, or one marked `[SagaIdentity]`.

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

Mocha keeps saga state in PostgreSQL through Entity Framework Core, or in memory for tests. Its sagas can also start from a request and answer it when they finish, send commands, publish events, and time out.

# Moving from Wolverine

Mocha and Wolverine use different message formats, so plan the move by message flow: move the services that exchange a message together.

1. Add the `Mocha`, `Mocha.Analyzers`, and transport packages.
2. Replace `UseWolverine` with `AddMessageBus()`, the generated registration method, and the transport.
3. Implement the handler interfaces on your handler classes, and rename `Handle` to `HandleAsync`.
4. Move inline `InvokeAsync` calls to the Mocha mediator, and replace remote `InvokeAsync<T>` calls with `RequestAsync`.
5. Move error policies, the outbox, and sagas to their Mocha counterparts.

The [Wolverine migration guide](../docs/mocha/migration/coming-from-wolverine.md) walks through every step with code.

# Mocha and Wolverine at a glance

<!-- The FeatureStatus links are placeholders until the issues and pull requests exist. -->

## Transports

|                   | Mocha                                                                                                  | Wolverine |
| ----------------- | ------------------------------------------------------------------------------------------------------ | --------- |
| RabbitMQ          | Yes                                                                                                    | Yes       |
| Azure Service Bus | Yes                                                                                                    | Yes       |
| PostgreSQL        | Preview                                                                                                | Yes       |
| SQL Server        | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| Amazon SQS/SNS    | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| Kafka             | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| In-memory         | Yes                                                                                                    | Yes       |

## Messaging

|                                | Mocha          | Wolverine             |
| ------------------------------ | -------------- | --------------------- |
| Publish and send               | Yes            | Yes                   |
| Request/reply                  | Yes            | Yes                   |
| Compile-time checked responses | Yes            | No                    |
| Batch consumers                | Yes            | Yes                   |
| Scheduled messages             | Yes            | Yes                   |
| Sagas                          | State machines | Classes by convention |
| In-process mediator            | Yes            | Yes                   |

## Reliability

|                                        | Mocha | Wolverine |
| -------------------------------------- | ----- | --------- |
| Retry and redelivery policies          | Yes   | Yes       |
| Circuit breaker and concurrency limits | Yes   | Yes       |
| Transactional outbox and inbox         | Yes   | Yes       |

## Storage

Databases for the outbox, inbox, scheduled messages, and saga state.

|            | Mocha                                                                                                  | Wolverine |
| ---------- | ------------------------------------------------------------------------------------------------------ | --------- |
| PostgreSQL | Yes                                                                                                    | Yes       |
| SQL Server | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |
| MySQL      | <FeatureStatus status="planned" href="https://github.com/ChilliCream/graphql-platform/issues/10425" /> | Yes       |

## Development

|                   | Mocha                                                                                                  | Wolverine                             |
| ----------------- | ------------------------------------------------------------------------------------------------------ | ------------------------------------- |
| License           | MIT                                                                                                    | MIT                                   |
| Handler discovery | Interfaces, checked at build time                                                                      | Naming conventions or markers         |
| Handler code      | Generated at build time                                                                                | Compiled at runtime, or pre-generated |
| Native AOT        | Yes                                                                                                    | With pre-generated code               |
| OpenTelemetry     | Yes                                                                                                    | Yes                                   |
| Test harness      | <FeatureStatus status="in-review" href="https://github.com/ChilliCream/graphql-platform/pull/10535" /> | Yes                                   |
| Target frameworks | .NET 8 to 11                                                                                           | .NET 9 and 10                         |

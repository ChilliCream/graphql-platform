---
title: "Mocha vs. Mediator"
metaTitle: "Mocha vs Mediator.SourceGenerator: Source-Generated .NET Mediators"
description: "Compare the Mocha mediator with Mediator, the source-generated mediator for .NET: build-time checks, middleware, handler lifetimes, and messaging between services."
featuredImage: "mocha-vs-mediator.png"
date: "2026-10-29"
tags: ["mocha", "comparison", "mediator"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "Mediator"
    url: "https://github.com/martinothamar/Mediator"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Does Mocha generate the mediator at build time like Mediator?"
    answer: "Yes. The Mocha.Analyzers source generator registers your handlers when you compile and builds each pipeline from generic code, so dispatch needs no reflection and the mediator packages are compatible with Native AOT."
  - question: "Can I keep my ICommand and IQuery types when I move from Mediator to Mocha?"
    answer: "Yes. Mocha uses the same names: ICommand, ICommand<TResponse>, IQuery<TResponse>, INotification, and handler interfaces of the same names. You change the namespace, rename Handle to HandleAsync, return ValueTask instead of ValueTask<Unit> from commands without a response, and dispatch with SendAsync, QueryAsync, and PublishAsync."
  - question: "Does Mocha middleware run for notifications?"
    answer: "Yes. Mocha middleware wraps commands, queries, and notifications, and each notification handler gets its own pipeline. A middleware can leave out a kind of message when the pipeline is built, for example to open transactions for commands only."
  - question: "Can the Mocha mediator send messages to other services?"
    answer: "The mediator dispatches inside a service. For messages between services, add the Mocha message bus: its handlers are registered by the same source generator and it adds retries, a transactional outbox, sagas, and request/reply."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license. Start with the mediator docs, or move from Mediator step by step with the migration guide. Moving a large codebase?"
  primary:
    label: "Get started with the mediator"
    href: "/docs/mocha/mediator"
  secondary:
    label: "Migrate from Mediator"
    href: "/docs/mocha/migration/coming-from-mediator"
  contact: "Talk to us about your migration"
---

# Mocha vs. Mediator: Source-Generated Messaging for .NET

If you're building a .NET application with CQRS, there's a good chance you've come across [Mediator](https://github.com/martinothamar/Mediator). It's a lightweight, MIT-licensed library that uses source generators to dispatch commands, queries, and notifications without relying on reflection at runtime. [Mocha](../docs/mocha) takes a similar approach. It generates the code needed to connect messages to their handlers at build time, keeping dispatch fast and predictable.

But while both libraries share that foundation, they make different choices about what a mediator should do. Mediator focuses on in-process messaging. Mocha builds on that foundation with module-level registration, stricter compile-time checks, middleware for all message types, built-in transactions and tracing, and a message bus for communication between services. Let's look at how those differences play out in a real application.

## Source generation that works with your application structure

Both Mocha and Mediator use source generators to discover handlers and generate the code that dispatches messages to them. Neither needs reflection to find a handler when a message arrives. The difference becomes more interesting when your application grows beyond a single project. Imagine an e-commerce application split into three modules: Ordering, Billing, and Shipping. Each module contains its own commands, queries, and handlers.

**Mocha generates a registration method for each project.** The `Mocha.Analyzers` source generator discovers the handlers in an assembly and generates a method named after it. For example, a project named `MyApp.Ordering` gets an `AddOrdering()` method. Registering your modules becomes straightforward:

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering()
    .AddBilling();
```

```csharp filename="Mediator"
builder.Services.AddMediator(
    (MediatorOptions options) =>
    {
        options.Assemblies =
        [
            typeof(PlaceOrder),
            typeof(CreateInvoice),
        ];
    });
```

</CodeComparison>

Mediator takes a different approach. Its source generator is installed in the outermost executable project, where it scans the referenced assemblies and generates a mediator for the application. This works well, particularly for smaller applications. However, it means handler discovery is coordinated from the application entry point rather than generated independently for each module.

Mocha's approach gives each module its own generated registration method. Adding a new module is simply a matter of calling its registration method. There is another difference worth knowing: Mediator's generator reads its configuration at compile time, so configuration values must be compile-time constants.

### Catching handler mistakes before the application starts

Source generation also gives both libraries an opportunity to catch configuration mistakes during compilation. Suppose you accidentally implement two handlers for the same command. Which one should run? For commands and queries, there should be exactly one handler. Mocha enforces that rule at build time.

- **Duplicate handlers:** Mocha reports a build error (`MO0002`), preventing compilation.
- **Missing handlers:** Mocha reports a build warning (`MO0001`).
- **Sending an unhandled message:** Mocha warns at the call site (`MO0020`) when a command or query has no handler in the project.

Notifications are different because they are designed to support multiple handlers. Mediator also detects duplicate and missing handlers, but reports them as warnings (`MSG0001` and `MSG0005`). Unless your project treats warnings as errors, the build can still succeed. For larger codebases, where handlers are spread across many projects, catching an ambiguous handler registration before deployment is particularly useful.

## Familiar handlers, with sensible defaults

If you've used Mediator before, Mocha's handler model will feel familiar. Both libraries distinguish between commands, queries, and notifications. Both support `ValueTask`, and both let handlers receive dependencies through constructor injection. A command handler that creates an order looks almost identical in either library.

<CodeComparison>

```csharp filename="Mocha" {5,7}
public record PlaceOrder(Guid ProductId, int Quantity)
    : ICommand<Guid>;

public class PlaceOrderHandler(AppDbContext db)
    : ICommandHandler<PlaceOrder, Guid>
{
    public async ValueTask<Guid> HandleAsync(
        PlaceOrder command,
        CancellationToken ct)
    {
        var order = new Order(command.ProductId,
            command.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return order.Id;
    }
}
```

```csharp filename="Mediator" {5,7}
public record PlaceOrder(Guid ProductId, int Quantity)
    : ICommand<Guid>;

public class PlaceOrderHandler(AppDbContext db)
    : ICommandHandler<PlaceOrder, Guid>
{
    public async ValueTask<Guid> Handle(
        PlaceOrder command,
        CancellationToken ct)
    {
        var order = new Order(command.ProductId,
            command.Quantity);

        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);

        return order.Id;
    }
}

// Required for the scoped AppDbContext
options.ServiceLifetime = ServiceLifetime.Scoped;
```

</CodeComparison>

The main difference in the handler itself is the method name: Mocha uses `HandleAsync`, while Mediator uses `Handle`. More importantly, **Mocha registers handlers as scoped by default.** That's a natural fit for typical ASP.NET Core applications, where dependencies such as Entity Framework's `DbContext` are already scoped.

Mediator defaults to singleton lifetimes for its mediator and handlers. You can change this through `ServiceLifetime`, and Mediator's own Clean Architecture sample uses `Scoped`. Neither approach prevents you from building the same application, but Mocha's default requires less configuration when handlers work with request-scoped services.

### Commands, queries, and notifications stay distinct

The difference also shows up when sending messages. Mocha provides separate methods for the three message types: `SendAsync` for commands, `QueryAsync` for queries, and `PublishAsync` for notifications. Mediator uses `Send` for both commands and queries, and `Publish` for notifications.

<CodeComparison>

```csharp filename="Mocha" {1,4,7}
var orderId = await sender.SendAsync(
    new PlaceOrder(productId, 2), ct);

var order = await sender.QueryAsync(
    new GetOrder(orderId), ct);

await publisher.PublishAsync(
    new OrderPlaced(orderId), ct);
```

```csharp filename="Mediator" {1,4,7}
var orderId = await mediator.Send(
    new PlaceOrder(productId, 2), ct);

var order = await mediator.Send(
    new GetOrder(orderId), ct);

await mediator.Publish(
    new OrderPlaced(orderId), ct);
```

</CodeComparison>

Both APIs are simple, but Mocha makes the intention of each operation explicit at the call site. When reading unfamiliar code, `QueryAsync` immediately tells you that the operation retrieves information, while `SendAsync` communicates that you're executing a command. It's a small distinction, but one that can make a CQRS codebase easier to navigate.

## Middleware that works across all message types

Most applications eventually need more than handlers. You might want to validate commands before executing them, measure how long queries take, wrap database operations in transactions, or record telemetry whenever a message is processed. This is where middleware comes in. Both libraries support adding behavior around handlers, but their pipelines work differently.

**Mocha middleware can wrap commands, queries, and notifications.** Each notification handler gets its own middleware pipeline, so the same cross-cutting concerns can apply consistently across all three message types.

<MochaVisual
  name="mediator"
  caption="A command passes through telemetry, transaction, and validation middleware before reaching its handler. The result then travels back through the pipeline."
/>

Mediator uses pipeline behaviors for commands, queries, and requests. Notifications are dispatched outside that pipeline. That means a timing behavior registered in Mediator won't automatically measure notification handlers. In Mocha, the same middleware can measure both.

### A timing middleware example

Let's say we want to log how long each message takes to process. Here's what that looks like in both libraries.

<CodeComparison>

```csharp filename="Mocha"
public sealed class TimingMiddleware
{
    public async ValueTask InvokeAsync(
        IMediatorContext context,
        MediatorDelegate next)
    {
        var log = context.Services.GetRequiredService<
            ILogger<TimingMiddleware>>();

        var started = Stopwatch.GetTimestamp();

        await next(context);

        log.LogInformation("{Message} took {Elapsed}",
            context.MessageType.Name,
            Stopwatch.GetElapsedTime(started));
    }

    public static MediatorMiddlewareConfiguration
        Create() => new(Build, "Timing");

    // Runs once per message type, at startup
    private static MediatorDelegate Build(
        MediatorMiddlewareFactoryContext factory,
        MediatorDelegate next)
    {
        var middleware = new TimingMiddleware();

        return ctx =>
            middleware.InvokeAsync(ctx, next);
    }
}

// Registration
.Use(TimingMiddleware.Create());
```

```csharp filename="Mediator"
public sealed class TimingBehavior<TMessage, TResponse>(
    ILogger<TimingBehavior<TMessage, TResponse>> log)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IMessage
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse>
            next,
        CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();

        var response = await next(message, ct);

        log.LogInformation("{Message} took {Elapsed}",
            typeof(TMessage).Name,
            Stopwatch.GetElapsedTime(started));

        return response;
    }
}

// Registration
options.PipelineBehaviors =
    [typeof(TimingBehavior<,>)];
```

</CodeComparison>

Mediator's generic pipeline behaviors are concise and will be familiar to developers who have worked with similar .NET libraries. Mocha takes a different approach by giving middleware access to a shared `IMediatorContext` and a delegate representing the next step in the pipeline. One particularly useful detail is **when Mocha builds its middleware pipeline**.

A middleware factory runs once per message type when the pipeline is constructed. It decides which delegate should execute whenever that message is dispatched. For example, suppose you want transaction middleware to run for commands but never for queries. You can check `factory.IsQuery()` while building the pipeline and simply return `next` for queries.

That means queries won't execute the transaction middleware at all. There's no need to check the message type on every dispatch. This also makes it possible to compose different middleware pipelines for different kinds of messages without scattering runtime conditionals throughout the application. Mediator offers a different set of tools for organizing cross-cutting behavior, including generic constraints, pre-processors, post-processors, and exception handlers.

Mocha handles those concerns through its middleware model instead.

## Transactions and tracing without writing the plumbing

Middleware is flexible, but there's a practical question: how much of it should you have to write yourself? Database transactions and distributed tracing are two common requirements in production applications. Mocha includes both out of the box. For Entity Framework Core, `UseEntityFrameworkTransactions` wraps command handlers in a database transaction. Queries and notifications are excluded from that middleware.

For observability, `AddInstrumentation` creates OpenTelemetry spans for messages using OpenTelemetry messaging conventions. You can enable both during registration:

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering()
    .UseEntityFrameworkTransactions<AppDbContext>()
    .AddInstrumentation();
```

```csharp filename="Mediator"
public sealed class TransactionBehavior<TMessage,
    TResponse>(AppDbContext db)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IBaseCommand
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse>
            next,
        CancellationToken ct)
    {
        await using var tx = await db.Database
            .BeginTransactionAsync(ct);

        var response = await next(message, ct);

        await tx.CommitAsync(ct);

        return response;
    }
}
```

</CodeComparison>

With Mediator, you can implement the same transaction behavior yourself, as shown above. That's not necessarily a disadvantage if your application has specific transaction requirements. Writing your own behavior gives you control over exactly when transactions begin, commit, or roll back. But if you want the common behavior, Mocha already provides it. The same applies to tracing. Mediator is introducing tracing support in its 3.1 release, which is currently a prerelease.

Mocha's built-in instrumentation means you can connect mediator activity to the rest of your OpenTelemetry setup without implementing your own tracing behavior. For applications that already use Entity Framework Core and OpenTelemetry, these integrations remove a fair amount of repetitive infrastructure code.

## From in-process messages to communication between services

So far, we've looked at two libraries solving roughly the same problem: getting a message to its handler inside a .NET application. But what happens when that message needs to leave the application? Imagine our e-commerce system has grown. Ordering is now its own service. Billing handles payments and invoices, Shipping manages fulfillment, and Search maintains a searchable view of the product catalog. When a customer places an order, several things need to happen:

- Ordering saves the order and returns a response to the customer.
- Billing receives an `OrderPlaced` event and creates an invoice.
- Shipping receives the same event and starts fulfillment.
- Other services can react independently without being called directly by Ordering.

At this point, an in-process mediator isn't enough. We need messaging between services.

<MochaVisual
  name="why-messaging"
  caption="Ordering responds immediately while Billing, Shipping, and Search process their messages independently in the background."
/>

This is where Mocha and Mediator take fundamentally different paths. **Mediator is intentionally an in-process messaging library.** It doesn't provide a distributed message bus, and its README points developers toward other libraries when communication between services is required. Mocha includes a message bus as part of the same framework.

That means you can use Mocha for commands and queries inside a service, then use its messaging infrastructure to communicate with other services.

### Publishing an event from Ordering

Suppose Ordering needs to publish an event whenever an order is created. With Mocha, you can configure the message bus with RabbitMQ and publish the event:

<CodeComparison>

```csharp filename="Mocha: ordering service"
builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();

// Publish to every service that handles it
await bus.PublishAsync(
    new OrderPlaced(orderId), ct);
```

```csharp filename="Mocha: billing service"
builder.Services
    .AddMessageBus()
    .AddBilling()
    .AddRabbitMQ();

public class OrderPlacedHandler(
    IInvoiceService invoices)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => await invoices.CreateAsync(
            message.OrderId, ct);
}
```

</CodeComparison>

Notice how similar the event handler looks to the command handlers we wrote earlier. It implements `IEventHandler<OrderPlaced>`, receives its dependencies through constructor injection, and handles the message through `HandleAsync`. The source generator discovers and registers these handlers just as it does mediator handlers. That's an important part of Mocha's design: **the programming model stays familiar as you move from in-process dispatch to distributed messaging.**

Of course, distributed messaging introduces problems that an in-process mediator doesn't need to solve. Messages can fail during delivery. Services can go offline. Events may need to be retried, and database changes sometimes need to be coordinated with published messages. Mocha's message bus provides infrastructure for those scenarios, including retries and redelivery, a transactional outbox, sagas, and request/reply messaging.

You don't need all of those features to build a simple application. But if your system eventually grows into multiple services, they're available without introducing a completely separate messaging framework and handler model. That doesn't make Mediator the wrong choice for in-process messaging. Its narrower scope is part of its appeal. The difference is that Mocha is designed to cover both sides of the boundary.

## Migrating from Mediator to Mocha

If you're already using Mediator, switching to Mocha doesn't mean rewriting your application architecture. The two libraries share many of the same concepts and interface names, including `ICommand`, `IQuery`, and `INotification`. Most handlers can be migrated with relatively small changes. The main steps are:

1. **Update the packages.** Replace `Mediator.SourceGenerator` and `Mediator.Abstractions` with `Mocha.Mediator` and `Mocha.Analyzers`. Add the analyzer to every project containing handlers.
2. **Update namespaces and message types.** Replace `using Mediator;` with `using Mocha.Mediator;` and convert any `IRequest` messages into commands or queries.
3. **Update handler methods.** Rename `Handle` to `HandleAsync`. Commands without a response should return `ValueTask` rather than `ValueTask<Unit>`.
4. **Update dispatch calls.** Use `SendAsync` for commands, `QueryAsync` for queries, and `PublishAsync` for notifications.
5. **Update registration and pipelines.** Replace `AddMediator(options => ...)` with `AddMediator()` and the generated module registration methods. Convert existing pipeline behaviors into Mocha middleware.

For straightforward handlers, the changes are mostly mechanical. The part that deserves more attention is middleware. Since Mocha uses a different pipeline model, existing behaviors need to be adapted rather than simply renamed. The [Mediator migration guide](../docs/mocha/migration/coming-from-mediator.md) walks through the process with code examples.

## Mocha vs. Mediator: Feature comparison

Both libraries cover the essentials of in-process messaging, but their differences become clearer when you compare registration, pipelines, and infrastructure.

### Messages

|                             | Mocha | Mediator |
| --------------------------- | ----- | -------- |
| Commands without a response | Yes   | Yes      |
| Commands with a response    | Yes   | Yes      |
| Queries as their own type   | Yes   | Yes      |
| Notifications               | Yes   | Yes      |

Both libraries provide the building blocks for CQRS. The main API difference is that Mocha uses distinct dispatch methods for commands and queries.

### Middleware and infrastructure

|                                  | Mocha                                 | Mediator                    |
| -------------------------------- | ------------------------------------- | --------------------------- |
| Cross-cutting behavior           | Middleware, filtered per message type | Behaviors, narrowed by type |
| Middleware for notifications     | Yes                                   | No                          |
| Pre- and post-processors         | As middleware                         | Yes                         |
| Exception handlers               | As middleware                         | Yes                         |
| Entity Framework transactions    | Built in                              | Write your own              |
| OpenTelemetry tracing            | Built in                              | In the 3.1 prerelease       |
| Concurrent notification handlers | Yes                                   | Yes                         |

Mocha provides a broader middleware pipeline and ready-made integrations for common infrastructure concerns. Mediator keeps its pipeline focused on requests, commands, and queries, with dedicated abstractions for processors and exception handling.

### Build time and runtime

|                            | Mocha                        | Mediator                         |
| -------------------------- | ---------------------------- | -------------------------------- |
| License                    | MIT                          | MIT                              |
| Handler registration       | Source generator per project | Source generator per application |
| Two handlers for a message | Build error                  | Build warning                    |
| Default handler lifetime   | Scoped                       | Singleton                        |
| Native AOT                 | Yes                          | Yes                              |
| Target frameworks          | .NET 8 to 11                 | .NET 8, .NET Standard 2.0        |
| Messaging between services | Yes                          | No                               |

Both libraries are MIT-licensed and available for commercial use without licensing fees. They also both support Native AOT and generate dispatch code at compile time, so reflection-free message dispatch isn't a reason to choose one over the other. The more meaningful differences are how they integrate into your application and how far their responsibilities extend.

## Which one should you choose?

Mediator and Mocha share a strong starting point: source-generated message dispatch, familiar CQRS abstractions, and no runtime reflection required to find handlers. If you want a focused library for commands, queries, and notifications inside a single application, **Mediator is a solid choice**. Its API is straightforward, its pipeline behaviors are flexible, and it deliberately leaves concerns such as distributed messaging to other tools.

Mocha is worth considering when you want more of the surrounding infrastructure to work together. Its per-project source generation fits naturally into modular applications. Its analyzers catch duplicate handlers at build time. Scoped handlers work with Entity Framework Core without additional lifetime configuration, and its middleware covers notifications as well as commands and queries.

Built-in transaction handling and OpenTelemetry instrumentation mean there's less infrastructure to write before an application is ready for production. And if the application eventually needs to communicate across service boundaries, Mocha's message bus lets you keep the same general handler model while adding distributed messaging capabilities.

**The biggest difference isn't how the two libraries dispatch a command. It's what happens as your application grows around that command.**

Mediator gives you a focused in-process messaging library. Mocha gives you that foundation alongside middleware, observability, transactions, and distributed messaging in one framework. Both are free under the MIT license, so the choice comes down to the architecture you're building and how much infrastructure you want your messaging library to provide.

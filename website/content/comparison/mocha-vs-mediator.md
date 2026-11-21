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

Mocha and Mediator both write your mediator at build time, so neither needs reflection to dispatch a message. Mocha goes further where a codebase grows: a registration method for every project, two handlers for one message as a build error, middleware that wraps notifications too, ready-made middleware for transactions and tracing, and a message bus on the same handlers when messages leave the process. Free for every company under the MIT license.

# Generated per project, checked at build time

The `Mocha.Analyzers` source generator runs in every project that has handlers and writes a registration method named after the assembly, here `AddOrdering()` for `MyApp.Ordering`. A modular application registers each module on its own: `AddOrdering()`, `AddBilling()`, `AddShipping()`.

[Mediator](https://github.com/martinothamar/Mediator), an MIT-licensed library by Martin Othamar, generates one mediator for the application. Its [README](https://github.com/martinothamar/Mediator/blob/main/README.md) tells you to install the source generator only in the outermost executable project, which then scans the assemblies it references. Its options are read by the generator, so they have to be compile-time constants.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering()  // written by the
    .AddBilling();  // source generator
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

Mistakes surface when you build. In Mocha, a message type with two handlers is a build error (`MO0002`), a message type without a handler is a build warning (`MO0001`), and sending a command or query that has no handler in the project is a warning at the call site (`MO0020`). Mediator reports both a duplicate and a missing handler as warnings (`MSG0001` and `MSG0005`), so the build passes unless you treat warnings as errors.

# Handlers are scoped, like the rest of your app

Mocha registers handlers as scoped by default, so a handler takes a `DbContext` the same way a controller does. Mediator registers its mediator and handlers as singletons unless you change `ServiceLifetime`; its Clean Architecture sample sets it to `Scoped`.

The handlers themselves look alike. Both libraries use `ICommand`, `IQuery`, and `INotification`, and both return a `ValueTask`.

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

// Needed for the scoped AppDbContext
options.ServiceLifetime = ServiceLifetime.Scoped;
```

</CodeComparison>

The call sites show what a message is. Mocha sends commands, queries queries, and publishes notifications. Mediator sends commands and queries alike.

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

# Middleware for every message, notifications included

A Mocha middleware wraps commands, queries, and notifications, and each notification handler gets its own pipeline. A middleware factory runs once per message type when the pipeline is built and returns the delegate that runs on every dispatch, so it can leave a kind of message out entirely: return `next` when `factory.IsQuery()` is true, and queries run without the middleware and without a type check on every call.

<MochaVisual
  name="mediator"
  caption="A command passes the telemetry, transaction, and validation middleware on its way to the handler, and the result returns through them."
/>

Mediator's pipeline behaviors wrap requests, commands, and queries. Notifications do not pass through them. A behavior is narrowed with generic constraints, and pre-processors, post-processors, and exception handlers are base classes on top of it.

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

# Transactions and tracing come with it

Mocha ships the middleware most applications write first. `UseEntityFrameworkTransactions` wraps every command handler in a database transaction and leaves queries and notifications out of it, and `AddInstrumentation` creates an OpenTelemetry span for every message, following the OpenTelemetry messaging conventions.

With Mediator, a transaction is a behavior you write and register yourself. Tracing is part of the 3.1 release, which is still a prerelease.

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

# Ready for messages that leave the process

When an order placed in one service has to reach the billing and shipping services, Mediator cannot help: it dispatches in memory only, and its README points to other libraries for messaging between services.

With Mocha, the message bus is part of the same framework. Event handlers look like notification handlers, the source generator registers them the same way, and the bus adds what messaging between services needs: retries and redelivery, a transactional outbox, sagas, and request/reply.

<MochaVisual
  name="why-messaging"
  caption="The ordering service answers right away while billing, shipping, and search pick up their messages in the background."
/>

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

# Moving from Mediator

Mocha uses the same message and handler names as Mediator, so most of a migration is renaming:

1. Replace the `Mediator.SourceGenerator` and `Mediator.Abstractions` packages with `Mocha.Mediator` and `Mocha.Analyzers`, and add `Mocha.Analyzers` to every project with handlers.
2. Change `using Mediator;` to `using Mocha.Mediator;`, and turn `IRequest` messages into commands or queries.
3. Rename `Handle` to `HandleAsync` in your handlers, and return `ValueTask` instead of `ValueTask<Unit>` from commands without a response.
4. Dispatch queries with `QueryAsync`, commands with `SendAsync`, and notifications with `PublishAsync`.
5. Replace `AddMediator(options => ...)` with `AddMediator()` and the generated registration methods, and rewrite behaviors as middleware.

The [Mediator migration guide](../docs/mocha/migration/coming-from-mediator.md) walks through every step with code.

# Mocha and Mediator at a glance

## Messages

|                             | Mocha | Mediator |
| --------------------------- | ----- | -------- |
| Commands without a response | Yes   | Yes      |
| Commands with a response    | Yes   | Yes      |
| Queries as their own type   | Yes   | Yes      |
| Notifications               | Yes   | Yes      |

## Pipeline

|                                  | Mocha                                 | Mediator                    |
| -------------------------------- | ------------------------------------- | --------------------------- |
| Cross-cutting behavior           | Middleware, filtered per message type | Behaviors, narrowed by type |
| Middleware for notifications     | Yes                                   | No                          |
| Pre- and post-processors         | As middleware                         | Yes                         |
| Exception handlers               | As middleware                         | Yes                         |
| Entity Framework transactions    | Yes                                   | Write your own              |
| OpenTelemetry tracing            | Yes                                   | In the 3.1 prerelease       |
| Concurrent notification handlers | Yes                                   | Yes                         |

## Build and runtime

|                            | Mocha                        | Mediator                         |
| -------------------------- | ---------------------------- | -------------------------------- |
| License                    | MIT                          | MIT                              |
| Handler registration       | Source generator per project | Source generator per application |
| Two handlers for a message | Build error                  | Build warning                    |
| Default handler lifetime   | Scoped                       | Singleton                        |
| Native AOT                 | Yes                          | Yes                              |
| Target frameworks          | .NET 8 to 11                 | .NET 8, .NET Standard 2.0        |
| Messaging between services | Yes                          | No                               |

---
title: "Mocha vs. MediatR"
metaTitle: "Mocha vs MediatR: An MIT-Licensed MediatR Alternative"
description: "Compare the Mocha mediator with MediatR for commands, queries, and notifications in .NET: licensing, source-generated dispatch, pipelines, and migration."
featuredImage: "mocha-vs-mediatr.png"
date: "2026-10-28"
tags: ["mocha", "comparison", "mediatr"]
author: Pascal Senn
authorUrl: https://github.com/pascalsenn
authorImageUrl: https://avatars.githubusercontent.com/u/14233220?v=4
about:
  - name: "Mocha"
    url: "/products/mocha"
  - name: "MediatR"
    url: "https://mediatr.io/"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Is MediatR still free?"
    answer: "Versions before 13.0 are licensed under Apache 2.0 and no longer get updates. From 13.0, MediatR is free under the copyleft RPL-1.5, or under the Community edition of its commercial license for companies with less than $5 million in annual revenue that meet its other conditions. Other companies pay $499 to $3,999 per year."
  - question: "Does Mocha use reflection to dispatch messages?"
    answer: "No. A source generator registers your handlers at build time and builds each pipeline from generic code, so dispatch needs no reflection and the mediator packages are compatible with Native AOT."
  - question: "Can MediatR and Mocha run in the same application?"
    answer: "Yes. They register different services, so you can move handlers over one at a time. Files that use both need a namespace alias, because both libraries define ISender, IPublisher, IMediator, and INotification."
  - question: "Do I need the message bus to use the Mocha mediator?"
    answer: "No. The mediator ships in its own packages, Mocha.Mediator and Mocha.Analyzers. Add the message bus later if your messages need to reach other services."
cta:
  title: "Try Mocha in your project."
  text: "Mocha is free for every company under the MIT license. Start with the mediator docs, or move from MediatR step by step with the migration guide. Moving a large codebase?"
  primary:
    label: "Get started with the mediator"
    href: "/docs/mocha/mediator"
  secondary:
    label: "Migrate from MediatR"
    href: "/docs/mocha/migration/coming-from-mediatr"
  contact: "Talk to us about your migration"
---

Jimmy Bogard's [MediatR](https://github.com/LuckyPennySoftware/MediatR) has been a familiar part of the .NET ecosystem for years. If you've worked with CQRS in ASP.NET Core, chances are you've used it to dispatch commands, execute queries, or publish notifications. And for good reason. MediatR gives applications a straightforward way to separate the code that handles a request from the code that sends it. But there's room to make that experience even better.

What if commands and queries were distinct types? What if the compiler could find missing handlers before you ever started your application? What if you could use the same programming model for in-process messages and communication between services? That's what we set out to build with **Mocha**.

Mocha takes the familiar mediator pattern and builds on it with source-generated handler registration, a flexible middleware pipeline, and an integrated message bus. It keeps the parts developers like about MediatR while removing some of the ceremony around them. And because Mocha is MIT licensed, it's free for every company, without license keys, revenue limits, or commercial editions. Let's look at how the two compare.

# Free for every company. No strings attached

Let's start with something that's become increasingly relevant for MediatR users: licensing. In July 2025, MediatR [changed its licensing model with version 13.0](https://github.com/LuckyPennySoftware/MediatR/releases/tag/v13.0.0). Until then, MediatR was available under Apache 2.0. Those older versions remain available under that license, but Lucky Penny Software no longer provides support or security updates for them.

Starting with version 13, MediatR uses a [dual-license model](https://github.com/LuckyPennySoftware/MediatR/blob/main/LICENSE.md). You can either comply with the Reciprocal Public License 1.5, which comes with copyleft obligations, or use a commercial license. There is a free Community edition, but eligibility depends on your organization. Among [other requirements](https://luckypennysoftware.com/license), your company must:

- Generate less than $5 million in annual revenue.
- Have raised no more than $10 million in outside capital.
- Not be a government organization.

If your company doesn't qualify, [commercial licenses](https://mediatr.io/) start at $499 per year for up to 10 developers, increase to $1,499 for up to 50 developers, and reach $3,999 for an unlimited number. Production deployments are also expected to [configure a license key](https://github.com/LuckyPennySoftware/MediatR/wiki/License-Key-Configuration).

**Mocha takes a different approach.**

Like [Hot Chocolate](https://chillicream.com/docs/hotchocolate), Mocha is MIT licensed. That means you can use it in personal projects, commercial products, internal enterprise applications, or services running at massive scale. There are no revenue thresholds to worry about, no developer seats to count, and no license keys to manage. Even the initial configuration reflects that simplicity.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering();
```

```csharp filename="MediatR 13 and later" {3,4}
builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey =
        builder.Configuration["MediatR:LicenseKey"];
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
});
```

</CodeComparison>

For us, the goal is simple: you should be able to choose a library because it's the right technical fit, without having to think about what happens when your company grows.

# Commands and queries should look like commands and queries

One of the things we like about MediatR is how easy it makes the mediator pattern. You define a request, write a handler, and send the request through `ISender`. That's it. However, MediatR treats commands and queries as the same thing: an `IRequest<TResponse>`. Whether you're placing an order, fetching a customer, or updating a product, the type system doesn't distinguish between reading and writing. That works, but we think the distinction is worth making explicit.

In Mocha, **commands and queries are separate concepts**. A command implements `ICommand` or `ICommand<TResponse>`. A query implements `IQuery<TResponse>`. This makes the intent of a message immediately visible. Here's what placing an order looks like in both libraries.

<CodeComparison>

```csharp filename="Mocha" {2,5,7}
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

```csharp filename="MediatR" {2,5,7}
public record PlaceOrder(Guid ProductId, int Quantity)
    : IRequest<Guid>;

public class PlaceOrderHandler(AppDbContext db)
    : IRequestHandler<PlaceOrder, Guid>
{
    public async Task<Guid> Handle(
        PlaceOrder request,
        CancellationToken ct)
    {
        var order = new Order(request.ProductId,
            request.Quantity);
        db.Orders.Add(order);
        await db.SaveChangesAsync(ct);
        return order.Id;
    }
}
```

</CodeComparison>

If you've used MediatR before, most of this should feel familiar. The handler receives a message, performs its work, and returns a result. Mocha uses `HandleAsync` and `ValueTask`, following the asynchronous conventions you'll find throughout the framework. The more interesting difference is in what the types communicate.

When you see `ICommand<Guid>`, you know you're looking at an operation that intends to change state. When you see `IQuery<Order>`, you know it intends to retrieve data. This isn't just useful when reading code. It also gives the framework information it can use to build different execution pipelines for commands and queries. For example, you might want every command to execute within a database transaction, while queries should skip that middleware entirely.

With separate message types, that distinction is built into the programming model.

## The difference carries through to the call site

In MediatR, you use `Send` for both commands and queries. Mocha gives each operation its own method.

<CodeComparison>

```csharp filename="Mocha" {1,4,7}
var orderId = await sender.SendAsync(
    new PlaceOrder(productId, 2), ct);

var order = await sender.QueryAsync(
    new GetOrder(orderId), ct);

await publisher.PublishAsync(
    new OrderPlaced(orderId), ct);
```

```csharp filename="MediatR" {1,4,7}
var orderId = await sender.Send(
    new PlaceOrder(productId, 2), ct);

var order = await sender.Send(
    new GetOrder(orderId), ct);

await publisher.Publish(
    new OrderPlaced(orderId), ct);
```

</CodeComparison>

It's a small difference, but one that becomes useful as an application grows. You can understand what a piece of code is doing without having to navigate to every message definition. `SendAsync` executes a command. `QueryAsync` executes a query. `PublishAsync` publishes a notification. No guessing required.

# What if the compiler could find your missing handlers?

Dependency injection is one of those things that usually works beautifully, right up until something isn't registered. In a traditional mediator setup, you might define a request, forget to register its handler, and only discover the problem when the application tries to execute it. MediatR handles registration through assembly scanning. When your application starts, it scans the configured assemblies for handlers and registers them with the dependency injection container.

When a request type is sent for the first time, MediatR creates its handler wrapper using reflection. Subsequent calls reuse that wrapper and resolve the handler and its pipeline behaviors through the service provider. It's a well-established approach, but it means some mistakes only become visible at runtime.

**Mocha moves handler discovery to build time.**

The `Mocha.Analyzers` package includes a source generator that finds your handlers when you compile the application. It then generates the dependency injection registration code for you. For an assembly named `MyApp.Ordering`, that means you get an `AddOrdering()` extension method.

<MochaVisual
  name="build-time"
  caption="Mocha discovers handlers and reports missing registrations during compilation. MediatR discovers handlers at startup, with missing handlers failing when a request is sent."
/>

Your application setup becomes:

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering(); // written by the source generator
```

```csharp filename="MediatR"
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
});
```

</CodeComparison>

There's no assembly scanning involved in Mocha's handler registration. But the biggest advantage isn't saving a little startup work. It's catching mistakes earlier.

## Missing handlers become compiler diagnostics

Imagine you've added a new `CancelOrder` command but haven't written its handler yet. With MediatR, that problem becomes visible when something tries to send `CancelOrder`. With Mocha, the analyzer can tell you about it when you build. Mocha currently reports several useful diagnostics:

- `MO0001`: A command or query doesn't have a handler.
- `MO0002`: A command or query has more than one handler.
- `MO0020`: You're sending a command or query without a handler in the project.

Missing handlers produce warnings, while duplicate handlers produce an error. Notifications are different, of course. They can intentionally have multiple handlers, so Mocha allows that. These diagnostics are particularly helpful during refactoring. If you rename a handler, move code around, or introduce a new message, the build can immediately point out registrations that need attention.

## No reflection in the dispatch path

The source generator also gives Mocha another advantage. Instead of discovering handlers dynamically, Mocha generates the code needed to connect messages to their handlers. Each message type gets a pipeline built from generic code, allowing dispatch without reflection. The mediator packages are also marked as compatible with Native AOT. This matters for applications where predictable startup behavior, trimming, and avoiding runtime reflection are important.

You don't have to use Native AOT to benefit from it, though. Even in a regular ASP.NET Core application, build-time registration removes work that would otherwise happen when the application starts.

# One middleware pipeline instead of several extension points

Most applications eventually need more than just handlers. You might want to validate commands before executing them, record how long queries take, open database transactions, or add telemetry around every operation. MediatR supports all of these scenarios. It provides pipeline behaviors, request pre-processors, request post-processors, and exception handlers. Each has its own interface and place in the request processing model. Mocha approaches this differently.

**Everything that happens around a handler is middleware.**

If you've worked with ASP.NET Core middleware, the idea should feel familiar. A middleware receives a context and a delegate representing the next step in the pipeline. It can execute code before the handler, invoke the next delegate, execute code afterward, or handle exceptions. That gives you a single model for validation, logging, transactions, telemetry, authorization, and other cross-cutting concerns.

<MochaVisual
  name="place-order"
  caption="A command travels through telemetry, transaction, and validation middleware before reaching its handler. The result then flows back through the same pipeline."
/>

## A practical example: measuring execution time

Let's say we want to log how long each message takes to execute. Both libraries make that possible, but the implementation looks a little different.

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

```csharp filename="MediatR"
public class TimingBehavior<TRequest, TResponse>(
    ILogger<TimingBehavior<TRequest, TResponse>> log)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var started = Stopwatch.GetTimestamp();
        var response = await next(ct);
        log.LogInformation("{Request} took {Elapsed}",
            typeof(TRequest).Name,
            Stopwatch.GetElapsedTime(started));
        return response;
    }
}

// Registration
cfg.AddOpenBehavior(typeof(TimingBehavior<,>));
```

</CodeComparison>

The MediatR version uses a generic pipeline behavior. Mocha uses middleware with a factory that constructs the delegate. That factory is an important part of the design.

## Build the pipeline once, not on every request

In Mocha, the middleware factory runs once per message type when its pipeline is constructed. At that point, the framework already knows which message the pipeline will handle. That allows middleware to make decisions before any requests are dispatched. Suppose you've written transaction middleware, but you only want it to run for commands. Queries shouldn't need a transaction, and ideally they shouldn't even have to check whether one is necessary.

With Mocha, the middleware factory can inspect the message type using `factory.IsQuery()`. If it's a query, the factory simply returns `next`. The transaction middleware is then completely absent from that query's execution pipeline. There is no transaction-related type check on every dispatch because the decision has already been made. You can also use `IsMessageAssignableTo<T>()` to apply middleware to specific message types or entire categories of messages.

This gives you a way to compose pipelines based on what each message actually needs. And because validation, exception handling, and pre- or post-processing all follow the same middleware model, you don't have to learn a different extension point for each concern.

# Notifications work the way you'd expect

Not everything needs to be a command or query. Sometimes you simply want to announce that something happened. An order was placed. A customer registered. A payment was completed. Both MediatR and Mocha support this through notifications. A notification implements `INotification` and can have any number of handlers. For example, publishing `OrderPlaced` might trigger a handler that sends an email and another that updates an internal projection.

By default, Mocha executes notification handlers sequentially. If you'd rather execute them concurrently, you can change that with a single option.

<CodeComparison>

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .ConfigureOptions(o =>
        o.NotificationPublishMode =
            NotificationPublishMode.Concurrent)
    .AddOrdering();
```

```csharp filename="MediatR"
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
    cfg.NotificationPublisher =
        new TaskWhenAllPublisher();
});
```

</CodeComparison>

If you're coming from MediatR, this part should be immediately familiar. And that's intentional. We don't think every concept needs to be reinvented. Where the existing programming model works well, Mocha keeps it recognizable. But notifications also lead us to a bigger question. What happens when the code that needs to react to an event lives in another service?

# Your application doesn't stop at the process boundary

Imagine you're building an ordering system. When a customer places an order, several things need to happen. The ordering service saves the order and returns a confirmation. The billing service creates an invoice. The shipping service prepares fulfillment. A search service might update its index. Those operations don't necessarily belong in the same application. And they certainly don't all need to happen before the customer gets a response.

This is where the limitations of an in-process mediator become apparent. MediatR is designed for communication within a single process. It doesn't provide a transport for sending messages to other services. That's not a flaw in MediatR. It's simply outside its scope. But it means that once your application grows beyond a single process, you'll need to introduce another messaging library.

And with that library often comes another set of abstractions, handler interfaces, registration conventions, and middleware concepts.

**Mocha is designed to cover both sides of that boundary.**

You can use its mediator for communication within an application and its message bus for communication between applications. Both are part of the same framework.

<MochaVisual
  name="why-messaging"
  caption="The ordering service can respond immediately while billing, shipping, and search process their messages independently in the background."
/>

## From an in-process notification to a distributed event

Let's take the `OrderPlaced` example. Within a single application, you might publish it as a notification. But once billing moves into its own service, the event needs to travel over a message transport. With Mocha, you can configure a message bus using RabbitMQ. In the ordering service, publishing an event looks like this:

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

Notice how similar the event handler looks to the command handler we wrote earlier. It implements a handler interface, receives a strongly typed message, and exposes a `HandleAsync` method. The source generator takes care of registering it. You don't have to abandon the programming model you've already learned just because the message is now crossing a process boundary.

## The things distributed messaging actually needs

Of course, sending a message over RabbitMQ is only part of the story. Once messages travel between services, new problems appear. What happens when a consumer fails? What if the message arrives while the database is unavailable? How do you reliably publish an event after committing a transaction? What if a business process spans multiple services? These are problems that an in-process mediator doesn't need to solve, but a message bus does.

Mocha's messaging capabilities include retries and redelivery, a transactional outbox, sagas for coordinating longer-running processes, and request/reply communication. That means you can start with a straightforward application using commands, queries, and notifications, then introduce asynchronous messaging when your architecture calls for it. You aren't forced to adopt distributed messaging on day one. But when you need it, the same framework is already there.

# Moving from MediatR doesn't mean starting over

If you already have an application built around MediatR, switching mediator libraries might sound like a lot of work. Fortunately, the underlying concepts are very similar. You're still defining messages, implementing handlers, and dispatching those messages through an abstraction. Most of the migration comes down to updating interfaces, method names, and registration. The typical steps are:

1. Install `Mocha.Mediator` and `Mocha.Analyzers`.
2. Replace `IRequest` with `ICommand` or `IQuery`, depending on what the message does.
3. Update handler interfaces to `ICommandHandler` or `IQueryHandler`.
4. Rename `Handle` to `HandleAsync` and change the return type to `ValueTask`.
5. Replace `AddMediatR` with `AddMediator()` and your generated assembly registration method.
6. Move existing pipeline behaviors into Mocha middleware.

You don't have to migrate everything at once, either.

**Mocha and MediatR can run side by side in the same application.**

That makes it possible to migrate one feature or assembly at a time. For example, you could move your ordering commands to Mocha while leaving the rest of your application on MediatR. Once you're comfortable with the new setup, you can continue moving handlers over. For a more detailed walkthrough, including code examples for each step, check out the [migration guide for MediatR users](../docs/mocha/migration/coming-from-mediatr.md).

<MigrationGuideCard href="/docs/mocha/migration/coming-from-mediatr" from="MediatR" />

# Feature comparison

Both libraries support the mediator pattern, but they make different trade-offs. MediatR is a focused, established library for in-process messaging. Mocha builds on that familiar programming model with explicit commands and queries, compile-time handler registration, middleware composition, and distributed messaging. Here's how their capabilities compare.

## Licensing

| Feature                   | Mocha       | MediatR                           |
| ------------------------- | ----------- | --------------------------------- |
| License                   | MIT         | RPL-1.5 or commercial, since 13.0 |
| Commercial license        | Not needed  | $499 to $3,999 per year           |
| Free commercial use       | Any company | Subject to Community eligibility  |
| License key in production | Not needed  | Logs a warning without one        |

## Commands, queries, and notifications

| Feature                     | Mocha | MediatR |
| --------------------------- | ----- | ------- |
| Commands without a response | Yes   | Yes     |
| Commands with a response    | Yes   | Yes     |
| Queries as their own type   | Yes   | No      |
| Notifications               | Yes   | Yes     |

## Pipeline and middleware

| Feature                          | Mocha                                 | MediatR                 |
| -------------------------------- | ------------------------------------- | ----------------------- |
| Cross-cutting behavior           | Middleware, filtered per message type | Typed generic behaviors |
| Pre- and post-processors         | As middleware                         | Yes                     |
| Exception handlers               | As middleware                         | Yes                     |
| Concurrent notification handlers | Yes                                   | Yes                     |

## Runtime and infrastructure

| Feature                    | Mocha                          | MediatR                                               |
| -------------------------- | ------------------------------ | ----------------------------------------------------- |
| Handler registration       | Source generator at build time | Assembly scanning at startup                          |
| Default handler lifetime   | Scoped                         | Transient                                             |
| Target frameworks          | .NET 8 to 11                   | .NET 8 to 10, .NET Standard 2.0, .NET Framework 4.6.2 |
| Messaging between services | Yes                            | No                                                    |

# A familiar pattern, with room to grow

MediatR has helped popularize a simple and useful way to structure .NET applications. We like that pattern, and Mocha isn't trying to replace it with something completely different. Instead, we've tried to make the everyday experience a little better.

Commands and queries express their intent directly. Source generators take care of registration and help catch mistakes while you're building. Middleware gives you one consistent way to handle cross-cutting concerns, without adding unnecessary work to every dispatch. And when your application grows beyond a single process, you can use the same framework to communicate between services.

You can start small, with a handful of commands and queries in an ASP.NET Core application. You can add notifications, validation, and transactions as needed. And if your architecture eventually calls for RabbitMQ, an outbox, or sagas, you don't have to introduce an entirely different programming model. All of this is available under the MIT license, for every company, without commercial restrictions or license keys.

**If you're already using MediatR, Mocha should feel familiar. The difference is what you get beyond the mediator.**

And if you're starting a new .NET application, we think it's worth giving Mocha a try.

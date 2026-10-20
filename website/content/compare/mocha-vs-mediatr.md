---
title: "Mocha vs MediatR"
metaTitle: "Mocha vs MediatR: An MIT-Licensed MediatR Alternative"
description: "Compare the Mocha mediator with MediatR for commands, queries, and notifications in .NET: licensing, source-generated dispatch, pipelines, and migration."
lead: "Mocha does what most teams use MediatR for, with less ceremony: commands and queries as separate types, handlers wired at build time instead of through reflection, one middleware model for every cross-cutting concern, and a message bus for when your messages leave the process. Free for every company under the MIT license, with no license key."
product: "mocha"
competitor: "MediatR"
lastChecked: "2026-10-20"
versions:
  product: "16.6"
  competitor: "14.2"
migrationGuide: "/docs/mocha/migration/coming-from-mediatr"
features:
  - title: "Licensing"
    rows:
      - label: "License"
        product: "MIT"
        competitor: "RPL-1.5 or commercial, since 13.0"
      - label: "Commercial license"
        product: "Not needed"
        competitor: "$499 to $3,999 per year"
      - label: "Free commercial use"
        product: "Any company"
        competitor: "Under $5M annual revenue"
      - label: "License key in production"
        product: "Not needed"
        competitor: "Logs a warning without one"
  - title: "Messages"
    rows:
      - label: "Commands without a response"
        product: true
        competitor: true
      - label: "Commands with a response"
        product: true
        competitor: true
      - label: "Queries as their own type"
        product: true
        competitor: false
      - label: "Notifications"
        product: true
        competitor: true
      - label: "Stream requests"
        product: false
        competitor: true
  - title: "Pipeline"
    rows:
      - label: "Cross-cutting behavior"
        product: "Middleware, filtered per message type"
        competitor: "Typed generic behaviors"
      - label: "Pre- and post-processors"
        product: "As middleware"
        competitor: true
      - label: "Exception handlers"
        product: "As middleware"
        competitor: true
      - label: "Concurrent notification handlers"
        product: true
        competitor: true
      - label: "Custom notification publishers"
        product: false
        competitor: true
      - label: "Polymorphic notification handlers"
        product: false
        competitor: true
  - title: "Runtime"
    rows:
      - label: "Handler registration"
        product: "Source generator at build time"
        competitor: "Assembly scanning at startup"
      - label: "Default handler lifetime"
        product: "Scoped"
        competitor: "Transient"
      - label: "Target frameworks"
        product: ".NET 8 to 11"
        competitor: ".NET 8 to 10, .NET Standard 2.0, .NET Framework 4.6.2"
      - label: "Messaging between services"
        product: true
        competitor: false
sources:
  - title: "MediatR license (LICENSE.md)"
    url: "https://github.com/LuckyPennySoftware/MediatR/blob/main/LICENSE.md"
  - title: "Lucky Penny Software license agreement"
    url: "https://luckypennysoftware.com/license"
  - title: "Lucky Penny Software licensing FAQ"
    url: "https://luckypennysoftware.com/faq"
  - title: "MediatR editions and pricing"
    url: "https://mediatr.io/"
  - title: "AutoMapper and MediatR going commercial, Jimmy Bogard, April 2, 2025"
    url: "https://www.jimmybogard.com/automapper-and-mediatr-going-commercial/"
  - title: "MediatR 13.0.0 release notes"
    url: "https://github.com/LuckyPennySoftware/MediatR/releases/tag/v13.0.0"
  - title: "MediatR 14.2.0 on NuGet"
    url: "https://www.nuget.org/packages/MediatR/14.2.0"
  - title: "MediatR license key configuration"
    url: "https://github.com/LuckyPennySoftware/MediatR/wiki/License-Key-Configuration"
  - title: "MediatR wiki: requests, notifications, and behaviors"
    url: "https://github.com/LuckyPennySoftware/MediatR/wiki"
  - title: "MediatR source: Mediator.cs at 14.2.0"
    url: "https://github.com/LuckyPennySoftware/MediatR/blob/v14.2.0/src/MediatR/Mediator.cs"
faq:
  - question: "Is Mocha free for commercial use?"
    answer: "Yes. Mocha is MIT licensed. Any company can use it in production for free, with no license key and no revenue limit."
  - question: "Is MediatR still free?"
    answer: "Versions before 13.0 are licensed under Apache 2.0 and no longer get updates. From 13.0, MediatR is free under the copyleft RPL-1.5, or under the Community edition of its commercial license for companies with less than $5 million in annual revenue that meet its other conditions. Other companies pay $499 to $3,999 per year."
  - question: "Can MediatR and Mocha run in the same application?"
    answer: "Yes. They register different services, so you can move handlers over one at a time. Files that use both need a namespace alias, because both libraries define ISender, IPublisher, IMediator, and INotification."
  - question: "Do I need the message bus to use the Mocha mediator?"
    answer: "No. The mediator ships in its own packages, Mocha.Mediator and Mocha.Analyzers. Add the message bus later if your messages need to reach other services."
  - question: "Does Mocha support .NET Framework?"
    answer: "No. Mocha targets .NET 8 and later."
gaps:
  - "Stream requests, like MediatR's IStreamRequest."
  - "Notification handlers that run for a base type or interface."
  - "Notification publishers beyond the sequential and concurrent modes."
  - ".NET Framework and .NET Standard. Mocha targets .NET 8 and later."
---

# Free for every company

Mocha is MIT licensed, like Hot Chocolate. Any company can use it in production for free. There is no paid edition, no license key, and no revenue limit.

MediatR changed its license with version 13.0 in July 2025. Versions up to 12.5 stay under the Apache 2.0 license, but Lucky Penny Software, the company behind MediatR, no longer provides support or security updates for them. Version 13 and later are dual licensed: you either accept the Reciprocal Public License 1.5, a copyleft license, or buy a commercial license. The commercial license is free as the Community edition if your company has less than $5 million in annual revenue, has raised no more than $10 million in outside capital, and is not a government body, among other conditions. Otherwise it costs $499 per year for up to 10 developers, $1,499 for up to 50, and $3,999 for an unlimited number, and production deployments are expected to set a license key.

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

# Commands and queries say what they do

MediatR models every message as a request. Mocha makes the intent part of the type: `ICommand` changes state, `IQuery` reads, and handlers implement `HandleAsync`, which returns a `ValueTask`. You can tell reads from writes in code review, and middleware can target either one, for example to open a transaction for commands only.

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

The call sites show the same split. A command is sent, a query is queried, and a notification is published.

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

# Wired at build time, not at startup

The `Mocha.Analyzers` source generator finds the handlers in your project when you compile and writes a registration method named after the assembly, here `AddOrdering()` for `MyApp.Ordering`. Each handler gets a pipeline built from generic code, so dispatch needs no reflection, and the mediator packages are marked compatible with Native AOT.

Mistakes surface when you build, not in production. A message type without a handler is a build warning (`MO0001`), a message type with two handlers is a build error (`MO0002`), and sending a command or query that has no handler in the project is a warning at the call site (`MO0020`).

MediatR does this work at runtime. It scans your assemblies when the application starts, creates a handler wrapper through reflection the first time each request type is sent, and resolves the handler and its behaviors from the service provider on every call. A request without a handler fails only when it is sent.

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

# One middleware model for every concern

Mocha has one concept for cross-cutting code: middleware. A middleware factory runs once per message type when the pipeline is built, and returns the delegate that runs on every dispatch. Code before `next` does what a pre-processor does, code after it what a post-processor does, and a `try`/`catch` around it handles exceptions.

MediatR splits the same job across four interfaces: pipeline behaviors, pre-processors, post-processors, and exception handlers. A behavior is a generic class, `IPipelineBehavior<TRequest, TResponse>`, resolved from the container on every request.

<CodeComparison>

```csharp filename="Mocha"
public sealed class TimingMiddleware(
    ILogger<TimingMiddleware> log)
{
    public async ValueTask InvokeAsync(
        IMediatorContext context,
        MediatorDelegate next)
    {
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
        var log = factory.Services
            .GetRequiredService<
                ILogger<TimingMiddleware>>();
        var middleware = new TimingMiddleware(log);
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

Because the factory knows the message type before the first dispatch, a middleware can leave a pipeline out entirely. Return `next` when `factory.IsQuery()` is true, and queries run without the middleware and without a type check on every call. `IsMessageAssignableTo<T>()` narrows a middleware to one message or base type.

# Notifications as you know them

One `INotification`, any number of handlers. Mocha runs them one after another by default, or concurrently with a single option.

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

# Ready for messages that leave the process

When an order placed in one service has to reach the billing and shipping services, MediatR cannot help: it handles in-process messages only, so you add a second library with its own way of writing handlers.

With Mocha, the message bus is part of the same framework. Event handlers look like notification handlers, the source generator registers them the same way, and the bus adds what messaging between services needs: retries and redelivery, a transactional outbox, sagas, and request/reply.

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

# Moving from MediatR

Most of a migration is renaming:

1. Add the `Mocha.Mediator` and `Mocha.Analyzers` packages.
2. Change `IRequest` to `ICommand` or `IQuery`, and `IRequestHandler` to the matching handler interface.
3. Rename `Handle` to `HandleAsync` and return a `ValueTask`.
4. Replace `AddMediatR` with `AddMediator()` and the generated registration method.
5. Rewrite behaviors as middleware.

Both libraries can run side by side while you move handlers over. The [MediatR migration guide](../docs/mocha/migration/coming-from-mediatr.md) walks through every step with code.

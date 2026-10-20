---
title: "Mocha vs MediatR"
metaTitle: "Mocha vs MediatR: An MIT-Licensed MediatR Alternative"
description: "Compare the Mocha mediator with MediatR for commands, queries, and notifications in .NET: licensing, source-generated dispatch, pipelines, and migration."
lead: "The mediator you know from MediatR, with commands and queries as separate types, handlers registered by a source generator at build time, and a message bus for when your messages leave the process. Free for every company under the MIT license."
product: "mocha"
competitor: "MediatR"
lastChecked: "2026-10-20"
versions:
  product: "16.6"
  competitor: "14.2"
migrationGuide: "/docs/mocha/migration/coming-from-mediatr"
highlights:
  - title: "MIT licensed, no license key"
    text: "Mocha is free for any company. Since version 13, MediatR is licensed under the copyleft RPL-1.5 or a commercial license, which is free only for smaller companies."
  - title: "Handlers registered at build time"
    text: "A source generator writes the registration code for every handler when you compile. MediatR scans assemblies at startup and builds handler wrappers through reflection."
  - title: "Commands and queries are separate"
    text: "ICommand and IQuery replace IRequest, so reads and writes are different types with different dispatch methods, and middleware can target either one."
  - title: "A message bus when you need one"
    text: "The same framework sends events and commands between services, with sagas, an outbox, and retries. MediatR handles in-process messages only."
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
---

Most teams use MediatR for one thing: sending a request to exactly one handler and publishing notifications to many, with behaviors around both. The Mocha mediator does the same job. This page walks through the differences in code, from the messages and handlers to registration, pipelines, and what happens when your messages need to leave the process.

# What you pay

MediatR changed its license with version 13.0 in July 2025. Versions up to 12.5 stay under the Apache 2.0 license, but Lucky Penny Software, the company behind MediatR, no longer provides support or security updates for them. Version 13 and later are dual licensed. You either accept the Reciprocal Public License 1.5, a copyleft license, or buy a commercial license.

The commercial license is free as the Community edition if your company has less than $5 million in annual revenue, has raised no more than $10 million in outside capital, and is not a government body, among other conditions. Otherwise it costs $499 per year for up to 10 developers, $1,499 for up to 50, and $3,999 for an unlimited number. Production deployments are expected to set a license key. Without one, MediatR logs a warning and keeps working.

```csharp filename="MediatR 13 and later"
builder.Services.AddMediatR(cfg =>
{
    cfg.LicenseKey =
        builder.Configuration["MediatR:LicenseKey"];
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
});
```

Mocha is MIT licensed, like Hot Chocolate. There is no paid edition, no license key, and no revenue limit.

# Messages and handlers

Both libraries describe a unit of work as a message and a handler. Mocha replaces `IRequest` with `ICommand` for changes and `IQuery` for reads, and handlers implement `HandleAsync`, which returns a `ValueTask`.

<CodeComparison>

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

</CodeComparison>

The split pays off at the call site and in the pipeline. A read is dispatched with `QueryAsync`, so you can tell from the code what changes state, and middleware can apply a transaction to commands only.

<CodeComparison>

```csharp filename="MediatR" {1,4,7}
var orderId = await sender.Send(
    new PlaceOrder(productId, 2), ct);

var order = await sender.Send(
    new GetOrder(orderId), ct);

await publisher.Publish(
    new OrderPlaced(orderId), ct);
```

```csharp filename="Mocha" {1,4,7}
var orderId = await sender.SendAsync(
    new PlaceOrder(productId, 2), ct);

var order = await sender.QueryAsync(
    new GetOrder(orderId), ct);

await publisher.PublishAsync(
    new OrderPlaced(orderId), ct);
```

</CodeComparison>

# Registration at build time

MediatR finds handlers by scanning your assemblies when the application starts. The first time a request type is sent, it creates a handler wrapper through reflection and caches it, then resolves the handler and its behaviors from the service provider on every call. A request without a handler fails when it is sent.

Mocha does this work when you compile. The `Mocha.Analyzers` source generator finds the handlers in your project and writes a registration method named after the assembly, here `AddOrdering()` for `MyApp.Ordering`. Each handler gets a pipeline built from generic code, so dispatch needs no reflection, and the mediator packages are marked compatible with Native AOT.

<CodeComparison>

```csharp filename="MediatR"
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
});
```

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .AddOrdering(); // written by the source generator
```

</CodeComparison>

The generator also checks your messages while it runs. A message type without a handler is a build warning (`MO0001`), a message type with two handlers is a build error (`MO0002`), and sending a command or query that has no handler in the project is a warning at the call site (`MO0020`).

# Behaviors become middleware

A MediatR behavior is a generic class, `IPipelineBehavior<TRequest, TResponse>`, resolved from the container on every request. Pre-processors, post-processors, and exception handlers are separate interfaces on top of that.

Mocha has one concept for all of these: middleware. A middleware factory runs once per message type when the pipeline is built and returns the delegate that runs on every dispatch. Code before `next` replaces a pre-processor, code after it replaces a post-processor, and a `try`/`catch` around it replaces an exception handler.

<CodeComparison>

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

</CodeComparison>

Because the factory knows the message type before the first dispatch, it can leave the middleware out of a pipeline entirely. Return `next` when `factory.IsQuery()` is true, and queries run without the middleware and without a type check on every call. `IsMessageAssignableTo<T>()` narrows a middleware to one message or base type.

# Notifications

Notifications work the same in both libraries: one `INotification`, any number of handlers. Both run the handlers one after another by default and can run them concurrently instead.

<CodeComparison>

```csharp filename="MediatR"
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(
        typeof(Program).Assembly);
    cfg.NotificationPublisher =
        new TaskWhenAllPublisher();
});
```

```csharp filename="Mocha"
builder.Services
    .AddMediator()
    .ConfigureOptions(o =>
        o.NotificationPublishMode =
            NotificationPublishMode.Concurrent)
    .AddOrdering();
```

</CodeComparison>

# When messages leave the process

MediatR stops at the process boundary. When an order placed in one service has to reach the billing and shipping services, you add a second library and a second way of writing handlers.

With Mocha, the next step is the message bus from the same framework. Event handlers look like notification handlers, the source generator registers them the same way, and the bus adds what messaging between services needs: retries and redelivery, a transactional outbox, sagas, and request/reply.

```csharp filename="Billing service"
builder.Services
    .AddMessageBus()
    .AddBilling()
    .AddRabbitMQ();

public class OrderPlacedHandler(IInvoiceService invoices)
    : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(
        OrderPlaced message,
        CancellationToken ct)
        => await invoices.CreateAsync(message.OrderId, ct);
}
```

```csharp filename="Ordering service"
await bus.PublishAsync(new OrderPlaced(orderId), ct);
```

# What Mocha does not have yet

- **Stream requests.** There is no equivalent of `IStreamRequest` and `IStreamRequestHandler`. Call a service that returns `IAsyncEnumerable<T>` directly for now.
- **Polymorphic notification handlers.** A notification handler runs for the exact type it declares, not for derived notifications.
- **Custom notification publishers.** The publish strategy is one of two built-in modes, sequential or concurrent.
- **.NET Framework and .NET Standard.** Mocha targets .NET 8 and later.

If one of these blocks you, see [what you can do about it](#missing-something).

# Moving from MediatR

Most of a migration is renaming:

1. Add the `Mocha.Mediator` and `Mocha.Analyzers` packages.
2. Change `IRequest` to `ICommand` or `IQuery`, and `IRequestHandler` to the matching handler interface.
3. Rename `Handle` to `HandleAsync` and return a `ValueTask`.
4. Replace `AddMediatR` with `AddMediator()` and the generated registration method.
5. Rewrite behaviors as middleware.

Both libraries can run side by side while you move handlers over. The [MediatR migration guide](../docs/mocha/migration/coming-from-mediatr.md) walks through every step with code.

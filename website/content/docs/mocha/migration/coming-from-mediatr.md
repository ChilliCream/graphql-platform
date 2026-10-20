---
title: "Migrate from MediatR"
metaTitle: "Migrate from MediatR to the Mocha Mediator"
description: "Map MediatR requests, notifications, handlers, and pipeline behaviors to Mocha commands, queries, notifications, and middleware, and migrate a project step by step."
---

The Mocha mediator covers the same ground as MediatR: in-process commands, queries, and notifications, dispatched through a middleware pipeline. The main differences are that Mocha splits requests into commands and queries, registers handlers with a source generator at build time instead of scanning assemblies at startup, and replaces typed pipeline behaviors with middleware that is compiled into each message's pipeline once.

This guide maps MediatR concepts to Mocha, lists what Mocha does not have, and walks you through migrating a project. For a side-by-side overview, including licensing, see [Mocha vs MediatR](../../../compare/mocha-vs-mediatr.md).

# Concept mapping

| MediatR                                                    | Mocha                                                                        | Key difference                                                                       |
| ---------------------------------------------------------- | ---------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| `IRequest`                                                 | `ICommand`                                                                   | Same concept.                                                                        |
| `IRequest<TResponse>` that changes state                   | `ICommand<TResponse>`                                                        | Mocha separates commands from queries.                                               |
| `IRequest<TResponse>` that only reads                      | `IQuery<TResponse>`                                                          | Dispatched with `QueryAsync` instead of `SendAsync`.                                 |
| `IRequestHandler<TRequest>`                                | `ICommandHandler<TCommand>`                                                  | `HandleAsync` returns `ValueTask`.                                                   |
| `IRequestHandler<TRequest, TResponse>`                     | `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>` | `HandleAsync` returns `ValueTask<TResponse>`.                                        |
| `INotification`, `INotificationHandler<T>`                 | `INotification`, `INotificationHandler<T>`                                   | Same names, in the `Mocha.Mediator` namespace. Handlers implement `HandleAsync`.     |
| `ISender.Send`                                             | `ISender.SendAsync` and `ISender.QueryAsync`                                 | Queries have their own method.                                                       |
| `IPublisher.Publish`                                       | `IPublisher.PublishAsync`                                                    | Same concept.                                                                        |
| `IMediator`                                                | `IMediator`                                                                  | Combines `ISender` and `IPublisher`, as in MediatR.                                  |
| `AddMediatR(cfg => cfg.RegisterServicesFromAssembly(...))` | `AddMediator().Add{Module}()`                                                | The registration method is generated at build time from the handlers in the project. |
| `IPipelineBehavior<TRequest, TResponse>`                   | Mediator middleware                                                          | Untyped middleware, filtered per message type when the pipeline is built.            |
| `IRequestPreProcessor`, `IRequestPostProcessor`            | Mediator middleware                                                          | Run code before or after `next`.                                                     |
| `IRequestExceptionHandler`, `IRequestExceptionAction`      | Mediator middleware                                                          | Wrap `next` in `try`/`catch`.                                                        |
| `INotificationPublisher` (`TaskWhenAllPublisher`)          | `NotificationPublishMode.Concurrent`                                         | Two built-in modes, sequential (default) and concurrent. No custom publishers.       |

# What Mocha does not have

Check these before you start. If your project depends on one of them, plan a replacement first.

- **Stream requests.** There is no equivalent of `IStreamRequest<T>` and `IStreamRequestHandler<T>`. Call a service that returns `IAsyncEnumerable<T>` directly instead.
- **Typed, open generic behaviors.** Middleware receives an untyped `IMediatorContext`. To target specific messages, filter in the middleware factory with `IsMessageAssignableTo<T>()`, `IsCommand()`, or `IsQuery()` and cast `context.Message`.
- **Separate processor and exception handler types.** Pre-processors, post-processors, exception handlers, and exception actions all become middleware.
- **Polymorphic notification handlers.** A notification handler runs for the exact notification type it declares. A handler for a base type or interface does not receive derived notifications.
- **.NET Framework and .NET Standard.** Mocha targets .NET 8 and later.

# Migrate step by step

The steps below migrate one project. Each step keeps the project compiling if you do them in order.

## 1. Install the packages

```bash
dotnet add package Mocha.Mediator
dotnet add package Mocha.Analyzers
```

`Mocha.Analyzers` contains the source generator that writes the registration method. Add it to every project that contains handlers.

## 2. Change the messages

Replace `IRequest` with `ICommand` or `IQuery`. Messages that change state become commands, messages that only read become queries.

```csharp
// MediatR
public record PlaceOrder(Guid ProductId, int Quantity) : IRequest<Guid>;
public record GetOrder(Guid OrderId) : IRequest<OrderDto?>;
public record OrderPlaced(Guid OrderId) : INotification;

// Mocha
using Mocha.Mediator;

public record PlaceOrder(Guid ProductId, int Quantity) : ICommand<Guid>;
public record GetOrder(Guid OrderId) : IQuery<OrderDto?>;
public record OrderPlaced(Guid OrderId) : INotification;
```

## 3. Change the handlers

Handlers implement the matching Mocha interface. `Handle` becomes `HandleAsync` and returns a `ValueTask`.

```csharp
// MediatR
public class PlaceOrderHandler(AppDbContext db) : IRequestHandler<PlaceOrder, Guid>
{
    public async Task<Guid> Handle(PlaceOrder request, CancellationToken cancellationToken)
    {
        var order = new Order { ProductId = request.ProductId, Quantity = request.Quantity };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}

// Mocha
public class PlaceOrderHandler(AppDbContext db) : ICommandHandler<PlaceOrder, Guid>
{
    public async ValueTask<Guid> HandleAsync(PlaceOrder command, CancellationToken cancellationToken)
    {
        var order = new Order { ProductId = command.ProductId, Quantity = command.Quantity };
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}
```

Query handlers implement `IQueryHandler<TQuery, TResponse>`, notification handlers implement `INotificationHandler<TNotification>`, and commands without a response use `ICommandHandler<TCommand>`.

## 4. Register the mediator

Replace `AddMediatR` with `AddMediator` and the generated registration method. The generator names the method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()`.

```csharp
// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Mocha
builder.Services
    .AddMediator()
    .AddOrdering();
```

To choose the method name yourself, add `[assembly: MediatorModule("Ordering")]` to the project. Handlers are registered as scoped services by default, where MediatR registers them as transient. Change this with `ConfigureOptions(o => o.ServiceLifetime = ...)`.

The generator also reports problems at build time, for example a message type without a handler (`MO0001`) or with more than one handler (`MO0002`). See [Diagnostics](../diagnostics.md) for the full list.

## 5. Update the call sites

`ISender`, `IPublisher`, and `IMediator` keep their names. Only the methods change.

```csharp
// MediatR
var orderId = await sender.Send(new PlaceOrder(productId, 2), cancellationToken);
var order = await sender.Send(new GetOrder(orderId), cancellationToken);
await publisher.Publish(new OrderPlaced(orderId), cancellationToken);

// Mocha
var orderId = await sender.SendAsync(new PlaceOrder(productId, 2), cancellationToken);
var order = await sender.QueryAsync(new GetOrder(orderId), cancellationToken);
await publisher.PublishAsync(new OrderPlaced(orderId), cancellationToken);
```

## 6. Move pipeline behaviors to middleware

A MediatR behavior is a generic class that DI creates for every request. A Mocha middleware is a factory that runs once per message type when the pipeline is built, and returns the delegate that runs on every dispatch. The factory can return `next` to leave a message type out of the middleware entirely.

```csharp
// MediatR
public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Request}", typeof(TRequest).Name);
        return await next();
    }
}

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Mocha
internal sealed class LoggingMiddleware(ILogger<LoggingMiddleware> logger)
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        logger.LogInformation("Handling {Message}", context.MessageType.Name);
        await next(context);
    }

    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Leave notifications out of this pipeline
                if (factoryCtx.IsNotification())
                {
                    return next;
                }

                var logger = factoryCtx.Services.GetRequiredService<ILogger<LoggingMiddleware>>();
                var middleware = new LoggingMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Logging");
}

builder.Services
    .AddMediator()
    .AddOrdering()
    .Use(LoggingMiddleware.Create());
```

Keep these differences in mind:

- **Scoped services.** The factory runs once, so resolve singletons such as loggers from `factoryCtx.Services`. Resolve scoped services such as a `DbContext` from `context.Services` inside `InvokeAsync`, once per dispatch.
- **The response.** `next` returns nothing. After `next` completes, the handler's response is in `context.Result`. Set `context.Result` to replace it, or to return a response without calling `next`.
- **Order.** Middleware runs in the order you call `Use`. Pass `before:` or `after:` with another middleware's key to position it, for example around the built-in `"Instrumentation"` middleware.
- **Validation for one message type.** Filter with `factoryCtx.IsMessageAssignableTo<PlaceOrder>()` and cast `context.Message` in `InvokeAsync`. To validate every command, filter with `IsCommand()` and `IsCommandWithResponse()` instead.

[Pipeline and middleware](../mediator/pipeline-and-middleware.md) covers the full middleware API.

## 7. Replace processors and exception handlers

Pre-processors run before `next`, post-processors after it. Exception handlers and exception actions wrap `next` in a `try`/`catch`. To recover from an exception, set `context.Result` to a fallback value and return without re-throwing.

```csharp
public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
{
    // IRequestPreProcessor
    logger.LogInformation("Starting {Message}", context.MessageType.Name);

    try
    {
        await next(context);
    }
    catch (OrderNotFoundException)
    {
        // IRequestExceptionHandler: recover with a fallback response
        context.Result = null;
        return;
    }

    // IRequestPostProcessor
    logger.LogInformation("Finished {Message}", context.MessageType.Name);
}
```

## 8. Choose the notification publish mode

MediatR runs notification handlers one after another by default, and `TaskWhenAllPublisher` runs them concurrently. Mocha has the same two modes.

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent)
    .AddOrdering();
```

In sequential mode, the first exception stops the remaining handlers. In concurrent mode, all handlers run and their exceptions are collected in an `AggregateException`.

## 9. Remove MediatR

Once nothing references the `MediatR` namespace, remove the package:

```bash
dotnet remove package MediatR
```

# Migrating in stages

MediatR and Mocha register different services, so both can run in the same application while you migrate handler by handler. Both libraries define `ISender`, `IPublisher`, `IMediator`, and `INotification`, so files that use both need a namespace alias:

```csharp
using MediatR;
using Mocha.Mediator;
using MochaSender = Mocha.Mediator.ISender;
```

A message is handled by whichever library its handler is registered with, so move a message and its handlers together.

# Next steps

- [Mediator](../mediator/index.md) covers messages, handlers, registration, and named mediators.
- [Pipeline and middleware](../mediator/pipeline-and-middleware.md) covers filtering, positioning, and Entity Framework Core transactions.
- When messages need to leave the process, the [message bus](../index.md) uses the same handler style for events, commands, and request/reply between services.

---
title: "Migrate from MediatR"
metaTitle: "Migrate from MediatR to the Mocha Mediator"
description: "Map MediatR requests, notifications, handlers, and pipeline behaviors to Mocha commands, queries, notifications, and middleware, and migrate a project step by step."
---

The Mocha mediator covers the same ground as MediatR: in-process commands, queries, and notifications, dispatched through a middleware pipeline. The main differences are that Mocha splits requests into commands and queries, registers handlers with a source generator at build time instead of scanning assemblies at startup, and replaces typed pipeline behaviors with middleware that is compiled into each message's pipeline once.

This guide maps MediatR concepts to Mocha and walks you through migrating a project. For a side-by-side overview, including licensing, see [Mocha vs. MediatR](../../../comparison/mocha-vs-mediatr.md).

# Migrate with a coding agent

To let a coding agent such as Claude Code, Codex, or GitHub Copilot do the migration, copy this prompt into it. The prompt points the agent to this guide and has it check with you before it changes code.

<AgentPrompt title="Migration prompt">

```text
Migrate this codebase from MediatR to the Mocha mediator, the MIT-licensed
in-process mediator from ChilliCream.

Follow the migration guide:
https://chillicream.com/docs/mocha/migration/coming-from-mediatr.md
Look up Mocha APIs in https://chillicream.com/docs/mocha/llms.txt instead of
guessing them.

1. Inventory the MediatR usage: requests, notifications, handlers, pipeline
   behaviors, pre- and post-processors, exception handlers, stream requests,
   notification publishers, and registration. If the codebase uses something
   the guide does not cover, stop and ask me how to proceed.
2. Decide for every request whether it is a command or a query, and show me
   the list before changing code.
3. Use the guide's mappings to migrate each message, its handlers, call sites,
   registration, and applicable pipeline components together. Build after
   each coherent batch.
4. Build, run the tests, and fix every diagnostic the Mocha source generator
   reports.
5. Remove MediatR once nothing references it. Summarize what changed and
   anything you could not migrate.
```

</AgentPrompt>

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
| `INotificationPublisher` (`TaskWhenAllPublisher`)          | `NotificationPublishMode.Concurrent`                                         | Two built-in modes, sequential (default) and concurrent.                             |

# Migrate step by step

Install the packages first. Migrate each message, its handlers, call sites, registration, and applicable pipeline components together, then build. Repeat until all messages have moved. Keep both mediators registered during a staged migration.

## 1. Install the packages

```bash
dotnet add package Mocha.Mediator
dotnet add package Mocha.Analyzers
```

`Mocha.Analyzers` contains the source generator that writes the registration method. Add it to every project that contains handlers. Mocha targets .NET 8 and later.

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

To choose the method name yourself, add `[assembly: MediatorModule("Ordering")]` to the project. Handlers are registered as scoped services by default, where MediatR registers them as transient.

The generator also reports problems at build time, for example a command or query without a handler (`MO0001`) or with more than one handler (`MO0002`). Notifications can have multiple handlers. See [Diagnostics](../diagnostics.md) for the full list.

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
internal sealed class LoggingMiddleware
{
    public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
    {
        var logger = context.Services.GetRequiredService<ILogger<LoggingMiddleware>>();
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

                var middleware = new LoggingMiddleware();
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

- **Services.** Resolve loggers, a `DbContext`, and other services from `context.Services` inside `InvokeAsync`. `factoryCtx.Services` in the factory does not contain your application's services.
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

In sequential mode, the first exception stops the remaining handlers. With multiple handlers in concurrent mode, Mocha collects failures reported through their returned tasks in an `AggregateException`. When awaited, MediatR's `TaskWhenAllPublisher` normally propagates one handler exception instead. A synchronous throw during dispatch can stop either publisher before later handlers run. Update callers that catch individual handler exceptions.

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

A message is handled by whichever library its handler is registered with, so move a message, its handlers, and call sites together. MediatR behaviors and processors do not run for messages dispatched through Mocha. Migrate the applicable cross-cutting behavior to Mocha middleware before moving each message.

# Next steps

- [Mediator](../mediator/index.md) covers messages, handlers, registration, and named mediators.
- [Pipeline and middleware](../mediator/pipeline-and-middleware.md) covers filtering, positioning, and Entity Framework Core transactions.
- When messages need to leave the process, the [message bus](../index.md) uses the same handler style for events, commands, and request/reply between services.

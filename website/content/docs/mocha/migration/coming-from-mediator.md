---
title: "Migrate from Mediator"
metaTitle: "Migrate from Mediator (Mediator.SourceGenerator) to the Mocha Mediator"
description: "Map Mediator commands, queries, notifications, handlers, options, and pipeline behaviors to the Mocha mediator, and migrate a project step by step."
---

The Mocha mediator and Mediator, the `Mediator.SourceGenerator` package, work the same way: in-process commands, queries, and notifications, handlers registered by a source generator at build time, and handlers that return a `ValueTask`. Both use the same names for messages and handlers, so most of a migration is renaming. The main differences are that Mocha writes a registration method for every project instead of one mediator for the application, and replaces typed pipeline behaviors with middleware that is compiled into each message's pipeline once.

This guide maps Mediator concepts to Mocha and walks you through migrating a project. For a side-by-side overview, see [Mocha vs. Mediator](../../../comparison/mocha-vs-mediator.md).

# Migrate with a coding agent

To let a coding agent such as Claude Code, Codex, or GitHub Copilot do the migration, copy this prompt into it. The prompt points the agent to this guide and has it check with you before it changes code.

<AgentPrompt title="Migration prompt">

```text
Migrate this codebase from Mediator to the Mocha mediator, the MIT-licensed
in-process mediator from ChilliCream.

Follow the migration guide:
https://chillicream.com/docs/mocha/migration/coming-from-mediator.md
Look up Mocha APIs in https://chillicream.com/docs/mocha/llms.txt instead of
guessing them.

1. Inventory the Mediator usage: the Mediator.SourceGenerator options,
   requests, commands, queries, notifications, handlers, pipeline behaviors,
   pre- and post-processors, exception handlers, stream messages, and
   notification publishers. If the codebase uses something the guide does not
   cover, stop and ask me how to proceed.
2. Decide for every IRequest whether it is a command or a query, and show me
   the list before changing code.
3. Migrate each message, its handlers, dispatch sites, registration, and
   applicable pipeline components together. Keep both mediators registered
   during a staged migration, and build after each coherent batch.
4. Build, run the tests, and fix every diagnostic the Mocha source generator
   reports.
5. Remove Mediator once nothing references it. Summarize what changed and
   anything you could not migrate.
```

</AgentPrompt>

# Concept mapping

| Mediator                                                        | Mocha                                                                      | Key difference                                                                                 |
| --------------------------------------------------------------- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| `ICommand`, `ICommand<TResponse>`                               | `ICommand`, `ICommand<TResponse>`                                          | Same names, in the `Mocha.Mediator` namespace.                                                 |
| `IQuery<TResponse>`                                             | `IQuery<TResponse>`                                                        | Dispatched with `QueryAsync` instead of `Send`.                                                |
| `IRequest`, `IRequest<TResponse>`                               | `ICommand`, `ICommand<TResponse>`, or `IQuery<TResponse>`                  | Mocha has no separate request type.                                                            |
| `INotification`                                                 | `INotification`                                                            | Same concept.                                                                                  |
| `ICommandHandler<TCommand>`                                     | `ICommandHandler<TCommand>`                                                | `HandleAsync` returns `ValueTask` instead of `ValueTask<Unit>`.                                |
| `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<,>`      | `ICommandHandler<TCommand, TResponse>`, `IQueryHandler<TQuery, TResponse>` | `Handle` becomes `HandleAsync`.                                                                |
| `IRequestHandler<,>`                                            | `ICommandHandler<,>` or `IQueryHandler<,>`                                 | Pick by whether the message changes state or reads.                                            |
| `INotificationHandler<T>`                                       | `INotificationHandler<T>`                                                  | `Handle` becomes `HandleAsync`.                                                                |
| `ISender.Send`                                                  | `ISender.SendAsync` and `ISender.QueryAsync`                               | Queries have their own method.                                                                 |
| `IPublisher.Publish`                                            | `IPublisher.PublishAsync`                                                  | Same concept.                                                                                  |
| The generated `Mediator` class and `IMediator`                  | `IMediator`                                                                | Inject `ISender`, `IPublisher`, or `IMediator`. There is no generated class to inject.         |
| `AddMediator(options => ...)` or `[assembly: MediatorOptions]`  | `AddMediator().Add{Module}()`                                              | The generator runs in every project with handlers and writes one registration method for each. |
| `options.Assemblies`                                            | `Mocha.Analyzers` in each project                                          | Each project registers its own handlers.                                                       |
| `options.Namespace`                                             | `[assembly: MediatorModule("...")]`                                        | Names the registration method instead of a namespace.                                          |
| `options.ServiceLifetime`                                       | Scoped by default                                                          | Mediator defaults to `Singleton`.                                                              |
| `options.NotificationPublisherType` with `TaskWhenAllPublisher` | `NotificationPublishMode.Concurrent`                                       | Two built-in modes, sequential (default) and concurrent.                                       |
| `IPipelineBehavior<TMessage, TResponse>`                        | Mediator middleware                                                        | Untyped middleware, filtered per message type when the pipeline is built.                      |
| `MessagePreProcessor`, `MessagePostProcessor`                   | Mediator middleware                                                        | Run code before or after `next`.                                                               |
| `MessageExceptionHandler`                                       | Mediator middleware                                                        | Wrap `next` in `try`/`catch`.                                                                  |
| `MSG0001` (duplicate handler), `MSG0005` (missing handler)      | `MO0002` (error), `MO0001` (warning)                                       | Two handlers for one message fail the build.                                                   |

# Migrate step by step

Install the packages first. Migrate each message, its handlers, dispatch sites, registration, and applicable pipeline components together, then build. Keep both mediators registered during a staged migration.

## 1. Install the packages

```bash
dotnet add package Mocha.Mediator
dotnet add package Mocha.Analyzers
```

`Mocha.Analyzers` contains the source generator that writes the registration method. Mediator's generator runs only in the executable project, but Mocha's runs in every project that contains handlers, so add `Mocha.Analyzers` to each of them. Mocha targets .NET 8 and later.

## 2. Change the messages

Change the namespace from `Mediator` to `Mocha.Mediator`. Commands, queries, and notifications keep their interfaces. Replace `IRequest` with `ICommand` or `IQuery`: messages that change state become commands, messages that only read become queries.

Mocha has no common `IMessage` or `IBaseCommand` marker. Remove those base interfaces from custom message markers, and replace their constraints with middleware message-kind or assignability filters.

```csharp
// Mediator
using Mediator;

public record PlaceOrder(Guid ProductId, int Quantity) : ICommand<Guid>;
public record GetOrder(Guid OrderId) : IRequest<OrderDto?>;
public record OrderPlaced(Guid OrderId) : INotification;

// Mocha
using Mocha.Mediator;

public record PlaceOrder(Guid ProductId, int Quantity) : ICommand<Guid>;
public record GetOrder(Guid OrderId) : IQuery<OrderDto?>;
public record OrderPlaced(Guid OrderId) : INotification;
```

## 3. Change the handlers

Handlers keep their interfaces. `Handle` becomes `HandleAsync`. A command without a response returns `ValueTask` instead of `ValueTask<Unit>`.

```csharp
// Mediator
public sealed class CancelOrderHandler(AppDbContext db) : ICommandHandler<CancelOrder>
{
    public async ValueTask<Unit> Handle(CancelOrder command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.OrderId], cancellationToken);
        order!.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        return Unit.Value;
    }
}

// Mocha
public sealed class CancelOrderHandler(AppDbContext db) : ICommandHandler<CancelOrder>
{
    public async ValueTask HandleAsync(CancelOrder command, CancellationToken cancellationToken)
    {
        var order = await db.Orders.FindAsync([command.OrderId], cancellationToken);
        order!.Cancel();
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

A handler of an `IRequest` becomes an `ICommandHandler` or an `IQueryHandler`, matching the message.

## 4. Register the mediator

Replace `AddMediator(options => ...)` with `AddMediator()` and the generated registration methods. The generator names each method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()` and handlers in `MyApp.Billing` with `AddBilling()`.

```csharp
// Mediator
builder.Services.AddMediator((MediatorOptions options) =>
{
    options.Assemblies = [typeof(PlaceOrder), typeof(CreateInvoice)];
    options.ServiceLifetime = ServiceLifetime.Scoped;
});

// Mocha
builder.Services
    .AddMediator()
    .AddOrdering()
    .AddBilling();
```

To choose a method name yourself, add `[assembly: MediatorModule("Ordering")]` to the project. Handlers are registered as scoped services by default, where Mediator registers them as singletons.

The generator also reports problems at build time. A command or query with more than one handler is an error (`MO0002`), one without a handler is a warning (`MO0001`), and sending a command or query that has no handler in the project is a warning at the call site (`MO0020`). Notifications can have multiple handlers. See [Diagnostics](../diagnostics.md) for the full list.

## 5. Update the call sites

`ISender`, `IPublisher`, and `IMediator` keep their names. Only the methods change. Code that injected the generated `Mediator` class injects one of the interfaces instead.

```csharp
// Mediator
var orderId = await mediator.Send(new PlaceOrder(productId, 2), cancellationToken);
var order = await mediator.Send(new GetOrder(orderId), cancellationToken);
await mediator.Publish(new OrderPlaced(orderId), cancellationToken);

// Mocha
var orderId = await mediator.SendAsync(new PlaceOrder(productId, 2), cancellationToken);
var order = await mediator.QueryAsync(new GetOrder(orderId), cancellationToken);
await mediator.PublishAsync(new OrderPlaced(orderId), cancellationToken);
```

## 6. Move pipeline behaviors to middleware

A Mediator behavior is a generic class, narrowed to message types with generic constraints. A Mocha middleware is a factory that runs once per message type when the pipeline is built, and returns the delegate that runs on every dispatch. The factory can return `next` to leave a message type out of the middleware entirely. Mocha middleware also wraps notification handlers, which Mediator behaviors do not.

```csharp
// Mediator
public sealed class LoggingBehavior<TMessage, TResponse>(ILogger<LoggingBehavior<TMessage, TResponse>> logger)
    : IPipelineBehavior<TMessage, TResponse>
    where TMessage : IBaseCommand
{
    public async ValueTask<TResponse> Handle(
        TMessage message,
        MessageHandlerDelegate<TMessage, TResponse> next,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Handling {Message}", typeof(TMessage).Name);
        return await next(message, cancellationToken);
    }
}

builder.Services.AddMediator((MediatorOptions options) =>
{
    options.PipelineBehaviors = [typeof(LoggingBehavior<,>)];
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
                // Only commands, like the IBaseCommand constraint
                if (!factoryCtx.IsCommand() && !factoryCtx.IsCommandWithResponse())
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
- **Ready-made middleware.** `UseEntityFrameworkTransactions<TContext>()` from `Mocha.EntityFrameworkCore` wraps every command handler in a database transaction, and `AddInstrumentation()` creates an OpenTelemetry span for every message.

[Pipeline and middleware](../mediator/pipeline-and-middleware.md) covers the full middleware API.

## 7. Replace processors and exception handlers

Pre-processors run before `next`, post-processors after it. Exception handlers wrap `next` in a `try`/`catch`. Where a `MessageExceptionHandler` returns `Handled(response)`, set `context.Result` to the fallback value and return without re-throwing. Where it returns `NotHandled`, re-throw.

```csharp
public async ValueTask InvokeAsync(IMediatorContext context, MediatorDelegate next)
{
    // MessagePreProcessor
    logger.LogInformation("Starting {Message}", context.MessageType.Name);

    try
    {
        await next(context);
    }
    catch (OrderNotFoundException)
    {
        // MessageExceptionHandler returning Handled(null)
        context.Result = null;
        return;
    }

    // MessagePostProcessor
    logger.LogInformation("Finished {Message}", context.MessageType.Name);
}
```

## 8. Choose the notification publish mode

Mediator runs notification handlers one after another with `ForeachAwaitPublisher`, the default, and concurrently with `TaskWhenAllPublisher`. Mocha has the same two modes. For failures reported through a handler's `ValueTask`, Mediator's sequential publisher continues with the remaining handlers and then throws an `AggregateException`, while Mocha stops at the first exception and throws it.

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent)
    .AddOrdering();
```

With multiple handlers in concurrent mode, both libraries collect failures reported through returned tasks in an `AggregateException`. In Mediator 3.0.2, a handler that throws synchronously before returning its `ValueTask` can escape either publisher's fast path and stop dispatch before later handlers run. A synchronous throw during Mocha's pipeline invocation can also stop concurrent dispatch before aggregation. Review callers that depend on failure propagation or all handlers running.

## 9. Remove Mediator

Once nothing references the `Mediator` namespace, remove the packages:

```bash
dotnet remove package Mediator.SourceGenerator
dotnet remove package Mediator.Abstractions
```

# Migrating in stages

Mediator and Mocha register different services, so both can run in the same application while you migrate handler by handler. They use the same type names, so files that use both need a namespace alias:

```csharp
using Mediator;
using MochaMediator = Mocha.Mediator;

public record PlaceOrder(Guid ProductId) : MochaMediator.ICommand<Guid>;
```

Both libraries also define an `AddMediator` extension method on `IServiceCollection`. Mediator generates its method in the `Microsoft.Extensions.DependencyInjection` namespace, which implicit usings import into every file, so in any file that imports `Mocha.Mediator`, call Mocha's through its class:

```csharp
Mocha.Mediator.MediatorServiceCollectionExtensions
    .AddMediator(builder.Services)
    .AddOrdering();
```

A message is handled by whichever library its handler is registered with, so move a message, its handlers, and dispatch sites together. Mediator behaviors and processors do not run for messages dispatched through Mocha. Migrate their cross-cutting behavior to Mocha middleware before moving each message, and filter it to preserve the original scope.

# Next steps

- [Mediator](../mediator/index.md) covers messages, handlers, registration, and named mediators.
- [Pipeline and middleware](../mediator/pipeline-and-middleware.md) covers filtering, positioning, and Entity Framework Core transactions.
- When messages need to leave the process, the [message bus](../index.md) uses the same handler style for events, commands, and request/reply between services.

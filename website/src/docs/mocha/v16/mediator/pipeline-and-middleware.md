---
title: "Pipeline & Middleware"
description: "Add cross-cutting concerns to the Mocha Mediator dispatch pipeline. Write middleware for logging, validation, transactions, and exception handling. Configure notification publish modes and OpenTelemetry instrumentation."
---

```csharp
public static class LoggingMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.Logging");

                return ctx =>
                {
                    logger.LogInformation("Handling {MessageType}...", ctx.MessageType.Name);

                    var sw = Stopwatch.StartNew();
                    var task = next(ctx);

                    if (task.IsCompletedSuccessfully)
                    {
                        sw.Stop();
                        logger.LogInformation("Handled {MessageType} in {ElapsedMs}ms",
                            ctx.MessageType.Name, sw.ElapsedMilliseconds);
                        return default;
                    }

                    return Awaited(task, sw, logger, ctx.MessageType.Name);

                    static async ValueTask Awaited(
                        ValueTask t, Stopwatch sw, ILogger log, string msgType)
                    {
                        await t.ConfigureAwait(false);
                        sw.Stop();
                        log.LogInformation("Handled {MessageType} in {ElapsedMs}ms",
                            msgType, sw.ElapsedMilliseconds);
                    }
                };
            },
            "Logging");
}
```

That is a middleware. It wraps every command, query, and notification with timing and logging. Register it with `.Use()` and it runs for every message that passes through the pipeline.

# How the pipeline works

The mediator compiles a middleware pipeline for each registered message type at application startup. Each middleware wraps the next one, forming a [chain of responsibility](https://refactoring.guru/design-patterns/chain-of-responsibility) that terminates at the handler. If you have used [middleware in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/), the mental model is the same - this is the [Pipes and Filters](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html) pattern applied to in-process message dispatch.

```text
SendAsync(PlaceOrderCommand)
  -> LoggingMiddleware
      -> ValidationMiddleware
          -> TransactionMiddleware
              -> PlaceOrderCommandHandler
          <- commit / rollback
      <- throw on invalid
  <- log elapsed time
```

The pipeline is built from two delegate types:

```csharp
// The terminal pipeline delegate - each step in the chain has this shape
public delegate ValueTask MediatorDelegate(IMediatorContext context);

// The factory that creates a middleware - runs once per message type at startup
public delegate MediatorDelegate MediatorMiddleware(
    MediatorMiddlewareFactoryContext context,
    MediatorDelegate next);
```

At startup, the mediator iterates every registered middleware in reverse order. Each factory receives the `next` delegate and returns a new delegate that wraps it. The result is a single compiled `MediatorDelegate` per message type. At runtime, dispatch is a direct delegate invocation - no reflection, no generic resolution.

```mermaid
graph LR
    A[SendAsync] --> B[Middleware 1]
    B --> C[Middleware 2]
    C --> D[Middleware N]
    D --> E[Handler]
    E -.-> D
    D -.-> C
    C -.-> B
    B -.-> A
```

# Write a middleware

A middleware is a static class with a `Create()` method that returns a `MediatorMiddlewareConfiguration`. The configuration holds two things: the factory delegate and an optional string key used for [positioning](#middleware-positioning).

The factory delegate receives two arguments:

| Argument                           | Available at           | Purpose                                                                             |
| ---------------------------------- | ---------------------- | ----------------------------------------------------------------------------------- |
| `MediatorMiddlewareFactoryContext` | Startup (compile time) | Resolve singleton services, inspect message/response types, opt out of the pipeline |
| `MediatorDelegate next`            | Startup (compile time) | The next middleware or handler in the chain                                         |

The factory returns a `MediatorDelegate` - the runtime function that receives `IMediatorContext` for each dispatch.

Here is a minimal timing middleware, step by step:

```csharp
public static class TimingMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // 1. Resolve services once at startup (not per request)
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.Timing");

                // 2. Return the runtime delegate
                return async ctx =>
                {
                    var sw = Stopwatch.StartNew();

                    await next(ctx); // 3. Call the next middleware or handler

                    sw.Stop();
                    logger.LogInformation(
                        "{MessageType} handled in {ElapsedMs}ms",
                        ctx.MessageType.Name,
                        sw.ElapsedMilliseconds);
                };
            },
            "Timing"); // 4. Key for positioning
}
```

Register it on the mediator builder:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()   // source-generated handler registration
    .Use(TimingMiddleware.Create());
```

The `IMediatorContext` available at runtime provides everything you need during dispatch:

| Property            | Type                 | Description                                                                 |
| ------------------- | -------------------- | --------------------------------------------------------------------------- |
| `Message`           | `object`             | The message instance being dispatched                                       |
| `MessageType`       | `Type`               | Runtime type of the message                                                 |
| `ResponseType`      | `Type`               | Expected response type (`typeof(void)` for void commands and notifications) |
| `Result`            | `object?`            | The handler's return value, readable by middleware after calling `next`     |
| `Services`          | `IServiceProvider`   | Scoped service provider for the current request                             |
| `CancellationToken` | `CancellationToken`  | Cancellation token for the operation                                        |
| `Features`          | `IFeatureCollection` | Per-request feature collection for sharing state between middleware         |
| `Runtime`           | `IMediatorRuntime`   | The mediator runtime that owns this context                                 |

## Short-circuiting

To prevent the handler from executing, return without calling `next`:

```csharp
return ctx =>
{
    if (ctx.Message is PlaceOrderCommand { Quantity: <= 0 })
        throw new ArgumentException("Quantity must be greater than zero.");

    return next(ctx); // only reached if validation passes
};
```

## Exception handling

Wrap `next` in a try/catch to handle exceptions:

```csharp
public static class ExceptionHandlingMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                var logger = factoryCtx.Services.GetRequiredService<ILoggerFactory>()
                    .CreateLogger("Pipeline.ExceptionHandler");

                return async ctx =>
                {
                    try
                    {
                        await next(ctx);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error handling {MessageType}",
                            ctx.MessageType.Name);
                        throw; // re-throw or set ctx.Result to recover
                    }
                };
            },
            "ExceptionHandling");
}
```

To recover from an exception instead of re-throwing, set `ctx.Result` to a fallback value and return normally.

## Synchronous fast-path optimization

When `next` completes synchronously (common for in-memory handlers), you can avoid the `async` state machine overhead by checking `IsCompletedSuccessfully`:

```csharp
return ctx =>
{
    logger.LogInformation("Before");

    var task = next(ctx);

    if (task.IsCompletedSuccessfully)
    {
        logger.LogInformation("After (sync)");
        return default;
    }

    return Awaited(task, logger);

    static async ValueTask Awaited(ValueTask t, ILogger log)
    {
        await t.ConfigureAwait(false);
        log.LogInformation("After (async)");
    }
};
```

This pattern avoids allocating an async state machine when the pipeline completes synchronously. Use it in performance-sensitive middleware; use plain `async`/`await` everywhere else.

# Compile-time filtering

The `MediatorMiddlewareFactoryContext` is available during pipeline compilation - before your application handles its first request. Use it to exclude your middleware from pipelines where it does not apply.

To opt out, return `next` directly from the factory. The middleware is not included in that pipeline at all - zero runtime cost, no delegate wrapper, no type check on every dispatch.

```csharp
public static class TransactionMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Skip notifications and queries at compile time
                if (factoryCtx.IsNotification() || factoryCtx.IsQuery())
                {
                    return next; // not included in this pipeline
                }

                return async ctx =>
                {
                    // Resolve DbContext from the scoped service provider
                    var db = ctx.Services.GetRequiredService<AppDbContext>();

                    await using var tx = await db.Database
                        .BeginTransactionAsync(ctx.CancellationToken);
                    try
                    {
                        await next(ctx);
                        await tx.CommitAsync(ctx.CancellationToken);
                    }
                    catch
                    {
                        await tx.RollbackAsync(ctx.CancellationToken);
                        throw;
                    }
                };
            },
            "Transaction");
}
```

## Message kind checks

| Method                    | Returns true when                             |
| ------------------------- | --------------------------------------------- |
| `IsCommand()`             | Void command (`ICommand`)                     |
| `IsCommandWithResponse()` | Command with response (`ICommand<TResponse>`) |
| `IsQuery()`               | Query (`IQuery<TResponse>`)                   |
| `IsNotification()`        | Notification (`INotification`)                |

## Type assignability checks

| Method                         | Returns true when                                                              |
| ------------------------------ | ------------------------------------------------------------------------------ |
| `IsMessageAssignableTo<T>()`   | Message type is assignable to `T`                                              |
| `IsMessageAssignableTo(Type)`  | Message type is assignable to the given type                                   |
| `IsResponseAssignableTo<T>()`  | Response type is assignable to `T` (false for void commands and notifications) |
| `IsResponseAssignableTo(Type)` | Response type is assignable to the given type                                  |

Use `IsMessageAssignableTo` to scope a middleware to a specific message or base type:

```csharp
public static class PlaceOrderValidationMiddleware
{
    public static MediatorMiddlewareConfiguration Create()
        => new(
            static (factoryCtx, next) =>
            {
                // Only compile into the PlaceOrderCommand pipeline
                if (!factoryCtx.IsMessageAssignableTo<PlaceOrderCommand>())
                {
                    return next;
                }

                return ctx =>
                {
                    if (ctx.Message is PlaceOrderCommand order && order.Quantity <= 0)
                        throw new ArgumentException("Quantity must be greater than zero.");
                    return next(ctx);
                };
            },
            "Validation");
}
```

Use `IsResponseAssignableTo` to filter by response type:

```csharp
// Only audit pipelines that return OrderResult
if (!factoryCtx.IsResponseAssignableTo<OrderResult>())
{
    return next;
}
```

## When to use compile-time vs. runtime checks

Use **compile-time filtering** (`MediatorMiddlewareFactoryContext`) when:

- You know at registration time which message kinds the middleware applies to
- You want zero overhead for pipelines that do not need the middleware
- You are filtering by message kind, response type, or base class

Use **runtime checks** (`IMediatorContext`) when:

- You need to inspect the actual message instance (check a property value)
- The decision depends on runtime state (feature flags, configuration)

Both approaches combine well - filter out entire message kinds at compile time, then do finer-grained checks at runtime for the pipelines that remain.

# Middleware positioning

The `Use` method accepts optional `before` and `after` parameters to control where the middleware sits in the pipeline.

| Call                                    | Behavior                                                  |
| --------------------------------------- | --------------------------------------------------------- |
| `Use(config)`                           | Appends to the end of the middleware list                 |
| `Use(config, before: "Logging")`        | Inserts before the middleware with key `"Logging"`        |
| `Use(config, after: "Instrumentation")` | Inserts after the middleware with key `"Instrumentation"` |

Only one of `before` or `after` can be specified at the same time. If the referenced key is not found, an `InvalidOperationException` is thrown at startup.

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()
    .Use(LoggingMiddleware.Create())                                        // position 1
    .Use(ValidationMiddleware.Create())                                     // position 2
    .Use(ExceptionHandlingMiddleware.Create())                              // position 3
    .Use(SecurityMiddleware.Create(), before: "Logging")                    // before "Logging"
    .Use(CorrelationIdMiddleware.Create(), after: "Logging");               // after "Logging"
```

Resulting order: Security -> Logging -> CorrelationId -> Validation -> ExceptionHandling -> Handler.

The `Key` property on `MediatorMiddlewareConfiguration` is optional. Middleware without a key can still be registered with `Use(config)`, but cannot be referenced by other middleware for relative positioning.

## Built-in middleware keys

| Key                            | Middleware                             | Added by                                                |
| ------------------------------ | -------------------------------------- | ------------------------------------------------------- |
| `"Instrumentation"`            | `MediatorDiagnosticMiddleware`         | Always present (added automatically by `AddMediator()`) |
| `"EntityFrameworkTransaction"` | `EntityFrameworkTransactionMiddleware` | `UseEntityFrameworkTransactions<TContext>()`            |

# Pipeline execution order

Middleware executes in registration order. The first registered middleware becomes the outermost layer - it runs first on the way in and last on the way out.

```text
Registered: [Instrumentation, Logging, Validation, Transaction]

Instrumentation         <- outermost (runs first)
  Logging
    Validation
      Transaction
        Handler         <- innermost
      Transaction returns
    Validation returns
  Logging returns
Instrumentation returns <- runs last on the way out
```

The `Instrumentation` middleware is always present as the first entry because `AddMediator()` adds it automatically. Your middleware registered via `Use()` follows in the order you call it.

# Notification publish modes

When a notification has multiple handlers, the **notification publish mode** controls how they are invoked. Configure it via `MediatorOptions`:

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Sequential)
    .AddCatalog();
```

| Mode         | Behavior                                                       | Default |
| ------------ | -------------------------------------------------------------- | ------- |
| `Sequential` | Invokes handler pipelines one at a time, in registration order | Yes     |
| `Concurrent` | Invokes all handler pipelines concurrently with `Task.WhenAll` | No      |

## Sequential mode (default)

Handlers execute one after another. If a handler throws, subsequent handlers do not execute and the exception propagates to the caller.

This is the right choice when handlers have ordering dependencies or when you want fail-fast behavior.

## Concurrent mode

All handlers execute in parallel. If any handler throws, the remaining handlers still run to completion and all exceptions are collected into an `AggregateException`.

```csharp
builder.Services
    .AddMediator()
    .ConfigureOptions(o => o.NotificationPublishMode = NotificationPublishMode.Concurrent)
    .AddCatalog();
```

> **Warning:** In concurrent mode, all handler pipelines share the same scoped `IServiceProvider`. Scoped services such as `DbContext` are not thread-safe and must not be used concurrently across handlers. If your notification handlers need scoped services, use `Sequential` mode or create a new scope inside each handler.

## Per-handler middleware pipelines

Each notification handler gets its own independently compiled middleware pipeline. When middleware is registered on the mediator, it wraps each notification handler individually - not the notification dispatch as a whole.

```text
PublishAsync(OrderPlacedNotification)
  ├── Pipeline for SendOrderConfirmationEmail:
  │     Instrumentation -> Logging -> SendOrderConfirmationEmail
  │
  └── Pipeline for UpdateAnalyticsDashboard:
        Instrumentation -> Logging -> UpdateAnalyticsDashboard
```

This means middleware like logging or exception handling runs independently around each handler. If middleware around one handler modifies `ctx.Result` or catches an exception, it does not affect the other handler's pipeline.

# Entity Framework Core transactions

The `Mocha.EntityFrameworkCore` package provides middleware that wraps command handlers in a database transaction. Install the package and call `UseEntityFrameworkTransactions`:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()
    .UseEntityFrameworkTransactions<AppDbContext>();
```

The middleware (key: `"EntityFrameworkTransaction"`):

1. Checks at compile time whether the pipeline is for a command. Queries and notifications are excluded by default - the middleware is not present in their pipelines at all.
2. Begins a database transaction
3. Calls the next middleware or handler
4. Commits the transaction on success
5. Rolls back on any exception

Your command handlers are responsible for calling `SaveChangesAsync` to persist their changes. The middleware handles the transaction lifecycle - your handlers do not need to call `BeginTransactionAsync`, `CommitAsync`, or `RollbackAsync`.

## Customizing transaction scope

To override which messages get a transaction, provide a `ShouldCreateTransaction` predicate:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()
    .UseEntityFrameworkTransactions<AppDbContext>(options =>
    {
        options.ShouldCreateTransaction = context =>
        {
            // Wrap everything except this specific query
            return context.MessageType != typeof(GetCachedReportQuery);
        };
    });
```

When `ShouldCreateTransaction` is set, the compile-time elimination for queries and notifications is disabled - the middleware is included in every pipeline and the predicate runs at dispatch time instead.

The `ShouldCreateTransaction` delegate receives the `IMediatorContext`, giving you access to the message type and instance for fine-grained control.

# Instrumentation and observability

The `MediatorDiagnosticMiddleware` (key: `"Instrumentation"`) is always present in the pipeline. By default it uses a no-op listener. To activate OpenTelemetry-compatible tracing, call `AddInstrumentation`:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()
    .AddInstrumentation();
```

This registers the `ActivityMediatorDiagnosticListener`, which follows the [OpenTelemetry messaging semantic conventions](https://opentelemetry.io/docs/specs/semconv/messaging/messaging-spans/):

- Creates an [`Activity`](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/distributed-tracing-concepts) (OpenTelemetry span) named `"{MessageTypeName} send"` for commands/queries or `"{MessageTypeName} publish"` for notifications
- Tags the span with `messaging.system` = `"mocha.mediator"`, `messaging.operation.type` = `"send"` or `"publish"`, and `messaging.message.type` = the message type name
- Sets `ActivityStatusCode.Ok` on success
- On error: adds an `exception` event with `exception.type` and `exception.message` tags, and sets `ActivityStatusCode.Error`

Configure your OpenTelemetry exporter to collect from the `Mocha.Mediator` source:

```csharp
builder.Services.AddOpenTelemetry()
    .WithTracing(t => t.AddSource("Mocha.Mediator"))
    .WithMetrics(m => m.AddMeter("Mocha.Mediator"));
```

## Custom diagnostic event listeners

To add your own instrumentation alongside or instead of the built-in listener, extend `MediatorDiagnosticEventListener`:

```csharp
public sealed class SlowMessageListener : MediatorDiagnosticEventListener
{
    public override IDisposable Execute(
        Type messageType, Type responseType, object message)
    {
        return new TimingScope(messageType);
    }

    public override void ExecutionError(
        Type messageType, Type responseType, object message, Exception exception)
    {
        // log or alert on errors
    }

    private sealed class TimingScope(Type messageType) : IDisposable
    {
        private readonly long _start = Stopwatch.GetTimestamp();

        public void Dispose()
        {
            var elapsed = Stopwatch.GetElapsedTime(_start);
            if (elapsed > TimeSpan.FromSeconds(1))
                Console.WriteLine($"Slow message: {messageType.Name} took {elapsed}");
        }
    }
}
```

Register it:

```csharp
builder.Services
    .AddMediator()
    .AddCatalog()
    .AddDiagnosticEventListener<SlowMessageListener>();
```

Multiple listeners can be registered. They all receive every diagnostic event in registration order.

# Troubleshooting

## Middleware does not run for a specific message type

If your middleware factory returns `next` for that message type (via compile-time filtering), the middleware is excluded from the pipeline entirely. Check your `IsCommand()`, `IsQuery()`, `IsNotification()`, or `IsMessageAssignableTo<T>()` conditions. The filtering runs once at startup, so you will not see any runtime indication that the middleware was skipped.

## Middleware runs in the wrong order

Middleware executes in registration order (first registered = outermost). Use `Use(config, before: "key")` or `Use(config, after: "key")` to control placement relative to other middleware. Check that the middleware you are referencing has a `Key` set in its `MediatorMiddlewareConfiguration`.

## Entity Framework transactions do not wrap queries

This is the default behavior. The `EntityFrameworkTransactionMiddleware` excludes queries and notifications at compile time. To include specific queries, set `ShouldCreateTransaction` in the options. Note that setting this predicate disables compile-time elimination - the middleware will be included in all pipelines and the predicate runs at dispatch time.

## No OpenTelemetry traces appear

The `MediatorDiagnosticMiddleware` is always present, but it uses a no-op listener by default. You must call `.AddInstrumentation()` to register the `ActivityMediatorDiagnosticListener`. You also need to configure your OpenTelemetry SDK to collect from the `Mocha.Mediator` source via `.AddSource("Mocha.Mediator")`.

## Services resolved in the factory vs. at runtime

Services resolved from `factoryCtx.Services` in the middleware factory are resolved once at startup from the mediator's internal service provider. Use this for singletons like `ILoggerFactory`. To resolve scoped services (like `DbContext`), use `ctx.Services` inside the runtime delegate instead.

## Notification handler throws but other handlers do not run

In `Sequential` mode (the default), the first handler exception stops execution of subsequent handlers. If you need all handlers to run regardless of failures, switch to `Concurrent` mode. In `Concurrent` mode, all handlers run to completion and exceptions are aggregated into an `AggregateException`.

## Scoped service exceptions in concurrent notifications

When using `NotificationPublishMode.Concurrent`, all handler pipelines execute in parallel but share the same scoped `IServiceProvider`. Scoped services like `DbContext` are not thread-safe. You will see race conditions or `ObjectDisposedException` if multiple handlers access the same scoped service concurrently. Switch to `Sequential` mode or create a new `IServiceScope` inside handlers that need their own scoped services.

> **Full demo:** The [Demo application](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/Demo) uses `UseEntityFrameworkTransactions` and `AddInstrumentation` alongside the mediator and message bus in a complete e-commerce system.

# Next steps

- **Mediator overview:** [Overview](/docs/mocha/v16/mediator) - messages, handlers, dispatching, and registration.
- **Message bus middleware:** [Middleware & Pipelines](/docs/mocha/v16/middleware-and-pipelines) - the message bus has its own three-layer pipeline (dispatch, receive, consume) using the same middleware model.
- **Cross service boundaries:** [Messaging Patterns](/docs/mocha/v16/messaging-patterns) - when your commands need to reach another service, switch to the message bus.
- **Coordinate workflows:** [Sagas](/docs/mocha/v16/sagas) - orchestrate multi-step processes across services.

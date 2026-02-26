---
title: "Middleware and Pipelines"
description: "Understand how Mocha's three middleware pipelines process messages. Learn which pipeline to target, how to write custom middleware, and how to control execution order."
---

Mocha's pipeline implements the [Pipes and Filters](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PipesAndFilters.html) pattern from Enterprise Integration Patterns. Every message flows through a chain of middleware before reaching your handler. Each filter in the chain can inspect, modify, short-circuit, or observe the message — then pass control to the next filter.

If you have used middleware in [ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/), the mental model is the same. Middleware wraps middleware in nested layers, like an onion. Each layer runs code before calling the next layer, and optionally runs more code after it returns.

Most of the time, the defaults work and you never configure middleware directly. This page is for when you need to add cross-cutting behavior — a database transaction per handler, tenant context from headers, custom rate limiting — or when you want to understand how the built-in reliability and observability features fit into the pipeline.

# How pipelines work

## The nesting model

Within a single pipeline, each middleware wraps the next. Execution flows inward through each layer, then unwinds outward as each layer returns:

```text
Message arrives
  → MiddlewareA.InvokeAsync
      → MiddlewareB.InvokeAsync
          → MiddlewareC.InvokeAsync
              → [handler or next pipeline]
          ← return (or exception propagates back here)
      ← return (or exception caught/rethrown here)
  ← return
```

A middleware that does not call `next(context)` short-circuits the pipeline — everything after it is skipped. The expiry middleware uses this to drop stale messages without invoking any downstream code. A middleware that wraps `next` in a try/catch can intercept exceptions from downstream — the fault middleware uses this to route failed messages to an error endpoint.

This wrapping model is why registration order matters: the first middleware registered becomes the outermost layer. It runs first on the way in, and last on the way out.

## The three pipelines

Mocha has three separate pipelines with different context types and different purposes:

```text
PublishAsync / SendAsync / RequestAsync
        │
        ▼
┌─────────────────────────┐
│   Dispatch Pipeline      │
│  Instrumentation         │
│  Serialization           │
│  → Transport sends       │
└─────────────────────────┘
        │
        ▼  (wire / InMemory)
        │
┌─────────────────────────┐
│   Receive Pipeline       │
│  TransportCircuitBreaker │
│  ConcurrencyLimiter      │
│  ReceiveInstrumentation  │
│  DeadLetter              │
│  Fault                   │
│  CircuitBreaker          │
│  Expiry                  │
│  MessageTypeSelection    │
│  Routing                 │
└─────────────────────────┘
        │
        ▼
┌─────────────────────────┐
│   Consumer Pipeline      │
│  Instrumentation         │
│  → Your Handler          │
└─────────────────────────┘
```

Each pipeline operates on a different context:

- **Dispatch** (`IDispatchContext`) — operates on the unserialized message object. Has the CLR message, headers, and destination address. Serialization happens at the end.
- **Receive** (`IReceiveContext`) — operates on the raw envelope from the transport. Has the serialized body, headers, and transport metadata. Message type resolution and routing happen here.
- **Consumer** (`IConsumeContext`) — operates on the deserialized message inside a specific consumer. Has the typed message, envelope metadata, and scoped services.

In a distributed system, the dispatch and receive pipelines run in different processes. With the InMemory transport, they run in the same process.

# Which pipeline should I target?

| Use case                                           | Pipeline                             |
| -------------------------------------------------- | ------------------------------------ |
| Add a header to every outgoing message             | Dispatch                             |
| Validate messages before sending                   | Dispatch                             |
| Rate-limit incoming messages at the endpoint level | Receive                              |
| Database transaction wrapping every handler        | Consumer                             |
| Time individual handler execution                  | Consumer                             |
| Extract tenant context from message headers        | Receive (before Routing) or Consumer |

# Consumer middleware

Consumer middleware wraps your handler execution. It is the most common customization point — use it for cross-cutting concerns that apply to every handler: database transactions, validation, timing, or tenant context resolution.

## Database unit-of-work example

A database transaction that commits on success and rolls back on failure is the canonical consumer middleware use case:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mocha;
using Mocha.Middlewares;

internal sealed class UnitOfWorkConsumerMiddleware
{
    public async ValueTask InvokeAsync(
        IConsumeContext context,
        ConsumerDelegate next)
    {
        // Resolve DbContext from the per-message DI scope
        var db = context.Services.GetRequiredService<AppDbContext>();

        await using var tx = await db.Database.BeginTransactionAsync();
        try
        {
            await next(context);       // Run the handler
            await tx.CommitAsync();    // Commit on success
        }
        catch
        {
            await tx.RollbackAsync();  // Roll back on any handler exception
            throw;                     // Re-throw so fault middleware can route the message
        }
    }

    public static ConsumerMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var middleware = new UnitOfWorkConsumerMiddleware();
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "UnitOfWork");
}
```

Register the middleware during bus configuration:

```csharp
builder.Services
    .AddMessageBus(bus =>
    {
        bus.UseConsume(UnitOfWorkConsumerMiddleware.Create());
    })
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();
```

The re-throw after `RollbackAsync` is intentional. The fault middleware further up the receive pipeline catches handler exceptions and routes the message to an error endpoint. If you swallow the exception, the message is silently dropped.

# Receive middleware

Receive middleware wraps the entire receive pipeline for a message, before message type resolution and routing. Use it for concerns that apply to raw envelopes: rate limiting, logging of all incoming messages, or metrics about transport-level behavior.

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Mocha;
using Mocha.Middlewares;

internal sealed class LoggingReceiveMiddleware(
    ILogger<LoggingReceiveMiddleware> logger)
{
    public async ValueTask InvokeAsync(
        IReceiveContext context,
        ReceiveDelegate next)
    {
        logger.LogInformation(
            "Receiving {MessageId}",
            context.MessageId);

        await next(context);

        logger.LogInformation(
            "Finished {MessageId}",
            context.MessageId);
    }

    public static ReceiveMiddlewareConfiguration Create()
        => new(
            static (context, next) =>
            {
                var logger = context.Services
                    .GetRequiredService<ILogger<LoggingReceiveMiddleware>>();
                var middleware = new LoggingReceiveMiddleware(logger);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "Logging");
}
```

Register after instrumentation so telemetry spans are already open when your middleware runs:

```csharp
builder.Services
    .AddMessageBus(bus =>
    {
        // Insert after ReceiveInstrumentation, before DeadLetter
        bus.AppendReceive(
            "ReceiveInstrumentation",
            LoggingReceiveMiddleware.Create());
    })
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();
```

`AppendReceive("ReceiveInstrumentation", ...)` places your middleware immediately after the named middleware. The receive pipeline then executes: TransportCircuitBreaker, ConcurrencyLimiter, ReceiveInstrumentation, **Logging**, DeadLetter, Fault, and so on.

# Dispatch middleware

Dispatch middleware wraps the outbound path. Use it for concerns on every outgoing message: adding headers, enforcing message schemas, or enriching with correlation context.

```csharp
internal sealed class TenantDispatchMiddleware
{
    private readonly string _tenantId;

    public TenantDispatchMiddleware(string tenantId)
    {
        _tenantId = tenantId;
    }

    public async ValueTask InvokeAsync(
        IDispatchContext context,
        DispatchDelegate next)
    {
        // Stamp every outgoing message with the tenant identifier
        context.Headers.Set("x-tenant", _tenantId);

        await next(context);
    }

    public static DispatchMiddlewareConfiguration Create(string tenantId)
        => new(
            (context, next) =>
            {
                var middleware = new TenantDispatchMiddleware(tenantId);
                return ctx => middleware.InvokeAsync(ctx, next);
            },
            "TenantDispatch");
}
```

Register before all other dispatch middleware so every outgoing message carries the header:

```csharp
builder.Services
    .AddMessageBus(bus =>
    {
        bus.PrependDispatch(TenantDispatchMiddleware.Create("acme"));
    })
    .AddEventHandler<OrderPlacedHandler>()
    .AddInMemory();
```

# The factory pattern and DI scoping

The factory lambda in `ReceiveMiddlewareConfiguration`, `ConsumerMiddlewareConfiguration`, and `DispatchMiddlewareConfiguration` runs once per message. Services resolved inside the lambda come from the request-scoped DI container for that message.

> **Avoid capturing services outside the lambda.** If you resolve a service outside the factory lambda, it is shared across all messages and behaves as a singleton — even if it was registered as scoped. This breaks scoped services like `DbContext`:
>
> ```csharp
> // Wrong: DbContext captured as a singleton
> var db = serviceProvider.GetRequiredService<AppDbContext>();
> return new ConsumerMiddlewareConfiguration(
>     (context, next) =>
>         ctx => middleware.InvokeAsync(ctx, next, db), // db is shared!
>     "UnitOfWork");
>
> // Correct: resolve inside the per-message factory
> return new ConsumerMiddlewareConfiguration(
>     static (context, next) =>
>     {
>         var db = context.Services.GetRequiredService<AppDbContext>();
>         var middleware = new UnitOfWorkConsumerMiddleware();
>         return ctx => middleware.InvokeAsync(ctx, next);
>     },
>     "UnitOfWork");
> ```

# Control middleware ordering

All ordering methods are available on `IMessageBusBuilder` for each pipeline type:

| Method                         | Pipeline | Behavior                                        |
| ------------------------------ | -------- | ----------------------------------------------- |
| `UseReceive(config)`           | Receive  | Add after built-in defaults                     |
| `PrependReceive(config)`       | Receive  | Insert at the beginning                         |
| `AppendReceive(config)`        | Receive  | Append at the end                               |
| `PrependReceive(key, config)`  | Receive  | Insert before the middleware with the given key |
| `AppendReceive(key, config)`   | Receive  | Insert after the middleware with the given key  |
| `UseDispatch(config)`          | Dispatch | Add after built-in defaults                     |
| `PrependDispatch(config)`      | Dispatch | Insert at the beginning                         |
| `AppendDispatch(config)`       | Dispatch | Append at the end                               |
| `PrependDispatch(key, config)` | Dispatch | Insert before the middleware with the given key |
| `AppendDispatch(key, config)`  | Dispatch | Insert after the middleware with the given key  |
| `UseConsume(config)`           | Consumer | Add after built-in defaults                     |
| `PrependConsume(config)`       | Consumer | Insert at the beginning                         |
| `AppendConsume(config)`        | Consumer | Append at the end                               |
| `PrependConsume(key, config)`  | Consumer | Insert before the middleware with the given key |
| `AppendConsume(key, config)`   | Consumer | Insert after the middleware with the given key  |

Middleware is compiled once at startup into a single delegate chain. Register all middleware during bus configuration, before the service provider is built. Middleware added after the bus starts has no effect.

Middleware can also be registered at transport or endpoint scope. Bus-level middleware applies to all transports and endpoints. Transport-level middleware applies to all endpoints on that transport. Endpoint-level middleware applies to a single endpoint. The most specific scope wins. This is the same scope hierarchy described in [Routing and Endpoints](/docs/mocha/v1/routing-and-endpoints).

# Built-in middleware and feature pages

The built-in middleware in the receive pipeline implements the reliability and observability features described on their own pages:

- The `CircuitBreaker` and `DeadLetter` middleware implement the circuit breaker and dead-letter behaviors described in [Reliability](/docs/mocha/v1/reliability). Use `PrependReceive` and `AppendReceive` with their keys to position your middleware relative to them.
- The `ReceiveInstrumentation` middleware generates the OpenTelemetry spans and metrics described in [Observability](/docs/mocha/v1/observability). Place logging or correlation middleware after `ReceiveInstrumentation` so telemetry context is available.

# Next steps

The pipeline handles failures automatically. Learn how circuit breaking, dead-letter routing, and the transactional outbox work in [Reliability](/docs/mocha/v1/reliability).

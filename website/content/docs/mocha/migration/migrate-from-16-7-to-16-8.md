---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: dispatch middleware ordering, delivery of stored messages, keyed outbox processors, and Entity Framework Core registration changes."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# API breaking changes

## `SkipScheduler()` is removed

`SkipScheduler()` and `SchedulingMiddlewareFeature` are removed. Stored scheduled messages are now delivered without running dispatch middleware again, so the scheduler no longer needs the flag. Remove calls that set it.

`SkipOutbox()` remains available for `IMessageBus` publishes and sends. A message published through `dbContext.Outbox(bus)` cannot skip the outbox, because it would be sent before the transaction commits, and `SkipOutbox()` throws an `InvalidOperationException` for it.

## `DispatchSchedulingMiddleware` is internal

`DispatchSchedulingMiddleware` is no longer public. To position your middleware relative to scheduling, anchor it to the `DispatchMiddlewares.Scheduling` key, for example `UseDispatch(config, before: DispatchMiddlewares.Scheduling.Key)`.

## `ConfigureEntityFrameworkServices` and `MessagingDbContextOptions` are internal

`ConfigureEntityFrameworkServices` and `MessagingDbContextOptions` are no longer public. To add your own services to a DbContext, use Entity Framework Core's `AddInterceptors` or `ReplaceService` when configuring the context.

## `OutboxMessage` takes a creation time

The `OutboxMessage` constructor now takes the creation time as a third argument, `OutboxMessage(Guid id, JsonDocument envelope, DateTime createdAt)`. Code that inserts outbox rows by hand passes the current UTC time.

## Resolve PostgreSQL outbox processors by DbContext type

`PostgresOutboxProcessor` is now registered only as a keyed singleton, with the registered DbContext type as its key. Replace unkeyed calls such as `GetRequiredService<PostgresOutboxProcessor>()` with:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

var processor = serviceProvider.GetRequiredKeyedService<PostgresOutboxProcessor>(
    typeof(AppDbContext));
```

The host starts an outbox worker for each registered context automatically. Each outbox context has its own relay. `IMessageBus` publishes and sends, and the replies and faults Mocha sends from handlers, use the outbox of the first context registered with `UsePostgresOutbox()`.

# Behavioral breaking changes

## Stored messages bypass middleware during delivery

Dispatch middleware runs when a message is published or sent, including scheduled messages. After commit, the outbox processor sends immediate messages directly through the transport and hands scheduled messages to the native scheduling store or configured fallback. Scheduler delivery also uses the stored envelope. Dispatch middleware does not run again during handoff, delivery, or retries.

Dispatch spans and metrics are therefore emitted when a stored (outboxed or scheduled) message is staged or stored, not when it is delivered. The delivery itself is traced by the `outbox send` and `scheduler send` spans, which record failures.

## Dispatch middleware ordering

The dispatch pipeline runs `DispatchMiddlewares.Instrumentation`, transport and application middleware, `DispatchMiddlewares.Serialization`, `DispatchMiddlewares.Scheduling`, and `OutboxDispatchMiddlewares.Outbox` when the outbox is enabled. The scheduling and outbox middleware persist the serialized message and do not call the next middleware.

- Middleware registered without an anchor now runs before serialization, so `context.Envelope` is not set yet. Change the message and headers before calling `next(context)`. Changes made after `await next(context)` do not affect the sent or stored message.
- Middleware anchored before or after `DispatchMiddlewares.Serialization.Key` runs for every message, including scheduled and outboxed messages. Use `after: DispatchMiddlewares.Serialization.Key` for middleware that works on the serialized envelope.
- The RabbitMQ routing key and Azure Service Bus message property middleware run before `DispatchMiddlewares.Serialization`, after application middleware registered without an anchor or on the bus, and keep a value whose header is already set. Middleware overrides a value by setting its header (`x-routing-key`, or a key from `AzureServiceBusMessageHeaders`) and reads the extracted values when it is anchored `after: DispatchMiddlewares.Serialization.Key`.

## `UseTransaction()` uses its own DbContext

`UseTransaction()` opens the consumer transaction on the DbContext of the `AddEntityFramework<TContext>` call it is configured on. Previously it used the context of the last `AddEntityFramework<TContext>` call.

## The outbox relay checks for messages when idle

Each outbox relay now checks the database for new messages when it has not been notified for `PollingInterval`, ten seconds by default. This delivers messages committed outside Entity Framework Core, for example by another process. Configure the interval with `UsePostgresOutbox(options => options.PollingInterval = ...)`.

# Publish and schedule with SaveChanges

Version 16.8 adds `dbContext.Outbox(bus)`, which writes published and sent messages in the same transaction as the entity changes of the context. Existing `IMessageBus.PublishAsync` calls keep their behavior. See [Publish with SaveChanges](../reliability.md#publish-with-savechanges) and [Schedule with SaveChanges](../scheduling.md#schedule-with-savechanges).

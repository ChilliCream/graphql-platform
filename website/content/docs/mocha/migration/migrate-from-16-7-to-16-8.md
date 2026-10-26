---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: dispatch middleware behavior, default outbox selection, and publishing or scheduling with SaveChanges."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# API breaking changes

## Outbox and scheduler bypass features are removed

`SkipOutbox()`, `SkipScheduler()`, `OutboxMiddlewareFeature`, and `SchedulingMiddlewareFeature` are removed. Remove calls and registrations that set these flags.

# Behavioral breaking changes

## Stored messages bypass middleware during delivery

Dispatch middleware runs when a message is published or sent, including scheduled messages. After commit, the outbox processor sends immediate messages directly through the transport and hands scheduled messages to the native scheduling store or configured fallback. Scheduler delivery also uses the stored envelope. Dispatch middleware does not run again during handoff, delivery, or retries.

The built-in ordering keys are `"Instrumentation"`, `"Scheduling"`, `"Serialization"`, and `"Outbox"` when enabled. Place enrichment middleware before `"Scheduling"`. The scheduling and outbox middleware serialize and persist their messages without invoking downstream middleware.

For a new message, `context.Envelope` is unset before serialization unless middleware supplies one. Update the message and metadata before calling `next(context)`. Changes made after `await next(context)` do not affect the stored message, including when publishing through `db.Outbox(bus)`.

## Select a default when configuring multiple outbox contexts

Direct `IMessageBus` publication now requires `UseDefaultOutbox<TContext>()` when more than one outbox context is configured; it previously used the first registration. Publication through `db.Outbox(bus)` uses that context instance and does not require a default.

## Resolve PostgreSQL outbox processors by DbContext type

`PostgresOutboxProcessor` is now registered only as a keyed singleton, with the registered DbContext type as its key. Replace unkeyed calls such as `GetRequiredService<PostgresOutboxProcessor>()` with:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Mocha.Outbox;

var processor = serviceProvider.GetRequiredKeyedService<PostgresOutboxProcessor>(
    typeof(AppDbContext));
```

The host continues to start an outbox worker for each registered context automatically.

# Publish with SaveChanges

Version 16.8 adds `db.Outbox(bus)`, which returns a `DbContextOutbox` for staging messages until that exact context saves:

```csharp
using Mocha.EntityFrameworkCore;

DbContextOutbox outbox = db.Outbox(bus);
await outbox.PublishAsync(message, cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

The save persists staged messages and entity changes in the same transaction. With EF automatic transactions enabled, no explicit transaction is needed. Existing `IMessageBus.PublishAsync` calls retain their direct outbox persistence behavior and the [transaction requirement introduced in 16.7](./migrate-from-16-6-to-16-7.md#the-postgres-outbox-requires-a-transaction).

## Use contexts created directly through a factory

The new API supports contexts resolved from DI and contexts created directly through regular or pooled factories. DbContext and outbox registrations can appear in either order:

```csharp
builder.Services
    .AddMessageBus()
    .AddEntityFramework<AppDbContext>(p => p.UsePostgresOutbox());

builder.Services.AddPooledDbContextFactory<AppDbContext>(options =>
    options.UseNpgsql(connectionString));
```

```csharp
await using var db = await factory.CreateDbContextAsync(cancellationToken);
DbContextOutbox outbox = db.Outbox(bus);

db.Orders.Add(order);

await outbox.PublishAsync(message, cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

Use the same `db.Outbox(bus)` call for scoped contexts and contexts created through regular or pooled factories. Use the returned outbox only during that context's lifetime or pooled lease.

Context instances must use the database and outbox table mapping configured for their relay. Configure a relay for every additional database or outbox table used by the application.

## Use the caller's messaging scope

`db.Outbox(bus)` runs middleware with the supplied bus's messaging services.

The active consumer's conversation and causation identifiers flow into published message metadata. Middleware can resolve caller-scoped services. You can also supply message metadata explicitly:

```csharp
await db.Outbox(bus).PublishAsync(
    message,
    new PublishOptions
    {
        Headers = new() { ["x-tenant"] = tenantId }
    },
    cancellationToken);
```

## Keep transaction ownership explicit inside handlers

Each context owns its staged messages and follows its own transaction. `UseTransaction()` manages the configured context in the handler scope; additional factory-created contexts do not automatically join that transaction. A factory-created context can commit even if the handler later fails, and a handler retry can repeat that work. Make the work idempotent or explicitly enlist the contexts in a shared transaction.

See [Publish with SaveChanges](../reliability.md#publish-with-savechanges) for retry, acceptance, and change-tracking behavior.

## Schedule with SaveChanges

Set `ScheduledTime` on the options passed to `db.Outbox(bus).PublishAsync` or `SendAsync`:

```csharp
await db.Outbox(bus).PublishAsync(
    message,
    new PublishOptions
    {
        ScheduledTime = DateTimeOffset.UtcNow.AddHours(1)
    },
    cancellationToken);

await db.SaveChangesAsync(cancellationToken);
```

The save persists scheduled messages and entity changes in the same transaction. After commit, the outbox processor hands messages to the native scheduling store or configured fallback for delivery at the scheduled time.

`AddPostgres()` registers native scheduling; it does not require `UsePostgresScheduling()` or a scheduled-message entity mapping. Transports without native scheduling, such as RabbitMQ, require the `UsePostgresScheduling()` fallback.

The outbox methods do not return a scheduling token and cannot be used with `CancelScheduledMessageAsync`. The existing `IMessageBus.SchedulePublishAsync` and `ScheduleSendAsync` APIs remain available. See [Schedule with SaveChanges](../scheduling.md#schedule-with-savechanges) for configuration and examples.

---
title: Migrate Mocha from 16.6 to 16.7
description: "Migration guide for Mocha v16.6 to v16.7: nullable dispatch timestamps, the Postgres outbox transaction requirement, EF Core outbox registration validation, and the new DbContext outbox API."
---

Update every `Mocha.*` package in the application to version 16.7 before applying these changes.

# API breaking changes

## Dispatch context SentAt is nullable

`SentAt` is now nullable (`DateTimeOffset?`) on both `IDispatchContext` and `DispatchContext`. It defaults to `null`, so code that reads it before dispatch must handle a missing timestamp.

Mocha now sets `SentAt` just before dispatching the message. It gets the current UTC time from the `TimeProvider` registered in your service collection, falling back to `TimeProvider.System` if you haven't registered one.

You can still set `SentAt` yourself. Mocha preserves a timestamp supplied on the context or envelope, with the context taking precedence if both are set.

# Behavioral breaking changes

## Configure one outbox DbContext type

Enabling the outbox through `UsePostgresOutbox()` or `UseOutboxCore()` now throws an `InvalidOperationException` during registration if a different outbox DbContext type is already registered. Previously, registration accepted multiple outbox context types.

Keep outbox configuration on one DbContext type per service collection. Other DbContext types can still be registered without an outbox. Multiple instances of the selected context type are supported. Repeating outbox registration for the same type does not duplicate its services.

## The Postgres outbox requires a transaction

`PublishAsync` and `SendAsync` through the outbox registered with `UsePostgresOutbox()` now require an open database transaction on the DbContext or an ambient `TransactionScope`. Without one, they throw an `InvalidOperationException` and no message is written. In 16.6, such a message was inserted into the outbox table immediately, outside any later `SaveChangesAsync`, so it was dispatched even when the entity changes were never saved.

Replies, faults, requests, and scheduled messages are not affected. Messages from [`DbContext.Outbox.PublishAsync`](#publish-with-savechanges) are written by `SaveChanges` and do not require a transaction.

The code paths that typically published without a transaction in 16.6 need one of these changes:

- **Consumers** get a transaction from `UseTransaction()`:

  ```diff
   builder.Services
       .AddMessageBus()
       .AddEntityFramework<AppDbContext>(p =>
       {
           p.UsePostgresOutbox();
  +        p.UseTransaction();
       })
       .AddRabbitMQ();
  ```

- **Mediator handlers** get a transaction from `UseEntityFrameworkTransactions<TContext>()`:

  ```diff
   builder.Services
       .AddMediator()
  +    .UseEntityFrameworkTransactions<AppDbContext>();
  ```

- **Other code**, such as API endpoints and background services, opens a transaction on the DbContext:

  ```diff
  +await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
  +
   db.Orders.Add(order);
   await bus.PublishAsync(new OrderPlaced(order.Id), cancellationToken);
   await db.SaveChangesAsync(cancellationToken);
  +
  +await transaction.CommitAsync(cancellationToken);
  ```

Direct bus publishes use the first DbContext registered with `UsePostgresOutbox()`. A transaction on a different DbContext does not satisfy the requirement.

To keep the 16.6 behavior, set `RequireTransaction` to `false`:

```diff
 builder.Services
     .AddMessageBus()
     .AddEntityFramework<AppDbContext>(p =>
     {
-        p.UsePostgresOutbox();
+        p.UsePostgresOutbox(o => o.RequireTransaction = false);
     })
     .AddRabbitMQ();
```

With `RequireTransaction` set to `false`, a message published outside a transaction is written to the outbox table immediately. Inside a transaction, it is written as part of that transaction.

See [Reliability](../reliability.md#transaction-requirements) for details.

# Publish with SaveChanges

Version 16.7 adds `DbContext.Outbox.PublishAsync` for staging messages until that exact context saves:

```csharp
using Mocha.EntityFrameworkCore;

await db.Outbox.PublishAsync(message, cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

The save persists staged messages and entity changes in the same transaction. Existing `IMessageBus.PublishAsync` calls retain their direct outbox persistence behavior; adopting the new API is optional.

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

db.Orders.Add(order);

await db.Outbox.PublishAsync(message, cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

Factory lifetimes are unchanged. Direct factory use does not require an additional scoped DbContext registration.

## Supply caller-specific metadata explicitly

`DbContext.Outbox` uses a private messaging service scope for each context, both inside and outside handlers. The scope is created on first publication and released when an unpooled context is disposed or a pooled context is returned to its pool. Publish middleware does not share request-scoped or handler-scoped service instances with the caller.

The active consumer's conversation and causation identifiers flow into published message metadata. Pass other caller-specific values, such as a tenant identifier, explicitly:

```csharp
await db.Outbox.PublishAsync(
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

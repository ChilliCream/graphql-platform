---
title: Migrate Mocha from 16.6 to 16.7
description: "Migration guide for Mocha v16.6 to v16.7: handle nullable dispatch timestamps and publish through the Postgres outbox with an open transaction or opt out of the transaction requirement."
---

Update every `Mocha.*` package in the application to version 16.7 before applying these changes.

# API breaking changes

## Dispatch context SentAt is nullable

`SentAt` is now nullable (`DateTimeOffset?`) on both `IDispatchContext` and `DispatchContext`. It defaults to `null`, so code that reads it before dispatch must handle a missing timestamp.

Mocha now sets `SentAt` just before dispatching the message. It gets the current UTC time from the `TimeProvider` registered in your service collection, falling back to `TimeProvider.System` if you haven't registered one.

You can still set `SentAt` yourself. Mocha preserves a timestamp supplied on the context or envelope, with the context taking precedence if both are set.

# Behavioral breaking changes

## The Postgres outbox requires a transaction

`PublishAsync` and `SendAsync` through the outbox registered with `UsePostgresOutbox()` now require an open database transaction on the DbContext or an ambient `TransactionScope`. Without one, they throw an `InvalidOperationException` and no message is written. In 16.6, such a message was inserted into the outbox table immediately, outside any later `SaveChangesAsync`, so it was dispatched even when the entity changes were never saved.

Replies, faults, requests, and scheduled messages are not affected.

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

---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: UseTransaction() opens the consumer transaction on the DbContext it is configured on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# Behavioral breaking changes

## `UseTransaction()` uses its own DbContext

`UseTransaction()` opens the consumer transaction on the DbContext of the `AddEntityFramework<TContext>` call it is configured on. Previously it used the context of the last `AddEntityFramework<TContext>` call.

Suppose you call `UseTransaction()` on `OrdersDbContext`, then register `InboxDbContext` last with only `UsePostgresInbox()`. In 16.7, a handler failure rolled back the inbox claim. In 16.8, the transaction opens on `OrdersDbContext` and the inbox claim commits before the handler runs. If the handler fails, the inbox skips redelivery of the same message to that consumer.

For consumers using `UsePostgresOutbox()`, `IMessageBus.PublishAsync` and `SendAsync` throw an `InvalidOperationException` by default unless the outbox DbContext has an open transaction or an ambient transaction is active. With `RequireTransaction` set to `false`, an outbox insert outside a transaction commits immediately, even if the handler later fails.

Calling `UseTransaction()` on both the business and outbox DbContexts opens two independent transactions. A consumer can receive the outbox message even if the business transaction later fails to commit.

To commit the inbox claim, business writes, and outbox entry together, configure them on the DbContext the handler uses for its writes. Register `UseTransaction()` before `UsePostgresInbox()` so the transaction opens before the inbox claims the message.

```csharp
builder.Services
    .AddMessageBus()
    .AddEntityFramework<OrdersDbContext>(ef =>
    {
        ef.UseTransaction();
        ef.UsePostgresInbox();
        ef.UsePostgresOutbox();
    });
```

---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: add the transaction id column and index to the Postgres outbox table, and learn how the outbox now delivers messages per transaction."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# Schema changes

## The outbox table has a transaction id column

The `OutboxMessage` entity has a new nullable property `TransactionId` and an index on (`TransactionId`, `Id`). Add a migration and apply it before the new version starts:

```shell
dotnet ef migrations add AddOutboxTransactionId
dotnet ef database update
```

The equivalent SQL for the default names is:

```sql
ALTER TABLE "outbox_messages" ADD COLUMN "transaction_id" uuid NULL;
CREATE INDEX "ix_outbox_messages_transaction_id" ON "outbox_messages" ("transaction_id", "id");
```

The previous version runs against the migrated table, so migrate first and deploy second.

Rows written before the upgrade have no transaction id and are delivered one by one.

While instances of the previous version still run, they can deliver messages of a transaction out of order.

Applications that use `EnsureCreated` on an existing database must add the column and index with the SQL above.

# Behavioral changes

## The outbox delivers messages per transaction

A failed message now holds back the later messages of the same transaction. Other transactions are not affected.

See [Reliability](../reliability.md#how-the-outbox-processor-works) for details.

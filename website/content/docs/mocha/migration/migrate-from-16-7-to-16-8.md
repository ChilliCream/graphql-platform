---
title: Migrate Mocha from 16.7 to 16.8
description: "Migration guide for Mocha v16.7 to v16.8: UseTransaction() opens the consumer transaction on the DbContext it is configured on."
---

Update every `Mocha.*` package in the application to version 16.8 before applying these changes.

# Behavioral breaking changes

## `UseTransaction()` uses its own DbContext

`UseTransaction()` opens the consumer transaction on the DbContext of the `AddEntityFramework<TContext>` call it is configured on. Previously it used the context of the last `AddEntityFramework<TContext>` call.

If `UsePostgresOutbox()` is configured on a different DbContext than `UseTransaction()`, messages that a consumer publishes or sends through `IMessageBus` are not written in the consumer transaction: `PublishAsync` and `SendAsync` throw an `InvalidOperationException`, or, with `RequireTransaction` set to `false`, write the message immediately, so it is sent even when the handler fails. Call `UseTransaction()` on the DbContext that has the outbox.

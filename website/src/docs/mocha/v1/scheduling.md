---
title: "Scheduling"
description: "Schedule messages for future delivery in Mocha using absolute times or relative delays, with durable Postgres persistence or in-memory scheduling for development."
---

# Scheduling

Sometimes a message should not be delivered right now. A welcome email goes out 30 minutes after signup. A payment retry fires 24 hours after the first failure. A saga timeout triggers if no response arrives within 5 minutes. Scheduling lets you hand a message to the bus with a future delivery time, and the infrastructure takes care of the rest.

```csharp
await bus.SchedulePublishAsync(
    new SendWelcomeEmail { UserId = userId },
    DateTimeOffset.UtcNow.AddMinutes(30),
    cancellationToken);
```

The call returns immediately. The message is persisted and delivered when the scheduled time arrives.

# Schedule a message

Mocha provides convenience extension methods on `IMessageBus` for scheduling with an absolute `DateTimeOffset`.

## Schedule with an absolute time

Use a `DateTimeOffset` when you know the exact delivery time:

```csharp
var scheduledTime = DateTimeOffset.UtcNow.AddHours(24);

// Schedule a publish (fan-out to all subscribers)
await bus.SchedulePublishAsync(
    new PaymentRetryEvent { OrderId = orderId },
    scheduledTime,
    cancellationToken);

// Schedule a send (directed to a single handler)
await bus.ScheduleSendAsync(
    new CleanupExpiredSessionsCommand { CutoffTime = cutoff },
    scheduledTime,
    cancellationToken);
```

# Schedule with options

The convenience methods are wrappers around `PublishOptions` and `SendOptions`. If you need to combine scheduling with other options like expiration or custom headers, set `ScheduledTime` directly on the options struct:

```csharp
await bus.PublishAsync(
    new PaymentRetryEvent { OrderId = orderId },
    new PublishOptions
    {
        ScheduledTime = DateTimeOffset.UtcNow.AddHours(24),
        ExpirationTime = DateTimeOffset.UtcNow.AddHours(48),
        Headers = new Dictionary<string, object?> { ["priority"] = "high" }
    },
    cancellationToken);
```

```csharp
await bus.SendAsync(
    new RetryPaymentCommand { PaymentId = paymentId },
    new SendOptions
    {
        ScheduledTime = DateTimeOffset.UtcNow.AddMinutes(30),
        ExpirationTime = DateTimeOffset.UtcNow.AddHours(1)
    },
    cancellationToken);
```

This gives full control over the dispatch options while still routing through the scheduling middleware.

# Set up store-based scheduling for RabbitMQ

The InMemory and PostgreSQL transports handle scheduling natively with no extra setup. RabbitMQ does not support native scheduling, so you need to configure a Postgres-backed message store that persists scheduled messages and dispatches them through a background worker.

**1. Add the NuGet packages.**

```bash
dotnet add package Mocha.EntityFrameworkCore
dotnet add package Mocha.EntityFrameworkCore.Postgres
```

**2. Add the `ScheduledMessage` entity to your DbContext model.**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.AddPostgresScheduledMessages();
}
```

This maps the `ScheduledMessage` entity to a `scheduled_messages` table with columns for the envelope, scheduled time, retry count, and error tracking.

**3. Register the scheduling services.**

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<PaymentRetryHandler>()
    .AddEntityFramework<AppDbContext>(p =>
    {
        p.UsePostgresScheduling();
    })
    .AddPostgres(connectionString);
```

| Call                             | Purpose                                                                                                  |
| -------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `UsePostgresScheduling()`        | Registers the background worker, scheduled message store, dispatch middleware, and EF Core interceptors. |
| `AddPostgresScheduledMessages()` | Adds the `ScheduledMessage` entity configuration to the EF Core model.                                   |

`UsePostgresScheduling()` wires up the full pipeline:

- A **dispatch middleware** that intercepts outgoing messages with a `ScheduledTime` and persists them to the store instead of sending them to the transport.
- An **`IScheduledMessageStore`** implementation that writes scheduled message rows using direct Npgsql inserts within the current EF Core transaction.
- A **background worker** that continuously polls for due messages and dispatches them through the bus.
- **EF Core interceptors** that signal the scheduler when `SaveChanges` or a transaction commit occurs, enabling low-latency wake-up.

**4. Create the database migration.**

After adding the model configuration, generate and apply an EF Core migration:

```bash
dotnet ef migrations add AddScheduledMessages
dotnet ef database update
```

# Transport scheduling behavior

Each transport handles scheduling differently. The dispatch scheduling middleware adapts automatically based on what the transport supports.

| Transport  | Scheduling type                       | Durability                   | Setup required                            |
| ---------- | ------------------------------------- | ---------------------------- | ----------------------------------------- |
| InMemory   | Native (in-process scheduler)         | Non-durable, lost on restart | None                                      |
| PostgreSQL | Native (scheduled_time column)        | Durable, survives restarts   | None                                      |
| RabbitMQ   | Store-based (via Postgres middleware) | Durable with Postgres store  | `UsePostgresScheduling()` + EF Core model |

**InMemory:** The transport schedules messages natively using an internal scheduler. Messages scheduled for a time in the past are delivered immediately. Scheduled messages are lost if the process restarts.

**PostgreSQL:** The transport handles scheduling natively. When you set `ScheduledTime`, the transport writes a `scheduled_time` column alongside the message. Messages are only delivered to consumers after the scheduled time has passed. No additional setup is required beyond the standard [PostgreSQL transport configuration](/docs/mocha/v1/transports/postgres).

**RabbitMQ:** RabbitMQ does not support native message scheduling. To enable scheduling, register `UsePostgresScheduling()` with an EF Core DbContext. The dispatch middleware intercepts scheduled messages before they reach the RabbitMQ transport and persists them to a Postgres `scheduled_messages` table. A background worker dispatches them at the scheduled time, routing through the RabbitMQ transport.

## Retry behavior

If a scheduled message fails to dispatch, the scheduler retries with exponential backoff. Each failed attempt increases the wait time before the next retry. After 10 attempts (the default `max_attempts`), the message is no longer eligible for dispatch. You can inspect failed messages by querying the `scheduled_messages` table and checking the `last_error` column.

## Multiple service instances

When multiple instances of your service are running, each scheduled message is processed by exactly one instance. There is no risk of duplicate delivery from the scheduler.

## Outbox integration

When both the transactional outbox and scheduling are configured, scheduled messages participate in the transaction correctly. The scheduling middleware runs in the dispatch pipeline before the outbox middleware. Messages with a `ScheduledTime` are intercepted by the scheduler and never reach the outbox. Messages dispatched by the background worker skip both the scheduler and the outbox, going directly to the transport. See [Reliability](/docs/mocha/v1/reliability) for outbox configuration.

# Schedule messages in sagas

Saga transitions and lifecycle actions support scheduled message dispatch through dedicated extension methods. This is useful for saga timeouts, reminder patterns, and delayed side effects.

## Schedule in a transition

```csharp
public class OrderSagaConfiguration : SagaConfiguration<OrderState>
{
    public override void Configure()
    {
        x.Initially()
            .OnEvent<OrderPlacedEvent>()
            .StateFactory(_ => new OrderState())
            .ScheduledPublish(
                TimeSpan.FromMinutes(30),
                state => new OrderReminderEvent { OrderId = state.OrderId })
            .TransitionTo("AwaitingPayment");

        x.During("AwaitingPayment")
            .OnEvent<PaymentReceivedEvent>()
            .ScheduledSend(
                TimeSpan.FromHours(1),
                state => new GenerateInvoiceCommand { OrderId = state.OrderId })
            .TransitionTo("Completed");
    }
}
```

## Schedule in a lifecycle action

Lifecycle descriptors (actions that run on saga creation, completion, or finalization) also support scheduling:

```csharp
x.WhenCompleted()
    .ScheduledPublish(
        TimeSpan.FromDays(7),
        state => new OrderFeedbackRequestEvent { OrderId = state.OrderId });
```

Both `ScheduledPublish` and `ScheduledSend` are available on `ISagaTransitionDescriptor` and `ISagaLifeCycleDescriptor`. The factory receives the current saga state and returns the message to schedule.

See [Sagas](/docs/mocha/v1/sagas) for the full saga configuration guide.

# API reference

## Extension methods on `IMessageBus`

| Method                    | Parameters                                                           | Description                                           |
| ------------------------- | -------------------------------------------------------------------- | ----------------------------------------------------- |
| `SchedulePublishAsync<T>` | `T message, DateTimeOffset scheduledTime, CancellationToken ct`      | Publishes a message for delivery at an absolute time. |
| `ScheduleSendAsync`       | `object message, DateTimeOffset scheduledTime, CancellationToken ct` | Sends a message for delivery at an absolute time.     |

All methods return `ValueTask` and complete when the message has been handed to the scheduling infrastructure.

## Scheduling properties on options

| Struct           | Property        | Type              | Default | Description                                               |
| ---------------- | --------------- | ----------------- | ------- | --------------------------------------------------------- |
| `PublishOptions` | `ScheduledTime` | `DateTimeOffset?` | `null`  | Scheduled delivery time. `null` means immediate delivery. |
| `SendOptions`    | `ScheduledTime` | `DateTimeOffset?` | `null`  | Scheduled delivery time. `null` means immediate delivery. |

`PublishOptions` and `SendOptions` have additional properties for expiration, headers, and other dispatch behavior. `ScheduledTime` can be combined with any of them.

## Saga extensions

| Method             | Available on                                            | Parameters                                       | Description                                 |
| ------------------ | ------------------------------------------------------- | ------------------------------------------------ | ------------------------------------------- |
| `ScheduledPublish` | `ISagaTransitionDescriptor`, `ISagaLifeCycleDescriptor` | `TimeSpan delay, Func<TState, TMessage> factory` | Publishes a message with a scheduled delay. |
| `ScheduledSend`    | `ISagaTransitionDescriptor`, `ISagaLifeCycleDescriptor` | `TimeSpan delay, Func<TState, TMessage> factory` | Sends a message with a scheduled delay.     |

## `ScheduledMessage` entity columns

| Column           | Type        | Description                                                           |
| ---------------- | ----------- | --------------------------------------------------------------------- |
| `id`             | `uuid`      | Primary key.                                                          |
| `envelope`       | `json`      | Serialized message envelope with headers and payload.                 |
| `scheduled_time` | `timestamp` | UTC time when the message becomes eligible for dispatch.              |
| `times_sent`     | `integer`   | Number of dispatch attempts.                                          |
| `max_attempts`   | `integer`   | Maximum dispatch attempts before the message is dropped. Default: 10. |
| `last_error`     | `jsonb`     | Last dispatch error (exception type, message, stack trace).           |
| `created_at`     | `timestamp` | UTC time when the scheduled message was created.                      |

## EF Core model builder

| Method                                        | Description                                                                              |
| --------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `modelBuilder.AddPostgresScheduledMessages()` | Applies the `ScheduledMessage` entity configuration with default table and column names. |

## Scheduling service registration

| Method                    | Description                                                                                                 |
| ------------------------- | ----------------------------------------------------------------------------------------------------------- |
| `UsePostgresScheduling()` | Registers the Postgres scheduling pipeline: store, dispatcher, background worker, and EF Core interceptors. |

# Troubleshooting

**Scheduled messages are not being delivered.**
Check that the background worker is running. Look for `Scheduler sleeping until ...` log entries at `Information` level. If there are no log entries, verify that `UsePostgresScheduling()` is registered in your service configuration. For InMemory transport, no additional setup is needed.

**Messages are delivered immediately instead of at the scheduled time.**
Messages scheduled for a time in the past are dispatched immediately. Verify that your `ScheduledTime` is in the future.

**"Could not deserialize message body" errors in logs.**
The dispatcher could not parse the stored envelope. This can happen if the message type was renamed or removed after the message was scheduled. The dispatcher drops messages it cannot deserialize and logs at `Critical` level.

**Scheduled messages fail repeatedly.**
The dispatcher records each failure in the `last_error` column and retries with exponential backoff. After 10 attempts, the message is no longer eligible for dispatch. Query the `scheduled_messages` table and inspect the `last_error` column for diagnostics:

```sql
SELECT id, scheduled_time, times_sent, last_error
FROM scheduled_messages
WHERE times_sent >= max_attempts;
```

**Multiple service instances dispatch the same message.**
This does not happen. The dispatcher uses row-level locking to ensure each message is processed by exactly one instance.

# Next steps

- [**Reliability**](/docs/mocha/v1/reliability) - Configure the transactional outbox and inbox for guaranteed delivery alongside scheduling.
- [**Sagas**](/docs/mocha/v1/sagas) - Build multi-step workflows with state machines, timeouts, and scheduled side effects.
- [**PostgreSQL Transport**](/docs/mocha/v1/transports/postgres) - Set up the Postgres transport that powers durable scheduling.
- [**Messaging Patterns**](/docs/mocha/v1/messaging-patterns) - Understand the difference between publish (fan-out) and send (point-to-point) when choosing which scheduling method to use.

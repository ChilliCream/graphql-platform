---
title: "Scheduling"
description: "Schedule messages for future delivery in Mocha using absolute times or relative delays, with durable Postgres or in-memory persistence."
---

Sometimes a message should not be delivered right now. A welcome email goes out 30 minutes after signup. A payment retry fires 24 hours after the first failure. A saga timeout triggers if no response arrives within 5 minutes. Scheduling lets you hand a message to the bus with a future delivery time, and the infrastructure takes care of the rest. If plans change, you can cancel a scheduled message before it is dispatched.

```csharp
var result = await bus.SchedulePublishAsync(
    new SendWelcomeEmail { UserId = userId },
    DateTimeOffset.UtcNow.AddMinutes(30),
    cancellationToken);

// result.Token can be used to cancel the message later
```

The call returns immediately with a `SchedulingResult`. The message is persisted and delivered when the scheduled time arrives. If you need to revoke the message before delivery, pass the token to `CancelScheduledMessageAsync`.

# Schedule a message

Mocha provides scheduling methods on `IMessageBus` for scheduling with an absolute `DateTimeOffset`.

## Schedule with an absolute time

Use a `DateTimeOffset` when you know the exact delivery time:

```csharp
var scheduledTime = DateTimeOffset.UtcNow.AddHours(24);

// Schedule a publish (fan-out to all subscribers)
var publishResult = await bus.SchedulePublishAsync(
    new PaymentRetryEvent { OrderId = orderId },
    scheduledTime,
    cancellationToken);

// Schedule a send (directed to a single handler)
var sendResult = await bus.ScheduleSendAsync(
    new CleanupExpiredSessionsCommand { CutoffTime = cutoff },
    scheduledTime,
    cancellationToken);
```

Both methods return a `SchedulingResult` with a `Token` you can use for cancellation and an `IsCancellable` flag that tells you whether cancellation is supported by the current scheduling infrastructure.

## Schedule with options

The scheduling methods also accept an options overload. If you need to combine scheduling with other options like expiration or custom headers, pass a `PublishOptions` or `SendOptions` struct:

```csharp
var result = await bus.SchedulePublishAsync(
    new PaymentRetryEvent { OrderId = orderId },
    DateTimeOffset.UtcNow.AddHours(24),
    new PublishOptions
    {
        ExpirationTime = DateTimeOffset.UtcNow.AddHours(48),
        Headers = new Dictionary<string, object?> { ["priority"] = "high" }
    },
    cancellationToken);
```

```csharp
var result = await bus.ScheduleSendAsync(
    new RetryPaymentCommand { PaymentId = paymentId },
    DateTimeOffset.UtcNow.AddMinutes(30),
    new SendOptions
    {
        ExpirationTime = DateTimeOffset.UtcNow.AddHours(1)
    },
    cancellationToken);
```

You can also set `ScheduledTime` directly on options when calling `PublishAsync` or `SendAsync`. This approach does not return a `SchedulingResult`, so you cannot cancel the message later:

```csharp
await bus.PublishAsync(
    new PaymentRetryEvent { OrderId = orderId },
    new PublishOptions
    {
        ScheduledTime = DateTimeOffset.UtcNow.AddHours(24),
        ExpirationTime = DateTimeOffset.UtcNow.AddHours(48),
    },
    cancellationToken);
```

# Cancel a scheduled message

When a scheduled message is no longer needed, cancel it before the scheduled time arrives. The `SchedulingResult` returned by `SchedulePublishAsync` and `ScheduleSendAsync` contains the token you need.

```csharp
// Schedule a payment reminder
var result = await bus.SchedulePublishAsync(
    new PaymentReminderEvent { OrderId = orderId },
    DateTimeOffset.UtcNow.AddHours(24),
    cancellationToken);

// Customer pays before the reminder fires - cancel it
var cancelled = await bus.CancelScheduledMessageAsync(
    result.Token!,
    cancellationToken);
```

`CancelScheduledMessageAsync` returns `true` if the message was successfully cancelled and `false` otherwise.

## When cancellation returns false

A `false` return does not necessarily mean something went wrong. It means the message is no longer in the store:

- **Already dispatched.** The scheduled time passed and the message was delivered. The cancellation window has closed.
- **Already cancelled.** A previous call already removed the message. Cancelling twice is safe - the second call returns `false`.
- **Token not found.** The token does not match any message in the store.

## SchedulingResult

Every call to `SchedulePublishAsync` or `ScheduleSendAsync` returns a `SchedulingResult`:

```csharp
var result = await bus.SchedulePublishAsync(message, scheduledTime, cancellationToken);

if (result.IsCancellable)
{
    // Store the token so you can cancel later
    await SaveTokenAsync(result.Token!);
}
```

| Property        | Type             | Description                                                                               |
| --------------- | ---------------- | ----------------------------------------------------------------------------------------- |
| `Token`         | `string?`        | An opaque token for cancelling this message, or `null` if cancellation is not supported.  |
| `ScheduledTime` | `DateTimeOffset` | The time at which the message is scheduled for delivery.                                  |
| `IsCancellable` | `bool`           | `true` when the scheduling infrastructure supports cancellation and a token was assigned. |

The scheduling middleware is always part of the dispatch pipeline. When you set a `ScheduledTime`, it resolves a scheduled message store for the current transport, the transport's own store first (for example the PostgreSQL or Azure Service Bus native store), then the `UsePostgresScheduling()` fallback store if one is registered. `IsCancellable` is `true` whenever a store was found and persisted the message. If no store is available for the transport, scheduling does not silently fall back to native delivery, it throws `NotSupportedException`.

## Real-world example: cancellable reminder

A common pattern is scheduling a reminder that should be revoked when the user completes the expected action.

```csharp
public class OrderService(IMessageBus bus, IOrderRepository orders)
{
    public async Task PlaceOrderAsync(Order order, CancellationToken ct)
    {
        await orders.SaveAsync(order, ct);

        // Remind the customer to pay in 24 hours
        var result = await bus.SchedulePublishAsync(
            new PaymentReminderEvent { OrderId = order.Id },
            DateTimeOffset.UtcNow.AddHours(24),
            ct);

        // Persist the token so we can cancel later
        if (result.IsCancellable)
        {
            order.ReminderToken = result.Token;
            await orders.SaveAsync(order, ct);
        }
    }

    public async Task ConfirmPaymentAsync(Guid orderId, CancellationToken ct)
    {
        var order = await orders.GetAsync(orderId, ct);

        // Payment received - cancel the reminder
        if (order.ReminderToken is not null)
        {
            await bus.CancelScheduledMessageAsync(order.ReminderToken, ct);
            order.ReminderToken = null;
            await orders.SaveAsync(order, ct);
        }
    }
}
```

# Set up store-based scheduling for RabbitMQ

The PostgreSQL and Azure Service Bus transports handle scheduling natively with no extra setup. InMemory and RabbitMQ have no scheduling store of their own, so a scheduled dispatch on either one throws `NotSupportedException` unless you configure a fallback: a Postgres-backed message store that persists scheduled messages and dispatches them through a background worker.

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

| Call                             | Purpose                                                                                                                     |
| -------------------------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `UsePostgresScheduling()`        | Registers everything needed for durable scheduling with Postgres, including the background worker and EF Core interceptors. |
| `AddPostgresScheduledMessages()` | Adds the `ScheduledMessage` entity configuration to the EF Core model.                                                      |

`UsePostgresScheduling()` sets up everything needed to persist scheduled messages in Postgres and dispatch them at the right time. Outgoing messages with a `ScheduledTime` are intercepted and written to the `scheduled_messages` table instead of being sent to the transport. A background worker continuously polls for due messages and dispatches them through the bus. EF Core interceptors signal the worker when `SaveChanges` or a transaction commit occurs, enabling low-latency wake-up.

When `UsePostgresScheduling()` is configured, `SchedulePublishAsync` and `ScheduleSendAsync` return cancellable results with tokens you can pass to `CancelScheduledMessageAsync`.

**4. Create the database migration.**

After adding the model configuration, generate and apply an EF Core migration:

```bash
dotnet ef migrations add AddScheduledMessages
dotnet ef database update
```

# Transport scheduling behavior

The PostgreSQL and Azure Service Bus transports have scheduling stores of their own. InMemory and RabbitMQ need the `UsePostgresScheduling()` fallback; without it, a scheduled dispatch on either transport throws `NotSupportedException`.

| Transport         | Scheduling type                           | Durability                      | Cancellation support                       | Setup required                            |
| ----------------- | ----------------------------------------- | ------------------------------- | ------------------------------------------ | ----------------------------------------- |
| InMemory          | None, requires `UsePostgresScheduling()` | Durable, via the fallback store | Yes, via the fallback store                | `UsePostgresScheduling()` + EF Core model |
| PostgreSQL        | Native (transport-owned store)            | Durable, survives restarts      | Yes (token prefixed `postgres-transport:`) | None                                      |
| RabbitMQ          | None, requires `UsePostgresScheduling()` | Durable, via the fallback store | Yes, via the fallback store                | `UsePostgresScheduling()` + EF Core model |
| Azure Service Bus | Native (`ScheduleMessageAsync`)           | Durable, broker-managed         | Yes (native)                               | None                                      |

**InMemory:** The transport has no scheduling store of its own. Scheduling a message throws `NotSupportedException` unless `UsePostgresScheduling()` is registered as a fallback. With the fallback configured, scheduled messages are persisted to Postgres and dispatched through a background worker, the same as RabbitMQ below.

**PostgreSQL:** The transport handles scheduling natively through its own scheduled message store, registered automatically when you call `AddPostgres()`. When you set `ScheduledTime`, the transport writes a `scheduled_time` column alongside the message instead of sending it immediately, and only delivers it to consumers after the scheduled time has passed. No additional setup is required beyond the standard [PostgreSQL transport configuration](./transports/postgres.md). Cancellation is supported - the returned token is prefixed `postgres-transport:` and can be passed to `CancelScheduledMessageAsync`.

**RabbitMQ:** RabbitMQ does not support native message scheduling. Scheduling a message throws `NotSupportedException` unless you register `UsePostgresScheduling()` with an EF Core DbContext. Once configured, scheduled messages are persisted to a Postgres `scheduled_messages` table instead of reaching the RabbitMQ transport, and a background worker dispatches them at the scheduled time, routing through the RabbitMQ transport. Cancellation is fully supported - the returned token is prefixed `postgres-scheduler:` and the `SchedulingResult` contains it for use with `CancelScheduledMessageAsync`.

**Azure Service Bus:** Azure Service Bus supports both **native scheduling and native cancellation** with no additional infrastructure. When you call `SchedulePublishAsync` or `ScheduleSendAsync`, the dispatch endpoint uses `ServiceBusSender.ScheduleMessageAsync` and the broker holds the message until the scheduled time. The returned `SchedulingResult.Token` encodes the entity path and the broker-assigned sequence number, so `CancelScheduledMessageAsync` can revoke the message via `ServiceBusSender.CancelScheduledMessageAsync` without a Postgres store, EF Core model, or background worker. `IsCancellable` is `true` for every scheduled ASB message. See the [Azure Service Bus transport](./transports/azure-service-bus.md) page for the full setup.

## Retry behavior

If a scheduled message fails to dispatch, the scheduler retries with exponential backoff. Each failed attempt increases the wait time before the next retry. After 10 attempts (the default `max_attempts`), the message is no longer eligible for dispatch. You can inspect failed messages by querying the `scheduled_messages` table and checking the `last_error` column.

## Multiple service instances

When multiple instances of your service are running, each scheduled message is processed by exactly one instance. There is no risk of duplicate delivery from the scheduler.

## Outbox integration

When both the transactional outbox and scheduling are configured, scheduled messages participate in the transaction correctly. Messages with a `ScheduledTime` are intercepted by the scheduler and never reach the outbox. Messages dispatched by the background worker skip both the scheduler and the outbox, going directly to the transport. See [Reliability](./reliability.md) for outbox configuration.

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

For automatic saga timeouts that cancel themselves on completion, see [Timeouts](./sagas.md#timeouts) in the Sagas guide.

See [Sagas](./sagas.md) for the full saga configuration guide.

# Troubleshooting

**Scheduled messages are not being delivered.**
Check that the background worker is running. Look for `Scheduler sleeping until ...` log entries at `Information` level. If there are no log entries, verify that `UsePostgresScheduling()` is registered in your service configuration. This applies to InMemory and RabbitMQ - neither has a scheduling store of its own, so both need the fallback to deliver scheduled messages at all.

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

**Cancellation returns false even though I have a valid token.**
The message was already dispatched before the cancellation request reached the store. Once the background worker picks up a message and delivers it, the row is deleted and cancellation is no longer possible. With the Azure Service Bus transport, the broker returns `MessageNotFound` once the scheduled message has been enqueued for delivery, which surfaces here as `false`. If you need a wider cancellation window, schedule messages further in the future or check `SchedulingResult.IsCancellable` to confirm the infrastructure supports cancellation.

**`SchedulePublishAsync`/`ScheduleSendAsync` throws `NotSupportedException` instead of returning a result.**
No scheduled message store is available for the current transport. `SchedulingResult.IsCancellable` is `true` whenever a call succeeds, there is no store that persists a message without returning a cancellable token. Configure `UsePostgresScheduling()` with an EF Core DbContext as a fallback, or use the PostgreSQL or [Azure Service Bus](./transports/azure-service-bus.md) transport, which registers its own store automatically.

# Next steps

- [**Reliability**](./reliability.md) - Configure the transactional outbox and inbox for guaranteed delivery alongside scheduling.
- [**Sagas**](./sagas.md) - Build multi-step workflows with state machines, timeouts, and scheduled side effects.
- [**PostgreSQL Transport**](./transports/postgres.md) - Set up the Postgres transport that powers durable scheduling.
- [**Messaging Patterns**](./messaging-patterns.md) - Understand the difference between publish (fan-out) and send (point-to-point) when choosing which scheduling method to use.

---
title: "Azure Service Bus Transport"
description: "Configure the Azure Service Bus transport in Mocha for managed cloud messaging with native scheduling, native dead-lettering, and Azure AD authentication."
---

# Azure Service Bus transport

The Azure Service Bus (ASB) transport connects Mocha to a fully managed Azure messaging namespace. It provisions queues, topics, and subscriptions automatically, dispatches publishes through topics and sends through queues, and exposes ASB-specific primitives - native scheduling with cancellation, broker-side dead-lettering with reason codes, and lock-renewal-aware acknowledgement. When you run on Azure and want a managed broker without operating the infrastructure yourself, this is the transport to use.

**When to choose Azure Service Bus over a self-hosted broker:**

- Your workload runs on Azure and you want managed messaging with SLAs, redundancy, and per-message billing.
- You need durable scheduling with the ability to cancel a scheduled message before delivery, and you do not want to deploy a Postgres scheduling store alongside your application.
- You want broker-native dead-lettering with structured reason codes that surface in Azure Monitor and Service Bus Explorer.
- You authenticate with Azure AD and want managed identities instead of shared access keys.

# Set up the Azure Service Bus transport

By the end of this section, you will have a Mocha bus connected to Azure Service Bus with automatic topology provisioning.

## Install the package

```bash
dotnet add package Mocha.Transport.AzureServiceBus
```

## Register with a connection string

The simplest setup passes a Service Bus connection string directly:

```csharp
using Mocha;
using Mocha.Transport.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddAzureServiceBus("Endpoint=sb://<namespace>.servicebus.windows.net/;SharedAccessKeyName=...;SharedAccessKey=...");

var app = builder.Build();
app.Run();
```

`.AddAzureServiceBus(connectionString)` creates a `ServiceBusClient` from the connection string, provisions topics, queues, and subscriptions for your registered handlers, and registers the scheduled-message store so `bus.CancelScheduledMessageAsync(token)` works against the broker.

## Register with a fully qualified namespace and a token credential

Use Azure AD authentication (managed identity, workload identity, or any other `TokenCredential`) instead of a shared access key:

```csharp
using Azure.Identity;
using Mocha;
using Mocha.Transport.AzureServiceBus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddAzureServiceBus(transport =>
    {
        transport.Namespace(
            "mynamespace.servicebus.windows.net",
            new DefaultAzureCredential());
    });

var app = builder.Build();
app.Run();
```

## Register with .NET Aspire

When using .NET Aspire, define a Service Bus resource in your AppHost and reference it from each service. The Aspire Service Bus emulator is convenient for local development - it requires entities to be pre-declared in `Config.json` because the emulator does not support runtime entity creation through the management API:

```csharp
// AppHost
var serviceBus = builder
    .AddAzureServiceBus("messaging")
    .RunAsEmulator(/* configure entities here */);

builder
    .AddProject<Projects.OrderService>("order-service")
    .WithReference(serviceBus)
    .WaitFor(serviceBus);
```

In each service, read the connection string from Aspire-injected configuration:

```csharp
var connectionString = builder.Configuration.GetConnectionString("messaging")!;

builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddAzureServiceBus(connectionString);
```

## Verify it works

Add an endpoint that publishes through the bus and verify the handler executes:

```csharp
app.MapPost("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlacedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-1",
        TotalAmount = 99.99m
    }, CancellationToken.None);

    return Results.Ok();
});
```

Send a POST request to `/orders` and check your application logs. You should see the handler process the event. You can also inspect the auto-provisioned topics, subscriptions, and queues in the Azure portal under your Service Bus namespace.

# How topology works

The transport maps Mocha's routing model onto Azure Service Bus topics and queues:

```mermaid
graph LR
    P[Publisher] -->|publish| T[Topic<br/>order-placed]
    T -->|subscription| Q[Queue<br/>billing.order-placed]
    Q -->|consume| C[Consumer]
```

**Events (publish/subscribe):** Each event type gets a topic. Each subscribing service gets a queue and a forwarding subscription that delivers messages from the topic into the queue. Publishing sends the message to the topic, which fans it out to all forwarded subscriber queues.

**Commands (send):** Each command type gets a queue. Sending delivers the message directly to that queue.

**Request/reply:** The transport creates a temporary reply queue per service instance (`response-{instanceId}`). The reply address is embedded in the request message so the responder knows where to send the reply.

## Default topology for handlers

Each handler-bound receive endpoint provisions three queues by convention - the main queue plus an `_error` queue (handler exceptions) and a `_skipped` queue (no matching consumer):

| Queue                         | Purpose                                                      |
| ----------------------------- | ------------------------------------------------------------ |
| `{service}.{handler}`         | Main inbound queue for the handler                           |
| `{service}.{handler}_error`   | Destination of `ReceiveFaultMiddleware` (handler exceptions) |
| `{service}.{handler}_skipped` | Destination of `ReceiveDeadLetterMiddleware` (unmatched)     |

This naming is identical across transports - see [Routing and Endpoints](/docs/mocha/v16/routing-and-endpoints) for the full convention.

# Configure transport-level defaults

You can set defaults that apply to all auto-provisioned queues, topics, and endpoints:

```csharp
builder.Services
    .AddMessageBus()
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.ConfigureDefaults(defaults =>
        {
            defaults.Queue.MaxDeliveryCount = 5;
            defaults.Queue.LockDuration = TimeSpan.FromMinutes(1);
            defaults.Queue.DefaultMessageTimeToLive = TimeSpan.FromDays(7);
            defaults.Queue.DeadLetteringOnMessageExpiration = true;
        });
    });
```

Available queue defaults:

| Property                           | Type        | Description                                                                       |
| ---------------------------------- | ----------- | --------------------------------------------------------------------------------- |
| `AutoProvision`                    | `bool?`     | Whether queues are auto-provisioned at startup                                    |
| `AutoDelete`                       | `bool?`     | Whether queues are auto-deleted when idle                                         |
| `AutoDeleteOnIdle`                 | `TimeSpan?` | Idle window before the broker may delete the queue                                |
| `LockDuration`                     | `TimeSpan?` | How long the broker holds a peek-lock on a delivered message                      |
| `MaxDeliveryCount`                 | `int?`      | Attempts before the broker dead-letters the message (`MaxDeliveryCountExceeded`)  |
| `DefaultMessageTimeToLive`         | `TimeSpan?` | TTL applied to messages that do not specify their own                             |
| `MaxSizeInMegabytes`               | `long?`     | Maximum queue size in megabytes                                                   |
| `RequiresSession`                  | `bool?`     | Whether the queue requires sessions (immutable after creation)                    |
| `EnablePartitioning`               | `bool?`     | Whether the queue is partitioned (immutable after creation)                       |
| `ForwardTo`                        | `string?`   | Auto-forward target for incoming messages                                         |
| `ForwardDeadLetteredMessagesTo`    | `string?`   | Auto-forward target for the entity's `$DeadLetterQueue`                           |
| `DeadLetteringOnMessageExpiration` | `bool?`     | Whether expired messages are moved to `$DeadLetterQueue` instead of being dropped |

Defaults never override explicitly configured values. If you call `WithMaxDeliveryCount(...)` on a specific queue, the per-queue value wins.

# Configure message properties per type

Azure Service Bus messages carry native broker properties - `SessionId`, `PartitionKey`, `ReplyToSessionId`, `To` - that drive session affinity, partition pinning, request/reply correlation, and autoforward chains. These properties depend on the payload, so Mocha configures them per message type through typed extractors that run at dispatch time. The shape mirrors [RabbitMQ routing keys](/docs/mocha/v16/transports/rabbitmq#routing-keys) - register an extractor next to the message contract and the transport wires it into the outbound `ServiceBusMessage` without bespoke per-endpoint code.

## Configure session affinity with `UseAzureServiceBusSessionId`

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddMessage<OrderEvent>(m => m
        // Session per order: every message for the same OrderId
        // is delivered in order to a single session receiver.
        .UseAzureServiceBusSessionId<OrderEvent>(msg => msg.OrderId))
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);

        // The destination queue must be created with RequiresSession = true.
        transport.DeclareQueue("orders")
            .WithRequiresSession(true);
    });
```

Use `UseAzureServiceBusSessionId<T>()` when the destination queue or subscription has `RequiresSession = true`, or when you need per-session FIFO processing. The broker routes every message with the same `SessionId` to the same session receiver, which holds an exclusive lock and consumes them in arrival order. See Microsoft Learn on [message sessions](https://learn.microsoft.com/azure/service-bus-messaging/message-sessions) for the complete FIFO and request-response patterns.

The extractor runs at dispatch time for each message. It receives the message instance and returns the session identifier string. Return `null` to publish without a `SessionId`. On a queue or topic that is both partitioned and session-aware, the broker uses the `SessionId` as the partition key - Mocha mirrors that by defaulting `PartitionKey = SessionId` when no partition-key extractor is configured, so you do not need to set both.

:::warning
Dispatching a null-session message to a `RequiresSession = true` queue fails at send time - the broker throws a `ServiceBusException` whose `Reason` is `ServiceBusFailureReason.SessionCannotBeLocked`.
:::

## Configure partitioning with `UseAzureServiceBusPartitionKey`

```csharp
builder.Services
    .AddMessageBus()
    .AddMessage<TenantEvent>(m => m
        // Pin every tenant's events to the same partition for
        // in-partition ordering on a non-session-aware entity.
        .UseAzureServiceBusPartitionKey<TenantEvent>(msg => msg.TenantId))
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.DeclareQueue("tenant-events")
            .WithEnablePartitioning(true);
    });
```

Use `UseAzureServiceBusPartitionKey<T>()` on partitioned queues and topics when you do not need sessions but still want ordered delivery within a partition, or when you want transactional sends to land on the same broker. Azure Service Bus assigns every message with the same partition key to the same messaging store, so consumers see per-key FIFO even across multiple partitions. See Microsoft Learn on [partitioned queues and topics](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-partitioning).

When a `SessionId` is also configured on the same message type, the broker requires `PartitionKey == SessionId`. Mocha enforces this at dispatch with a fail-fast check and throws:

```text
PartitionKey must equal SessionId when both are set on an Azure Service Bus message.
```

This `InvalidOperationException` surfaces before the message reaches the broker, so a mismatch never costs you a round trip. If you want the automatic `PartitionKey = SessionId` behavior, configure only `UseAzureServiceBusSessionId<T>()` and let the transport default the partition key.

:::note
When the extractor returns `null`, no partition key is set and Service Bus picks a partition with an internal round-robin - use this only when you do not need per-key ordering.
:::

## Configure reply correlation with `UseAzureServiceBusReplyToSessionId`

```csharp
public sealed class GetOrderRequest : IEventRequest<OrderResponse>
{
    public required string OrderId { get; init; }
    // Unique per requester instance - typically a process GUID.
    public required string RequesterId { get; init; }
}

builder.Services
    .AddMessageBus()
    .AddMessage<GetOrderRequest>(m => m
        // Every reply lands on a session keyed by the requester,
        // so a shared reply queue fans back out cleanly.
        .UseAzureServiceBusReplyToSessionId<GetOrderRequest>(
            req => req.RequesterId))
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);
    });
```

Use `UseAzureServiceBusReplyToSessionId<T>()` for multiplexed request/reply over a single shared reply queue. Each requester sets its own `ReplyToSessionId` on outbound requests, and the responder copies that value into the reply's `SessionId` so each requester receives only its own replies. This is the [Return Address](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ReturnAddress.html) pattern applied over a session-aware reply queue. See Microsoft Learn on [message routing and correlation](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-messages-payloads#message-routing-and-correlation) for the full multiplexed request/reply protocol.

The extractor lives on the request type, not the response - the requester is the one that tells the responder where replies should land.

:::tip
`ReplyToSessionId` is capped at **128 characters**. Use a stable identifier per requester instance (a GUID created at process start is idiomatic) so replies reach the right receiver even after reconnects.
:::

## Configure autoforward chaining with `UseAzureServiceBusTo`

```csharp
builder.Services
    .AddMessageBus()
    .AddMessage<AuditEvent>(m => m
        // Logical destination for autoforward hops or downstream
        // inspection. Not used by the broker for routing today.
        .UseAzureServiceBusTo<AuditEvent>(msg => msg.TargetQueue))
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);
    });
```

Use `UseAzureServiceBusTo<T>()` when you participate in an [autoforwarding](https://learn.microsoft.com/azure/service-bus-messaging/service-bus-auto-forwarding) chain and want to annotate messages with their logical final destination, or when a downstream component inspects `To` as part of custom routing logic.

:::note
`To` is broker-reserved. Microsoft Learn explicitly notes that "the **To** property is reserved for future use and might eventually be interpreted by the broker" - today the broker does not use it for routing. Rely on topic subscriptions, autoforward rules, or application properties for real routing decisions, and treat `To` as a metadata annotation only.
:::

## Override per dispatch via headers

```csharp
public static class TenantAwareBusExtensions
{
    // Pass per-call overrides for the four ASB properties by setting
    // the framework header on PublishOptions before the message
    // properties middleware runs.
    public static ValueTask PublishForTenantAsync<T>(
        this IMessageBus bus,
        T message,
        string tenantId,
        CancellationToken cancellationToken)
        where T : class
    {
        return bus.PublishAsync(
            message,
            new PublishOptions
            {
                Headers = new Dictionary<string, object?>
                {
                    [AzureServiceBusMessageHeaders.SessionId] = tenantId
                }
            },
            cancellationToken);
    }
}
```

The dispatch middleware that invokes the four extractors checks `context.Headers` first and skips the extractor whenever the corresponding `x-mocha-*` header is already set. User-set headers always win over the registered extractor, which lets send-site code override the per-type default for a single dispatch without reconfiguring the bus. The headers are defined as string constants on `AzureServiceBusMessageHeaders`:

| Constant                                         | Header key                    |
| ------------------------------------------------ | ----------------------------- |
| `AzureServiceBusMessageHeaders.SessionId`        | `x-mocha-session-id`          |
| `AzureServiceBusMessageHeaders.PartitionKey`     | `x-mocha-partition-key`       |
| `AzureServiceBusMessageHeaders.ReplyToSessionId` | `x-mocha-reply-to-session-id` |
| `AzureServiceBusMessageHeaders.To`               | `x-mocha-to`                  |

The headers are framework-internal and are filtered out of `ApplicationProperties` on both send and receive, so they do not leak into the consumer-side header bag. This gives you one uniform override channel - whether from a send extension, a pipeline middleware, or a test harness.

## Reference

`SessionId`, `PartitionKey`, and `ReplyToSessionId` are each capped at **128 characters** by the broker.

| Extension method                                   | Sets on `ServiceBusMessage` | Header key                    | Gotcha                                                                                           |
| -------------------------------------------------- | --------------------------- | ----------------------------- | ------------------------------------------------------------------------------------------------ |
| `UseAzureServiceBusSessionId<T>(extractor)`        | `SessionId`                 | `x-mocha-session-id`          | Defaults `PartitionKey` to the same value when no partition-key extractor is set.                |
| `UseAzureServiceBusPartitionKey<T>(extractor)`     | `PartitionKey`              | `x-mocha-partition-key`       | Must equal `SessionId` when both are set, else dispatch throws `InvalidOperationException`.      |
| `UseAzureServiceBusReplyToSessionId<T>(extractor)` | `ReplyToSessionId`          | `x-mocha-reply-to-session-id` | Configure on the request type, not the response.                                                 |
| `UseAzureServiceBusTo<T>(extractor)`               | `To`                        | `x-mocha-to`                  | Broker-reserved; not used for routing today. Useful for autoforward chains or custom inspection. |

Extractors are the right tool when the ASB property is derived from the payload. When you need to declare the entities they land on - session-aware queues, partitioned topics, or autoforward targets - reach for [the topology builder](#declare-custom-topology) in the next section.

# Declare custom topology

Mocha auto-provisions topology by default. To declare additional topics, queues, or subscriptions:

```csharp
builder.Services
    .AddMessageBus()
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.DeclareTopic("order-events");

        transport.DeclareQueue("billing-orders")
            .WithMaxDeliveryCount(5)
            .WithLockDuration(TimeSpan.FromMinutes(1));

        transport.DeclareSubscription("order-events", "billing-orders");
    });
```

To bind handlers explicitly to specific queues:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<ProcessOrderCommandHandler>()
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);
        transport.BindHandlersExplicitly();

        transport.DeclareQueue("process-order");

        transport.Endpoint("process-order-ep")
            .Queue("process-order")
            .Handler<ProcessOrderCommandHandler>();

        transport.DispatchEndpoint("send-demo")
            .ToQueue("process-order")
            .Send<ProcessOrderCommand>();
    });
```

# Control auto-provisioning

When infrastructure is managed externally - for example through Bicep, Terraform, the Azure Service Bus emulator's `Config.json`, or a CI/CD pipeline - disable auto-provisioning so the transport expects entities to already exist:

```csharp
builder.Services
    .AddMessageBus()
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);
        transport.AutoProvision(false);
    });
```

With auto-provisioning disabled, the transport will not call the management API to create topics, queues, or subscriptions. All entities must already exist on the namespace before the transport starts. Individual resources can opt back in via `.AutoProvision(true)` when most topology is managed externally but a few entities need to be created dynamically.

# Scheduling

Azure Service Bus schedules messages natively. The dispatch endpoint calls `ServiceBusSender.ScheduleMessageAsync` and the broker holds the message until the scheduled time:

```csharp
var result = await bus.SchedulePublishAsync(
    new PaymentReminderEvent { OrderId = orderId },
    DateTimeOffset.UtcNow.AddHours(24),
    cancellationToken);

if (result.IsCancellable)
{
    // Persist the token alongside the order so we can cancel later
    await orders.SaveReminderTokenAsync(orderId, result.Token!, cancellationToken);
}
```

Cancellation is supported natively - no Postgres store, EF Core model, or background worker is required:

```csharp
await bus.CancelScheduledMessageAsync(reminderToken, cancellationToken);
```

The token encodes the entity path and the broker-assigned sequence number, and `CancelScheduledMessageAsync` revokes the message via `ServiceBusSender.CancelScheduledMessageAsync`. If the message has already been dispatched, the broker returns `MessageNotFound` and Mocha surfaces this as `false`.

ASB is the only transport with both **native scheduling and native cancellation**. See [Scheduling](/docs/mocha/v16/scheduling) for the full scheduling API.

# Dead-lettering

The Azure Service Bus transport offers three dead-letter paths in increasing order of power. Pick the one whose semantics fit the failure you are modeling.

## 1. Handler exception → `_error` queue (default, transport-agnostic)

When your handler throws, the cross-transport `ReceiveFaultMiddleware` catches the exception, attaches `fault-*` headers (exception type, message, stack trace, timestamp), and forwards the original envelope to the convention-named `{queue}_error` queue:

```csharp
public class ProcessInvoiceHandler : IEventHandler<ProcessInvoice>
{
    public ValueTask HandleAsync(ProcessInvoice message, CancellationToken ct)
    {
        // Throwing here forwards the message to {queue}_error
        throw new InvalidOperationException("Downstream service is unavailable.");
    }
}
```

The acknowledgement middleware then completes the lock against the broker so the message does not redeliver. This is the path most applications use - it is consistent across all transports and works without ASB-specific code.

## 2. Broker-managed `$DeadLetterQueue`

Azure Service Bus dead-letters messages itself when broker-side conditions are met:

| Condition               | Reason code                 |
| ----------------------- | --------------------------- |
| Delivery count exceeded | `MaxDeliveryCountExceeded`  |
| Message TTL expired     | `TTLExpiredException`       |
| Filter evaluation error | `FilterEvaluationException` |

These messages land in the entity's `$DeadLetterQueue` sub-entity (`{queue}/$DeadLetterQueue`), separate from Mocha's `_error` queue. To consolidate operations, opt the endpoint's queue into forwarding broker-dead-lettered messages into the Mocha-managed `_error` queue:

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<OrderPlacedEventHandler>()
    .AddAzureServiceBus(transport =>
    {
        transport.ConnectionString(connectionString);

        transport.Handler<OrderPlacedEventHandler>()
            .ConfigureEndpoint(e => e.UseNativeDeadLetterForwarding());
    });
```

`UseNativeDeadLetterForwarding()` sets `ForwardDeadLetteredMessagesTo = "{queueName}_error"` on the underlying queue at provisioning time. Messages dead-lettered by the broker for `MaxDeliveryCountExceeded` or `TTLExpiredException` are forwarded into the same `_error` queue used by handler exceptions, so operators have one place to look.

If you have already configured `WithForwardDeadLetteredMessagesTo("custom-target")` on the same queue, the transport surfaces a configuration conflict at provisioning - it will not silently override your choice.

## 3. Explicit native dead-letter with reason codes

For domain-level failures where you want a structured reason code visible in Azure Monitor and Service Bus Explorer, dead-letter the message yourself through the ASB-specific message context. The context is exposed on the `IConsumeContext` available to any `IConsumer<T>`:

```csharp
public class ProcessInvoiceConsumer : IConsumer<ProcessInvoice>
{
    public async ValueTask ConsumeAsync(IConsumeContext<ProcessInvoice> context)
    {
        var message = context.Message;

        if (string.IsNullOrEmpty(message.CustomerId))
        {
            await context.AzureServiceBus().DeadLetterAsync(
                reason: "InvalidPayload",
                description: "Missing customer id",
                properties: new Dictionary<string, object>
                {
                    ["InvoiceId"] = message.InvoiceId
                },
                context.CancellationToken);

            return;
        }

        // ... normal processing
    }
}
```

If your processing code lives in an `IEventHandler<T>`, resolve the context through whichever scoped accessor you have wired up - or refactor to `IConsumer<T>` when you need direct access to broker primitives like `DeadLetterAsync`.

The message is moved to the entity's `$DeadLetterQueue` with `DeadLetterReason = "InvalidPayload"` and `DeadLetterErrorDescription = "Missing customer id"`. Both fields are first-class columns in Service Bus Explorer and queryable through Azure Monitor.

After `DeadLetterAsync` returns, the acknowledgement middleware skips the redundant `Complete` call - the lock is already released. If a `MessageLockLost` is observed (because, for example, the lock had already expired before the handler called `DeadLetterAsync`), the middleware treats the message as already settled and continues silently.

# `IAzureServiceBusMessageContext`

Resolve the ASB-specific context from any active `IMessageContext` via the `AzureServiceBus()` extension method (or the non-throwing `TryGetAzureServiceBus(out ...)`). `IConsumeContext<T>` derives from `IMessageContext`, so consumer implementations have direct access:

```csharp
public class ReviewCustomerConsumer : IConsumer<ReviewCustomer>
{
    public async ValueTask ConsumeAsync(IConsumeContext<ReviewCustomer> context)
    {
        var asb = context.AzureServiceBus();

        // Inspect broker-managed metadata
        var deliveries = asb.DeliveryCount;
        var lockUntil = asb.LockedUntil;

        // Native dead-letter with structured reason code
        await asb.DeadLetterAsync(
            "BusinessReject",
            "Customer flagged for review",
            cancellationToken: context.CancellationToken);
    }
}
```

The interface exposes:

| Member                                               | Purpose                                                                                    |
| ---------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| `Message` (`ServiceBusReceivedMessage`)              | The raw message as delivered by the broker                                                 |
| `EntityPath`                                         | The queue or subscription the message was received from                                    |
| `DeliveryCount`                                      | The broker-tracked delivery count                                                          |
| `LockedUntil`                                        | The absolute time at which the broker-managed lock expires                                 |
| `DeadLetterAsync(reason, description?, properties?)` | Move the message to `$DeadLetterQueue` with structured reason metadata                     |
| `AbandonAsync(propertiesToModify?)`                  | Return the message to the queue for redelivery, optionally updating application properties |

The context is pooled together with the receive context and is only valid for the duration of the handler invocation. Calling `context.AzureServiceBus()` from a handler running on a different transport throws `InvalidOperationException` - use `TryGetAzureServiceBus` if you write transport-agnostic handlers.

# Best practices

- **Default to `_error`.** Handler exceptions should fall through to the `Fault` middleware. The `_error` queue is consistent across transports and keeps your handlers portable.
- **Enable `UseNativeDeadLetterForwarding()` to consolidate ops.** When you want a single queue to monitor in production, forward broker-dead-lettered messages into `_error` so `MaxDeliveryCountExceeded` and handler exceptions share one operational surface.
- **Use `IAzureServiceBusMessageContext.DeadLetterAsync` for domain-level rejection.** Reach for the native API only when you want a structured reason code visible to operators in Service Bus Explorer or Azure Monitor (`InvalidPayload`, `BusinessReject`, `DuplicateRequest`, etc.). Generic infrastructure failures still belong in the `_error` queue with a stack trace.
- **Tune `MaxDeliveryCount` deliberately.** The default is 10. Combined with retry middleware, a high count can produce many handler invocations before broker dead-lettering kicks in. Lower the count if you would rather see failures in `$DeadLetterQueue` sooner.
- **Avoid building consumer endpoints over `$DeadLetterQueue`.** Treat the broker DLQ as an operations surface, not a normal pipeline destination. The same advice applies in MassTransit and other Service Bus clients - dead-lettered messages are typically inspected, fixed, and resubmitted, not auto-processed.

# Troubleshooting

**Handler called `DeadLetterAsync` but the message is not in the DLQ.**
The receiver must be in `PeekLock` mode (the Mocha default) for native dead-lettering to be supported. The transport configures `PeekLock` automatically; if you have customized the `ServiceBusProcessor` options, make sure you have not switched to `ReceiveAndDelete`.

**The broker DLQ fills up with `MaxDeliveryCountExceeded`.**
Either tune the queue's `MaxDeliveryCount` lower so failures surface sooner, or enable `UseNativeDeadLetterForwarding()` on the endpoint so broker-dead-lettered messages are forwarded into your `_error` queue and aggregated with handler exceptions.

**`UseNativeDeadLetterForwarding()` fails at startup with a forwarding conflict.**
Both `WithForwardDeadLetteredMessagesTo("...")` and `UseNativeDeadLetterForwarding()` are configured on the same queue. The transport refuses to silently override the explicit forwarding target. Pick one.

**`CancelScheduledMessageAsync` returns false.**
The most common causes: the scheduled time has already passed (the broker enqueued the message and returned `MessageNotFound` to the cancel call), `IsCancellable` was `false` on the original `SchedulingResult`, or the token came from a different transport than the one that created the message. The transport's cancel path is idempotent - calling it twice is safe.

**`context.AzureServiceBus()` throws `InvalidOperationException`.**
The current message did not originate from the Azure Service Bus transport. Use `context.TryGetAzureServiceBus(out var asb)` if you write transport-agnostic handlers and want to no-op on other transports.

**Receive endpoint logs `MessageLockLost`.**
The broker reclaimed the lock before the acknowledgement middleware could `Complete` or `Abandon` the message - usually because the handler ran longer than `LockDuration`. The acknowledgement middleware swallows this exception (the broker will redeliver per its own rules), but you should consider either lengthening `LockDuration` on the queue or shortening the handler.

# Next steps

- [Transports Overview](/docs/mocha/v16/transports) - Understand the transport abstraction and lifecycle.
- [Scheduling](/docs/mocha/v16/scheduling) - Schedule messages for future delivery and cancel them natively through Azure Service Bus.
- [Routing and Endpoints](/docs/mocha/v16/routing-and-endpoints) - Understand how `_error` and `_skipped` endpoints fit the receive pipeline.
- [Reliability](/docs/mocha/v16/reliability) - Configure fault handling, retries, the transactional outbox, and the idempotent inbox.
- [Middleware and Pipelines](/docs/mocha/v16/middleware-and-pipelines) - Customize the receive and dispatch pipelines.

> **Runnable example:** [AzureServiceBusTransport](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/examples/AzureServiceBusTransport)
>
> **Multi-service demo:** The AzureServiceBusTransport example runs OrderService, ShippingService, and NotificationService against the local Azure Service Bus emulator orchestrated through .NET Aspire, demonstrating publish/subscribe, send, request/reply, sagas, and batch processing on a managed broker.

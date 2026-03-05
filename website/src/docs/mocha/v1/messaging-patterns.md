---
title: "Messaging Patterns"
description: "Learn the three core messaging patterns in Mocha - events (pub/sub), send (fire-and-forget), and request/reply - and when to use each one."
---

# Messaging patterns

Every message you send answers one of three questions: "Who needs to know?" - an event, broadcast to anyone who cares. "Who should act?" - a command, directed at a single handler. "What is the result?" - a request that blocks until the handler replies. Choosing the wrong pattern is the most common messaging architecture mistake. This page explains each pattern, when to use it, and the anti-pattern to avoid.

| Pattern             | Bus method     | Handler interface                           | Delivery                                    |
| ------------------- | -------------- | ------------------------------------------- | ------------------------------------------- |
| **Event** (pub/sub) | `PublishAsync` | `IEventHandler<TEvent>`                     | One-to-many: all subscribers receive a copy |
| **Request** (send)  | `SendAsync`    | `IEventRequestHandler<TRequest>`            | One-to-one: a single handler processes it   |
| **Request/Reply**   | `RequestAsync` | `IEventRequestHandler<TRequest, TResponse>` | One-to-one: sender awaits a typed response  |

# Events (pub/sub)

Events represent something that happened. The publisher does not know or care who receives the event - that question is answered by whoever subscribes. Zero, one, or many handlers can react to the same event type. If no handler is registered, the event is silently discarded.

This implements the [Publish-Subscribe Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html) pattern.

**Naming convention:** Name events using noun-verb past tense. `OrderPlaced`, `PaymentCompleted`, `UserRegistered`. The past tense signals that something already happened - the publisher is not directing anyone to act.

<TopologyVisualization data='{"services":[{"host":{"serviceName":"OrderService","assemblyName":"OrderService.dll","instanceId":"order-svc-1"},"messageTypes":[{"identity":"msg:OrderPlacedEvent","runtimeType":"OrderPlacedEvent","runtimeTypeFullName":"MyApp.Messages.OrderPlacedEvent","isInterface":false,"isInternal":false}],"consumers":[{"name":"BillingHandler","identityType":"BillingHandler","identityTypeFullName":"MyApp.Handlers.BillingHandler"},{"name":"NotificationHandler","identityType":"NotificationHandler","identityTypeFullName":"MyApp.Handlers.NotificationHandler"}],"routes":{"inbound":[{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlacedEvent","consumerName":"BillingHandler","endpoint":{"name":"billing-handler","address":"loopback://localhost/q/billing-handler","transportName":"InMemory"}},{"kind":"subscribe","messageTypeIdentity":"msg:OrderPlacedEvent","consumerName":"NotificationHandler","endpoint":{"name":"notification-handler","address":"loopback://localhost/q/notification-handler","transportName":"InMemory"}}],"outbound":[{"kind":"publish","messageTypeIdentity":"msg:OrderPlacedEvent","endpoint":{"name":"OrderPlacedEvent","address":"loopback://localhost/c/OrderPlacedEvent","transportName":"InMemory"}}]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"billing-handler","kind":"default","address":"loopback://localhost/q/billing-handler","source":{"address":"loopback://localhost/q/billing-handler"}},{"name":"notification-handler","kind":"default","address":"loopback://localhost/q/notification-handler","source":{"address":"loopback://localhost/q/notification-handler"}}],"dispatchEndpoints":[{"name":"OrderPlacedEvent","kind":"default","address":"loopback://localhost/c/OrderPlacedEvent","destination":{"address":"loopback://localhost/c/OrderPlacedEvent"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"channel","name":"OrderPlacedEvent","address":"loopback://localhost/c/OrderPlacedEvent","flow":"inbound","properties":{"type":"publish"}},{"kind":"queue","name":"billing-handler","address":"loopback://localhost/q/billing-handler","flow":"outbound","properties":{}},{"kind":"queue","name":"notification-handler","address":"loopback://localhost/q/notification-handler","flow":"outbound","properties":{}}],"links":[{"kind":"subscription","address":"loopback://localhost/sub/OrderPlacedEvent-billing-handler","source":"loopback://localhost/c/OrderPlacedEvent","target":"loopback://localhost/q/billing-handler","direction":"forward","properties":{}},{"kind":"subscription","address":"loopback://localhost/sub/OrderPlacedEvent-notification-handler","source":"loopback://localhost/c/OrderPlacedEvent","target":"loopback://localhost/q/notification-handler","direction":"forward","properties":{}}]}}]}' trace='{"traceId":"event-pubsub-trace-001","activities":[{"id":"ep-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":45,"status":"ok","operation":"publish","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","transport":"InMemory"},{"id":"ep-2","parentId":"ep-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","endpointName":"OrderPlacedEvent","endpointAddress":"loopback://localhost/c/OrderPlacedEvent","transport":"InMemory"},{"id":"ep-3","parentId":"ep-2","startTime":"2024-06-15T10:30:00.010Z","durationMs":3,"status":"ok","operation":"receive","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","endpointName":"billing-handler","endpointAddress":"loopback://localhost/q/billing-handler","transport":"InMemory"},{"id":"ep-4","parentId":"ep-3","startTime":"2024-06-15T10:30:00.013Z","durationMs":12,"status":"ok","operation":"consume","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","consumerName":"BillingHandler"},{"id":"ep-5","parentId":"ep-2","startTime":"2024-06-15T10:30:00.012Z","durationMs":3,"status":"ok","operation":"receive","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","endpointName":"notification-handler","endpointAddress":"loopback://localhost/q/notification-handler","transport":"InMemory"},{"id":"ep-6","parentId":"ep-5","startTime":"2024-06-15T10:30:00.015Z","durationMs":8,"status":"ok","operation":"consume","messageType":"OrderPlacedEvent","messageTypeIdentity":"msg:OrderPlacedEvent","consumerName":"NotificationHandler"}]}' />

## Publish an event and handle it

By the end of this section, you will have two independent handlers both processing the same published event.

### Define the event

```csharp
namespace MyApp.Messages;

public sealed record OrderPlacedEvent
{
    public required Guid OrderId { get; init; }
    public required string CustomerId { get; init; }
    public required decimal TotalAmount { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
```

### Implement two handlers

```csharp
using Mocha;

namespace MyApp.Handlers;

// Handler 1: Create an invoice when an order is placed
public class BillingHandler(ILogger<BillingHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Creating invoice for order {OrderId}, amount {Amount}",
            message.OrderId,
            message.TotalAmount);

        // Create invoice logic here
        await Task.CompletedTask;
    }
}

// Handler 2: Send a notification when an order is placed
public class NotificationHandler(ILogger<NotificationHandler> logger)
    : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Sending confirmation to customer {CustomerId} for order {OrderId}",
            message.CustomerId,
            message.OrderId);

        // Send email/SMS logic here
        await Task.CompletedTask;
    }
}
```

Each handler implements `IEventHandler<OrderPlacedEvent>`. Both receive the same event independently. If one handler fails, the other still processes its copy.

### Register and publish

```csharp
builder.Services
    .AddMessageBus()
    .AddEventHandler<BillingHandler>()
    .AddEventHandler<NotificationHandler>()
    .AddInMemory();
```

From an endpoint, publish the event through the injected bus:

```csharp
app.MapPost("/orders", async (IMessageBus bus) =>
{
    await bus.PublishAsync(new OrderPlacedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerId = "customer-42",
        TotalAmount = 149.99m,
        CreatedAt = DateTimeOffset.UtcNow
    }, CancellationToken.None);

    return Results.Ok();
});
```

Expected output:

```text
info: MyApp.Handlers.BillingHandler[0]
      Creating invoice for order 3f2504e0-4f89-11d3-9a0c-0305e82c3301, amount 149.99
info: MyApp.Handlers.NotificationHandler[0]
      Sending confirmation to customer customer-42 for order 3f2504e0-4f89-11d3-9a0c-0305e82c3301
```

Both handlers execute. The order of execution is not guaranteed.

## How to chain events across services

A common pattern is for a handler to publish a new event after completing its work. This creates an event chain that coordinates multiple services without coupling them.

```csharp
public class OrderPlacedEventHandler(
    BillingDbContext db,
    IMessageBus messageBus,
    ILogger<OrderPlacedEventHandler> logger) : IEventHandler<OrderPlacedEvent>
{
    public async ValueTask HandleAsync(
        OrderPlacedEvent message,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Order placed: {OrderId}, creating invoice",
            message.OrderId);

        // Create and process payment...

        // Publish a downstream event
        await messageBus.PublishAsync(
            new PaymentCompletedEvent
            {
                PaymentId = Guid.NewGuid(),
                OrderId = message.OrderId,
                Amount = message.TotalAmount,
                PaymentMethod = "CreditCard",
                ProcessedAt = DateTimeOffset.UtcNow
            },
            cancellationToken);
    }
}
```

The billing service handles `OrderPlacedEvent` and publishes `PaymentCompletedEvent`. A shipping service can subscribe to `PaymentCompletedEvent` without knowing about billing. Each service reacts to events it cares about.

# Send (fire-and-forget)

A send represents an instruction directed at a specific handler. Unlike events, a send has exactly one handler. The sender knows what it wants done but does not wait for a typed response - the message either succeeds or faults.

This implements the [Command Message](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html) pattern.

**Naming convention:** Name send messages using verb-noun present tense. `ReserveInventory`, `ProcessPayment`, `ScheduleShipment`. The imperative form signals intent - you are telling a specific service what to do.

Use send for fire-and-forget operations: reserving inventory, scheduling a job, triggering a side effect in another service.

<TopologyVisualization data='{"services":[{"host":{"serviceName":"OrderService","assemblyName":"OrderService.dll","instanceId":"order-svc-1"},"messageTypes":[{"identity":"msg:ReserveInventoryCommand","runtimeType":"ReserveInventoryCommand","runtimeTypeFullName":"MyApp.Messages.ReserveInventoryCommand","isInterface":false,"isInternal":false}],"consumers":[],"routes":{"inbound":[],"outbound":[{"kind":"send","messageTypeIdentity":"msg:ReserveInventoryCommand","endpoint":{"name":"inventory-handler","address":"loopback://localhost/q/inventory-handler","transportName":"InMemory"}}]},"sagas":[]},{"host":{"serviceName":"InventoryService","assemblyName":"InventoryService.dll","instanceId":"inventory-svc-1"},"messageTypes":[{"identity":"msg:ReserveInventoryCommand","runtimeType":"ReserveInventoryCommand","runtimeTypeFullName":"MyApp.Messages.ReserveInventoryCommand","isInterface":false,"isInternal":false}],"consumers":[{"name":"ReserveInventoryCommandHandler","identityType":"ReserveInventoryCommandHandler","identityTypeFullName":"MyApp.Handlers.ReserveInventoryCommandHandler"}],"routes":{"inbound":[{"kind":"request","messageTypeIdentity":"msg:ReserveInventoryCommand","consumerName":"ReserveInventoryCommandHandler","endpoint":{"name":"inventory-handler","address":"loopback://localhost/q/inventory-handler","transportName":"InMemory"}}],"outbound":[]},"sagas":[]}],"transports":[{"identifier":"inmemory","name":"InMemory","schema":"loopback","transportType":"InMemoryTransport","receiveEndpoints":[{"name":"inventory-handler","kind":"default","address":"loopback://localhost/q/inventory-handler","source":{"address":"loopback://localhost/q/inventory-handler"}}],"dispatchEndpoints":[{"name":"inventory-handler","kind":"default","address":"loopback://localhost/q/inventory-handler","destination":{"address":"loopback://localhost/q/inventory-handler"}}],"topology":{"address":"loopback://localhost","entities":[{"kind":"queue","name":"inventory-handler","address":"loopback://localhost/q/inventory-handler","flow":"inbound","properties":{"type":"send"}}],"links":[]}}]}' trace='{"traceId":"command-send-trace-001","activities":[{"id":"cs-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":30,"status":"ok","operation":"send","messageType":"ReserveInventoryCommand","messageTypeIdentity":"msg:ReserveInventoryCommand","transport":"InMemory"},{"id":"cs-2","parentId":"cs-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ReserveInventoryCommand","messageTypeIdentity":"msg:ReserveInventoryCommand","endpointName":"inventory-handler","endpointAddress":"loopback://localhost/q/inventory-handler","transport":"InMemory"},{"id":"cs-3","parentId":"cs-2","startTime":"2024-06-15T10:30:00.008Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ReserveInventoryCommand","messageTypeIdentity":"msg:ReserveInventoryCommand","endpointName":"inventory-handler","endpointAddress":"loopback://localhost/q/inventory-handler","transport":"InMemory"},{"id":"cs-4","parentId":"cs-3","startTime":"2024-06-15T10:30:00.011Z","durationMs":15,"status":"ok","operation":"consume","messageType":"ReserveInventoryCommand","messageTypeIdentity":"msg:ReserveInventoryCommand","consumerName":"ReserveInventoryCommandHandler"}]}' />

<Warning>

**Send in disguise.** If your "event" expects exactly one handler to take action, it should be a send. Use `SendAsync`, not `PublishAsync`. Publishing a message that requires a single specific handler breaks the semantic contract of events and makes the system harder to reason about.

</Warning>

## Send a message and handle it

By the end of this section, you will send a message to a single handler and verify it executes.

### Define the message

```csharp
namespace MyApp.Messages;

public sealed record ReserveInventoryCommand
{
    public required Guid OrderId { get; init; }
    public required Guid ProductId { get; init; }
    public required int Quantity { get; init; }
}
```

### Implement the handler

```csharp
using Mocha;

namespace MyApp.Handlers;

public class ReserveInventoryCommandHandler(
    AppDbContext db,
    ILogger<ReserveInventoryCommandHandler> logger)
    : IEventRequestHandler<ReserveInventoryCommand>
{
    public async ValueTask HandleAsync(
        ReserveInventoryCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Reserving {Quantity} units of product {ProductId} for order {OrderId}",
            request.Quantity,
            request.ProductId,
            request.OrderId);

        var product = await db.Products
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException(
                $"Product {request.ProductId} not found");
        }

        product.StockQuantity -= request.Quantity;
        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Reserved {Quantity} units. Remaining stock: {Remaining}",
            request.Quantity,
            product.StockQuantity);
    }
}
```

`IEventRequestHandler<TRequest>` (single type parameter) handles the command without returning a value. If the handler throws, the bus generates a fault.

### Register and send

```csharp
builder.Services
    .AddMessageBus()
    .AddRequestHandler<ReserveInventoryCommandHandler>()
    .AddInMemory();
```

Send the command:

```csharp
await bus.SendAsync(new ReserveInventoryCommand
{
    OrderId = Guid.NewGuid(),
    ProductId = productId,
    Quantity = 3
}, cancellationToken);
```

Expected output:

```text
info: MyApp.Handlers.ReserveInventoryCommandHandler[0]
      Reserving 3 units of product a1b2c3d4-... for order e5f6a7b8-...
info: MyApp.Handlers.ReserveInventoryCommandHandler[0]
      Reserved 3 units. Remaining stock: 97
```

`SendAsync` completes after the message is dispatched to the transport. It does not wait for the handler to finish processing.

## How to wait for command acknowledgment

When you need confirmation that the handler processed the command, use `RequestAsync` instead of `SendAsync`. Mocha sends an automatic `AcknowledgedEvent` when the handler completes.

```csharp
// Fire-and-forget: returns after dispatch
await bus.SendAsync(command, cancellationToken);

// Wait for acknowledgment: returns after the handler completes
await bus.RequestAsync(command, cancellationToken);
```

The `RequestAsync` overload that accepts `object` (no `IEventRequest<T>` constraint) waits for the handler's acknowledgment without expecting a typed response. If the handler throws, `RequestAsync` throws a `ResponseTimeoutException`.

# Request/Reply

Request/reply is for when the sender needs a typed response. The sender dispatches a request, Mocha routes it to a handler, the handler returns a response, and Mocha delivers the response back to the sender. The entire round trip completes within a single `await`.

This implements the [Request-Reply](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html) pattern.

When you call `RequestAsync`, Mocha creates a temporary reply address and embeds it in the request envelope. The handler reads the reply address from the envelope and sends the response back to it. A correlation ID links the request and response across the transport. This is what makes `ResponseTimeoutException` possible - if the reply never arrives at the temporary address, the timeout fires.

<TopologyVisualization data='{"services":[{"host":{"serviceName":"RequestReply","assemblyName":"RequestReply","instanceId":"de1ab3cc-3802-44af-8523-b316a57228d1"},"messageTypes":[{"identity":"urn:message:mocha.events:not-acknowledged-event","runtimeType":"NotAcknowledgedEvent","runtimeTypeFullName":"Mocha.Events.NotAcknowledgedEvent","isInterface":false,"isInternal":true,"enclosedMessageIdentities":["urn:message:mocha.events:not-acknowledged-event"]},{"identity":"urn:message:mocha.events:acknowledged-event","runtimeType":"AcknowledgedEvent","runtimeTypeFullName":"Mocha.Events.AcknowledgedEvent","isInterface":false,"isInternal":true,"enclosedMessageIdentities":["urn:message:mocha.events:acknowledged-event"]},{"identity":"urn:message:global:process-refund-command","runtimeType":"ProcessRefundCommand","runtimeTypeFullName":"ProcessRefundCommand","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:process-refund-command","urn:message:mocha:i-event-request[process-refund-response]","urn:message:mocha:i-event-request"]},{"identity":"urn:message:global:process-refund-response","runtimeType":"ProcessRefundResponse","runtimeTypeFullName":"ProcessRefundResponse","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:process-refund-response"]}],"consumers":[{"name":"Reply","identityType":"ReplyConsumer","identityTypeFullName":"Mocha.ReplyConsumer","isBatch":false},{"name":"ProcessRefundCommandHandler","identityType":"ProcessRefundCommandHandler","identityTypeFullName":"ProcessRefundCommandHandler","isBatch":false}],"routes":{"inbound":[{"kind":"request","messageTypeIdentity":"urn:message:global:process-refund-command","consumerName":"ProcessRefundCommandHandler","endpoint":{"name":"process-refund","address":"memory://localhost/process-refund","transportName":"memory"}},{"kind":"reply","consumerName":"Reply","endpoint":{"name":"Replies","address":"memory://localhost/Replies","transportName":"memory"}}],"outbound":[{"kind":"send","messageTypeIdentity":"urn:message:global:process-refund-command","endpoint":{"name":"q/process-refund","address":"memory://localhost/q/process-refund","transportName":"memory"}}]},"sagas":[]}],"transports":[{"identifier":"memory://requestreply/","name":"memory","schema":"memory","transportType":"InMemoryMessagingTransport","receiveEndpoints":[{"name":"Replies","kind":"reply","address":"memory://localhost/Replies","source":{"address":"memory://requestreply/q/response-de1ab3cc380244af8523b316a57228d1"}},{"name":"process-refund","kind":"default","address":"memory://localhost/process-refund","source":{"address":"memory://requestreply/q/process-refund"}}],"dispatchEndpoints":[{"name":"Replies","kind":"reply","address":"memory://localhost/Replies","destination":{"address":"memory://requestreply/q/response-de1ab3cc380244af8523b316a57228d1"}},{"name":"q/process-refund","kind":"default","address":"memory://localhost/q/process-refund","destination":{"address":"memory://requestreply/q/process-refund"}}],"topology":{"address":"memory://requestreply/","entities":[{"kind":"queue","name":"process-refund","address":"memory://requestreply/q/process-refund","flow":"outbound"}],"links":[{"kind":"bind","address":"memory://requestreply/b/t/.process-refund/t/process-refund","source":"memory://requestreply/e/.process-refund","target":"memory://requestreply/e/process-refund","direction":"forward"},{"kind":"bind","address":"memory://requestreply/b/t/process-refund/q/process-refund","source":"memory://requestreply/e/process-refund","target":"memory://requestreply/q/process-refund","direction":"forward"}]}}]}' trace='{"traceId":"request-reply-trace-001","activities":[{"id":"rr-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":60,"status":"ok","operation":"send","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","transport":"memory"},{"id":"rr-2","parentId":"rr-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","endpointName":"q/process-refund","endpointAddress":"memory://localhost/q/process-refund","transport":"memory"},{"id":"rr-3","parentId":"rr-2","startTime":"2024-06-15T10:30:00.008Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","endpointName":"process-refund","endpointAddress":"memory://localhost/process-refund","transport":"memory"},{"id":"rr-4","parentId":"rr-3","startTime":"2024-06-15T10:30:00.011Z","durationMs":20,"status":"ok","operation":"consume","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","consumerName":"ProcessRefundCommandHandler"},{"id":"rr-5","parentId":"rr-4","startTime":"2024-06-15T10:30:00.028Z","durationMs":2,"status":"ok","operation":"reply","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","transport":"memory","conversationId":"conv-rr-001"},{"id":"rr-6","parentId":"rr-5","startTime":"2024-06-15T10:30:00.031Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","endpointName":"Replies","endpointAddress":"memory://localhost/Replies","transport":"memory"},{"id":"rr-7","parentId":"rr-6","startTime":"2024-06-15T10:30:00.038Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","endpointName":"Replies","endpointAddress":"memory://localhost/Replies","transport":"memory"}]}' />

## Send a request and await a response

By the end of this section, you will send a request to a handler and use the typed response.

### Define the request and response

The request record must implement `IEventRequest<TResponse>`. This marker interface tells Mocha the expected response type and enables compile-time correlation.

```csharp
using Mocha;

namespace MyApp.Messages;

public sealed record ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

public sealed record ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
```

### Implement the handler

```csharp
using Mocha;

namespace MyApp.Handlers;

public class ProcessRefundCommandHandler(
    BillingDbContext db,
    ILogger<ProcessRefundCommandHandler> logger)
    : IEventRequestHandler<ProcessRefundCommand, ProcessRefundResponse>
{
    public async ValueTask<ProcessRefundResponse> HandleAsync(
        ProcessRefundCommand request,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Processing refund of {Amount} for order {OrderId}",
            request.Amount,
            request.OrderId);

        // Process refund logic...

        return new ProcessRefundResponse
        {
            RefundId = Guid.NewGuid(),
            OrderId = request.OrderId,
            Amount = request.Amount,
            Success = true,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }
}
```

`IEventRequestHandler<TRequest, TResponse>` requires `TRequest : IEventRequest<TResponse>`. The return value is sent back to the caller's reply endpoint. The return value must not be `null` - if you return `null`, the consumer throws an `InvalidOperationException`.

### Register and request

```csharp
builder.Services
    .AddMessageBus()
    .AddRequestHandler<ProcessRefundCommandHandler>()
    .AddInMemory();
```

Send the request and use the response:

```csharp
var response = await bus.RequestAsync(
    new ProcessRefundCommand
    {
        OrderId = orderId,
        Amount = 49.99m,
        Reason = "Defective product",
        CustomerId = "customer-42"
    },
    cancellationToken);

Console.WriteLine($"Refund {response.RefundId}: {response.Amount:C}, success={response.Success}");
```

Expected output:

```text
info: MyApp.Handlers.ProcessRefundCommandHandler[0]
      Processing refund of 49.99 for order e5f6a7b8-...
Refund d4c3b2a1-...: $49.99, success=True
```

## How to handle timeouts

If the handler does not respond within the configured timeout, `RequestAsync` throws a `ResponseTimeoutException`. You can catch this and handle it:

```csharp
try
{
    var response = await bus.RequestAsync(
        new ProcessRefundCommand
        {
            OrderId = orderId,
            Amount = 49.99m,
            Reason = "Defective product",
            CustomerId = "customer-42"
        },
        cancellationToken);

    // Use response...
}
catch (ResponseTimeoutException ex)
{
    logger.LogWarning(
        "Refund request timed out for order {OrderId}: {Message}",
        orderId,
        ex.Message);

    // Retry, fall back, or notify the user
}
```

Common causes of timeouts: the handler service is not running, the handler registration is missing, or the handler is taking longer than the timeout window.

## How to use `IEventRequest<TResponse>` correctly

The `IEventRequest<TResponse>` marker interface connects the request type to its response type at compile time. This enables two things:

1. **Type-safe `RequestAsync`.** The compiler infers `TResponse` from the request parameter, so `bus.RequestAsync(request)` returns `ValueTask<ProcessRefundResponse>` without you specifying the type.
2. **Handler constraint.** `IEventRequestHandler<TRequest, TResponse>` requires `TRequest : IEventRequest<TResponse>`, preventing mismatched request/response pairings at compile time.

```csharp
// The compiler infers TResponse = ProcessRefundResponse
// because ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
var response = await bus.RequestAsync(refundCommand, cancellationToken);
```

If your request record does not implement `IEventRequest<TResponse>`, you cannot use the typed `RequestAsync<TResponse>` overload.

# When to use which pattern

| Question                             | Event (`PublishAsync`) | Send (`SendAsync`)        | Request/Reply (`RequestAsync`)  |
| ------------------------------------ | ---------------------- | ------------------------- | ------------------------------- |
| How many handlers?                   | Zero or more           | Exactly one               | Exactly one                     |
| Does the sender need a response?     | No                     | No                        | Yes                             |
| Does the sender know who handles it? | No                     | Yes (by routing)          | Yes (by routing)                |
| Does the sender wait for processing? | No                     | No                        | Yes                             |
| Handler interface                    | `IEventHandler<T>`     | `IEventRequestHandler<T>` | `IEventRequestHandler<T, TRes>` |

**Use events** when multiple parts of the system need to react to something that happened. The publisher does not care who listens or what they do with the event. Examples: order placed, payment completed, user signed up.

**Use send** when you need to tell a specific service to do something but do not need a result back. The sender knows the operation should happen but does not need to wait for it. Examples: reserve inventory, send an email, schedule a cleanup job.

**Use request/reply** when the sender needs data back or needs to know whether the operation succeeded with details. The call blocks until the response arrives. Examples: process a refund and get the refund ID, look up product details, validate an address.

# See also

- [Microsoft Azure Architecture: Publisher-Subscriber Pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/publisher-subscriber) - When to use pub/sub and when not to, including considerations for idempotency and message ordering.
- [CodeOpinion: Commands & Events - What's the difference?](https://codeopinion.com/commands-events-whats-the-difference/) - A concise explanation of the semantic distinction between commands and events.
- [EIP: Publish-Subscribe Channel](https://www.enterpriseintegrationpatterns.com/patterns/messaging/PublishSubscribeChannel.html) - The canonical definition of the pub/sub pattern.
- [EIP: Command Message](https://www.enterpriseintegrationpatterns.com/patterns/messaging/CommandMessage.html) - Commands as a way to invoke behavior in another service via messaging.
- [EIP: Request-Reply](https://www.enterpriseintegrationpatterns.com/patterns/messaging/RequestReply.html) - Correlation IDs and reply queues explained.

> **Runnable examples:** [EventPubSub](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/MessagingPatterns/EventPubSub), [SendFireAndForget](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/MessagingPatterns/SendFireAndForget), [RequestReply](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/MessagingPatterns/RequestReply)
>
> **Full demo:** The [Demo application](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Demo) uses all three patterns: [Demo.Catalog](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Demo/Demo.Catalog) publishes `OrderPlacedEvent` (pub/sub), [Demo.Billing](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Demo/Demo.Billing) handles `ProcessRefundCommand` (send), and sagas use `RequestAsync` for request/reply coordination.

Ready to implement these patterns? See [Handlers and Consumers](/docs/mocha/v1/handlers-and-consumers).

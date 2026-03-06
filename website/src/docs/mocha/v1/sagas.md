---
title: "Sagas"
description: "Learn how to orchestrate long-running business processes with saga state machines in Mocha, including state management, persistence, and parallel coordination."
---

# Sagas

The saga pattern was introduced by Garcia-Molina & Salem in 1987 as a way to manage long-lived transactions without holding distributed locks. In a messaging system, sagas implement the [Process Manager pattern](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html) - they coordinate a sequence of messages, track state across them, and drive a business process to completion. Mocha sagas use orchestration-style coordination: one central state machine issues commands and waits for replies, rather than choreographing services through shared events. See [microservices.io](https://microservices.io/patterns/data/saga.html) and the [Microsoft Azure Architecture: Saga pattern](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga) for broader context on the pattern.

A saga loads or creates state when a message arrives, applies the configured transition, dispatches any side-effects (publish or send), and persists the result. When the saga reaches a final state, its persisted state is deleted and an optional response is sent back to the originator.

# When to use sagas vs. handlers

| Scenario                                     | Use a handler | Use a saga |
| -------------------------------------------- | ------------- | ---------- |
| Single message in, single action out         | Yes           | No         |
| Process completes in one step                | Yes           | No         |
| Process spans multiple messages over time    | No            | Yes        |
| Need to coordinate parallel operations       | No            | Yes        |
| Need persistent state across failures        | No            | Yes        |
| Need to send a response after multiple steps | No            | Yes        |

Handlers are simpler and appropriate when a single message triggers a single action. Sagas add value when the workflow requires multiple steps, waits for replies, or must survive process restarts.

A common mistake is using a saga for work that fits in a handler. If your handler calls `SendAsync` and does not need to wait for a reply before responding, a plain handler is sufficient. Reach for a saga when the process must pause and resume based on future messages.

# State machine diagram

The refund saga built in the tutorial below has three states. Each arrow is labeled with the message that triggers the transition:

```mermaid
stateDiagram-v2
    [*] --> AwaitingRefund : RequestQuickRefundRequest\n(create state, send ProcessRefundCommand)
    AwaitingRefund --> Completed : ProcessRefundResponse\n(update state with result)
    Completed --> [*] : send QuickRefundResponse\n(delete persisted state)
```

This is the shape you will build in the tutorial. Keep this diagram in mind as you work through the steps - each code block maps to one of these transitions.

<TopologyVisualization data='{"services":[{"host":{"serviceName":"BasicSaga","assemblyName":"BasicSaga","instanceId":"99abf31b-e5a5-4881-826c-6850c42a05d5"},"messageTypes":[{"identity":"urn:message:mocha.events:not-acknowledged-event","runtimeType":"NotAcknowledgedEvent","runtimeTypeFullName":"Mocha.Events.NotAcknowledgedEvent","isInterface":false,"isInternal":true,"enclosedMessageIdentities":["urn:message:mocha.events:not-acknowledged-event"]},{"identity":"urn:message:mocha.events:acknowledged-event","runtimeType":"AcknowledgedEvent","runtimeTypeFullName":"Mocha.Events.AcknowledgedEvent","isInterface":false,"isInternal":true,"enclosedMessageIdentities":["urn:message:mocha.events:acknowledged-event"]},{"identity":"urn:message:global:process-refund-command","runtimeType":"ProcessRefundCommand","runtimeTypeFullName":"ProcessRefundCommand","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:process-refund-command","urn:message:mocha:i-event-request[process-refund-response]","urn:message:mocha:i-event-request"]},{"identity":"urn:message:global:process-refund-response","runtimeType":"ProcessRefundResponse","runtimeTypeFullName":"ProcessRefundResponse","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:process-refund-response"]},{"identity":"urn:message:global:request-quick-refund-request","runtimeType":"RequestQuickRefundRequest","runtimeTypeFullName":"RequestQuickRefundRequest","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:request-quick-refund-request","urn:message:mocha:i-event-request[quick-refund-response]","urn:message:mocha:i-event-request"]},{"identity":"urn:message:global:quick-refund-response","runtimeType":"QuickRefundResponse","runtimeTypeFullName":"QuickRefundResponse","isInterface":false,"isInternal":false,"enclosedMessageIdentities":["urn:message:global:quick-refund-response"]}],"consumers":[{"name":"Reply","identityType":"ReplyConsumer","identityTypeFullName":"Mocha.ReplyConsumer","isBatch":false},{"name":"ProcessRefundCommandHandler","identityType":"ProcessRefundCommandHandler","identityTypeFullName":"ProcessRefundCommandHandler","isBatch":false},{"name":"quick-refund-saga","identityType":"QuickRefundSaga","identityTypeFullName":"QuickRefundSaga","sagaName":"quick-refund-saga","isBatch":false}],"routes":{"inbound":[{"kind":"request","messageTypeIdentity":"urn:message:global:process-refund-command","consumerName":"ProcessRefundCommandHandler","endpoint":{"name":"process-refund","address":"memory://localhost/process-refund","transportName":"memory"}},{"kind":"request","messageTypeIdentity":"urn:message:global:request-quick-refund-request","consumerName":"quick-refund-saga","endpoint":{"name":"request-quick-refund-request","address":"memory://localhost/request-quick-refund-request","transportName":"memory"}},{"kind":"reply","messageTypeIdentity":"urn:message:global:process-refund-response","consumerName":"quick-refund-saga","endpoint":{"name":"Replies","address":"memory://localhost/Replies","transportName":"memory"}},{"kind":"reply","consumerName":"Reply","endpoint":{"name":"Replies","address":"memory://localhost/Replies","transportName":"memory"}}],"outbound":[{"kind":"send","messageTypeIdentity":"urn:message:global:process-refund-command","endpoint":{"name":"q/process-refund","address":"memory://localhost/q/process-refund","transportName":"memory"}},{"kind":"send","messageTypeIdentity":"urn:message:global:request-quick-refund-request","endpoint":{"name":"q/request-quick-refund-request","address":"memory://localhost/q/request-quick-refund-request","transportName":"memory"}}]},"sagas":[{"name":"quick-refund-saga","stateType":"RefundSagaState","stateTypeFullName":"RefundSagaState","consumerName":"quick-refund-saga","states":[{"name":"__Initial","isInitial":true,"isFinal":false,"onEntry":{},"transitions":[{"eventType":"RequestQuickRefundRequest","eventTypeFullName":"RequestQuickRefundRequest","transitionTo":"AwaitingRefund","transitionKind":"request","autoProvision":true,"send":[{"messageType":"ProcessRefundCommand","messageTypeFullName":"ProcessRefundCommand"}]}]},{"name":"AwaitingRefund","isInitial":false,"isFinal":false,"onEntry":{},"transitions":[{"eventType":"ProcessRefundResponse","eventTypeFullName":"ProcessRefundResponse","transitionTo":"Completed","transitionKind":"reply","autoProvision":true}]},{"name":"Completed","isInitial":false,"isFinal":true,"onEntry":{},"response":{"eventType":"QuickRefundResponse","eventTypeFullName":"QuickRefundResponse"},"transitions":[]}]}]}],"transports":[{"identifier":"memory://basicsaga/","name":"memory","schema":"memory","transportType":"InMemoryMessagingTransport","receiveEndpoints":[{"name":"Replies","kind":"reply","address":"memory://localhost/Replies","source":{"address":"memory://basicsaga/q/response-99abf31be5a54881826c6850c42a05d5"}},{"name":"process-refund","kind":"default","address":"memory://localhost/process-refund","source":{"address":"memory://basicsaga/q/process-refund"}},{"name":"request-quick-refund-request","kind":"default","address":"memory://localhost/request-quick-refund-request","source":{"address":"memory://basicsaga/q/request-quick-refund-request"}}],"dispatchEndpoints":[{"name":"Replies","kind":"reply","address":"memory://localhost/Replies","destination":{"address":"memory://basicsaga/q/response-99abf31be5a54881826c6850c42a05d5"}},{"name":"q/process-refund","kind":"default","address":"memory://localhost/q/process-refund","destination":{"address":"memory://basicsaga/q/process-refund"}},{"name":"q/request-quick-refund-request","kind":"default","address":"memory://localhost/q/request-quick-refund-request","destination":{"address":"memory://basicsaga/q/request-quick-refund-request"}}],"topology":{"address":"memory://basicsaga/","entities":[{"kind":"topic","name":".process-refund","address":"memory://basicsaga/e/.process-refund","flow":"inbound"},{"kind":"topic","name":"process-refund","address":"memory://basicsaga/e/process-refund","flow":"inbound"},{"kind":"topic","name":".request-quick-refund-request","address":"memory://basicsaga/e/.request-quick-refund-request","flow":"inbound"},{"kind":"topic","name":"request-quick-refund-request","address":"memory://basicsaga/e/request-quick-refund-request","flow":"inbound"},{"kind":"queue","name":"response-99abf31be5a54881826c6850c42a05d5","address":"memory://basicsaga/q/response-99abf31be5a54881826c6850c42a05d5","flow":"outbound"},{"kind":"queue","name":"process-refund","address":"memory://basicsaga/q/process-refund","flow":"outbound"},{"kind":"queue","name":"request-quick-refund-request","address":"memory://basicsaga/q/request-quick-refund-request","flow":"outbound"}],"links":[{"kind":"bind","address":"memory://basicsaga/b/t/.process-refund/t/process-refund","source":"memory://basicsaga/e/.process-refund","target":"memory://basicsaga/e/process-refund","direction":"forward"},{"kind":"bind","address":"memory://basicsaga/b/t/process-refund/q/process-refund","source":"memory://basicsaga/e/process-refund","target":"memory://basicsaga/q/process-refund","direction":"forward"},{"kind":"bind","address":"memory://basicsaga/b/t/.request-quick-refund-request/t/request-quick-refund-request","source":"memory://basicsaga/e/.request-quick-refund-request","target":"memory://basicsaga/e/request-quick-refund-request","direction":"forward"},{"kind":"bind","address":"memory://basicsaga/b/t/request-quick-refund-request/q/request-quick-refund-request","source":"memory://basicsaga/e/request-quick-refund-request","target":"memory://basicsaga/q/request-quick-refund-request","direction":"forward"}]}}]}' trace='{"traceId":"saga-orchestration-trace-001","activities":[{"id":"so-1","parentId":null,"startTime":"2024-06-15T10:30:00.000Z","durationMs":120,"status":"ok","operation":"send","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"urn:message:global:request-quick-refund-request","transport":"memory"},{"id":"so-2","parentId":"so-1","startTime":"2024-06-15T10:30:00.001Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"urn:message:global:request-quick-refund-request","endpointName":"q/request-quick-refund-request","endpointAddress":"memory://localhost/q/request-quick-refund-request","transport":"memory"},{"id":"so-3","parentId":"so-2","startTime":"2024-06-15T10:30:00.008Z","durationMs":3,"status":"ok","operation":"receive","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"urn:message:global:request-quick-refund-request","endpointName":"request-quick-refund-request","endpointAddress":"memory://localhost/request-quick-refund-request","transport":"memory"},{"id":"so-4","parentId":"so-3","startTime":"2024-06-15T10:30:00.011Z","durationMs":25,"status":"ok","operation":"consume","messageType":"RequestQuickRefundRequest","messageTypeIdentity":"urn:message:global:request-quick-refund-request","consumerName":"quick-refund-saga"},{"id":"so-5","parentId":"so-4","startTime":"2024-06-15T10:30:00.012Z","durationMs":22,"status":"ok","operation":"saga-transition","sagaName":"quick-refund-saga","sagaInstanceId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","fromState":"__Initial","toState":"AwaitingRefund","eventType":"RequestQuickRefundRequest"},{"id":"so-6","parentId":"so-5","startTime":"2024-06-15T10:30:00.013Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","endpointName":"q/process-refund","endpointAddress":"memory://localhost/q/process-refund","transport":"memory"},{"id":"so-7","parentId":"so-6","startTime":"2024-06-15T10:30:00.020Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","endpointName":"process-refund","endpointAddress":"memory://localhost/process-refund","transport":"memory"},{"id":"so-8","parentId":"so-7","startTime":"2024-06-15T10:30:00.023Z","durationMs":20,"status":"ok","operation":"consume","messageType":"ProcessRefundCommand","messageTypeIdentity":"urn:message:global:process-refund-command","consumerName":"ProcessRefundCommandHandler"},{"id":"so-9","parentId":"so-8","startTime":"2024-06-15T10:30:00.043Z","durationMs":2,"status":"ok","operation":"dispatch","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","endpointName":"Replies","endpointAddress":"memory://localhost/Replies","transport":"memory"},{"id":"so-10","parentId":"so-9","startTime":"2024-06-15T10:30:00.050Z","durationMs":3,"status":"ok","operation":"receive","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","endpointName":"Replies","endpointAddress":"memory://localhost/Replies","transport":"memory"},{"id":"so-11","parentId":"so-10","startTime":"2024-06-15T10:30:00.053Z","durationMs":15,"status":"ok","operation":"consume","messageType":"ProcessRefundResponse","messageTypeIdentity":"urn:message:global:process-refund-response","consumerName":"quick-refund-saga"},{"id":"so-12","parentId":"so-11","startTime":"2024-06-15T10:30:00.054Z","durationMs":12,"status":"ok","operation":"saga-transition","sagaName":"quick-refund-saga","sagaInstanceId":"3f2504e0-4f89-11d3-9a0c-0305e82c3301","fromState":"AwaitingRefund","toState":"Completed","eventType":"ProcessRefundResponse"}]}' />

> **Warning: messages can arrive out of order.**
>
> In a distributed system, message delivery order is not guaranteed. If your saga handles two message types that could both initiate the process, configure both as initial transitions. Do not assume the first message you define in code is the first message the saga will receive in production.

# Tutorial: Build a refund saga

By the end of this section, you will have a working saga that receives a refund request, sends a command to the billing service, and returns a response when the refund completes.

## Define the saga state

Saga state must extend `SagaStateBase`. This base class provides `Id` (unique saga instance identifier), `State` (current state name), `Errors` (error history), and `Metadata` (custom key-value data).

```csharp
using Mocha.Sagas;

namespace MyApp.Sagas;

public class RefundSagaState : SagaStateBase
{
    // Order information captured when the saga starts
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }

    // Results populated during processing
    public Guid? RefundId { get; set; }
    public decimal? RefundedAmount { get; set; }
    public string? FailureReason { get; set; }

    // Factory method to create state from the initiating request
    public static RefundSagaState FromQuickRefund(RequestQuickRefundRequest request)
        => new()
        {
            OrderId = request.OrderId,
            Amount = request.Amount,
            CustomerId = request.CustomerId,
            Reason = request.Reason
        };

    // Helper to create the command sent to billing
    public ProcessRefundCommand ToProcessRefund()
        => new()
        {
            OrderId = OrderId,
            Amount = Amount,
            Reason = Reason,
            CustomerId = CustomerId
        };
}
```

Properties marked `required` are set when the saga starts. Mutable properties (`set`) are updated as transitions execute. The factory method converts the initiating message into the state object. Helper methods create the commands the saga dispatches to other services.

## Define the message contracts

```csharp
using Mocha;

namespace MyApp.Messages;

// The request that starts the saga (implements IEventRequest for request/reply)
public sealed class RequestQuickRefundRequest : IEventRequest<QuickRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string CustomerId { get; init; }
    public required string Reason { get; init; }
}

// The response returned when the saga completes
public sealed class QuickRefundResponse
{
    public required Guid OrderId { get; init; }
    public required bool Success { get; init; }
    public Guid? RefundId { get; init; }
    public decimal? RefundedAmount { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset CompletedAt { get; init; }
}

// Command sent by the saga to the billing service
public sealed class ProcessRefundCommand : IEventRequest<ProcessRefundResponse>
{
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required string Reason { get; init; }
    public required string CustomerId { get; init; }
}

// Response from the billing service
public sealed class ProcessRefundResponse
{
    public required Guid RefundId { get; init; }
    public required Guid OrderId { get; init; }
    public required decimal Amount { get; init; }
    public required bool Success { get; init; }
    public string? FailureReason { get; init; }
    public required DateTimeOffset ProcessedAt { get; init; }
}
```

## Define the saga

Subclass `Saga<TState>` and override `Configure` to define the state machine.

```csharp
using Mocha.Sagas;
using MyApp.Messages;

namespace MyApp.Sagas;

public sealed class QuickRefundSaga : Saga<RefundSagaState>
{
    // State name constants
    private const string AwaitingRefund = nameof(AwaitingRefund);
    private const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        // 1. Initial state: receive the refund request, create state, send command to billing
        descriptor
            .Initially()
            .OnRequest<RequestQuickRefundRequest>()
            .StateFactory(RefundSagaState.FromQuickRefund)
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingRefund);

        // 2. Awaiting refund: handle the billing service's reply
        descriptor
            .During(AwaitingRefund)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) =>
            {
                if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason
                        ?? "Refund processing failed";
                }
            })
            .TransitionTo(Completed);

        // 3. Final state: build and send the response back to the original requester
        descriptor
            .Finally(Completed)
            .Respond(state => new QuickRefundResponse
            {
                OrderId = state.OrderId,
                Success = state.RefundId.HasValue,
                RefundId = state.RefundId,
                RefundedAmount = state.RefundedAmount,
                FailureReason = state.FailureReason,
                CompletedAt = DateTimeOffset.UtcNow
            });
    }
}
```

The state machine has three states:

1. **Initial** - receives `RequestQuickRefundRequest`, creates `RefundSagaState`, sends `ProcessRefundCommand` to billing, transitions to `AwaitingRefund`.
2. **AwaitingRefund** - receives `ProcessRefundResponse` from billing, updates state with the result, transitions to `Completed`.
3. **Completed** (final) - builds `QuickRefundResponse` from state and sends it back to the original caller. The persisted saga state is then deleted.

## Register the saga

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMessageBus()
    .AddSaga<QuickRefundSaga>()
    .AddRabbitMQ();

var app = builder.Build();
app.Run();
```

`.AddSaga<T>()` registers the saga with the bus. The saga's consumer is created automatically and listens for the message types defined in the state machine transitions.

## Trigger the saga

From the sender side, use `RequestAsync` to start the saga and await the final response:

```csharp
var response = await bus.RequestAsync(
    new RequestQuickRefundRequest
    {
        OrderId = orderId,
        Amount = 49.99m,
        CustomerId = "customer-42",
        Reason = "Defective product"
    },
    cancellationToken);

Console.WriteLine($"Refund {response.RefundId}: success={response.Success}");
```

Expected output:

```text
info: Mocha.Sagas.Saga[0]
      Created saga state QuickRefundSaga 3f2504e0-4f89-11d3-9a0c-0305e82c3301
info: Mocha.Sagas.Saga[0]
      Entering state QuickRefundSaga Initial
info: Mocha.Sagas.Saga[0]
      Sending event QuickRefundSaga ProcessRefundCommand
info: Mocha.Sagas.Saga[0]
      Transitioning state QuickRefundSaga AwaitingRefund by event ProcessRefundResponse
info: Mocha.Sagas.Saga[0]
      Entering state QuickRefundSaga Completed
info: Mocha.Sagas.Saga[0]
      Replying to saga QuickRefundSaga ... QuickRefundResponse
info: Mocha.Sagas.Saga[0]
      Saga completed QuickRefundSaga 3f2504e0-4f89-11d3-9a0c-0305e82c3301
Refund d4c3b2a1-...: success=True
```

The saga receives the request, sends a command to billing, waits for the reply, builds a response, and completes. The caller's `RequestAsync` resolves with the typed `QuickRefundResponse`.

# How-to guides

## Coordinate parallel operations

When a saga needs to wait for multiple replies before proceeding, model each combination as a separate state. The `ReturnProcessingSaga` demonstrates this pattern - after inspection, it sends both a refund command and a restock command in parallel, then handles whichever reply arrives first.

```csharp
public sealed class ReturnProcessingSaga : Saga<RefundSagaState>
{
    private const string AwaitingInspection = nameof(AwaitingInspection);
    private const string AwaitingBothReplies = nameof(AwaitingBothReplies);
    private const string RestockDoneAwaitingRefund = nameof(RestockDoneAwaitingRefund);
    private const string RefundDoneAwaitingRestock = nameof(RefundDoneAwaitingRestock);
    private const string Completed = nameof(Completed);

    protected override void Configure(ISagaDescriptor<RefundSagaState> descriptor)
    {
        // Start: package received, send inspection command
        descriptor
            .Initially()
            .OnEvent<ReturnPackageReceivedEvent>()
            .StateFactory(RefundSagaState.FromReturnPackageReceived)
            .Send((_, state) => state.ToInspectReturn())
            .TransitionTo(AwaitingInspection);

        // After inspection: send refund AND restock in parallel
        descriptor
            .During(AwaitingInspection)
            .OnReply<InspectReturnResponse>()
            .Then((state, response) => state.InspectionResult = response.Result)
            .Send((_, state) => state.ToRestockInventory())
            .Send((_, state) => state.ToProcessRefund())
            .TransitionTo(AwaitingBothReplies);

        // Restock arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<RestockInventoryResponse>()
            .Then((state, response) =>
            {
                state.InventoryRestocked = response.Success;
                state.QuantityRestocked = response.QuantityRestocked;
            })
            .TransitionTo(RestockDoneAwaitingRefund);

        // Refund arrives first
        descriptor
            .During(AwaitingBothReplies)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) =>
            {
                if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                }
            })
            .TransitionTo(RefundDoneAwaitingRestock);

        // Second reply arrives: restock after refund
        descriptor
            .During(RefundDoneAwaitingRestock)
            .OnReply<RestockInventoryResponse>()
            .Then((state, response) =>
            {
                state.InventoryRestocked = response.Success;
                state.QuantityRestocked = response.QuantityRestocked;
            })
            .TransitionTo(Completed);

        // Second reply arrives: refund after restock
        descriptor
            .During(RestockDoneAwaitingRefund)
            .OnReply<ProcessRefundResponse>()
            .Then((state, response) =>
            {
                if (response.Success)
                {
                    state.RefundId = response.RefundId;
                    state.RefundedAmount = response.Amount;
                }
                else
                {
                    state.FailureReason = response.FailureReason;
                }
            })
            .TransitionTo(Completed);

        // Done
        descriptor.Finally(Completed);
    }
}
```

The key insight: `AwaitingBothReplies` has two transitions, one for each reply type. Whichever arrives first moves the saga to a "one done, waiting for the other" state. The second reply completes the saga. This avoids race conditions without explicit locking.

## Start a saga from an event

Not all sagas begin with a request/reply. Use `.OnEvent<T>()` instead of `.OnRequest<T>()` when the saga is initiated by a published event.

```csharp
descriptor
    .Initially()
    .OnEvent<ReturnPackageReceivedEvent>()
    .StateFactory(RefundSagaState.FromReturnPackageReceived)
    .Send((_, state) => state.ToInspectReturn())
    .TransitionTo(AwaitingInspection);
```

When using `.OnEvent<T>()`, the saga does not capture a reply address. No response is sent when the saga completes. Use `.OnRequest<T>()` when the caller expects a response via `RequestAsync`.

## Publish events from transitions

To publish an event as a side-effect of a transition, use `.Publish()` on the transition descriptor:

```csharp
descriptor
    .During(AwaitingRefund)
    .OnReply<ProcessRefundResponse>()
    .Then((state, response) =>
    {
        state.RefundId = response.RefundId;
        state.RefundedAmount = response.Amount;
    })
    .Publish((_, state) => new RefundCompletedEvent
    {
        OrderId = state.OrderId,
        RefundId = state.RefundId!.Value,
        Amount = state.RefundedAmount!.Value
    })
    .TransitionTo(Completed);
```

`.Publish()` dispatches the event after the transition action runs. The saga automatically adds the `saga-id` header to published messages so downstream consumers can correlate them back to the saga instance.

## Publish or send on state entry

To dispatch messages when entering a state (not tied to a specific transition), use `.OnEntry()` on the state descriptor:

```csharp
descriptor
    .During(AwaitingInspection)
    .OnEntry()
    .Publish(state => new InspectionStartedEvent
    {
        OrderId = state.OrderId,
        ReturnId = state.ReturnId!.Value
    });
```

On-entry actions run every time the saga enters that state, regardless of which transition caused it.

## Handle faults and compensation

### Compensating transactions

When a service-spanning process fails partway through, you cannot roll back completed steps the way a database transaction would. Instead, you issue compensating transactions - commands that logically undo the work already done. For example, if a refund succeeds but restocking fails, you may need to reverse the refund.

This is a fundamental property of distributed systems: without distributed ACID isolation, partial failure is always possible. The [Microsoft Azure Architecture documentation](https://learn.microsoft.com/en-us/azure/architecture/patterns/saga) classifies transactions in a saga as compensable (can be undone), pivot (the point of no return), or retryable (will eventually succeed). Design your `OnFault()` handlers with this taxonomy in mind.

### Implement fault handling

When a command sent by a saga fails, the receiving service sends a `NotAcknowledgedEvent` back instead of the expected reply. Use `OnFault()` to define a transition that handles this case and runs compensation logic.

```csharp
descriptor
    .During(AwaitingRefund)
    .OnFault()
    .Then((state, fault) =>
    {
        state.FailureReason = fault.ErrorMessage;
        state.FailureStage = "Refund";
    })
    .Send((_, state) => new ReverseChargeCommand
    {
        OrderId = state.OrderId,
        Amount = state.Amount
    })
    .TransitionTo("Compensating");
```

`OnFault()` is an extension method on `ISagaStateDescriptor<TState>` that registers a transition for `NotAcknowledgedEvent`. The fault record carries `ErrorCode`, `ErrorMessage`, `CorrelationId`, and `MessageId` so your compensation logic can identify what failed.

For sagas that send multiple commands in parallel, add `OnFault()` transitions in each waiting state. This ensures you can compensate regardless of which step failed.

## Handle timeouts

Long-running sagas may need to protect against the case where an expected reply never arrives. Use `OnTimeout()` to define what happens when the saga has been waiting too long.

### Schedule a timeout when entering a waiting state

Register the timeout as an on-entry action on the state that needs a deadline:

```csharp
descriptor
    .During(AwaitingRefund)
    .OnEntry()
    .ScheduleTimeout(TimeSpan.FromMinutes(5));  // fire SagaTimedOutEvent after 5 minutes

descriptor
    .During(AwaitingRefund)
    .OnTimeout()
    .Then((state, _) =>
    {
        state.FailureReason = "Refund timed out after waiting 5 minutes";
    })
    .Send((_, state) => new ReverseChargeCommand
    {
        OrderId = state.OrderId,
        Amount = state.Amount
    })
    .TransitionTo("TimedOut");

descriptor.Finally("TimedOut")
    .Respond(state => new QuickRefundResponse
    {
        OrderId = state.OrderId,
        Success = false,
        FailureReason = state.FailureReason,
        CompletedAt = DateTimeOffset.UtcNow
    });
```

When the saga transitions out of `AwaitingRefund` via a normal reply, the scheduled timeout is cancelled automatically. If the timeout fires first, `OnTimeout()` runs the configured transition - in this case, dispatching compensation and moving to `TimedOut`.

### What to do when a timeout fires

A fired timeout means the downstream service did not reply within the expected window. Common responses:

- Transition to a compensation state and issue undo commands for completed steps.
- Transition to a final state and send a failure response back to the original caller.
- Transition to a retry state and re-send the original command (only if idempotent).

Do not leave the saga in the same waiting state after a timeout. The saga must advance so it does not wait forever.

## Configure saga persistence with Postgres

By default, saga state is stored in memory and lost on restart. For production, use a persistent store. Mocha provides a Postgres-backed saga store via Entity Framework Core.

**1. Add the EF Core entity to your `DbContext`.**

```csharp
using Microsoft.EntityFrameworkCore;
using Mocha.Sagas.EfCore;

public class CatalogDbContext : DbContext
{
    public DbSet<SagaState> SagaStates => Set<SagaState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the saga state table
        modelBuilder.AddPostgresSagas();
    }
}
```

**2. Register the Postgres saga store with the bus.**

```csharp
builder.Services
    .AddMessageBus()
    .AddSaga<QuickRefundSaga>()
    .AddSaga<ReturnProcessingSaga>()
    .AddEntityFramework<CatalogDbContext>(p =>
    {
        p.AddPostgresSagas();
    })
    .AddRabbitMQ();
```

**3. Create an EF Core migration.**

```bash
dotnet ef migrations add Sagas
dotnet ef database update
```

## Create a saga with the fluent API

For simple sagas that do not need a dedicated class, use `Saga.Create<TState>()`:

```csharp
var saga = Saga.Create<RefundSagaState>(descriptor =>
{
    descriptor
        .Initially()
        .OnRequest<RequestQuickRefundRequest>()
        .StateFactory(RefundSagaState.FromQuickRefund)
        .Send((_, state) => state.ToProcessRefund())
        .TransitionTo("AwaitingRefund");

    descriptor
        .During("AwaitingRefund")
        .OnReply<ProcessRefundResponse>()
        .Then((state, response) =>
        {
            state.RefundId = response.RefundId;
            state.RefundedAmount = response.Amount;
        })
        .TransitionTo("Completed");

    descriptor
        .Finally("Completed")
        .Respond(state => new QuickRefundResponse
        {
            OrderId = state.OrderId,
            Success = state.RefundId.HasValue,
            RefundId = state.RefundId,
            RefundedAmount = state.RefundedAmount,
            FailureReason = state.FailureReason,
            CompletedAt = DateTimeOffset.UtcNow
        });
});
```

This produces the same state machine as the class-based approach. The fluent API is useful for tests and prototyping.

# Concurrency

If two messages for the same saga instance arrive simultaneously - for example, two parallel replies landing within milliseconds of each other - one succeeds and the other retries. The Postgres saga store uses optimistic concurrency with a version column. The second writer detects the version mismatch, reloads the latest state, and retries the transition.

This retry is automatic and transparent. You do not need to handle it in your saga code. However, if a saga endpoint processes a very high volume of concurrent messages for the same instance, retry contention can add latency. In that case, consider hosting high-concurrency sagas on dedicated endpoints with constrained parallelism.

# How saga correlation works

When a saga sends a command, Mocha attaches a `saga-id` header to the outgoing message. When the reply arrives, the saga runtime reads this header to find the existing saga instance and load its persisted state.

For event-initiated sagas, correlation uses the `ICorrelatable` interface:

```csharp
using Mocha.Sagas;

public sealed record SagaTimedOutEvent(Guid SagaId) : ICorrelatable
{
    public Guid? CorrelationId => SagaId;
}
```

Messages that implement `ICorrelatable` are matched to saga instances by their `CorrelationId`. This is how sagas handle events that are not direct replies to commands the saga sent.

The correlation lookup order is:

1. Check if the message implements `ICorrelatable` and has a non-null `CorrelationId`.
2. Check the message headers for a `saga-id` header.
3. If neither is found, treat the message as an initiating event and create a new saga instance.

# Next steps

Understand how transports work in [Transports](/docs/mocha/v1/transports).

> **Runnable examples:** [BasicSaga](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/Sagas/BasicSaga), [ParallelSaga](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Examples/Sagas/ParallelSaga)
>
> **Full demo:** [Demo.Catalog](https://github.com/ChilliCream/graphql-platform/tree/main/src/Mocha/src/Demo/Demo.Catalog) implements two production-style sagas: `QuickRefundSaga` (a simple two-state refund flow) and `ReturnProcessingSaga` (a complex multi-state saga with parallel inspection, restocking, and refund steps).

---
title: "Migrate from Wolverine"
metaTitle: "Migrate from Wolverine to the Mocha Message Bus"
description: "Map Wolverine handlers, InvokeAsync, cascading messages, error policies, the durable outbox, and sagas to Mocha, and plan a migration service by service."
---

Mocha and Wolverine cover the same ground: in-process commands and queries, messages between services over a broker, retries and redelivery, a transactional outbox and inbox, and sagas. The main differences are that Mocha finds handlers through interfaces instead of naming conventions, generates its code with a source generator when you compile instead of at runtime, and splits in-process dispatch (the [mediator](../mediator/index.md)) from messaging between services (the message bus). The part that needs planning is that a Mocha service and a Wolverine service cannot exchange messages with each other.

This guide maps Wolverine concepts to Mocha and walks you through migrating a service. For a side-by-side overview, see [Mocha vs. Wolverine](../../../comparison/mocha-vs-wolverine.md).

# Migrate with a coding agent

To let a coding agent such as Claude Code, Codex, or GitHub Copilot do the migration, copy this prompt into it. The prompt points the agent to this guide and has it check with you before it changes code.

<AgentPrompt title="Migration prompt">

```text
Migrate this codebase from Wolverine to Mocha, the MIT-licensed .NET mediator
and message bus from ChilliCream.

Follow the migration guide:
https://chillicream.com/docs/mocha/migration/coming-from-wolverine.md
Look up Mocha APIs in https://chillicream.com/docs/mocha/llms.txt instead of
guessing them.

1. Inventory the Wolverine usage: transports, handlers, cascading messages,
   InvokeAsync calls to local and remote handlers, error policies, durable
   messaging and the outbox, sagas, batch handlers, scheduled messages, and
   middleware. If the codebase uses something the guide does not cover, stop
   and ask me how to proceed.
2. Propose a plan that moves the services by message flow, as the guide
   describes, and wait for my approval.
3. Migrate one service at a time, in the order of the guide's steps. Keep the
   solution building after every step, and keep message contracts unchanged
   unless the guide says otherwise.
4. Build, run the tests, fix every diagnostic the Mocha source generator
   reports, and start the services to check that every message flow still
   works.
5. Remove Wolverine once nothing references it. Summarize what changed, what
   behaves differently, and anything you could not migrate.
```

</AgentPrompt>

# Plan the migration by message flow

Mocha and Wolverine use different message envelopes and different exchange and queue names, so a Mocha service cannot consume what a Wolverine service publishes, and the other way around. Migrate the services that exchange a message together, or move one message flow at a time and keep both libraries registered in a service until all its flows have moved. Both libraries register an `IMessageBus` service in different namespaces, so files that use both need a namespace alias.

# Concept mapping

| Wolverine                                                         | Mocha                                                                               | Key difference                                                                         |
| ----------------------------------------------------------------- | ----------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------- |
| `UseWolverine(opts => ...)`                                       | `AddMessageBus()`, and `AddMediator()` for inline calls                             | Configuration is a chain of builder calls.                                             |
| Handler discovery by naming conventions                           | `.Add{Module}()`                                                                    | Generated at build time from the handlers that implement Mocha's interfaces.           |
| `codegen write`, `TypeLoadMode`, `WolverineFx.RuntimeCompilation` | Nothing to configure                                                                | The source generator writes the code on every build.                                   |
| `opts.UseRabbitMq()`, `opts.UseAzureServiceBus(...)`              | `.AddRabbitMQ()`, `.AddAzureServiceBus(...)`                                        | RabbitMQ reads the `IConnectionFactory` from dependency injection.                     |
| `Handle(T message)` for an event                                  | `IEventHandler<T>`                                                                  | `HandleAsync(message, cancellationToken)`. Dependencies come through the constructor.  |
| `Handle(T message)` for a command                                 | `IEventRequestHandler<T>`                                                           | Commands are sent to one queue, named after the message type.                          |
| `Handle(T message)` that returns a response                       | `IEventRequestHandler<TRequest, TResponse>`                                         | The request implements `IEventRequest<TResponse>`.                                     |
| A handler that takes `Envelope` or `IMessageContext`              | `IConsumer<T>`                                                                      | `ConsumeAsync(IConsumeContext<T>)` gives access to headers and message metadata.       |
| Cascading messages returned from a handler                        | `IMessageBus.PublishAsync` or `SendAsync` in the handler                            | Messages are sent explicitly.                                                          |
| `InvokeAsync` and `InvokeAsync<T>` on a local handler             | Mediator `SendAsync` and `QueryAsync`                                               | Mediator messages implement `ICommand`, `ICommand<TResponse>`, or `IQuery<TResponse>`. |
| `InvokeAsync<T>` on a remote handler                              | `IMessageBus.RequestAsync`                                                          | The response type comes from the request type.                                         |
| `PublishAsync`, `SendAsync`                                       | `IMessageBus.PublishAsync`, `SendAsync`                                             | Same concept.                                                                          |
| `ScheduleAsync`                                                   | `IMessageBus.ScheduleSendAsync`, `SchedulePublishAsync`                             | The result carries a token to cancel the message.                                      |
| `PublishMessage<T>().ToRabbitQueue(...)`                          | Routing by convention, or `AddMessage<T>(m => m.Send(r => r.ToQueue("...")))`       | Commands go to the queue named after the message type.                                 |
| `Policies.OnException<T>().RetryTimes(...)`                       | `AddResilience(policy => policy.On<T>().Retry(...))`                                | Same concept.                                                                          |
| `.Then.ScheduleRetry(...)`                                        | `.ThenRedeliver(...)`                                                               | Same concept.                                                                          |
| `.MoveToErrorQueue()`, `.Discard()`                               | `.DeadLetter()`, `.Discard()`                                                       | Same concept.                                                                          |
| The dead letter queue                                             | `{endpoint}_error` endpoint                                                         | One error endpoint per receive endpoint.                                               |
| `PersistMessagesWithPostgresql` and the durable outbox policies   | `AddEntityFramework<TContext>(p => { p.UsePostgresOutbox(); p.UseTransaction(); })` | The outbox lives in your `DbContext`.                                                  |
| The durable inbox                                                 | `p.UsePostgresInbox()`                                                              | Deduplicates by message ID and consumer.                                               |
| `AutoApplyTransactions()`, `[Transactional]`                      | `p.UseTransaction()`, mediator `UseEntityFrameworkTransactions<TContext>()`         | Wraps every bus handler, or every mediator command, in a transaction.                  |
| `Before`, `After`, and `Finally` middleware                       | Consumer middleware, or mediator middleware                                         | Middleware wraps `next` instead of matching method names.                              |
| `Saga` with `Start` and `Handle` methods                          | `Saga<TState>`                                                                      | States are strings, transitions are configured in `Configure`.                         |
| Saga identity conventions and `[SagaIdentity]`                    | The saga ID, from the `saga-id` header or `ICorrelatable.CorrelationId`             | Mocha correlates by saga ID only. The state factory can set the ID.                    |
| `TimeoutMessage`                                                  | `Timeout(...)` on the saga, with `OnTimeout()`                                      | One deadline per saga, canceled when the saga reaches a final state.                   |
| `MarkCompleted()`                                                 | A final state, `Finally(...)`                                                       | Reaching a final state completes the saga.                                             |
| `BatchMessagesOf<T>` with a `T[]` handler                         | `IBatchEventHandler<T>` and `AddBatchHandler<T>(...)`                               | The handler receives an `IMessageBatch<T>`.                                            |

# Migrate step by step

## 1. Install the packages

```bash
dotnet add package Mocha
dotnet add package Mocha.Analyzers
dotnet add package Mocha.Transport.RabbitMQ
```

Use `Mocha.Transport.AzureServiceBus` for Azure Service Bus, and add `Mocha.Mediator` for handlers you call inline with `InvokeAsync`. `Mocha.Analyzers` contains the source generator that writes the registration method. Add it to every project that contains handlers. Mocha targets .NET 8 and later.

## 2. Register the bus

```csharp
// Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.UseRabbitMq()
        .AutoProvision()
        .UseConventionalRouting();
});

// Mocha
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory { HostName = "localhost" });

builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

The generator names the registration method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()`. With Aspire, `builder.AddRabbitMQClient("rabbitmq")` registers the connection factory for you. See [RabbitMQ](../transports/rabbitmq.md) and [Azure Service Bus](../transports/azure-service-bus.md) for the transport options.

Mocha creates exchanges and queues at startup from naming conventions, like `AutoProvision` and conventional routing do. The names differ from Wolverine's. [Routing and endpoints](../routing-and-endpoints.md) explains the conventions and how to override them.

## 3. Turn handlers into handler classes

A Mocha handler implements an interface that names its message, so the class and method names no longer matter. Move dependencies from method parameters to the constructor, and rename the method to `HandleAsync`, which returns a `ValueTask`.

```csharp
// Wolverine
public static class OrderPlacedHandler
{
    public static Task Handle(
        OrderPlaced message, IInvoiceService invoices, CancellationToken cancellationToken)
        => invoices.CreateAsync(message.OrderId, cancellationToken);
}

// Mocha: an event
public class OrderPlacedHandler(IInvoiceService invoices) : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        => await invoices.CreateAsync(message.OrderId, cancellationToken);
}
```

A handler of a command becomes an `IEventRequestHandler<TCommand>`. A handler that returns a response becomes an `IEventRequestHandler<TRequest, TResponse>`, and the request declares its response:

```csharp
// Wolverine
public record CheckOrderStatus(Guid OrderId);
public record OrderStatus(Guid OrderId, string Status);

public static class CheckOrderStatusHandler
{
    public static OrderStatus Handle(CheckOrderStatus request)
        => new(request.OrderId, "Shipped");
}

// Mocha
public record CheckOrderStatus(Guid OrderId) : IEventRequest<OrderStatus>;
public record OrderStatus(Guid OrderId, string Status);

public class CheckOrderStatusHandler : IEventRequestHandler<CheckOrderStatus, OrderStatus>
{
    public ValueTask<OrderStatus> HandleAsync(CheckOrderStatus request, CancellationToken cancellationToken)
        => ValueTask.FromResult(new OrderStatus(request.OrderId, "Shipped"));
}
```

A Wolverine handler that returns other messages cascades them. In Mocha, inject `IMessageBus` and publish or send them. Messages published or sent from a handler carry the conversation ID of the incoming message, and their causation ID is the incoming message's ID.

```csharp
// Wolverine
public static class ShipOrderHandler
{
    public static OrderShipped Handle(ShipOrder command, AppDbContext db)
    {
        var shipment = new Shipment(command.OrderId);
        db.Shipments.Add(shipment);
        return new OrderShipped(command.OrderId);
    }
}

// Mocha
public class ShipOrderHandler(AppDbContext db, IMessageBus bus) : IEventRequestHandler<ShipOrder>
{
    public async ValueTask HandleAsync(ShipOrder command, CancellationToken cancellationToken)
    {
        var shipment = new Shipment(command.OrderId);
        db.Shipments.Add(shipment);
        await bus.PublishAsync(new OrderShipped(command.OrderId), cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
```

With the outbox from step 7, the event is written in the same transaction as the shipment and sent after the commit, as a cascaded message is sent after the handler succeeds.

When a handler reads headers or the envelope, implement Mocha's `IConsumer<T>` instead. Its `ConsumeAsync` method receives an `IConsumeContext<T>`. See [Handlers and consumers](../handlers-and-consumers.md).

## 4. Move inline calls to the mediator

Wolverine uses `IMessageBus.InvokeAsync` both for handlers in the same process and for requests to other services. Mocha splits the two. For a handler in the same process, use the mediator: messages implement `ICommand`, `ICommand<TResponse>`, or `IQuery<TResponse>`, and handlers implement the matching interface.

```csharp
// Wolverine
var orderId = await bus.InvokeAsync<Guid>(new PlaceOrder(productId), cancellationToken);

// Mocha
public record PlaceOrder(Guid ProductId) : ICommand<Guid>;

public class PlaceOrderHandler(AppDbContext db) : ICommandHandler<PlaceOrder, Guid>
{
    public async ValueTask<Guid> HandleAsync(PlaceOrder command, CancellationToken cancellationToken)
    {
        var order = new Order(command.ProductId);
        db.Orders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        return order.Id;
    }
}

builder.Services
    .AddMediator()
    .AddOrdering();

var orderId = await sender.SendAsync(new PlaceOrder(productId), cancellationToken);
```

The source generator writes `AddOrdering()` for both the mediator and the bus, each with the handlers it can dispatch. Mediator middleware replaces Wolverine middleware on these calls. See [Mediator](../mediator/index.md) and [Pipeline and middleware](../mediator/pipeline-and-middleware.md).

## 5. Update publish, send, and request call sites

```csharp
// Wolverine
await bus.PublishAsync(new OrderPlaced(orderId));

await bus.SendAsync(new ReserveInventory(orderId));

var status = await bus.InvokeAsync<OrderStatus>(new CheckOrderStatus(orderId), cancellationToken);

// Mocha
await bus.PublishAsync(new OrderPlaced(orderId), cancellationToken);

await bus.SendAsync(new ReserveInventory(orderId), cancellationToken);

var status = await bus.RequestAsync(new CheckOrderStatus(orderId), cancellationToken);
```

In Mocha, `bus` is `Mocha.IMessageBus`. `SendAsync` sends to the queue of the message type's handler by convention. To send somewhere else, configure a route with `AddMessage<T>(m => m.Send(r => r.ToQueue("...")))` or set `SendOptions.Endpoint` on the call.

`RequestAsync` waits up to two minutes for a response, where a remote `InvokeAsync<T>` waits five seconds. A failing handler surfaces as a `RemoteErrorException`, a missing response as a `ResponseTimeoutException`. To give up sooner, pass a cancellation token that is canceled after your timeout.

## 6. Replace error policies

Both libraries choose what happens to a failed message by exception type. The chains map one to one.

```csharp
// Wolverine
builder.Host.UseWolverine(opts =>
{
    opts.Policies.OnException<ValidationException>().MoveToErrorQueue();

    opts.Policies.OnAnyException()
        .RetryTimes(3)
        .Then.ScheduleRetry(5.Minutes(), 15.Minutes(), 30.Minutes())
        .Then.MoveToErrorQueue();
});

// Mocha
builder.Services
    .AddMessageBus()
    .AddResilience(policy =>
    {
        policy.On<ValidationException>().DeadLetter();

        policy.Default()
            .Retry()
            .ThenRedeliver()
            .ThenDeadLetter();
    })
    .AddOrdering()
    .AddRabbitMQ();
```

`Retry()` retries three times with exponential backoff, and `ThenRedeliver()` redelivers after 5, 15, and 30 minutes. Both take arguments to change the counts and delays. Without `AddResilience`, Mocha does not retry: a failing message goes straight to the error endpoint, where Wolverine tries three times first. Delayed redelivery needs a scheduling store. RabbitMQ has none of its own, so register the PostgreSQL scheduling store, otherwise Mocha redelivers immediately. See [Exception policies](../exception-policies.md) and [Scheduling](../scheduling.md).

## 7. Move the outbox and inbox

Mocha's outbox and inbox use Entity Framework Core with PostgreSQL. Map the entities in your `DbContext` and register them on the bus:

```csharp
// Wolverine
opts.Services.AddDbContextWithWolverineIntegration<AppDbContext>(x => x.UseNpgsql(connectionString));
opts.PersistMessagesWithPostgresql(connectionString, "wolverine");
opts.UseEntityFrameworkCoreTransactions();
opts.Policies.AutoApplyTransactions();
opts.Policies.UseDurableLocalQueues();
opts.Policies.UseDurableOutboxOnAllSendingEndpoints();
opts.Policies.UseDurableInboxOnAllListeners();

// Mocha
builder.Services.AddDbContext<AppDbContext>(x => x.UseNpgsql(connectionString));

builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddEntityFramework<AppDbContext>(p =>
    {
        p.UsePostgresOutbox();
        p.UseTransaction();
        p.UsePostgresInbox();
    })
    .AddRabbitMQ();
```

The outbox and inbox are in the `Mocha.EntityFrameworkCore.Postgres` package. In `OnModelCreating`, add `modelBuilder.AddPostgresOutbox()` and `modelBuilder.AddPostgresInbox()`, then add a database migration. With the outbox enabled, `PublishAsync` and `SendAsync` inside a handler write to the outbox table in the handler's transaction. Let Wolverine dispatch its pending messages before you switch, because Mocha does not read Wolverine's tables. See [Reliability](../reliability.md).

## 8. Rewrite sagas

A Wolverine saga is a class whose methods are found by convention. A Mocha saga is a state machine: the same messages move it between named states, and the actions run on each transition.

```csharp
// Wolverine
public record OrderSubmitted(Guid OrderId);
public record OrderAccepted([property: SagaIdentity] Guid OrderId);
public record OrderExpired(Guid Id) : TimeoutMessage(30.Minutes());

public class OrderSaga : Saga
{
    public Guid Id { get; set; }

    public static (OrderSaga, OrderExpired) Start(OrderSubmitted submitted)
        => (new OrderSaga { Id = submitted.OrderId }, new OrderExpired(submitted.OrderId));

    public void Handle(OrderAccepted accepted) => MarkCompleted();

    public void Handle(OrderExpired expired) => MarkCompleted();
}

// Mocha
public class OrderState : SagaStateBase
{
}

public record OrderSubmitted(Guid OrderId);

public record OrderAccepted(Guid OrderId) : ICorrelatable
{
    public Guid? CorrelationId => OrderId;
}

public sealed class OrderSaga : Saga<OrderState>
{
    private const string Submitted = nameof(Submitted);
    private const string Accepted = nameof(Accepted);

    protected override void Configure(ISagaDescriptor<OrderState> descriptor)
    {
        // One deadline for the saga, canceled when it reaches a final state
        descriptor.Timeout(TimeSpan.FromMinutes(30));

        descriptor
            .Initially()
            .OnEvent<OrderSubmitted>()
            .StateFactory(e => new OrderState { Id = e.OrderId })
            .TransitionTo(Submitted);

        descriptor
            .During(Submitted)
            .OnEvent<OrderAccepted>()
            .TransitionTo(Accepted);

        descriptor
            .During(Submitted)
            .OnTimeout()
            .TransitionTo(StateNames.TimedOut);

        descriptor.Finally(Accepted);
    }
}
```

The main differences:

- **Correlation.** Wolverine finds the saga through a member named by convention or marked `[SagaIdentity]`. Mocha correlates by the saga's ID. Set the ID in the state factory, here to the order ID, and let later events return it from `ICorrelatable.CorrelationId`. Replies to commands the saga sent carry the ID in the `saga-id` header. The message that starts a saga must not return an `ICorrelatable.CorrelationId` or carry a `saga-id` header, because Mocha then looks for an existing instance instead of creating one.
- **Completion.** `MarkCompleted()` becomes a transition to a final state.
- **Timeouts.** `Timeout(TimeSpan)` replaces a returned `TimeoutMessage`, and `OnTimeout()` handles it. Mocha has one deadline per saga: `Timeout(TimeSpan)` on the descriptor schedules it when the saga starts, cancels it when the saga reaches a final state, and adds the final state `StateNames.TimedOut`. The timeout needs a scheduling store. On RabbitMQ, register the PostgreSQL scheduling store with `UsePostgresScheduling()`, otherwise a saga with a timeout fails to start. A saga that needs several deadlines can schedule its own messages with `ScheduledSend` in a transition.
- **Persistence.** The generated registration method registers the saga, so call `.AddSaga<OrderSaga>()` only when you register handlers by hand. Store saga state in PostgreSQL with `AddEntityFramework<AppDbContext>(p => p.AddPostgresSagas())` and `modelBuilder.AddPostgresSagas()`, or in memory with `services.AddInMemorySagas()` for tests.

[Sagas](../sagas.md) covers state, transitions, faults, compensation, and timeouts.

## 9. Move batch handlers and scheduled messages

A Wolverine batch handler takes an array. A Mocha batch handler implements `IBatchEventHandler<T>` and receives an `IMessageBatch<T>`, which you can iterate like a list:

```csharp
// Wolverine
opts.BatchMessagesOf<OrderPlaced>(batching =>
{
    batching.BatchSize = 50;
    batching.TriggerTime = 10.Seconds();
});

// Mocha
builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddBatchHandler<OrderPlacedBatchHandler>(opts =>
    {
        opts.MaxBatchSize = 50;
        opts.BatchTimeout = TimeSpan.FromSeconds(10);
    })
    .AddRabbitMQ();
```

`ScheduleAsync` becomes `ScheduleSendAsync` or `SchedulePublishAsync`, which take a `DateTimeOffset` and return a `SchedulingResult`. Pass its `Token` to `bus.CancelScheduledMessageAsync` to cancel the message. Azure Service Bus and the PostgreSQL transport schedule natively. RabbitMQ needs the PostgreSQL scheduling store: register it with `p.UsePostgresScheduling()` in `AddEntityFramework`, add `modelBuilder.AddPostgresScheduledMessages()` in `OnModelCreating`, and add a database migration. See [Handlers and consumers](../handlers-and-consumers.md#batch-handler) and [Scheduling](../scheduling.md).

## 10. Remove code generation and update tests

Once no handler depends on Wolverine, remove the `WolverineFx.*` packages, the generated code in `Internal/Generated`, and any `codegen write` step in your build. Mocha's source generator runs on every build, so there is nothing to generate or check in.

Mocha emits OpenTelemetry traces and metrics after you call `.AddInstrumentation()` on the bus. Subscribe to the `Mocha` activity source and meter instead of Wolverine's. See [Observability](../observability.md).

Tests that used tracked sessions can run the bus with the in-memory transport instead: register `.AddInMemory()` from the `Mocha.Transport.InMemory` package in place of the broker and assert on the effects of your handlers. Mediator handlers can be tested by sending through `ISender` or by calling the handler directly.

# Next steps

- [Handlers and consumers](../handlers-and-consumers.md) covers every handler interface.
- [Mediator](../mediator/index.md) covers in-process commands, queries, and notifications.
- [Reliability](../reliability.md) covers fault handling, the outbox, and the inbox.
- [Sagas](../sagas.md) covers state machines in depth.

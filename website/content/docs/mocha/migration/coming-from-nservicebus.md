---
title: "Migrate from NServiceBus"
metaTitle: "Migrate from NServiceBus to the Mocha Message Bus"
description: "Map NServiceBus endpoints, message handlers, routing, callbacks, recoverability, the outbox, and sagas to Mocha, and plan a migration service by service."
---

Mocha and NServiceBus share most of their concepts: services that publish events and send commands over a broker, message handlers, replies, immediate and delayed retries, a transactional outbox, and sagas with timeouts. Most of a migration is renaming. The part that needs planning is that a Mocha service and an NServiceBus endpoint cannot exchange messages with each other.

This guide maps NServiceBus concepts to Mocha and walks you through migrating a service. For a side-by-side overview, including licensing, see [Mocha vs. NServiceBus](../../../comparison/mocha-vs-nservicebus.md).

# Migrate with a coding agent

To let a coding agent such as Claude Code, Codex, or GitHub Copilot do the migration, copy this prompt into it. The prompt points the agent to this guide and has it check with you before it changes code.

<AgentPrompt title="Migration prompt">

```text
Migrate this codebase from NServiceBus to Mocha, the MIT-licensed .NET message
bus from ChilliCream.

Follow the migration guide:
https://chillicream.com/docs/mocha/migration/coming-from-nservicebus.md
Look up Mocha APIs in https://chillicream.com/docs/mocha/llms.txt instead of
guessing them.

1. Inventory the NServiceBus usage: endpoints, transports, persistence,
   message types, handlers, command routes, callbacks, recoverability
   settings, the outbox, sagas and their timeouts, and delayed messages. If
   the codebase uses something the guide does not cover, stop and ask me how
   to proceed.
2. Propose a plan that moves the services by message flow, as the guide
   describes, and wait for my approval.
3. Migrate one message flow at a time, moving communicating services together
   or keeping both libraries registered until the flow has moved. Build after
   each coherent batch and keep message contracts unchanged unless the guide
   says otherwise. Publish reply-producing saga triggers from the initial
   transition in the same transaction as the saga state.
4. Build, run the tests, fix every diagnostic the Mocha source generator
   reports, and start the services to check that every message flow still
   works.
5. Remove NServiceBus once nothing references it. Summarize what changed, what
   behaves differently, and anything you could not migrate.
```

</AgentPrompt>

# Plan the migration by message flow

Mocha and NServiceBus use different message envelopes and different exchange and queue names, so a Mocha service cannot consume what an NServiceBus endpoint sends or publishes, and the other way around. Migrate the services that exchange a message together, or move one message flow at a time and keep both libraries registered in a service until all its flows have moved. The two libraries register separate services, so they can run in the same application.

# Concept mapping

| NServiceBus                                              | Mocha                                                                               | Key difference                                                                                           |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------- | -------------------------------------------------------------------------------------------------------- |
| `EndpointConfiguration` and `AddNServiceBusEndpoint`     | `AddMessageBus()`                                                                   | Configuration is a chain of builder calls. The service name comes from `.Host(h => h.ServiceName(...))`. |
| Assembly scanning or `[Handler]` registration            | `.Add{Module}()`                                                                    | Generated at build time from the handlers in the project.                                                |
| `UseTransport(new RabbitMQTransport(...))`               | `.AddRabbitMQ()`, `.AddAzureServiceBus(...)`                                        | RabbitMQ reads the `IConnectionFactory` from dependency injection.                                       |
| `ICommand`, `IEvent`, `IMessage`, or message conventions | Plain classes and records                                                           | Only requests declare their response, with `IEventRequest<TResponse>`.                                   |
| `IHandleMessages<T>` for an event                        | `IEventHandler<T>`                                                                  | `HandleAsync(message, cancellationToken)` receives the message, not a context.                           |
| `IHandleMessages<T>` for a command                       | `IEventRequestHandler<T>`                                                           | Commands are sent to one queue, named after the message type.                                            |
| `IHandleMessages<T>` that calls `context.Reply`          | `IEventRequestHandler<TRequest, TResponse>`                                         | The handler returns the response.                                                                        |
| `IMessageHandlerContext` for headers                     | `IConsumer<T>`                                                                      | `ConsumeAsync(IConsumeContext<T>)` gives access to headers and message metadata.                         |
| `IMessageSession.Publish`, `context.Publish`             | `IMessageBus.PublishAsync`                                                          | Inject `IMessageBus` into handlers. The conversation ID flows from the incoming message.                 |
| `Send` with `routing.RouteToEndpoint(...)`               | `IMessageBus.SendAsync`                                                             | No route table. The destination comes from the message type, a configured route, or `SendOptions`.       |
| Callbacks (`Request<T>`) from `NServiceBus.Callbacks`    | `IMessageBus.RequestAsync`                                                          | Built in. Returns the response message directly.                                                         |
| `SendOptions.DelayDeliveryWith`, `DoNotDeliverBefore`    | `IMessageBus.ScheduleSendAsync`, `SchedulePublishAsync`                             | Publishing can be scheduled too. The result carries a token to cancel the message.                       |
| `Recoverability().Immediate(...)`                        | `AddResilience(policy => policy.Default().Retry(...))`                              | Exception policies choose retry, redelivery, dead letter, or discard per exception type.                 |
| `Recoverability().Delayed(...)`                          | `.ThenRedeliver(...)`                                                               | Takes a count and base delay or explicit intervals.                                                      |
| `AddUnrecoverableException<T>()`                         | `policy.On<T>().DeadLetter()`                                                       | Same concept.                                                                                            |
| `error` queue                                            | `{endpoint}_error` endpoint                                                         | One error endpoint per receive endpoint.                                                                 |
| `EnableOutbox()`                                         | `AddEntityFramework<TContext>(p => { p.UsePostgresOutbox(); p.UseTransaction(); })` | Your handlers write through their own `DbContext`.                                                       |
| Outbox deduplication                                     | `p.UsePostgresInbox()`                                                              | Deduplicates by message ID and consumer.                                                                 |
| `Saga<TData>`, `ContainSagaData`                         | `Saga<TState>`, `SagaStateBase`                                                     | States are strings, transitions are configured in `Configure`.                                           |
| `ConfigureHowToFindSaga` and `MapSaga`                   | The saga ID, from the `saga-id` header or `ICorrelatable.CorrelationId`             | Mocha correlates by saga ID only. The state factory can set the ID.                                      |
| `RequestTimeout<T>` and `IHandleTimeouts<T>`             | `Timeout(...)` on the saga, with `OnTimeout()`                                      | One deadline per saga, canceled when the saga reaches a final state.                                     |
| `MarkAsComplete()`                                       | A final state, `Finally(...)`                                                       | Reaching a final state completes the saga.                                                               |
| `ReplyToOriginator`                                      | `OnRequest<T>()` and `.Respond(...)` on a final state                               | A saga started by a request answers it when it finishes.                                                 |

# Migrate step by step

## 1. Install the packages

```bash
dotnet add package Mocha
dotnet add package Mocha.Analyzers
dotnet add package Mocha.Transport.RabbitMQ
```

Use `Mocha.Transport.AzureServiceBus` for Azure Service Bus. `Mocha.Analyzers` contains the source generator that writes the registration method. Add it to every project that contains handlers. Mocha targets .NET 8 and later.

## 2. Register the bus

```csharp
// NServiceBus
var endpointConfiguration = new EndpointConfiguration("Ordering");
endpointConfiguration.UseSerialization<SystemJsonSerializer>();
endpointConfiguration.EnableInstallers();

var routing = endpointConfiguration.UseTransport(
    new RabbitMQTransport(RoutingTopology.Conventional(QueueType.Quorum), "host=localhost"));
routing.RouteToEndpoint(typeof(ReserveInventory), "Inventory");

builder.Services.AddNServiceBusEndpoint(endpointConfiguration);

// Mocha
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory { HostName = "localhost" });

builder.Services
    .AddMessageBus()
    .Host(h => h.ServiceName("ordering"))
    .AddOrdering()
    .AddRabbitMQ();
```

The generator names the registration method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()`. With Aspire, `builder.AddRabbitMQClient("rabbitmq")` registers the connection factory for you. If the NServiceBus endpoint also uses Aspire's RabbitMQ resource during migration, pass its management endpoint to `RabbitMQTransport.ManagementApiConfiguration` rather than assuming port 15672. See [RabbitMQ](../transports/rabbitmq.md) and [Azure Service Bus](../transports/azure-service-bus.md) for the transport options.

Mocha creates exchanges and queues at startup from naming conventions, like `EnableInstallers` does, and needs no command routes. The service name prefixes the queues of event handlers, so each service gets its own copy of an event. [Routing and endpoints](../routing-and-endpoints.md) explains the conventions and how to override them.

## 3. Turn message handlers into handlers

Pick the handler interface by what the message is. Most handlers only need the message, so `HandleAsync` receives the message and a cancellation token instead of a context. The `ICommand` and `IEvent` marker interfaces are no longer needed.

```csharp
// NServiceBus
public record OrderPlaced(Guid OrderId) : IEvent;

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IHandleMessages<OrderPlaced>
{
    public Task Handle(OrderPlaced message, IMessageHandlerContext context)
    {
        logger.LogInformation("Order {OrderId} placed", message.OrderId);
        return Task.CompletedTask;
    }
}

// Mocha: an event
public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} placed", message.OrderId);
        return ValueTask.CompletedTask;
    }
}
```

A handler of a command becomes an `IEventRequestHandler<TCommand>`. A handler that replies becomes an `IEventRequestHandler<TRequest, TResponse>` and returns the response:

```csharp
// NServiceBus
public record CheckOrderStatus(Guid OrderId) : IMessage;
public record OrderStatus(Guid OrderId, string Status) : IMessage;

public class CheckOrderStatusHandler : IHandleMessages<CheckOrderStatus>
{
    public Task Handle(CheckOrderStatus message, IMessageHandlerContext context)
        => context.Reply(new OrderStatus(message.OrderId, "Shipped"));
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

When a handler reads headers or message metadata, implement Mocha's `IConsumer<T>` instead. Its `ConsumeAsync` method receives an `IConsumeContext<T>`. See [Handlers and consumers](../handlers-and-consumers.md).

## 4. Update publish, send, and request call sites

Replace `IMessageSession` and the handler context with `IMessageBus`. Inside a handler, inject `IMessageBus` through the constructor. Messages published or sent from a handler carry the conversation ID of the incoming message, and their causation ID is the incoming message's ID. Unlike NServiceBus, Mocha does not pass on the correlation ID.

```csharp
// NServiceBus
await messageSession.Publish(new OrderPlaced(orderId), cancellationToken);

await messageSession.Send(new ReserveInventory(orderId), cancellationToken);

// With NServiceBus.Callbacks
var status = await messageSession.Request<OrderStatus>(new CheckOrderStatus(orderId), cancellationToken);

// Mocha
await bus.PublishAsync(new OrderPlaced(orderId), cancellationToken);

await bus.SendAsync(new ReserveInventory(orderId), cancellationToken);

var status = await bus.RequestAsync(new CheckOrderStatus(orderId), cancellationToken);
```

`SendAsync` sends to the queue of the message type's handler by convention. To send somewhere else, configure a route with `AddMessage<T>(m => m.Send(r => r.ToQueue("...")))` or set `SendOptions.Endpoint` on the call.

`RequestAsync` waits up to two minutes for a response. A failing handler surfaces as a `RemoteErrorException`, a missing response as a `ResponseTimeoutException`. To give up sooner, pass a cancellation token that is canceled after your timeout. Remove `EnableCallbacks()` and `MakeInstanceUniquelyAddressable(...)`: Mocha gives every service instance its own reply queue.

## 5. Replace recoverability

NServiceBus configures recoverability once per endpoint. Mocha configures exception policies on the bus, a transport, or a single receive endpoint, and picks the policy by exception type.

```csharp
// NServiceBus
var recoverability = endpointConfiguration.Recoverability();
recoverability.AddUnrecoverableException<ValidationException>();
recoverability.Immediate(immediate => immediate.NumberOfRetries(3));
recoverability.Delayed(delayed =>
{
    delayed.NumberOfRetries(3);
    delayed.TimeIncrease(TimeSpan.FromMinutes(5));
});

// Mocha
builder.Services
    .AddMessageBus()
    .AddResilience(policy =>
    {
        policy.On<ValidationException>().DeadLetter();

        policy.Default()
            .Retry(3, TimeSpan.Zero)
            .ThenRedeliver(
            [
                TimeSpan.FromMinutes(5),
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(15)
            ]);
    })
    .AddOrdering()
    .AddRabbitMQ();
```

Inventory the effective recoverability policy for each NServiceBus endpoint, including defaults, and configure each Mocha bus explicitly. Without `AddResilience`, Mocha does not retry: a failing message goes straight to the error endpoint. `Retry(3, TimeSpan.Zero)` requests three immediate retries; `Retry()` without arguments uses exponential backoff. `ThenRedeliver()` without arguments redelivers after 5, 15, and 30 minutes. After retries and redeliveries are exhausted, the fault pipeline routes the message to the error endpoint. Delayed redelivery needs a scheduling store. RabbitMQ has none of its own, so register the PostgreSQL scheduling store, otherwise Mocha redelivers immediately. See [Exception policies](../exception-policies.md) and [Scheduling](../scheduling.md).

## 6. Move the outbox

The NServiceBus outbox stores outgoing messages through the endpoint's persistence, and handlers write their data through the synchronized storage session. Mocha's outbox and inbox use Entity Framework Core with PostgreSQL, so handlers write through their own `DbContext`:

```csharp
// NServiceBus
var persistence = endpointConfiguration.UsePersistence<SqlPersistence>();
var dialect = persistence.SqlDialect<SqlDialect.PostgreSql>();
dialect.JsonBParameterModifier(p => ((NpgsqlParameter)p).NpgsqlDbType = NpgsqlDbType.Jsonb);
persistence.ConnectionBuilder(() => new NpgsqlConnection(connectionString));
endpointConfiguration.EnableOutbox();

// Mocha
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

The outbox and inbox are in the `Mocha.EntityFrameworkCore.Postgres` package. Import `Mocha.EntityFrameworkCore`, `Mocha.Inbox`, and `Mocha.Outbox` for the registration and model extensions. In `OnModelCreating`, add `modelBuilder.AddPostgresOutbox()` and `modelBuilder.AddPostgresInbox()`, then add a database migration. The NServiceBus outbox also deduplicates incoming messages. In Mocha, that is the job of the inbox, so register both. Let the NServiceBus outbox dispatch its pending messages before you switch, because Mocha does not read its tables.

To send messages with the outbox outside a handler, as the transactional session does, open a transaction on the `DbContext`:

```csharp
await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

db.Orders.Add(order);
await bus.PublishAsync(new OrderPlaced(order.Id), cancellationToken);
await db.SaveChangesAsync(cancellationToken);

await transaction.CommitAsync(cancellationToken);
```

The PostgreSQL outbox requires a transaction by default. For independent sends and publishes outside a transaction, configure `UsePostgresOutbox(o => o.RequireTransaction = false)`; calls inside an explicit transaction remain transactional.

If the EF Core provider enables a retrying execution strategy, execute the entire explicit transaction through that strategy. For Aspire PostgreSQL, `builder.AddNpgsqlDbContext<AppDbContext>("db", settings => settings.DisableRetry = true)` is another option; it disables EF database retries for that context. Verify the strategy also supports the transactions used by consumer middleware. See [Reliability](../reliability.md).

## 7. Rewrite sagas

An NServiceBus saga handles each message in its own method and keeps its state in saga data. A Mocha saga is a state machine: the same messages move it between named states, and the actions run on each transition. In the Mocha example, `OrderSubmitted` starts the saga and its initial transition publishes `OrderPlaced`, which another service can answer with `OrderAccepted`.

```csharp
// NServiceBus
public class OrderSagaData : ContainSagaData
{
    public Guid OrderId { get; set; }
}

public class OrderSaga
    : Saga<OrderSagaData>,
      IAmStartedByMessages<OrderSubmitted>,
      IHandleMessages<OrderAccepted>,
      IHandleTimeouts<OrderExpired>
{
    protected override void ConfigureHowToFindSaga(SagaPropertyMapper<OrderSagaData> mapper)
        => mapper.MapSaga(s => s.OrderId)
            .ToMessage<OrderSubmitted>(m => m.OrderId)
            .ToMessage<OrderAccepted>(m => m.OrderId);

    public Task Handle(OrderSubmitted message, IMessageHandlerContext context)
        => RequestTimeout<OrderExpired>(context, TimeSpan.FromMinutes(30));

    public Task Handle(OrderAccepted message, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }

    public Task Timeout(OrderExpired state, IMessageHandlerContext context)
    {
        MarkAsComplete();
        return Task.CompletedTask;
    }
}

// Mocha
public class OrderState : SagaStateBase
{
}

public record OrderSubmitted(Guid OrderId);
public record OrderPlaced(Guid OrderId);

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
            .Publish((_, state) => new OrderPlaced(state.Id))
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

- **Correlation.** NServiceBus maps a property of each message to a property of the saga data. Mocha correlates by the saga's ID. Set the ID in the state factory, here to the order ID, and let later events return it from `ICorrelatable.CorrelationId`. Replies to commands the saga sent carry the ID in the `saga-id` header. The message that starts a saga must not return an `ICorrelatable.CorrelationId` or carry a `saga-id` header, because Mocha then looks for an existing instance instead of creating one.
- **Ordering.** Mocha ignores a correlated event when its saga instance does not exist. Publish a message that can produce a correlated reply from the initial transition, as above, and commit the saga state and outgoing message together using the transactional outbox. Use `OnSend<T>()` for a point-to-point command and `OnEvent<T>()` for a published event.
- **Completion.** `MarkAsComplete()` becomes a transition to a final state. Every saga needs a final state, and every state must be able to reach one. Entering a final state deletes the persisted saga state, so retain completed status separately if it must remain queryable.
- **Timeouts.** `Timeout(TimeSpan)` replaces `RequestTimeout`, and `OnTimeout()` replaces `IHandleTimeouts<T>`. Mocha has one deadline per saga: `Timeout(TimeSpan)` on the descriptor schedules it when the saga starts, cancels it when the saga reaches a final state, and adds the final state `StateNames.TimedOut`. NServiceBus timeout messages already scheduled for a completed saga can remain in the transport until delivery. Generated saga registration requires a public parameterless constructor; obtain a configuration-dependent duration before the saga is initialized. The timeout needs a scheduling store. On RabbitMQ, register the PostgreSQL scheduling store with `UsePostgresScheduling()`, otherwise a saga with a timeout fails to start. A saga that needs several deadlines can schedule its own messages with `ScheduledSend` in a transition.
- **Replies.** `OnRequest<T>()` starts a saga from a request and `.Respond(...)` on a final state answers it, where NServiceBus calls `ReplyToOriginator`.
- **Persistence.** The generated registration method registers the saga, so call `.AddSaga<OrderSaga>()` only when you register handlers by hand. Store saga state in PostgreSQL with `AddEntityFramework<AppDbContext>(p => p.AddPostgresSagas())` and `modelBuilder.AddPostgresSagas()`, or in memory with `services.AddInMemorySagas()` for tests. Import `Mocha.Sagas`, `Mocha.Sagas.EfCore`, and `Mocha.Scheduling` for saga and scheduled-message persistence.
- **Async transition work.** `Then` actions are synchronous. For asynchronous work in the consume scope, override `OnHandleTransitionAsync`, call the base implementation, and use `context.Services` and `context.CancellationToken`. Use the same scoped `DbContext` and receive transaction for business updates, or dispatch a message to a separate handler.

[Sagas](../sagas.md) covers state, transitions, faults, compensation, and timeouts.

## 8. Move delayed delivery

```csharp
// NServiceBus
var sendOptions = new SendOptions();
sendOptions.DelayDeliveryWith(TimeSpan.FromMinutes(30));
await messageSession.Send(new SendReminder(orderId), sendOptions, cancellationToken);

// Mocha
var result = await bus.ScheduleSendAsync(
    new SendReminder(orderId),
    DateTimeOffset.UtcNow.AddMinutes(30),
    cancellationToken);
```

`result.Token` cancels the message with `bus.CancelScheduledMessageAsync`. Azure Service Bus and the PostgreSQL transport schedule natively. RabbitMQ needs the PostgreSQL scheduling store: register it with `p.UsePostgresScheduling()` in `AddEntityFramework`, add `modelBuilder.AddPostgresScheduledMessages()` in `OnModelCreating`, and add a database migration. See [Scheduling](../scheduling.md).

## 9. Update observability and tests

Mocha emits OpenTelemetry traces and metrics after you call `.AddInstrumentation()` on the bus. Subscribe to the `Mocha` activity source and meter instead of the NServiceBus ones. See [Observability](../observability.md).

Tests that used `NServiceBus.Testing` can run the bus with the in-memory transport instead: register `.AddInMemory()` from the `Mocha.Transport.InMemory` package in place of the broker and assert on the effects of your handlers. Handlers that only depend on their constructor arguments can also be called directly.

# Next steps

- [Handlers and consumers](../handlers-and-consumers.md) covers every handler interface.
- [Messaging patterns](../messaging-patterns.md) covers publish/subscribe, send, and request/reply.
- [Reliability](../reliability.md) covers fault handling, the outbox, and the inbox.
- [Sagas](../sagas.md) covers state machines in depth.

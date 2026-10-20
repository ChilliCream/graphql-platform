---
title: "Migrate from MassTransit"
metaTitle: "Migrate from MassTransit to the Mocha Message Bus"
description: "Map MassTransit consumers, publish and send, request clients, retries, the outbox, and state machine sagas to Mocha, and plan a migration service by service."
---

Mocha and MassTransit share most of their concepts: a bus that publishes events and sends commands over a broker, consumers that handle them, request/reply, retries and redelivery, a transactional outbox, and state machine sagas. Most of a migration is renaming. The parts that need planning are the transports and stores Mocha supports, and the fact that a Mocha service and a MassTransit service cannot exchange messages with each other.

This guide maps MassTransit concepts to Mocha, lists what Mocha does not have, and walks you through migrating a service. For a side-by-side overview, including licensing, see [Mocha vs MassTransit](../../../compare/mocha-vs-masstransit.md).

# Check what you depend on

Mocha does not cover everything MassTransit does. Check this list before you start.

| If you use                                                                    | In Mocha                                                                                                                                    |
| ----------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------- |
| RabbitMQ or Azure Service Bus                                                 | Supported.                                                                                                                                  |
| The PostgreSQL SQL transport                                                  | Mocha has its own PostgreSQL transport. It is in preview.                                                                                   |
| Amazon SQS/SNS, ActiveMQ, or the SQL Server transport                         | Not supported.                                                                                                                              |
| Kafka or Event Hubs riders                                                    | Not supported.                                                                                                                              |
| An outbox, inbox, or saga repository on PostgreSQL                            | Supported through Entity Framework Core.                                                                                                    |
| An outbox, inbox, or saga repository on SQL Server, MongoDB, Redis, or Marten | Not supported. The store interfaces are public, so you can implement your own.                                                              |
| Courier routing slips                                                         | Not supported. Model the workflow as a saga.                                                                                                |
| Job consumers                                                                 | Not supported.                                                                                                                              |
| Quartz.NET or Hangfire scheduling                                             | Not supported. Mocha schedules natively on Azure Service Bus, and through a PostgreSQL store on the other transports.                       |
| The test harness                                                              | Not available. Run tests against the in-memory transport.                                                                                   |
| Serializers other than System.Text.Json                                       | Not supported.                                                                                                                              |
| MassTransit Mediator                                                          | Use the [Mocha mediator](../mediator/index.md). The [MediatR guide](./coming-from-mediatr.md) covers the same handler and middleware model. |

Mocha targets .NET 8 and later.

# Plan the migration by message flow

Mocha and MassTransit use different message envelopes and different exchange and queue names, so a Mocha service cannot consume what a MassTransit service publishes, and the other way around. Migrate the services that exchange a message together, or move one message flow at a time and keep both buses registered in a service until all its flows have moved. The two libraries register separate services, so they can run in the same application.

# Concept mapping

| MassTransit                                              | Mocha                                                                               | Key difference                                                                           |
| -------------------------------------------------------- | ----------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------- |
| `AddMassTransit(x => ...)`                               | `AddMessageBus()`                                                                   | Configuration is a chain of builder calls instead of one callback.                       |
| `x.AddConsumers(assembly)`                               | `.Add{Module}()`                                                                    | Generated at build time from the handlers in the project.                                |
| `x.UsingRabbitMq(...)`, `x.UsingAzureServiceBus(...)`    | `.AddRabbitMQ()`, `.AddAzureServiceBus(...)`                                        | RabbitMQ reads the `IConnectionFactory` from dependency injection.                       |
| `IConsumer<T>` for an event                              | `IEventHandler<T>`                                                                  | `HandleAsync(message, cancellationToken)` receives the message, not a context.           |
| `IConsumer<T>` for a command                             | `IEventRequestHandler<T>`                                                           | Commands are sent to one queue, named after the message type.                            |
| `IConsumer<T>` that calls `RespondAsync`                 | `IEventRequestHandler<TRequest, TResponse>`                                         | The handler returns the response. The request implements `IEventRequest<TResponse>`.     |
| `IConsumer<T>` that reads headers or the context         | `IConsumer<T>`                                                                      | `ConsumeAsync(IConsumeContext<T>)` gives access to headers and message metadata.         |
| `IPublishEndpoint.Publish`                               | `IMessageBus.PublishAsync`                                                          | Same concept.                                                                            |
| `ISendEndpointProvider.GetSendEndpoint(uri)` and `Send`  | `IMessageBus.SendAsync`                                                             | The destination comes from the message type, a configured route, or `SendOptions`.       |
| `IRequestClient<T>.GetResponse<TResponse>`               | `IMessageBus.RequestAsync`                                                          | Returns the response message directly. One response type per request.                    |
| `UseMessageRetry`                                        | `AddResilience(policy => policy.Default().Retry(...))`                              | Exception policies choose retry, redelivery, dead letter, or discard per exception type. |
| `UseDelayedRedelivery`, `UseScheduledRedelivery`         | `.ThenRedeliver(...)`                                                               | Same concept.                                                                            |
| `{queue}_error` and `{queue}_skipped` queues             | `{endpoint}_error` and `{endpoint}_skipped` endpoints                               | Same naming pattern.                                                                     |
| `AddEntityFrameworkOutbox<TContext>` with `UseBusOutbox` | `AddEntityFramework<TContext>(p => { p.UsePostgresOutbox(); p.UseTransaction(); })` | PostgreSQL only.                                                                         |
| The inbox in the Entity Framework outbox                 | `p.UsePostgresInbox()`                                                              | Deduplicates by message ID and consumer.                                                 |
| `MassTransitStateMachine<TInstance>`                     | `Saga<TState>`                                                                      | States are strings, transitions are configured in `Configure`.                           |
| `CorrelateById`, `CorrelateBy`                           | The saga ID, from the `saga-id` header or `ICorrelatable.CorrelationId`             | Mocha correlates by saga ID only. The state factory can set the ID.                      |
| `IMessageScheduler.SchedulePublish`                      | `IMessageBus.SchedulePublishAsync`                                                  | Returns a token to cancel the scheduled message.                                         |
| `AddMassTransitTestHarness`                              | The in-memory transport                                                             | No dedicated harness.                                                                    |

# Migrate step by step

## 1. Install the packages

```bash
dotnet add package Mocha
dotnet add package Mocha.Analyzers
dotnet add package Mocha.Transport.RabbitMQ
```

Use `Mocha.Transport.AzureServiceBus` for Azure Service Bus. `Mocha.Analyzers` contains the source generator that writes the registration method. Add it to every project that contains handlers.

## 2. Register the bus

```csharp
// MassTransit
builder.Services.AddMassTransit(x =>
{
    x.AddConsumers(typeof(Program).Assembly);
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host("localhost", "/", h =>
        {
            h.Username("guest");
            h.Password("guest");
        });
        cfg.ConfigureEndpoints(context);
    });
});

// Mocha
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory
    {
        HostName = "localhost",
        VirtualHost = "/",
        UserName = "guest",
        Password = "guest"
    });

builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

The generator names the registration method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()`. With Aspire, `builder.AddRabbitMQClient("rabbitmq")` registers the connection factory for you. See [RabbitMQ](../transports/rabbitmq.md) and [Azure Service Bus](../transports/azure-service-bus.md) for the transport options.

Mocha creates exchanges and queues at startup from naming conventions, like `ConfigureEndpoints` does. The names differ from MassTransit's. [Routing and endpoints](../routing-and-endpoints.md) explains the conventions and how to override them.

## 3. Turn consumers into handlers

Pick the handler interface by what the consumer does. Most consumers only need the message, so `HandleAsync` receives the message and a cancellation token instead of a context.

```csharp
// MassTransit
public class OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger) : IConsumer<OrderPlaced>
{
    public Task Consume(ConsumeContext<OrderPlaced> context)
    {
        logger.LogInformation("Order {OrderId} placed", context.Message.OrderId);
        return Task.CompletedTask;
    }
}

// Mocha: an event
public class OrderPlacedHandler(ILogger<OrderPlacedHandler> logger) : IEventHandler<OrderPlaced>
{
    public ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
    {
        logger.LogInformation("Order {OrderId} placed", message.OrderId);
        return ValueTask.CompletedTask;
    }
}
```

A consumer of a command becomes an `IEventRequestHandler<TCommand>`. A consumer that responds becomes an `IEventRequestHandler<TRequest, TResponse>` and returns the response:

```csharp
// MassTransit
public record CheckOrderStatus(Guid OrderId);
public record OrderStatus(Guid OrderId, string Status);

public class CheckOrderStatusConsumer : IConsumer<CheckOrderStatus>
{
    public Task Consume(ConsumeContext<CheckOrderStatus> context)
        => context.RespondAsync(new OrderStatus(context.Message.OrderId, "Shipped"));
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

When a consumer reads headers or message metadata, implement Mocha's `IConsumer<T>` instead. Its `ConsumeAsync` method receives an `IConsumeContext<T>`. See [Handlers and consumers](../handlers-and-consumers.md).

## 4. Update publish, send, and request call sites

```csharp
// MassTransit
await publishEndpoint.Publish(new OrderPlaced(orderId), cancellationToken);

var endpoint = await sendEndpointProvider.GetSendEndpoint(new Uri("queue:reserve-inventory"));
await endpoint.Send(new ReserveInventory(orderId), cancellationToken);

var response = await client.GetResponse<OrderStatus>(new CheckOrderStatus(orderId), cancellationToken);
var status = response.Message;

// Mocha
await bus.PublishAsync(new OrderPlaced(orderId), cancellationToken);

await bus.SendAsync(new ReserveInventory(orderId), cancellationToken);

var status = await bus.RequestAsync(new CheckOrderStatus(orderId), cancellationToken);
```

`bus` is an `IMessageBus`. `SendAsync` sends to the queue of the message type's handler by convention. To send somewhere else, configure a route with `AddMessage<T>(m => m.Send(r => r.ToQueue("...")))` or set `SendOptions.Endpoint` on the call.

`RequestAsync` waits up to two minutes for a response. A failing handler surfaces as a `RemoteErrorException`, a missing response as a `ResponseTimeoutException`. To give up sooner, pass a cancellation token that is canceled after your timeout.

## 5. Replace retries and redelivery

MassTransit configures retries as middleware on the endpoint. Mocha configures them as exception policies, on the bus or on a single handler.

```csharp
// MassTransit
x.AddConfigureEndpointsCallback((context, name, cfg) =>
{
    cfg.UseDelayedRedelivery(r => r.Intervals(
        TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromMinutes(30)));
    cfg.UseMessageRetry(r => r.Immediate(3));
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

`Retry()` retries three times with exponential backoff. `ThenRedeliver()` redelivers after 5, 15, and 30 minutes. Both take arguments to change the counts and delays. Delayed redelivery needs a scheduling store. RabbitMQ has none of its own, so register the PostgreSQL scheduling store, otherwise Mocha redelivers immediately. See [Exception policies](../exception-policies.md) and [Scheduling](../scheduling.md).

## 6. Move the outbox and inbox

Mocha's outbox and inbox use Entity Framework Core with PostgreSQL. Map the entities in your `DbContext` and register them on the bus:

```csharp
// MassTransit
x.AddEntityFrameworkOutbox<AppDbContext>(o =>
{
    o.UsePostgres();
    o.UseBusOutbox();
});

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

In `OnModelCreating`, replace MassTransit's `AddInboxStateEntity()`, `AddOutboxMessageEntity()`, and `AddOutboxStateEntity()` with `modelBuilder.AddPostgresOutbox()` and `modelBuilder.AddPostgresInbox()`, then add a database migration. Drain MassTransit's outbox before you switch, because Mocha does not read MassTransit's outbox tables. See [Reliability](../reliability.md).

## 7. Rewrite sagas

A MassTransit state machine and a Mocha saga describe the same thing: states, the events that move between them, and the actions that run on each transition.

```csharp
// MassTransit
public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
}

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = null!;
    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<OrderAccepted> OrderAccepted { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        Event(() => OrderSubmitted, x => x.CorrelateById(m => m.Message.OrderId));
        Event(() => OrderAccepted, x => x.CorrelateById(m => m.Message.OrderId));

        Initially(
            When(OrderSubmitted)
                .TransitionTo(Submitted));

        During(Submitted,
            When(OrderAccepted)
                .Finalize());
    }
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
        descriptor
            .Initially()
            .OnEvent<OrderSubmitted>()
            .StateFactory(e => new OrderState { Id = e.OrderId })
            .TransitionTo(Submitted);

        descriptor
            .During(Submitted)
            .OnEvent<OrderAccepted>()
            .TransitionTo(Accepted);

        descriptor.Finally(Accepted);
    }
}
```

The main differences:

- **Correlation.** MassTransit correlates by any message property. Mocha correlates by the saga's ID. Set the ID in the state factory, here to the order ID, and let later events return it from `ICorrelatable.CorrelationId`. Replies to commands the saga sent carry the ID in the `saga-id` header. The message that starts a saga must not carry a correlation ID, because Mocha then looks for an existing instance instead of creating one.
- **Persistence.** Register the saga with `.AddSaga<OrderSaga>()`. Store saga state in PostgreSQL with `AddEntityFramework<AppDbContext>(p => p.AddPostgresSagas())` and `modelBuilder.AddPostgresSagas()`, or in memory with `services.AddInMemorySagas()` for tests.
- **Requests.** `OnRequest<T>()` starts a saga from a request and `.Respond(...)` on a final state answers it, like a MassTransit saga that responds to its initiating request.
- **Timeouts.** `Timeout(TimeSpan)` on the descriptor schedules a timeout for the whole saga, and `OnTimeout()` handles it.

[Sagas](../sagas.md) covers state, transitions, faults, compensation, and timeouts.

## 8. Move scheduled messages

```csharp
// MassTransit
await scheduler.SchedulePublish(DateTime.UtcNow.AddMinutes(30), new SendReminder(orderId));

// Mocha
var result = await bus.SchedulePublishAsync(
    new SendReminder(orderId),
    DateTimeOffset.UtcNow.AddMinutes(30),
    cancellationToken);
```

`result.Token` cancels the message with `bus.CancelScheduledMessageAsync`. Azure Service Bus and the PostgreSQL transport schedule natively. RabbitMQ needs the PostgreSQL scheduling store, registered with `p.UsePostgresScheduling()` in `AddEntityFramework`. See [Scheduling](../scheduling.md).

## 9. Update observability and tests

Mocha emits OpenTelemetry traces and metrics after you call `.AddInstrumentation()` on the bus. Subscribe to the `Mocha` activity source and meter instead of `MassTransit`. See [Observability](../observability.md).

Tests that used the MassTransit test harness can run the bus with the in-memory transport instead: register `.AddInMemory()` in place of the broker and assert on the effects of your handlers.

# Next steps

- [Handlers and consumers](../handlers-and-consumers.md) covers every handler interface.
- [Messaging patterns](../messaging-patterns.md) covers publish/subscribe, send, and request/reply.
- [Reliability](../reliability.md) covers fault handling, the outbox, and the inbox.
- [Sagas](../sagas.md) covers state machines in depth.

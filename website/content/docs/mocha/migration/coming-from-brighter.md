---
title: "Migrate from Brighter"
metaTitle: "Migrate from Brighter to the Mocha Mediator and Message Bus"
description: "Map Brighter commands, events, request handlers, the command processor, publications and subscriptions, resilience pipelines, and the outbox to Mocha."
---

Brighter uses one model for two jobs: the command processor dispatches commands and events to handlers in the same process, and the same handlers consume messages that other services post over a broker. Mocha splits the two. The [mediator](../mediator/index.md) dispatches commands, queries, and notifications in the process, and the message bus carries events, commands, and requests between services. One source generator registers the handlers of both.

Most of a migration is deciding which side each handler belongs to and renaming. The part that needs planning is that a Mocha service and a Brighter service cannot exchange messages with each other.

This guide maps Brighter concepts to Mocha and walks you through migrating a service. For a side-by-side overview, see [Mocha vs. Brighter](../../../comparison/mocha-vs-brighter.md).

# Migrate with a coding agent

To let a coding agent such as Claude Code, Codex, or GitHub Copilot do the migration, copy this prompt into it. The prompt points the agent to this guide and has it check with you before it changes code.

<AgentPrompt title="Migration prompt">

```text
Migrate this codebase from Brighter to Mocha, the MIT-licensed .NET mediator
and message bus from ChilliCream.

Follow the migration guide:
https://chillicream.com/docs/mocha/migration/coming-from-brighter.md
Look up Mocha APIs in https://chillicream.com/docs/mocha/llms.txt instead of
guessing them.

1. Inventory the Brighter usage: commands, events, request handlers and
   whether each one runs in the process or consumes from a subscription,
   publications and subscriptions, Darker queries, Call, resilience pipelines
   and requeue settings, the outbox and inbox, and scheduled messages. If the
   codebase uses something the guide does not cover, stop and ask me how to
   proceed.
2. Propose a plan that moves the services by message flow, as the guide
   describes, and wait for my approval.
3. Migrate one service at a time, in the order of the guide's steps. Keep the
   solution building after every step, and keep message contracts unchanged
   unless the guide says otherwise.
4. Build, run the tests, fix every diagnostic the Mocha source generator
   reports, and start the services to check that every message flow still
   works.
5. Remove Brighter once nothing references it. Summarize what changed, what
   behaves differently, and anything you could not migrate.
```

</AgentPrompt>

# Plan the migration by message flow

Mocha and Brighter use different message envelopes and different exchange and queue names, so a Mocha service cannot consume what a Brighter service posts, and the other way around. Migrate the services that exchange a message together, or move one message flow at a time and keep both libraries registered in a service until all its flows have moved. The two libraries register separate services, so they can run in the same application. Handlers that only run in the process can move at any time. If you use Brighter's `RMQ.Sync` transport, switch to `RMQ.Async` first: it uses RabbitMQ.Client 6, and Mocha needs RabbitMQ.Client 7.

# Concept mapping

| Brighter                                                              | Mocha                                                                 | Key difference                                                             |
| --------------------------------------------------------------------- | --------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `AddBrighter()` and `AutoFromAssemblies()`                            | `AddMediator().Add{Module}()` and `AddMessageBus().Add{Module}()`     | Generated at build time from the handlers in the project.                  |
| `AddProducers` with a publication per message type                    | `.AddRabbitMQ()`, `.AddAzureServiceBus(...)`                          | Exchanges and queues come from naming conventions.                         |
| `AddConsumers` with a subscription per message type                   | The bus handlers                                                      | Each handler gets a queue by convention. The bus starts with the host.     |
| `ServiceActivatorHostedService`                                       | Nothing to register                                                   | Same concept.                                                              |
| `Command`, `Event`                                                    | Mediator: `ICommand`, `INotification`. Bus: plain classes and records | No base class and no message ID to pass.                                   |
| `RequestHandlerAsync<T>` for a command you `SendAsync`                | `ICommandHandler<TCommand>` or `ICommandHandler<TCommand, TResponse>` | `HandleAsync` returns a `ValueTask`, or the response.                      |
| `RequestHandlerAsync<T>` for an event you `PublishAsync`              | `INotificationHandler<TNotification>`                                 | Same concept.                                                              |
| `RequestHandlerAsync<T>` for a message from a subscription            | `IEventHandler<T>` or `IEventRequestHandler<T>`                       | Events reach every subscribing service, commands one queue.                |
| `return await base.HandleAsync(...)`                                  | Nothing                                                               | Middleware calls the next step. Handlers do not.                           |
| `IAmACommandProcessor.SendAsync`, `PublishAsync`                      | `ISender.SendAsync`, `IPublisher.PublishAsync`                        | Queries have their own method, `QueryAsync`.                               |
| `PostAsync` of a `Command`                                            | `IMessageBus.SendAsync`                                               | Sent to the queue named after the message type.                            |
| `PostAsync` of an `Event`                                             | `IMessageBus.PublishAsync`                                            | Reaches every service that handles it.                                     |
| `DepositPostAsync` and `ClearOutboxAsync`                             | `IMessageBus.SendAsync` or `PublishAsync` with the outbox enabled     | The outbox writes in your transaction and dispatches after the commit.     |
| The outbox sweeper                                                    | The outbox processor                                                  | Runs as a hosted service. Nothing to configure.                            |
| `Call<TRequest, TResponse>` with `Request` and `Reply`                | `IMessageBus.RequestAsync` with `IEventRequest<TResponse>`            | Asynchronous. The handler returns the response.                            |
| `IAmAMessageMapper<T>`                                                | Nothing                                                               | Messages are serialized with System.Text.Json.                             |
| `[UseResiliencePipelineAsync]` on a handler of broker messages        | `AddResilience(policy => policy.Default().Retry(...))`                | Exception policies apply to the bus, a transport, or one receive endpoint. |
| `[DeferMessageOnErrorAsync]`, `requeueCount`, `requeueDelay`          | `.ThenRedeliver(...)`                                                 | Takes a count and base delay or explicit intervals.                        |
| `[RejectMessageOnErrorAsync]`, the dead letter channel                | `.DeadLetter()`, the `{endpoint}_error` endpoint                      | A message that fails without a policy goes to the error endpoint too.      |
| `[UseResiliencePipelineAsync]`, `[FeatureSwitchAsync]` in the process | Mediator middleware                                                   | Registered once for every message, filtered per message type.              |
| `[RequestLoggingAsync]`                                               | `.AddInstrumentation()`, or a middleware                              | Spans follow the OpenTelemetry messaging conventions.                      |
| `[UseInboxAsync]` and the inbox                                       | `p.UsePostgresInbox()`                                                | Deduplicates by message ID and consumer.                                   |
| `SendAsync` or `PostAsync` with a delay                               | `IMessageBus.ScheduleSendAsync`, `SchedulePublishAsync`               | The result carries a token to cancel the message.                          |

# Migrate step by step

## 1. Install the packages

```bash
dotnet add package Mocha.Mediator
dotnet add package Mocha
dotnet add package Mocha.Analyzers
dotnet add package Mocha.Transport.RabbitMQ
```

`Mocha.Mediator` is for handlers you call in the process, `Mocha` and the transport for messages between services. Use `Mocha.Transport.AzureServiceBus` for Azure Service Bus. `Mocha.Analyzers` contains the source generator that writes the registration methods. Add it to every project that contains handlers. Mocha targets .NET 8 and later.

## 2. Sort your handlers

Every Brighter handler derives from `RequestHandlerAsync<T>`, so decide for each one where it goes in Mocha:

- A handler you call with `SendAsync` or `PublishAsync` on the command processor runs in the process. It becomes a mediator handler.
- A handler that receives messages through a subscription consumes them from a broker. It becomes a bus handler.

A handler that does both becomes two handlers. They can share the code they call.

## 3. Register the mediator and the bus

```csharp
// Brighter: posting service
var producerRegistry = new RmqProducerRegistryFactory(
    rmqConnection,
    [
        new RmqPublication<OrderPlaced>
        {
            Topic = new RoutingKey("order.placed"),
            MakeChannels = OnMissingChannel.Create
        }
    ]).Create();

builder.Services
    .AddBrighter()
    .AddProducers(configure => configure.ProducerRegistry = producerRegistry)
    .AutoFromAssemblies();

// Brighter: consuming service
builder.Services
    .AddConsumers(options =>
    {
        options.Subscriptions =
        [
            new RmqSubscription<OrderPlaced>(
                new SubscriptionName("billing.order-placed"),
                new ChannelName("billing.order.placed"),
                new RoutingKey("order.placed"),
                messagePumpType: MessagePumpType.Proactor,
                makeChannels: OnMissingChannel.Create)
        ];
        options.DefaultChannelFactory = new ChannelFactory(new RmqMessageConsumerFactory(rmqConnection));
    })
    .AutoFromAssemblies();

builder.Services.AddHostedService<ServiceActivatorHostedService>();

// Mocha: every service
builder.Services.AddSingleton<IConnectionFactory>(_ =>
    new ConnectionFactory { HostName = "localhost" });

builder.Services
    .AddMediator()
    .AddOrdering();

builder.Services
    .AddMessageBus()
    .AddOrdering()
    .AddRabbitMQ();
```

The generator names the registration method after the last segment of the assembly name, so handlers in `MyApp.Ordering` are registered with `AddOrdering()`. It writes one for the mediator and one for the bus, each with the handlers it can dispatch. With Aspire, `builder.AddRabbitMQClient("rabbitmq")` registers the connection factory for you.

Mocha creates exchanges and queues at startup from naming conventions, so the publications, subscriptions, and routing keys go away. An event published by one service reaches a queue for every service that handles it, and a command goes to the queue named after its type. [Routing and endpoints](../routing-and-endpoints.md) explains the conventions and how to override them.

## 4. Move in-process handlers to the mediator

Commands implement `ICommand` or `ICommand<TResponse>`, events dispatched in the process implement `INotification`, and Darker queries become `IQuery<TResponse>`. Handlers implement the matching interface and no longer call `base.HandleAsync`.

```csharp
// Brighter
public class PlaceOrder(Guid productId) : Command(Id.Random())
{
    public Guid ProductId { get; } = productId;
}

public class PlaceOrderHandler(AppDbContext db) : RequestHandlerAsync<PlaceOrder>
{
    [RequestLoggingAsync(step: 1, timing: HandlerTiming.Before)]
    public override async Task<PlaceOrder> HandleAsync(
        PlaceOrder command, CancellationToken cancellationToken = default)
    {
        db.Orders.Add(new Order(command.ProductId));
        await db.SaveChangesAsync(cancellationToken);
        return await base.HandleAsync(command, cancellationToken);
    }
}

await commandProcessor.SendAsync(new PlaceOrder(productId), cancellationToken: cancellationToken);

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

var orderId = await sender.SendAsync(new PlaceOrder(productId), cancellationToken);
```

A Mocha command can return a result, so a handler no longer needs to set a property on the command for the caller to read. Queries are dispatched with `sender.QueryAsync`, notifications with `publisher.PublishAsync`.

Pipeline attributes become middleware. A middleware is registered once on the mediator, runs for every message, and can leave message types out when the pipeline is built. `.AddInstrumentation()` replaces request logging with OpenTelemetry traces, and `UseEntityFrameworkTransactions<TContext>()` wraps every command handler in a database transaction. See [Pipeline and middleware](../mediator/pipeline-and-middleware.md).

## 5. Move subscription handlers to the bus

A handler of broker messages implements a bus handler interface: `IEventHandler<T>` for events and `IEventRequestHandler<T>` for commands. Messages are plain records.

```csharp
// Brighter
public class OrderPlaced(Guid orderId) : Event(Id.Random())
{
    public Guid OrderId { get; } = orderId;
}

public class OrderPlacedHandler(IInvoiceService invoices) : RequestHandlerAsync<OrderPlaced>
{
    public override async Task<OrderPlaced> HandleAsync(
        OrderPlaced @event, CancellationToken cancellationToken = default)
    {
        await invoices.CreateAsync(@event.OrderId, cancellationToken);
        return await base.HandleAsync(@event, cancellationToken);
    }
}

// Mocha
public record OrderPlaced(Guid OrderId);

public class OrderPlacedHandler(IInvoiceService invoices) : IEventHandler<OrderPlaced>
{
    public async ValueTask HandleAsync(OrderPlaced message, CancellationToken cancellationToken)
        => await invoices.CreateAsync(message.OrderId, cancellationToken);
}
```

When a handler reads headers or message metadata, implement Mocha's `IConsumer<T>` instead. Its `ConsumeAsync` method receives an `IConsumeContext<T>`. See [Handlers and consumers](../handlers-and-consumers.md).

## 6. Update post and call sites

`PostAsync` becomes `SendAsync` for commands and `PublishAsync` for events on `IMessageBus`. Message mappers are no longer needed. `Call` becomes `RequestAsync`, and the request declares its response:

```csharp
// Brighter
await commandProcessor.PostAsync(new OrderPlaced(orderId), cancellationToken: cancellationToken);

var status = commandProcessor.Call<CheckOrderStatus, OrderStatus>(
    new CheckOrderStatus { OrderId = orderId },
    timeOut: TimeSpan.FromSeconds(5));

// Mocha
public record CheckOrderStatus(Guid OrderId) : IEventRequest<OrderStatus>;
public record OrderStatus(Guid OrderId, string Status);

await bus.PublishAsync(new OrderPlaced(orderId), cancellationToken);

var status = await bus.RequestAsync(new CheckOrderStatus(orderId), cancellationToken);
```

The handler of a request implements `IEventRequestHandler<TRequest, TResponse>` and returns the response. `RequestAsync` waits up to two minutes for a response. A failing handler surfaces as a `RemoteErrorException`, a missing response as a `ResponseTimeoutException`. To give up sooner, pass a cancellation token that is canceled after your timeout.

## 7. Replace resilience pipelines and requeues

Brighter acknowledges and drops a message whose handler throws, unless the handler requeues or rejects it. Mocha moves a failed message to the endpoint's error queue, with the exception in its headers, and exception policies decide whether it is retried or redelivered first. Replace the attributes and subscription settings with one policy on the bus:

```csharp
// Brighter
var resiliencePipelineRegistry = new ResiliencePipelineRegistry<string>()
    .AddBrighterDefault();

resiliencePipelineRegistry.TryAddBuilder("Retry",
    (builder, _) => builder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
    }));

public class PlaceOrderHandler : RequestHandlerAsync<PlaceOrder>
{
    [DeferMessageOnErrorAsync(step: 0, delayMilliseconds: 300_000)]
    [UseResiliencePipelineAsync("Retry", step: 1)]
    public override async Task<PlaceOrder> HandleAsync(
        PlaceOrder command, CancellationToken cancellationToken = default)
    {
        // ...
        return await base.HandleAsync(command, cancellationToken);
    }
}

new RmqSubscription<PlaceOrder>(
    new SubscriptionName("place-order"),
    new ChannelName("place.order"),
    new RoutingKey("place.order"),
    requeueCount: 4,
    deadLetterChannelName: new ChannelName("place.order.dlq"),
    deadLetterRoutingKey: new RoutingKey("place.order.dlq"),
    messagePumpType: MessagePumpType.Proactor,
    makeChannels: OnMissingChannel.Create);

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

`Retry()` retries three times with exponential backoff, and `ThenRedeliver()` redelivers after 5, 15, and 30 minutes. Both take arguments to change the counts and delays. To give one handler its own policy, configure the policy on the handler's receive endpoint. Delayed redelivery needs a scheduling store. RabbitMQ has none of its own, so register the PostgreSQL scheduling store, otherwise Mocha redelivers immediately. See [Exception policies](../exception-policies.md) and [Scheduling](../scheduling.md).

## 8. Move the outbox and inbox

Mocha's outbox and inbox use Entity Framework Core with PostgreSQL. With the outbox enabled, `PublishAsync` and `SendAsync` write to the outbox table in the current transaction, so handlers no longer deposit and clear messages themselves:

```csharp
// Brighter
var tx = await transactionProvider.GetTransactionAsync(cancellationToken);
// ...write the order with tx
var id = await commandProcessor.DepositPostAsync(
    new OrderPlaced(order.Id), transactionProvider, cancellationToken: cancellationToken);
await transactionProvider.CommitAsync(cancellationToken);
await commandProcessor.ClearOutboxAsync([id], cancellationToken: cancellationToken);

// Mocha: registration
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

// Mocha: in a bus handler
db.Orders.Add(order);
await bus.PublishAsync(new OrderPlaced(order.Id), cancellationToken);
await db.SaveChangesAsync(cancellationToken);
```

The outbox and inbox are in the `Mocha.EntityFrameworkCore.Postgres` package. In `OnModelCreating`, add `modelBuilder.AddPostgresOutbox()` and `modelBuilder.AddPostgresInbox()`, then add a database migration. `UseTransaction()` wraps every bus handler in a transaction. Outside a bus handler, for example in a mediator command handler or an API endpoint, open a transaction on the `DbContext` or use `UseEntityFrameworkTransactions<TContext>()` on the mediator. Let the Brighter sweeper dispatch its pending messages before you switch, because Mocha does not read Brighter's outbox table. See [Reliability](../reliability.md).

## 9. Move scheduled messages

```csharp
// Brighter
await commandProcessor.PostAsync(
    TimeSpan.FromMinutes(30),
    new SendReminder(orderId),
    cancellationToken: cancellationToken);

// Mocha
var result = await bus.ScheduleSendAsync(
    new SendReminder(orderId),
    DateTimeOffset.UtcNow.AddMinutes(30),
    cancellationToken);
```

`result.Token` cancels the message with `bus.CancelScheduledMessageAsync`. Azure Service Bus and the PostgreSQL transport schedule natively. RabbitMQ needs the PostgreSQL scheduling store: register it with `p.UsePostgresScheduling()` in `AddEntityFramework`, add `modelBuilder.AddPostgresScheduledMessages()` in `OnModelCreating`, and add a database migration. See [Scheduling](../scheduling.md).

## 10. Update observability and tests

After you call `.AddInstrumentation()`, the bus emits OpenTelemetry traces and metrics through the `Mocha` activity source and meter, and the mediator emits traces through the `Mocha.Mediator` activity source. Subscribe to them instead of Brighter's. See [Observability](../observability.md).

Tests can run the bus with the in-memory transport: register `.AddInMemory()` from the `Mocha.Transport.InMemory` package in place of the broker and assert on the effects of your handlers. Mediator handlers can be tested by sending through `ISender` or by calling the handler directly.

# Next steps

- [Mediator](../mediator/index.md) covers in-process commands, queries, and notifications.
- [Handlers and consumers](../handlers-and-consumers.md) covers every bus handler interface.
- [Reliability](../reliability.md) covers fault handling, the outbox, and the inbox.
- [Sagas](../sagas.md) covers processes that span several messages.

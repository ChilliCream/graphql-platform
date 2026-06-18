using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Descriptors;

/// <summary>
/// Verifies the identity, entity-only lowering, convergence, saga placement, and axis-A claim
/// behavior of the unified <c>t.Queue(name)</c> front door.
/// </summary>
public class RabbitMQUnifiedQueueTests
{
    // --- Identity ---

    [Fact]
    public void Queue_Should_CreateEndpoint_When_ConsumerAttached()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "orders");

        // assert: the unified handle materialized exactly one receive endpoint named "orders"
        Assert.NotNull(endpoint);
        Assert.Equal("orders", endpoint.Queue.Name);
    }

    [Fact]
    public void Queue_Should_ResolveIdenticalHandle_When_CalledTwiceWithSameName()
    {
        // arrange
        // Calling t.Queue("orders") a second time must return the same backing endpoint adapter,
        // not create a second receive endpoint.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                var first = t.Queue("orders");
                first.Consumer<OrderSpyConsumer>();
                var second = t.Queue("orders");
                second.BindExplicitly();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoints = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .Where(e => e.Queue.Name == "orders")
            .ToList();

        // assert: only one endpoint exists for "orders"
        Assert.Single(endpoints);
    }

    [Fact]
    public void Queue_Should_SetQueueNameAsEndpointName_When_NoExplicitEndpointName()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("my-queue").Consumer<OrderSpyConsumer>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "my-queue");

        // assert: the endpoint name equals the queue name when no explicit endpoint name is given
        Assert.NotNull(endpoint);
        Assert.Equal("my-queue", endpoint.Name);
    }

    // --- Entity-only ---

    [Fact]
    public void Queue_Should_NotMaterializeReceiveEndpoint_When_NoConsumersOrReceives()
    {
        // arrange
        // An entity-only Queue() handle (no consumer, no Receives) lowers to a declared queue
        // entity without entering the receive-endpoint lifecycle.
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .FirstOrDefault(e => e.Queue.Name == "audit");

        // assert: no receive endpoint was created for the entity-only queue
        Assert.Null(endpoint);
    }

    [Fact]
    public void Queue_Should_MaterializeReceiveEndpoint_When_ReceivesAdded()
    {
        // arrange
        // Adding Receives<T>() on a Queue() handle crosses the lazy-materialization threshold:
        // the handle must produce a receive endpoint. A registered consumer is required so the
        // lifecycle validation can connect the declared Receives route to a handler.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Receives<OrderCreated>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .SingleOrDefault(e => e.Queue.Name == "orders");

        // assert: a receive endpoint was materialized because Receives<T> was declared
        Assert.NotNull(endpoint);
    }

    // --- Convergence ---

    [Fact]
    public void QueueEndpointDeclareQueue_Should_ConvergeToOneEntity_When_SameName()
    {
        // arrange
        // Two paths target the same queue name "orders":
        //   1. t.Queue("orders") unified front door (the primary surface, creates a builder)
        //   2. t.DeclareQueue("orders") at transport level (declared provenance)
        // The W2b AddQueue merge rules must converge both into exactly one queue entity with no
        // exception. The second DeclareQueue call below also verifies descriptor-level deduplication.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.DeclareQueue("orders").AutoProvision(true);
                t.DeclareQueue("orders").AutoProvision(false);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act
        var description = transport.Describe();
        var queues = topology.Queues.Where(q => q.Name == "orders").ToList();

        // assert: exactly one "orders" queue entity, no duplicate or exception
        Assert.Single(queues);
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    // --- Saga ---

    [Fact]
    public void SagaEndpoint_Should_Describe_When_ConfiguredViaUnifiedQueue()
    {
        // arrange
        // A saga that processes OrderStarted events and sends a request (with an OnReply route).
        // When the saga is combined with a t.Queue("order-processor") entity-only front-door handle,
        // the convention must not emit an exchange chain for the reply type (OrderResult). The start
        // event exchange chain appears; the entity-only queue declared via Queue() also appears.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderProcessSaga>();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindImplicitly();
                // t.Queue("order-processor") creates an entity-only dispatch-target queue alongside
                // the saga's auto-discovered consume endpoint.
                t.Queue("order-processor");
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert: no exchange or binding chain for OrderResult (the reply type) appears; the
        // entity-only "order-processor" queue and the saga's convention endpoint are both present.
        RabbitMQDescribeSnapshot.Create(description).MatchSnapshot();
    }

    // --- Axis-A claim ---

    [Fact]
    public void BindExplicitly_Should_NotThrow_When_HandlerAttachedViaQueue()
    {
        // arrange
        // Under BindExplicitly, a handler registered via t.Queue("q").Handler<T>() is an
        // explicit axis-A claim (3.6) and must not require a separate t.Handler<T>() call. The build
        // must succeed without any unconnected-route diagnostic.
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Handler<OrderCreatedHandler>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        Assert.Contains(
            transport.ReceiveEndpoints.OfType<RabbitMQReceiveEndpoint>(),
            e => e.Queue.Name == "orders");
    }

    [Fact]
    public void BindExplicitly_Should_NotAutoDiscoverHandler_When_HandlerAttachedViaQueue()
    {
        // arrange
        // Under BindExplicitly, a handler registered via the front door must not also
        // trigger auto-discovery on a separate convention-named endpoint. Exactly one receive
        // endpoint should exist, holding the queue name specified via Queue().
        var runtime = CreateRuntime(
            b => b.AddEventHandler<OrderCreatedHandler>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("my-orders").Handler<OrderCreatedHandler>();
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var ordersEndpoints = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .Where(e => e.Configuration.ConsumerIdentities.Contains(typeof(OrderCreatedHandler)))
            .ToList();

        // assert: handler lands on exactly one endpoint with the declared queue name
        Assert.Single(ordersEndpoints);
        Assert.Equal("my-orders", ordersEndpoints[0].Queue.Name);
    }

    // --- Queue shape and fault/skipped queue configuration via the front-door handle ---

    [Fact]
    public void Queue_Should_ApplyQueueShapeArguments_When_WithArgumentCalled()
    {
        // arrange
        // WithArgument on the unified handle stores the argument on the queue descriptor.
        // For entity-only queues the argument appears on the topology queue entity;
        // for consuming endpoints it flows through the queue descriptor's arguments.
        var runtime = CreateRuntime(
            b => { },
            t =>
            {
                t.BindExplicitly();
                t.Queue("audit").WithArgument("x-message-ttl", 30_000);
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;

        // act: entity-only queue lowers to a topology queue entity with the argument
        var queue = topology.Queues.SingleOrDefault(q => q.Name == "audit");

        // assert: the TTL argument was propagated to the lowered queue entity
        Assert.NotNull(queue);
        Assert.True(queue.Arguments.ContainsKey("x-message-ttl"));
        Assert.Equal(30_000, queue.Arguments["x-message-ttl"]);
    }

    [Fact]
    public void Queue_Should_ConfigureErrorQueueViaQueueHandle_When_ErrorQueueCalled()
    {
        // arrange
        // The ErrorQueue(name) verb on the unified handle must configure routing with
        // the verbatim name.
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .ErrorQueue("LEGACY.Orders.Error");
            });
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints
            .OfType<RabbitMQReceiveEndpoint>()
            .Single(e => e.Queue.Name == "orders");

        // assert: the error queue name is stored verbatim in the route
        Assert.Equal("rabbitmq:q/LEGACY.Orders.Error", endpoint.Configuration.ErrorEndpoint?.OriginalString);
        Assert.False(endpoint.Configuration.IsErrorEndpointDisabled);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IRabbitMQMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configureTransport(t);
            })
            .BuildRuntime();
        return runtime;
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }

    // Saga types for the saga front-door test.

    public sealed class OrderStarted;

    public sealed class OrderResult;

    public sealed class OrderProcessState : SagaStateBase;

    public sealed class OrderProcessRequest : IEventRequest<OrderResult>;

    public sealed class OrderProcessSaga : Saga<OrderProcessState>
    {
        protected override void Configure(ISagaDescriptor<OrderProcessState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<OrderStarted>()
                .StateFactory(_ => new OrderProcessState())
                .Send((_, _) => new OrderProcessRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<OrderResult>().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}

using Microsoft.Extensions.DependencyInjection;
using Mocha.Sagas;
using Mocha.Transport.InMemory.Tests.Helpers;
using CookieCrumble;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryTopologyConventionTests
{
    [Fact]
    public void Topology_Should_MatchSnapshot_When_EventHandler()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_RequestHandler()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b => b.AddRequestHandler<ProcessPaymentHandler>());

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_RequestResponseHandler()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b => b.AddRequestHandler<GetOrderStatusHandler>());

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_MultipleEventHandlers()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<OrderCreatedHandler2>();
        });

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_MatchSnapshot_When_MixedHandlers()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        // assert
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_MatchSnapshot_When_EventHandler()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(b => b.AddEventHandler<OrderCreatedHandler>());

        var description = transport.Describe();

        // assert
        var snapshot = TopologySnapshotHelper.CreateDescribeSnapshot(description);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Describe_Should_MatchSnapshot_When_MixedHandlers()
    {
        // arrange & act
        var (_, transport, _) = CreateTopology(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        var description = transport.Describe();

        // assert
        var snapshot = TopologySnapshotHelper.CreateDescribeSnapshot(description);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void AddEventHandler_Should_CreateTopicQueueAndBinding_When_Registered()
    {
        // arrange & act
        var (runtime, _, topology) = CreateTopology(b => b.AddEventHandler<OrderCreatedHandler>());

        var consumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var route = runtime.Router.GetInboundByConsumer(consumer).First();
        var queueName = route.Endpoint!.Name;

        // assert - a queue must exist for the handler's receive endpoint
        Assert.Contains(topology.Queues, q => q.Name == queueName);

        // assert - a binding exists connecting a topic to the queue
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queueName);
    }

    [Fact]
    public void AddEventHandler_Should_CreatePublishTopic_When_Registered()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(b => b.AddEventHandler<OrderCreatedHandler>());

        // assert
        Assert.Contains(topology.Topics, t => t.Name.EndsWith(".order-created"));
    }

    [Fact]
    public void AddRequestHandler_Should_CreateQueue_When_Registered()
    {
        // arrange & act
        var (_, transport, topology) = CreateTopology(b => b.AddRequestHandler<ProcessPaymentHandler>());

        const string expectedQueueName = "process-payment";

        // assert - queue exists
        Assert.Contains(topology.Queues, q => q.Name == expectedQueueName);

        // assert - receive endpoint exists
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == expectedQueueName);
    }

    [Fact]
    public void AddRequestHandler_Should_CreateQueueAndReplyEndpoint_When_ResponseType()
    {
        // arrange & act
        var (_, transport, topology) = CreateTopology(b => b.AddRequestHandler<GetOrderStatusHandler>());

        const string expectedQueueName = "get-order-status";

        // assert - queue for the request type exists
        Assert.Contains(topology.Queues, q => q.Name == expectedQueueName);

        // assert - a reply receive endpoint is created (needed for request-response)
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Reply);

        // assert - a reply dispatch endpoint is created
        Assert.Contains(transport.DispatchEndpoints, e => e.Kind == DispatchEndpointKind.Reply);
    }

    [Fact]
    public void AddEventHandler_Should_CreateSeparateQueues_When_MultipleHandlersForSameEvent()
    {
        // arrange & act
        var (runtime, _, topology) = CreateTopology(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddEventHandler<OrderCreatedHandler2>();
        });

        var consumer1 = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var consumer2 = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler2));

        var queue1Name = runtime.Router.GetInboundByConsumer(consumer1).First().Endpoint!.Name;
        var queue2Name = runtime.Router.GetInboundByConsumer(consumer2).First().Endpoint!.Name;

        // assertthe two handler queues are distinct
        Assert.NotEqual(queue1Name, queue2Name);

        // assertboth queues exist in topology
        Assert.Contains(topology.Queues, q => q.Name == queue1Name);
        Assert.Contains(topology.Queues, q => q.Name == queue2Name);

        // assertboth queues have bindings from a topic
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queue1Name);
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queue2Name);

        // assertthey share a common publish topic for OrderCreated
        var publishTopicName = topology
            .Topics.Select(t => t.Name)
            .FirstOrDefault(n => n.Contains('.') && n.EndsWith("order-created"));

        Assert.NotNull(publishTopicName);
    }

    [Fact]
    public void AddHandlers_Should_CreateIndependentTopology_When_EventAndRequestRegistered()
    {
        // arrange & act
        var (runtime, transport, topology) = CreateTopology(b =>
        {
            b.AddEventHandler<OrderCreatedHandler>();
            b.AddRequestHandler<ProcessPaymentHandler>();
        });

        var eventConsumer = runtime.Consumers.First(c => c.Name == nameof(OrderCreatedHandler));
        var requestConsumer = runtime.Consumers.First(c => c.Name == nameof(ProcessPaymentHandler));

        var eventQueueName = runtime.Router.GetInboundByConsumer(eventConsumer).First().Endpoint!.Name;
        var requestQueueName = runtime.Router.GetInboundByConsumer(requestConsumer).First().Endpoint!.Name;

        // assertboth queues exist and are different
        Assert.NotEqual(eventQueueName, requestQueueName);
        Assert.Contains(topology.Queues, q => q.Name == eventQueueName);
        Assert.Contains(topology.Queues, q => q.Name == requestQueueName);

        // asserteach has its own receive endpoint
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == eventQueueName);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == requestQueueName);
    }

    [Fact]
    public async Task Receives_Should_StillDeliver_When_QueueOwnedByEndpoint()
    {
        // arrange
        // The endpoint now owns its queue via OnDiscoverTopology instead of the convention.
        // Publishing a message through the convention topology must still reach the handler.
        var recorder = new MessageRecorder();
        await using var provider = await new ServiceCollection()
            .AddSingleton(recorder)
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory()
            .BuildServiceProvider();

        var bus = provider.GetRequiredService<IMessageBus>();

        // act
        await bus.PublishAsync(new OrderCreated { OrderId = "EOQ-1" }, CancellationToken.None);

        // assert
        Assert.True(
            await recorder.WaitAsync(TimeSpan.FromSeconds(10)),
            "Handler did not receive message after endpoint-owns-queue move");

        var msg = Assert.IsType<OrderCreated>(Assert.Single(recorder.Messages));
        Assert.Equal("EOQ-1", msg.OrderId);
    }

    [Fact]
    public void Topology_Should_OmitConventionBinding_When_SagaHasOnReplyTransition()
    {
        // arrange
        // A saga with an OnReply transition registers an InboundRouteKind.Reply route. The receive
        // convention must skip reply routes so no spurious topic or binding appears for the reply type.
        var services = new ServiceCollection();
        services.AddInMemorySagas();
        var builder = services.AddMessageBus();
        builder.AddSaga<OrderStockCheckSaga>();
        var runtime = builder
            .AddInMemory(t => t.BindHandlersImplicitly())
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // act
        var snapshot = TopologySnapshotHelper.CreateSnapshot(topology);

        // assert
        // Only the start event chain appears. No topic or binding for
        // StockInfoResult (the reply type) should be present.
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_SuppressQueueBinding_When_TypeRouteAutoBindFalse()
    {
        // arrange
        // per-type auto-binding is off for OrderCreated, so the convention must not create the
        // topic-to-queue binding into the endpoint queue; the type-owned publish and send topics
        // are kept because they belong to the type, not the queue.
        var transport = CreateTransport(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .Receives<OrderCreated>(r => r.AutoBind(false));
            });

        // act
        var description = transport.Describe();

        // assert
        var snapshot = TopologySnapshotHelper.CreateDescribeSnapshot(description);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void Topology_Should_KeepTopics_When_QueueAutoBindFalse()
    {
        // arrange
        // queue-scope auto-binding is off, so no binding is created into the queue; the type-owned
        // publish and send topics still appear because suppression scope removes only the queue
        // bindings, not the type-owned topic entities.
        var transport = CreateTransport(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("orders");
                t.Endpoint("orders").Queue("orders")
                    .Consumer<OrderSpyConsumer>()
                    .AutoBind(false);
            });

        // act
        var description = transport.Describe();

        // assert
        var snapshot = TopologySnapshotHelper.CreateDescribeSnapshot(description);
        snapshot.MatchSnapshot();
    }

    [Fact]
    public void DiscoverTopology_Should_BindFromExplicitTopic_When_MessageHasExplicitPublishDestination()
    {
        // arrange
        // When a message type names an explicit publish topic the receive convention must bind from
        // that exact topic instead of the convention-derived chain.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<OrderCreated>(d => d.Publish(r => r.ToInMemoryTopic("custom-orders-topic")));
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder.AddInMemory().BuildRuntime();
        var topology = (InMemoryMessagingTopology)
            runtime.Transports.OfType<InMemoryMessagingTransport>().Single().Topology;

        // act
        var explicitTopic = topology.Topics.FirstOrDefault(t => t.Name == "custom-orders-topic");
        var bindingFromExplicit = topology.Bindings.OfType<InMemoryQueueBinding>()
            .FirstOrDefault(b => b.Source.Name == "custom-orders-topic");

        // assert
        Assert.NotNull(explicitTopic);
        Assert.NotNull(bindingFromExplicit);
    }

    [Fact]
    public void DiscoverTopology_Should_Throw_When_MessageHasExplicitQueueDestination()
    {
        // arrange
        // When a consumed message type is routed to an explicit queue the resolver returns Queue kind,
        // and there is no topic chain to bind from. The convention must reject this at build time.
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.AddMessage<OrderCreated>(d => d.Publish(r => r.ToInMemoryQueue("direct-q")));
        builder.AddEventHandler<OrderCreatedHandler>();

        // act
        var act = () => builder.AddInMemory().BuildRuntime();

        // assert
        Assert.Throws<InvalidOperationException>(act);
    }

    private static (
        MessagingRuntime Runtime,
        InMemoryMessagingTransport Transport,
        InMemoryMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.Host(h => h.ServiceName("test-app"));
        configure(builder);
        var runtime = builder.AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }

    private static InMemoryMessagingTransport CreateTransport(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IInMemoryMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        builder.Host(h => h.ServiceName("test-app"));
        configureBuilder(builder);
        var runtime = builder.AddInMemory(configureTransport).BuildRuntime();
        return runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
    }

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }

    public sealed class StockCheckStarted;

    public sealed class StockInfoResult;

    public sealed class GetStockInfoRequest : IEventRequest<StockInfoResult>;

    public sealed class StockCheckState : SagaStateBase;

    public sealed class OrderStockCheckSaga : Saga<StockCheckState>
    {
        protected override void Configure(ISagaDescriptor<StockCheckState> descriptor)
        {
            descriptor
                .Initially()
                .OnEvent<StockCheckStarted>()
                .StateFactory(_ => new StockCheckState())
                .Send((_, _) => new GetStockInfoRequest())
                .TransitionTo("Awaiting");

            descriptor.During("Awaiting").OnReply<StockInfoResult>().TransitionTo("Done");

            descriptor.Finally("Done");
        }
    }
}

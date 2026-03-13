using Microsoft.Extensions.DependencyInjection;
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

        // assert -- a queue must exist for the handler's receive endpoint
        Assert.Contains(topology.Queues, q => q.Name == queueName);

        // assert -- a binding exists connecting a topic to the queue
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

        // assert -- queue exists
        Assert.Contains(topology.Queues, q => q.Name == expectedQueueName);

        // assert -- receive endpoint exists
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == expectedQueueName);
    }

    [Fact]
    public void AddRequestHandler_Should_CreateQueueAndReplyEndpoint_When_ResponseType()
    {
        // arrange & act
        var (_, transport, topology) = CreateTopology(b => b.AddRequestHandler<GetOrderStatusHandler>());

        const string expectedQueueName = "get-order-status";

        // assert -- queue for the request type exists
        Assert.Contains(topology.Queues, q => q.Name == expectedQueueName);

        // assert -- a reply receive endpoint is created (needed for request-response)
        Assert.Contains(transport.ReceiveEndpoints, e => e.Kind == ReceiveEndpointKind.Reply);

        // assert -- a reply dispatch endpoint is created
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

        // assert -- the two handler queues are distinct
        Assert.NotEqual(queue1Name, queue2Name);

        // assert -- both queues exist in topology
        Assert.Contains(topology.Queues, q => q.Name == queue1Name);
        Assert.Contains(topology.Queues, q => q.Name == queue2Name);

        // assert -- both queues have bindings from a topic
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queue1Name);
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queue2Name);

        // assert -- they share a common publish topic for OrderCreated
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

        // assert -- both queues exist and are different
        Assert.NotEqual(eventQueueName, requestQueueName);
        Assert.Contains(topology.Queues, q => q.Name == eventQueueName);
        Assert.Contains(topology.Queues, q => q.Name == requestQueueName);

        // assert -- each has its own receive endpoint
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == eventQueueName);
        Assert.Contains(transport.ReceiveEndpoints, e => e.Name == requestQueueName);
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

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken) => default;
    }
}

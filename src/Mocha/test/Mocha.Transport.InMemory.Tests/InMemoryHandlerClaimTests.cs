using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryHandlerClaimTests
{
    [Fact]
    public void Handler_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");

        Assert.NotNull(endpoint);
        Assert.Contains(typeof(OrderCreatedHandler), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_ApplyConfig_When_ConfigureEndpointCalled()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>()
                    .ConfigureEndpoint(e => e.MaxConcurrency(5));
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Single(e => e.Name == "order-created");

        Assert.Equal(5, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Consumer_Should_CreateEndpoint_When_Called()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Consumer<TestOrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "test-order");

        Assert.NotNull(endpoint);
        Assert.Contains(typeof(TestOrderConsumer), endpoint.Configuration.ConsumerIdentities);
    }

    [Fact]
    public void Handler_Should_MergeWithExisting_When_ConventionNameMatchesExplicitEndpoint()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Endpoint("order-created").MaxConcurrency(10);
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert - only one endpoint with that name, containing both the handler and the concurrency setting
        var endpoints = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Where(e => e.Name == "order-created")
            .ToList();

        var endpoint = Assert.Single(endpoints);
        Assert.Contains(typeof(OrderCreatedHandler), endpoint.Configuration.ConsumerIdentities);
        Assert.Equal(10, endpoint.Configuration.MaxConcurrency);
    }

    [Fact]
    public void Handler_Should_CreateSeparateEndpoints_When_MultipleHandlersClaimed()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHandler<OrderCreatedHandler2>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
                t.Handler<OrderCreatedHandler2>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert
        var endpointNames = transport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .Where(e => e.Name == "order-created" || e.Name == "order-created-handler-2")
            .Select(e => e.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(2, endpointNames.Count);
        Assert.Contains("order-created", endpointNames);
        Assert.Contains("order-created-handler-2", endpointNames);
    }

    [Fact]
    public void Handler_Should_BindToNonDefaultTransport_When_ClaimedByInMemory()
    {
        // arrange & act
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHandler<OrderCreatedHandler2>()
            // Default transport - unclaimed handlers bind here automatically.
            .AddInMemory(t =>
            {
                t.Name("default");
                t.IsDefaultTransport();
            })
            // Non-default transport - only explicitly claimed handlers bind here.
            .AddInMemory(t =>
            {
                t.Name("inmemory");
                t.BindHandlersExplicitly();
                t.Handler<OrderCreatedHandler>();
            })
            .BuildRuntime();

        var defaultTransport = runtime.Transports
            .OfType<InMemoryMessagingTransport>()
            .Single(t => t.Name == "default");

        var nonDefaultTransport = runtime.Transports
            .OfType<InMemoryMessagingTransport>()
            .Single(t => t.Name == "inmemory");

        // assert - claimed handler is on the non-default transport
        var claimedEndpoint = nonDefaultTransport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name == "order-created");

        Assert.NotNull(claimedEndpoint);
        Assert.Contains(typeof(OrderCreatedHandler), claimedEndpoint.Configuration.ConsumerIdentities);

        // assert - unclaimed handler fell through to the default transport
        var unclaimedEndpoint = defaultTransport.ReceiveEndpoints
            .OfType<InMemoryReceiveEndpoint>()
            .SingleOrDefault(e => e.Name.EndsWith("order-created-handler-2"));

        Assert.NotNull(unclaimedEndpoint);

        // assert - neither transport has the other's handler endpoint
        Assert.DoesNotContain(
            nonDefaultTransport.ReceiveEndpoints.OfType<InMemoryReceiveEndpoint>(),
            e => e.Name.EndsWith("order-created-handler-2"));

        Assert.DoesNotContain(
            defaultTransport.ReceiveEndpoints.OfType<InMemoryReceiveEndpoint>(),
            e => e.Name == "order-created");
    }

    [Fact]
    public void Send_Should_RouteToExplicitDestination_When_HandlerForSameMessageBoundExplicitly()
    {
        // arrange & act
        // the same bus handles ProcessPayment via an explicitly bound handler endpoint and
        // also sends ProcessPayment to an explicit destination queue.
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddMessage<ProcessPayment>(d => d.Send(r => r.ToInMemoryQueue("my-queue")))
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.DeclareQueue("payment-q");
                t.DeclareQueue("my-queue");
                t.Endpoint("payment-endpoint").Queue("payment-q").Handler<ProcessPaymentHandler>();
            })
            .BuildRuntime();

        // assert - the send route resolves to the explicit destination instead of the convention endpoint
        var route = runtime.Router.OutboundRoutes.Single(r =>
            r.Kind == OutboundRouteKind.Send && r.MessageType.RuntimeType == typeof(ProcessPayment));

        Assert.Contains("q/my-queue", route.Endpoint.Address.ToString());
    }

    [Fact]
    public void Receives_Should_NotAutoDiscoverClaimedType_OntoSecondEndpoint()
    {
        // arrange & act
        // In implicit mode OrderCreated is claimed by the configured "test-order" endpoint through
        // TestOrderConsumer. A second, unplaced handler of OrderCreated must not auto-discover its own
        // framework endpoint; the claimed type converges onto the claiming endpoint instead.
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddEventHandler<OrderCreatedHandler2>()
            .AddInMemory(t => t.Consumer<TestOrderConsumer>())
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();

        // assert - no framework endpoint was fabricated for the unplaced second handler
        Assert.DoesNotContain(
            transport.ReceiveEndpoints.OfType<InMemoryReceiveEndpoint>(),
            e => e.Name.EndsWith("order-created-handler-2"));

        // assert - the unplaced handler's route converged onto the claiming endpoint
        var handler2 = runtime.Consumers.Single(c => c.Name == nameof(OrderCreatedHandler2));
        var route = runtime.Router.GetInboundByConsumer(handler2).Single();
        Assert.Equal("test-order", route.Endpoint!.Name);
    }

    [Fact]
    public void BindHandlersExplicitly_Should_StillGenerateTopology_When_AutoBindDefaultOn()
    {
        // arrange & act
        // BindHandlersExplicitly governs only consumer binding (axis A); with AutoBind left at its
        // default the convention still gap-fills the consume topology for the claimed endpoint.
        var runtime = new ServiceCollection()
            .AddMessageBus()
            .AddConsumer<TestOrderConsumer>()
            .AddInMemory(t =>
            {
                t.BindHandlersExplicitly();
                t.Consumer<TestOrderConsumer>();
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var consumer = runtime.Consumers.Single(c => c.Name == nameof(TestOrderConsumer));
        var queueName = runtime.Router.GetInboundByConsumer(consumer).Single().Endpoint!.Name;

        // assert - the queue exists and a topic-to-queue binding was generated despite explicit binding
        Assert.Contains(topology.Queues, q => q.Name == queueName);
        Assert.Contains(topology.Bindings.OfType<InMemoryQueueBinding>(), b => b.Destination.Name == queueName);
    }

    public sealed class TestOrderConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }

    public sealed class ProcessPaymentHandler : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            return default;
        }
    }
}

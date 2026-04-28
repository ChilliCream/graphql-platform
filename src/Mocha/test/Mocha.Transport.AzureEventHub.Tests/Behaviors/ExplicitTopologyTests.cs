using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Behaviors;

public class ExplicitTopologyTests
{
    [Fact]
    public void BuildRuntime_Should_CreateTopicAndSubscription_When_ExplicitTopologyDeclared()
    {
        // arrange & act
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersExplicitly();
                t.DeclareTopic("custom-hub");
                t.DeclareSubscription("custom-hub", "my-group");

                t.Endpoint("custom-ep")
                    .Handler<OrderCreatedHandler>()
                    .Hub("custom-hub")
                    .ConsumerGroup("my-group");

                t.DispatchEndpoint("custom-dispatch")
                    .ToHub("custom-hub")
                    .Publish<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        var topic = Assert.Single(topology.Topics, t => t.Name == "custom-hub");
        Assert.NotNull(topic.Address);

        var subscription = Assert.Single(topology.Subscriptions, s => s.ConsumerGroup == "my-group");
        Assert.Equal("custom-hub", subscription.TopicName);
    }

    [Fact]
    public void BuildRuntime_Should_CreateTopicAndSubscription_When_ImplicitBindingWithExplicitTopology()
    {
        // arrange & act
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersImplicitly();
                t.DeclareTopic("custom-hub");
                t.DeclareSubscription("custom-hub", "my-group");

                t.Endpoint("custom-ep")
                    .Handler<OrderCreatedHandler>()
                    .Hub("custom-hub")
                    .ConsumerGroup("my-group");

                t.DispatchEndpoint("custom-dispatch")
                    .ToHub("custom-hub")
                    .Publish<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        Assert.Contains(topology.Topics, t => t.Name == "custom-hub");

        var subscription = Assert.Single(topology.Subscriptions, s => s.ConsumerGroup == "my-group");
        Assert.Equal("custom-hub", subscription.TopicName);
    }

    [Fact]
    public void BuildRuntime_Should_CreateDispatchEndpoint_When_ExplicitTopologyWithDispatch()
    {
        // arrange & act
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersExplicitly();
                t.DeclareTopic("dispatch-hub");

                t.Endpoint("recv-ep")
                    .Handler<OrderCreatedHandler>()
                    .Hub("dispatch-hub");

                t.DispatchEndpoint("send-ep")
                    .ToHub("dispatch-hub")
                    .Publish<OrderCreated>();
            })
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var dispatchEndpoint = Assert.Single(
            transport.DispatchEndpoints,
            e => e.Name == "send-ep");
        Assert.NotNull(dispatchEndpoint);
    }

    [Fact]
    public void BuildRuntime_Should_SetConsumerGroupToDefault_When_NotSpecifiedOnSubscription()
    {
        // arrange & act
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersExplicitly();
                t.DeclareTopic("default-group-hub");
                t.DeclareSubscription("default-group-hub", "$Default");

                t.Endpoint("ep")
                    .Handler<OrderCreatedHandler>()
                    .Hub("default-group-hub");
            })
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        var subscription = Assert.Single(
            topology.Subscriptions,
            s => s.TopicName == "default-group-hub");
        Assert.Equal("$Default", subscription.ConsumerGroup);
    }

    [Fact]
    public void BuildRuntime_Should_CreateMultipleTopics_When_MultipleExplicitTopicsDeclared()
    {
        // arrange & act
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddEventHandler<OrderCreatedHandler>()
            .AddRequestHandler<ProcessPaymentHandler>()
            .AddEventHub(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                t.BindHandlersExplicitly();
                t.DeclareTopic("hub-orders");
                t.DeclareTopic("hub-payments");

                t.Endpoint("orders-ep")
                    .Handler<OrderCreatedHandler>()
                    .Hub("hub-orders");

                t.Endpoint("payments-ep")
                    .Handler<ProcessPaymentHandler>()
                    .Hub("hub-payments");

                t.DispatchEndpoint("dispatch-orders")
                    .ToHub("hub-orders")
                    .Publish<OrderCreated>();

                t.DispatchEndpoint("dispatch-payments")
                    .ToHub("hub-payments")
                    .Send<ProcessPayment>();
            })
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;

        Assert.Contains(topology.Topics, t => t.Name == "hub-orders");
        Assert.Contains(topology.Topics, t => t.Name == "hub-payments");
        Assert.Equal(2, transport.DispatchEndpoints.Count(e =>
            e.Name == "dispatch-orders" || e.Name == "dispatch-payments"));
    }

    public sealed class ProcessPaymentHandler(MessageRecorder recorder) : IEventRequestHandler<ProcessPayment>
    {
        public ValueTask HandleAsync(ProcessPayment request, CancellationToken cancellationToken)
        {
            recorder.Record(request);
            return default;
        }
    }
}

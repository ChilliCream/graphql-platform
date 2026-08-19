using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Routing;

public class AzureServiceBusReceiveTopologyTests
{
    [Fact]
    public void DiscoverTopology_Should_SubscribePublishTopicToConsumerQueue_When_BindingIsImplicit()
    {
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.BindImplicitly());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);
        var topicName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        var publishEndpoint = Assert.IsType<AzureServiceBusDispatchEndpoint>(
            runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(OrderCreated))));
        var subscription = topology.Subscriptions.Single(s =>
            s.Source.Name == topicName && s.Destination.Name == endpoint.Queue.Name);

        Assert.Equal(topicName, publishEndpoint.Topic?.Name);
        Assert.Equal(TopologyOrigin.Convention, subscription.Origin);
    }

    [Fact]
    public void DiscoverTopology_Should_SendDirectlyToQueue_When_RouteIsSend()
    {
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Send(_ => { })),
            t => t.BindImplicitly());
        var queueName = runtime.Naming.GetSendEndpointName(typeof(OrderCreated));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .Single(e => e.Queue?.Name == queueName);

        Assert.Equal("q/" + queueName, endpoint.Name);
        Assert.Null(endpoint.Topic);
    }

    [Fact]
    public void DiscoverTopology_Should_SuppressConventionSubscription_When_QueueBindsExplicitly()
    {
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.Queue("orders").Consumer<OrderSpyConsumer>().BindExplicitly());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);
        var topicName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        var subscriptions = topology.Subscriptions.Where(s =>
            s.Source.Name == topicName && s.Destination.Name == endpoint.Queue.Name).ToList();

        Assert.Empty(subscriptions);
    }

    [Fact]
    public void DiscoverTopology_Should_SubscribeExplicitTopic_When_PublishDestinationConfigured()
    {
        var runtime = CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d =>
                    d.Publish(r => r.ToAzureServiceBusTopic("custom-orders")));
            },
            t => t.BindImplicitly());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);

        var subscription = topology.Subscriptions.Single(s =>
            s.Source.Name == "custom-orders" && s.Destination.Name == endpoint.Queue.Name);

        Assert.Equal(TopologyOrigin.Convention, subscription.Origin);
    }

    [Fact]
    public void DiscoverTopology_Should_SubscribeCustomDispatchTopic_When_PublishRouteUsesDispatchEndpoint()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t => t.DispatchEndpoint("custom-orders-endpoint").ToTopic("custom-orders").Publish<OrderCreated>());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);

        // act
        var subscription = topology.Subscriptions.Single(s =>
            s.Source.Name == "custom-orders" && s.Destination.Name == endpoint.Queue.Name);

        // assert
        Assert.Equal(TopologyOrigin.Convention, subscription.Origin);
    }

    [Fact]
    public void DiscoverTopology_Should_NotProvisionTopic_When_QueueBindsExplicitlyAndTopicIsUndeclared()
    {
        // arrange
        // Receive topology discovery runs before dispatch topology discovery, so an unguarded
        // EnsureTopic on the receive side would implicitly create "custom-orders" and mask the
        // fact that it was never declared, letting the explicit-mode dispatch endpoint complete
        // successfully against a topic the user never provisioned.

        // act
        var exception = Record.Exception(() => CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.DispatchEndpoint("custom-orders-endpoint").ToTopic("custom-orders").Publish<OrderCreated>();
            }));

        // assert
        Assert.Equal("Topic not found", Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void DiscoverTopology_Should_NotSubscribeConsumerQueue_When_QueueBindsExplicitlyAndTopicIsDeclared()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddConsumer<OrderSpyConsumer>(),
            t =>
            {
                t.BindExplicitly();
                t.DeclareTopic("custom-orders");
                t.Queue("orders").Consumer<OrderSpyConsumer>();
                t.DispatchEndpoint("custom-orders-endpoint").ToTopic("custom-orders").Publish<OrderCreated>();
            });
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);

        // act
        var subscriptions = topology.Subscriptions
            .Where(s => s.Source.Name == "custom-orders" && s.Destination.Name == endpoint.Queue.Name)
            .ToList();

        // assert
        Assert.Empty(subscriptions);
    }

    [Fact]
    public void DiscoverTopology_Should_SubscribeExplicitTopic_When_PublishDestinationTargetsCurrentNamespace()
    {
        // arrange
        var runtime = CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d =>
                    d.Publish(r => r.Destination(new Uri("azuresb://localhost/t/custom-orders"))));
            },
            t => t.BindImplicitly());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);

        // act
        var subscription = topology.Subscriptions.Single(s =>
            s.Source.Name == "custom-orders" && s.Destination.Name == endpoint.Queue.Name);

        // assert
        Assert.Equal(TopologyOrigin.Convention, subscription.Origin);
    }

    [Fact]
    public void DiscoverTopology_Should_Throw_When_PublishDestinationTargetsAnotherNamespace()
    {
        // arrange
        var destination = new Uri("azuresb://other-namespace/t/custom-orders");

        // act
        var exception = Record.Exception(() => CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d => d.Publish(r => r.Destination(destination)));
            },
            t => t.BindImplicitly()));

        // assert
        Assert.Equal(
            "No transport can handle address: " + destination,
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void DiscoverTopology_Should_NotSubscribeConsumerQueue_When_PublishTargetsQueue()
    {
        var runtime = CreateRuntime(
            b =>
            {
                b.AddConsumer<OrderSpyConsumer>();
                b.AddMessage<OrderCreated>(d =>
                    d.Publish(r => r.ToAzureServiceBusQueue("published-orders")));
            },
            t => t.BindImplicitly());
        var (topology, endpoint) = ResolveConsumerEndpoint(runtime);
        var dispatchEndpoint = Assert.IsType<AzureServiceBusDispatchEndpoint>(
            runtime.GetPublishEndpoint(runtime.GetMessageType(typeof(OrderCreated))));
        var subscriptions = topology.Subscriptions
            .Where(s => s.Destination.Name == endpoint.Queue.Name)
            .ToList();

        Assert.Equal("published-orders", dispatchEndpoint.Queue?.Name);
        Assert.Empty(subscriptions);
    }

    private static (AzureServiceBusMessagingTopology Topology, AzureServiceBusReceiveEndpoint Endpoint)
        ResolveConsumerEndpoint(MessagingRuntime runtime)
    {
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var endpoint = transport.ReceiveEndpoints
            .OfType<AzureServiceBusReceiveEndpoint>()
            .Single(e => e.Kind == ReceiveEndpointKind.Default);
        return ((AzureServiceBusMessagingTopology)transport.Topology, endpoint);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configureBuilder,
        Action<IAzureServiceBusMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configureBuilder(builder);
        return builder
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configureTransport(t);
            })
            .BuildRuntime();
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    public sealed class OrderSpyConsumer : IConsumer<OrderCreated>
    {
        public ValueTask ConsumeAsync(IConsumeContext<OrderCreated> context) => default;
    }
}

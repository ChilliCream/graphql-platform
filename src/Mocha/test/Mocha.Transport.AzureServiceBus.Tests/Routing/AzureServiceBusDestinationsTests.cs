using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Routing;

public class AzureServiceBusDestinationsTests
{
    [Fact]
    public void Resolve_Should_UseConventionQueue_When_RouteIsSend()
    {
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Send(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var expectedName = runtime.Naming.GetSendEndpointName(typeof(OrderCreated));

        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
        Assert.Equal("q/" + expectedName, resolution.EndpointName);
    }

    [Fact]
    public void Resolve_Should_UseConventionTopic_When_RouteIsPublish()
    {
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        Assert.Equal(AzureServiceBusDestinationKind.Topic, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
        Assert.Equal("t/" + expectedName, resolution.EndpointName);
    }

    [Fact]
    public void Resolve_Should_UseExplicitQueue_When_RouteTargetsQueue()
    {
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.ToAzureServiceBusQueue("orders-queue"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders-queue", resolution.Name);
        Assert.Equal("q/orders-queue", resolution.EndpointName);
    }

    [Fact]
    public void Resolve_Should_UseExplicitTopic_When_RouteTargetsTopic()
    {
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Publish(r => r.ToAzureServiceBusTopic("orders-topic"))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            runtime.Naming,
            route);

        Assert.Equal(AzureServiceBusDestinationKind.Topic, resolution.Kind);
        Assert.Equal("orders-topic", resolution.Name);
        Assert.Equal("t/orders-topic", resolution.EndpointName);
    }

    [Theory]
    [InlineData("azuresb:t/orders", "orders")]
    [InlineData("topic:orders", "orders")]
    [InlineData("topic://orders", "orders")]
    public void TryResolveSourceTopic_Should_ResolveTopicAddress_When_AddressIsSupported(
        string address,
        string expected)
    {
        var success = AzureServiceBusDestinations.TryResolveSourceTopic(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            new Uri(address),
            out var topicName);

        Assert.True(success);
        Assert.Equal(expected, topicName);
    }

    [Fact]
    public void TryResolveSourceTopic_Should_ReturnFalse_When_AddressTargetsQueue()
    {
        var success = AzureServiceBusDestinations.TryResolveSourceTopic(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            new Uri("azuresb:q/orders"),
            out var topicName);

        Assert.False(success);
        Assert.Null(topicName);
    }

    [Theory]
    [InlineData("queue:orders", "orders", true)]
    [InlineData("queue://orders", "orders", true)]
    [InlineData("topic:orders", "orders", false)]
    [InlineData("topic://orders", "orders", false)]
    public void CreateEndpointConfiguration_Should_ResolveNeutralAddress(
        string address,
        string expectedName,
        bool isQueue)
    {
        var runtime = CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        var configuration = Assert.IsType<AzureServiceBusDispatchEndpointConfiguration>(
            transport.CreateEndpointConfiguration(runtime, new Uri(address)));

        Assert.IsType<AzureServiceBusRoutingStrategy>(transport.Routing);
        Assert.Equal(isQueue ? expectedName : null, configuration.QueueName);
        Assert.Equal(isQueue ? null : expectedName, configuration.TopicName);
        Assert.Equal((isQueue ? "q/" : "t/") + expectedName, configuration.Name);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddAzureServiceBus(t => t.ConnectionString(DummyConnectionString))
            .BuildRuntime();
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";
}

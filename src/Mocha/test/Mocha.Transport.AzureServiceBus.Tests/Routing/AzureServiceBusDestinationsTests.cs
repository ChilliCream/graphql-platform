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
            LocalNamespace,
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
            LocalNamespace,
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
            LocalNamespace,
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
            LocalNamespace,
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

    [Fact]
    public void TryResolveSourceTopic_Should_ReturnFalse_When_QueueSchemeHasTopicMarker()
    {
        // arrange

        // act
        var success = AzureServiceBusDestinations.TryResolveSourceTopic(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            new Uri("queue://host/t/orders"),
            out var topicName);

        // assert
        Assert.False(success);
        Assert.Null(topicName);
    }

    [Fact]
    public void Resolve_Should_FallBackToConventionTopic_When_TopicSchemeHasQueueMarker()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Publish(r => r.Destination(new Uri("topic://host/q/orders")))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            LocalNamespace,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(AzureServiceBusDestinationKind.Topic, resolution.Kind);
        Assert.Equal(expectedName, resolution.Name);
    }

    [Fact]
    public void Resolve_Should_ResolveExplicitQueue_When_NeutralQueueSchemeHasQueueMarker()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.Destination(new Uri("queue://host/q/orders")))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            LocalNamespace,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders", resolution.Name);
        Assert.Equal("q/orders", resolution.EndpointName);
    }

    [Fact]
    public void TryResolveSourceTopic_Should_ResolveTopic_When_NeutralTopicSchemeHasTopicMarker()
    {
        // arrange

        // act
        var success = AzureServiceBusDestinations.TryResolveSourceTopic(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            new Uri("topic://host/t/orders"),
            out var topicName);

        // assert
        Assert.True(success);
        Assert.Equal("orders", topicName);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_ResolveQueue_When_AddressIsTopologyAddress()
    {
        var runtime = CreateRuntime(_ => { }, t => t.DeclareQueue("orders"));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var queue = ((AzureServiceBusMessagingTopology)transport.Topology).Queues
            .Single(q => q.Name == "orders");

        var endpoint = runtime.GetDispatchEndpoint(queue.Address);

        Assert.Equal("q/orders", endpoint.Name);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_DecodeQueueName_When_NameIsUriEncoded()
    {
        var runtime = CreateRuntime(_ => { }, t => t.DeclareQueue("space name"));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var queue = ((AzureServiceBusMessagingTopology)transport.Topology).Queues
            .Single(q => q.Name == "space name");

        var endpoint = runtime.GetDispatchEndpoint(queue.Address);

        Assert.Equal("q/space name", endpoint.Name);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_Throw_When_AddressIsOnAnotherNamespace()
    {
        var runtime = CreateRuntime(_ => { });
        var address = new Uri("azuresb://other-namespace/q/orders");

        var exception = Record.Exception(() => runtime.GetDispatchEndpoint(address));

        Assert.Equal(
            "No transport can handle address: " + address,
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void Resolve_Should_PreserveSlashInName_When_NameContainsSlash()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.Destination(new Uri("azuresb:q/orders/eu")))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            LocalNamespace,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders/eu", resolution.Name);
        Assert.Equal("q/orders/eu", resolution.EndpointName);
    }

    [Fact]
    public void Resolve_Should_UseExplicitQueue_When_DestinationTargetsCurrentNamespace()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.Destination(new Uri("azuresb://localhost/q/orders")))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolution = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            LocalNamespace,
            runtime.Naming,
            route);

        // assert
        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolution.Kind);
        Assert.Equal("orders", resolution.Name);
        Assert.Equal("q/orders", resolution.EndpointName);
    }

    [Fact]
    public void Resolve_Should_Throw_When_DestinationTargetsAnotherNamespace()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.Destination(new Uri("azuresb://localhost/q/orders")))));
        var messageType = runtime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = runtime.Router.GetOutboundByMessageType(messageType).Single();
        var otherNamespace = new Uri("azuresb://other-namespace/");

        // act
        var exception = Record.Exception(() => AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            otherNamespace,
            runtime.Naming,
            route));

        // assert
        Assert.Equal(
            "Explicit destination 'azuresb://localhost/q/orders' targets Azure Service Bus namespace "
            + "'localhost', but this transport is connected to namespace 'other-namespace'. Route the "
            + "message through a transport connected to that namespace, or omit the host to target the "
            + "current namespace implicitly.",
            Assert.IsType<InvalidOperationException>(exception).Message);
    }

    [Fact]
    public void Resolve_Should_TreatExplicitDestination_When_NamespacesDifferAcrossTwoTransports()
    {
        // arrange
        var primaryRuntime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d =>
                d.Send(r => r.Destination(new Uri("azuresb://primary-namespace/q/orders")))),
            connectionString: "Endpoint=sb://primary-namespace/;SharedAccessKeyName=test;SharedAccessKey=test");
        var secondaryRuntime = CreateRuntime(
            _ => { },
            connectionString: "Endpoint=sb://secondary-namespace/;SharedAccessKeyName=test;SharedAccessKey=test");

        var primaryTransport = primaryRuntime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var secondaryTransport = secondaryRuntime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var messageType = primaryRuntime.Messages.GetMessageType(typeof(OrderCreated))!;
        var route = primaryRuntime.Router.GetOutboundByMessageType(messageType).Single();

        // act
        var resolvedOnOwningTransport = AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            primaryTransport.Topology.Address,
            primaryRuntime.Naming,
            route);
        var exception = Record.Exception(() => AzureServiceBusDestinations.Resolve(
            AzureServiceBusTransportConfiguration.DefaultSchema,
            secondaryTransport.Topology.Address,
            primaryRuntime.Naming,
            route));

        // assert
        Assert.Equal(AzureServiceBusDestinationKind.Queue, resolvedOnOwningTransport.Kind);
        Assert.Equal("orders", resolvedOnOwningTransport.Name);
        Assert.IsType<InvalidOperationException>(exception);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IMessageBusHostBuilder> configure,
        Action<IAzureServiceBusMessagingTransportDescriptor>? configureTransport = null,
        string connectionString = DummyConnectionString)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        return builder
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(connectionString);
                configureTransport?.Invoke(t);
            })
            .BuildRuntime();
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    private static readonly Uri LocalNamespace = new("azuresb://localhost/");
}

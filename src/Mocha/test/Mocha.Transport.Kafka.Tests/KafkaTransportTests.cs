using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests;

public class KafkaTransportTests
{
    [Fact]
    public void Schema_Should_BeKafka_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = KafkaBusFixture.CreateTopology(_ => { });

        // assert
        Assert.Equal("kafka", transport.Schema);
    }

    [Fact]
    public void TopologyAddress_Should_UseKafkaScheme_When_TransportCreated()
    {
        // arrange & act
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        // assert
        Assert.Equal("kafka", topology.Address.Scheme);
    }

    [Fact]
    public void Topology_Should_BeKafkaMessagingTopology_When_TransportCreated()
    {
        // arrange & act
        var (_, transport, _) = KafkaBusFixture.CreateTopology(_ => { });

        // assert
        Assert.IsType<KafkaMessagingTopology>(transport.Topology);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_KafkaSchemeMatchesAddress()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.DispatchEndpoint("ep").ToTopic("my-topic").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        var existingEndpoint = transport.DispatchEndpoints.First();
        var address = existingEndpoint.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(address, out var endpoint);

        // assert
        Assert.True(found);
        Assert.NotNull(endpoint);
        Assert.Same(existingEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ResolveToTopic_When_TopicSchemeUsed()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events");
            t.DispatchEndpoint("ep").ToTopic("events").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        var topicUri = new Uri("topic://events");

        // act
        var found = transport.TryGetDispatchEndpoint(topicUri, out var endpoint);

        // assert
        Assert.True(found, "TryGetDispatchEndpoint should resolve topic:// URI");
        Assert.NotNull(endpoint);
        Assert.IsType<KafkaTopic>(endpoint!.Destination);
        Assert.Equal("events", ((KafkaTopic)endpoint.Destination).Name);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_Resolve_When_TopologyBaseAddressUsed()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("base-topic");
            t.DispatchEndpoint("ep").ToTopic("base-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var topology = (KafkaMessagingTopology)transport.Topology;

        var dispatchEndpoint = transport.DispatchEndpoints.FirstOrDefault(e =>
            e.Destination?.Address != null && topology.Address.IsBaseOf(e.Destination.Address));

        Assert.NotNull(dispatchEndpoint);
        var destinationAddress = dispatchEndpoint!.Destination.Address;

        // act
        var found = transport.TryGetDispatchEndpoint(destinationAddress, out var endpoint);

        // assert
        Assert.True(found);
        Assert.NotNull(endpoint);
        Assert.Same(dispatchEndpoint, endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_UnknownUri()
    {
        // arrange
        var (_, transport, _) = KafkaBusFixture.CreateTopology(_ => { });
        var unknownUri = new Uri("http://unknown-host/nonexistent");

        // act
        var found = transport.TryGetDispatchEndpoint(unknownUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_TopicNotFound()
    {
        // arrange
        var (_, transport, _) = KafkaBusFixture.CreateTopology(_ => { });
        var nonexistentUri = new Uri("topic://nonexistent-topic");

        // act
        var found = transport.TryGetDispatchEndpoint(nonexistentUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void TryGetDispatchEndpoint_Should_ReturnFalse_When_KafkaSchemeNoMatch()
    {
        // arrange
        var (_, transport, _) = KafkaBusFixture.CreateTopology(_ => { });
        var kafkaUri = new Uri("kafka://no-match/some-endpoint");

        // act
        var found = transport.TryGetDispatchEndpoint(kafkaUri, out var endpoint);

        // assert
        Assert.False(found);
        Assert.Null(endpoint);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_TopicSchemeUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("topic:my-topic"));

        // assert
        Assert.NotNull(config);
        var kafkaConfig = Assert.IsType<KafkaDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", kafkaConfig.TopicName);
        Assert.Equal("t/my-topic", kafkaConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_KafkaSchemeTopicPath()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("kafka:///t/my-topic"));

        // assert
        Assert.NotNull(config);
        var kafkaConfig = Assert.IsType<KafkaDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", kafkaConfig.TopicName);
        Assert.Equal("t/my-topic", kafkaConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateReplyConfig_When_KafkaRepliesPath()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("kafka:///replies"));

        // assert
        Assert.NotNull(config);
        var kafkaConfig = Assert.IsType<KafkaDispatchEndpointConfiguration>(config);
        Assert.Equal(DispatchEndpointKind.Reply, kafkaConfig.Kind);
        Assert.Equal("Replies", kafkaConfig.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_CreateTopicConfig_When_TopologyBaseTopicUri()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;
        var topologyAddress = ((KafkaMessagingTopology)transport.Topology).Address;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri(topologyAddress, "t/my-topic"));

        // assert
        Assert.NotNull(config);
        var kafkaConfig = Assert.IsType<KafkaDispatchEndpointConfiguration>(config);
        Assert.Equal("my-topic", kafkaConfig.TopicName);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_ReturnNull_When_UnknownScheme()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();
        var context = (IMessagingConfigurationContext)runtime;

        // act
        var config = transport.CreateEndpointConfiguration(context, new Uri("http://foo/bar"));

        // assert
        Assert.Null(config);
    }

    [Fact]
    public void DispatchEndpoints_Should_HaveTopicDestination_When_PublishConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events-t");
            t.DispatchEndpoint("events").ToTopic("events-t").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var topicEndpoints = transport.DispatchEndpoints
            .Where(e => e.Destination is KafkaTopic)
            .ToList();

        // assert
        Assert.NotEmpty(topicEndpoints);
        var topicDispatch = topicEndpoints.First();
        Assert.NotNull(topicDispatch.Destination?.Address);
        Assert.Contains("/t/", topicDispatch.Destination!.Address!.AbsolutePath);
    }

    [Fact]
    public async Task DisposeAsync_Should_NotThrow_When_Called()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act & assert - no exception thrown
        await transport.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_CalledTwice()
    {
        // arrange
        var runtime = CreateRuntimeWithHandlers(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act - call twice
        await transport.DisposeAsync();
        await transport.DisposeAsync();

        // assert - no exception
    }

    private static MessagingRuntime CreateRuntimeWithHandlers(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddKafka(t => t.BootstrapServers("localhost:9092"))
            .BuildRuntime();
        return runtime;
    }
}

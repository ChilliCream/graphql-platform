using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Descriptors;

public class KafkaDescriptorTests
{
    [Fact]
    public void Transport_Should_UseDefaultSchema_When_NoSchemaConfigured()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        Assert.Equal("kafka", transport.Schema);
    }

    [Fact]
    public void Transport_Should_UseCustomSchema_When_SchemaConfigured()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t => t.Schema("custom"));
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        Assert.Equal("custom", transport.Schema);
    }

    [Fact]
    public void Transport_Should_UseDefaultName_When_NoNameConfigured()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(_ => { });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        Assert.Equal("kafka", transport.Name);
    }

    [Fact]
    public void Transport_Should_UseCustomName_When_NameConfigured()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t => t.Name("my-transport"));
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        Assert.Equal("my-transport", transport.Name);
    }

    [Fact]
    public void Transport_Should_BeDefault_When_IsDefaultTransportCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t => t.IsDefaultTransport());
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        Assert.NotNull(transport);
        Assert.Single(runtime.Transports);
    }

    [Fact]
    public void BootstrapServers_Should_SetConfiguration_When_Called()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t => t.BootstrapServers("broker1:9092,broker2:9092"));
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert - topology address should reflect the first bootstrap server
        var topology = (KafkaMessagingTopology)transport.Topology;
        Assert.Contains("broker1", topology.Address.Host);
    }

    [Fact]
    public void ConfigureProducer_Should_AcceptOverrides_When_Called()
    {
        // arrange & act - should not throw
        var runtime = KafkaBusFixture.CreateRuntime(t =>
            t.ConfigureProducer(p => p.LingerMs = 10));
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert - transport was created successfully with producer config
        Assert.NotNull(transport);
    }

    [Fact]
    public void ConfigureConsumer_Should_AcceptOverrides_When_Called()
    {
        // arrange & act - should not throw
        var runtime = KafkaBusFixture.CreateRuntime(t =>
            t.ConfigureConsumer(c => c.MaxPollIntervalMs = 300_000));
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert - transport was created successfully with consumer config
        Assert.NotNull(transport);
    }

    [Fact]
    public void AutoProvision_Should_SetFlag_When_Called()
    {
        // arrange & act
        var (_, _, topology) = KafkaBusFixture.CreateTopologyWithTransport(
            t => t.AutoProvision(false));

        // assert
        Assert.False(topology.AutoProvision);
    }

    [Fact]
    public void AddKafka_Should_RegisterTransport_When_Called()
    {
        // arrange
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();

        // act
        var runtime = builder
            .AddKafka(t => t.BootstrapServers("localhost:9092"))
            .BuildRuntime();

        // assert
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().SingleOrDefault();
        Assert.NotNull(transport);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_ToTopicCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.DispatchEndpoint("ep").ToTopic("my-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<KafkaTopic>(endpoint.Destination);
        Assert.Equal("my-topic", ((KafkaTopic)endpoint.Destination).Name);
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("my-topic", endpoint.Topic!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterSendRoute_When_SendCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("payments");
            t.DispatchEndpoint("ep").ToTopic("payments").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<KafkaTopic>(endpoint.Destination);
        Assert.Equal("payments", endpoint.Topic!.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_RegisterPublishRoute_When_PublishCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events");
            t.DispatchEndpoint("ep").ToTopic("events").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>().Single(e => e.Name == "ep");
        Assert.IsType<KafkaTopic>(endpoint.Destination);
        Assert.Equal("events", endpoint.Topic!.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetTopicName_When_TopicCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("custom-topic");
            t.Endpoint("ep").Topic("custom-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.Equal("custom-topic", endpoint.Topic.Name);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindCalled()
    {
        // arrange & act
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("err-topic");
            t.Endpoint("ep").Topic("err-topic").Kind(ReceiveEndpointKind.Error);
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().Single(e => e.Name == "ep");
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }
}

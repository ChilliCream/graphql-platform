using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests;

public class KafkaReceiveEndpointTests
{
    [Fact]
    public void ConsumerGroupId_Should_DefaultToEndpointName_When_NotExplicitlySet()
    {
        // arrange - the descriptor defaults ConsumerGroupId to the endpoint name
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.Endpoint("ep").Topic("my-topic").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "my-topic");

        // assert
        Assert.Equal("ep", endpoint.ConsumerGroupId);
    }

    [Fact]
    public void ConsumerGroupId_Should_UseExplicitValue_When_ConsumerGroupSet()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.Endpoint("ep").Topic("my-topic").ConsumerGroup("custom-group").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "my-topic");

        // assert
        Assert.Equal("custom-group", endpoint.ConsumerGroupId);
    }

    [Fact]
    public void Topic_Should_ResolveFromTopology_When_TopicNameConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("resolved-topic").Partitions(6);
            t.Endpoint("ep").Topic("resolved-topic").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "resolved-topic");

        // assert
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("resolved-topic", endpoint.Topic.Name);
        Assert.Equal(6, endpoint.Topic.Partitions);
        Assert.IsType<KafkaTopic>(endpoint.Source);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetKind_When_KindConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("err-topic");
            t.Endpoint("err-ep").Topic("err-topic").Kind(ReceiveEndpointKind.Error);
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "err-topic");

        // assert
        Assert.Equal(ReceiveEndpointKind.Error, endpoint.Kind);
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetSourceAddress_When_TopicConfigured()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("addr-topic");
            t.Endpoint("ep").Topic("addr-topic").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "addr-topic");

        // assert
        Assert.NotNull(endpoint.Source.Address);
        Assert.Contains("t/addr-topic", endpoint.Source.Address.ToString());
    }

    [Fact]
    public void ReceiveEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.Endpoint("ep").Topic("my-topic").Handler<OrderCreatedHandler>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.ReceiveEndpoints.OfType<KafkaReceiveEndpoint>().First(e => e.Topic.Name == "my-topic");

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("kafka", endpoint.Address.Scheme);
    }

    private static MessagingRuntime CreateRuntime(Action<IKafkaMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        builder.AddEventHandler<OrderCreatedHandler>();
        var runtime = builder
            .AddKafka(t =>
            {
                t.BootstrapServers("localhost:9092");
                configure(t);
            })
            .BuildRuntime();
        return runtime;
    }
}

using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests;

public class KafkaDispatchEndpointTests
{
    [Fact]
    public void Topic_Should_ResolveFromTopology_When_TopicNameConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic").Partitions(6);
            t.DispatchEndpoint("ep").ToTopic("my-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-topic" });

        // assert
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("my-topic", endpoint.Topic!.Name);
        Assert.Equal(6, endpoint.Topic.Partitions);
        Assert.IsType<KafkaTopic>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_HaveCorrectName_When_TopicConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.DispatchEndpoint("ep").ToTopic("my-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-topic" });

        // assert
        Assert.Equal("ep", endpoint.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_SetDestinationAddress_When_TopicConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.DispatchEndpoint("ep").ToTopic("my-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-topic" });

        // assert
        Assert.NotNull(endpoint.Destination.Address);
        Assert.Contains("t/my-topic", endpoint.Destination.Address.ToString());
    }

    [Fact]
    public void DispatchEndpoint_Should_SetAddress_When_Completed()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("my-topic");
            t.DispatchEndpoint("ep").ToTopic("my-topic");
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "my-topic" });

        // assert
        Assert.NotNull(endpoint.Address);
        Assert.Equal("kafka", endpoint.Address.Scheme);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_SendConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("payments");
            t.DispatchEndpoint("ep").ToTopic("payments").Send<ProcessPayment>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "payments" });

        // assert
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("payments", endpoint.Topic!.Name);
        Assert.IsType<KafkaTopic>(endpoint.Destination);
    }

    [Fact]
    public void DispatchEndpoint_Should_TargetTopic_When_PublishConfigured()
    {
        // arrange
        var runtime = KafkaBusFixture.CreateRuntime(t =>
        {
            t.DeclareTopic("events");
            t.DispatchEndpoint("ep").ToTopic("events").Publish<OrderCreated>();
        });
        var transport = runtime.Transports.OfType<KafkaMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints.OfType<KafkaDispatchEndpoint>()
            .First(e => e.Topic is { Name: "events" });

        // assert
        Assert.NotNull(endpoint.Topic);
        Assert.Equal("events", endpoint.Topic!.Name);
        Assert.IsType<KafkaTopic>(endpoint.Destination);
    }
}

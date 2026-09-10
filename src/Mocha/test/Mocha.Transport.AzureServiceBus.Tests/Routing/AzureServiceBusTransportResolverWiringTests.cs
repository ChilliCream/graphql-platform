using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Routing;

public class AzureServiceBusTransportResolverWiringTests
{
    [Fact]
    public void CreateEndpointConfiguration_Should_UseConventionQueueName_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Send(_ => { })));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var expectedName = runtime.Naming.GetSendEndpointName(typeof(OrderCreated));

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .FirstOrDefault(e => e.Queue?.Name == expectedName);

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("q/" + expectedName, endpoint.Name);
        Assert.Null(endpoint.Topic);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseConventionTopicName_When_DestinationNotConfigured()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddMessage<OrderCreated>(d => d.Publish(_ => { })));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var expectedName = runtime.Naming.GetPublishEndpointName(typeof(OrderCreated));

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .FirstOrDefault(e => e.Topic?.Name == expectedName);

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("t/" + expectedName, endpoint.Name);
        Assert.Null(endpoint.Queue);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseExplicitQueueName_When_ExplicitQueueDestinationConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Send(r => r.ToAzureServiceBusQueue("orders-queue"))));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .FirstOrDefault(e => e.Queue?.Name == "orders-queue");

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("q/orders-queue", endpoint.Name);
    }

    [Fact]
    public void CreateEndpointConfiguration_Should_UseExplicitTopicName_When_ExplicitTopicDestinationConfigured()
    {
        // arrange
        var runtime = CreateRuntime(
            b => b.AddMessage<OrderCreated>(d => d.Publish(r => r.ToAzureServiceBusTopic("orders-topic"))));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .FirstOrDefault(e => e.Topic?.Name == "orders-topic");

        // assert
        Assert.NotNull(endpoint);
        Assert.Equal("t/orders-topic", endpoint.Name);
    }

    [Theory]
    [InlineData("primary")]
    [InlineData("secondary")]
    public void CreateEndpointConfiguration_Should_ClaimNeutralSchemes_When_TransportIsAzureServiceBus(
        string transportName)
    {
        // arrange
        // Two Azure Service Bus transports, one default and one not. queue: and topic: are neutral
        // schemes both transports must be able to claim; EndpointRouter decides which one is selected.
        var runtime = CreateMultiTransportRuntime();
        var transport = runtime.Transports
            .OfType<AzureServiceBusMessagingTransport>()
            .Single(t => t.Name == transportName);

        // act
        var queueConfig = (AzureServiceBusDispatchEndpointConfiguration)transport.CreateEndpointConfiguration(
            runtime, new Uri("queue:order-commands"))!;
        var topicConfig = (AzureServiceBusDispatchEndpointConfiguration)transport.CreateEndpointConfiguration(
            runtime, new Uri("topic:orders"))!;

        // assert
        new
        {
            Queue = new { queueConfig.QueueName, queueConfig.TopicName, queueConfig.Name },
            Topic = new { topicConfig.QueueName, topicConfig.TopicName, topicConfig.Name }
        }.MatchInlineSnapshot(
            """
            {
              "Queue": {
                "QueueName": "order-commands",
                "TopicName": null,
                "Name": "q/order-commands"
              },
              "Topic": {
                "QueueName": null,
                "TopicName": "orders",
                "Name": "t/orders"
              }
            }
            """);
    }

    private static MessagingRuntime CreateMultiTransportRuntime()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        return builder
            .AddAzureServiceBus(t =>
            {
                t.Name("primary");
                t.Schema("primary");
                t.IsDefaultTransport();
                t.ConnectionString(DummyConnectionString);
            })
            .AddAzureServiceBus(t =>
            {
                t.Name("secondary");
                t.Schema("secondary");
                t.ConnectionString(DummyConnectionString);
            })
            .BuildRuntime();
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

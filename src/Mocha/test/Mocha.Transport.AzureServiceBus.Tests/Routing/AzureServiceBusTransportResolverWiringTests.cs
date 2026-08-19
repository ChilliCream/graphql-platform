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

    [Fact]
    public void NeutralScheme_Should_BeClaimed_When_TransportIsDefault()
    {
        // arrange
        // Two Azure Service Bus transports; the one under test carries IsDefaultTransport().
        // queue: and topic: are neutral schemes supported by Azure Service Bus transports.
        var runtime = CreateMultiTransportRuntime();
        var primary = runtime.Transports
            .OfType<AzureServiceBusMessagingTransport>()
            .Single(t => t.Name == "primary");

        // act
        var queueConfig = primary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var topicConfig = primary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.NotNull(queueConfig);
        Assert.NotNull(topicConfig);
    }

    [Fact]
    public void NeutralScheme_Should_BeClaimable_When_TransportIsNotDefault()
    {
        // arrange
        // Two Azure Service Bus transports; the one under test is not the default. It still
        // advertises capability, while EndpointRouter decides whether this candidate is selected.
        var runtime = CreateMultiTransportRuntime();
        var secondary = runtime.Transports
            .OfType<AzureServiceBusMessagingTransport>()
            .Single(t => t.Name == "secondary");

        // act
        var queueConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("queue:order-commands"));
        var topicConfig = secondary.CreateEndpointConfiguration(runtime, new Uri("topic:orders"));

        // assert
        Assert.NotNull(queueConfig);
        Assert.NotNull(topicConfig);
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

using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

public class AzureServiceBusDispatchTargetTests
{
    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";

    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedQueue_When_QueueNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("azuresb:q/reporting.events"));

        // assert
        var azureEndpoint = Assert.IsType<AzureServiceBusDispatchEndpoint>(endpoint);
        Assert.NotNull(azureEndpoint.Queue);
        Assert.False(azureEndpoint.Queue.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, azureEndpoint.Queue.Origin);
        Assert.Same(azureEndpoint.Queue, azureEndpoint.Destination);
        AzureServiceBusDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "azuresb",
              "TransportType": "AzureServiceBusMessagingTransport",
              "Entities": [
                {
                  "Kind": "queue",
                  "Name": "reporting.events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_AddUnprovisionedTopic_When_TopicNotDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(t => t.BindExplicitly());
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("azuresb:t/reporting-events"));

        // assert
        var azureEndpoint = Assert.IsType<AzureServiceBusDispatchEndpoint>(endpoint);
        Assert.NotNull(azureEndpoint.Topic);
        Assert.False(azureEndpoint.Topic.AutoProvision);
        Assert.Equal(TopologyOrigin.Endpoint, azureEndpoint.Topic.Origin);
        Assert.Same(azureEndpoint.Topic, azureEndpoint.Destination);
        AzureServiceBusDescribeSnapshot.Create(transport.Describe()).MatchInlineSnapshot(
            """
            {
              "Schema": "azuresb",
              "TransportType": "AzureServiceBusMessagingTransport",
              "Entities": [
                {
                  "Kind": "topic",
                  "Name": "reporting-events",
                  "AutoProvision": false,
                  "Origin": "endpoint"
                }
              ],
              "Links": []
            }
            """);
    }

    [Fact]
    public void GetDispatchEndpoint_Should_BindDeclaredQueue_When_QueueDeclaredUnderExplicitBinding()
    {
        // arrange
        var runtime = CreateRuntime(t =>
        {
            t.BindExplicitly();
            t.DeclareQueue("reporting.events");
        });

        // act
        var endpoint = runtime.GetDispatchEndpoint(new Uri("azuresb:q/reporting.events"));

        // assert
        var azureEndpoint = Assert.IsType<AzureServiceBusDispatchEndpoint>(endpoint);
        Assert.NotNull(azureEndpoint.Queue);
        Assert.Equal(TopologyOrigin.Declared, azureEndpoint.Queue.Origin);
        Assert.Same(azureEndpoint.Queue, azureEndpoint.Destination);
    }

    private static MessagingRuntime CreateRuntime(
        Action<IAzureServiceBusMessagingTransportDescriptor> configureTransport)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        return builder
            .AddAzureServiceBus(t =>
            {
                t.ConnectionString(DummyConnectionString);
                configureTransport(t);
            })
            .BuildRuntime();
    }
}

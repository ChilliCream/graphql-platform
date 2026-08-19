using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Routing;

public class AzureServiceBusProvisioningConvergenceTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void DiscoverTopology_Should_RetainDeclaredAutoProvision_When_QueueAndDirectDispatchEndpointBothTargetIt(
        bool declareQueueFirst)
    {
        // arrange
        var runtime = CreateRuntime(
            _ => { },
            t =>
            {
                if (declareQueueFirst)
                {
                    t.DeclareQueue("orders").AutoProvision(false);
                    t.DispatchEndpoint("to-orders").ToQueue("orders").Send<OrderCreated>();
                }
                else
                {
                    t.DispatchEndpoint("to-orders").ToQueue("orders").Send<OrderCreated>();
                    t.DeclareQueue("orders").AutoProvision(false);
                }
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .Single(e => e.Queue?.Name == "orders");

        // assert
        new
        {
            QueueCount = topology.Queues.Count(q => q.Name == "orders"),
            Queue = new { endpoint.Queue!.Name, endpoint.Queue.AutoProvision },
            EndpointName = endpoint.Name,
            EndpointQueueName = endpoint.Queue.Name
        }.MatchInlineSnapshot(
            """
            {
              "QueueCount": 1,
              "Queue": {
                "Name": "orders",
                "AutoProvision": false
              },
              "EndpointName": "to-orders",
              "EndpointQueueName": "orders"
            }
            """);
    }

    [Fact]
    public void CreateConfiguration_Should_UseLastSuppliedName_When_SubscriptionDeclaredRepeatedlyForSameTopicAndQueue()
    {
        // arrange
        var runtime = CreateRuntime(
            _ => { },
            t =>
            {
                t.DeclareTopic("orders");
                t.DeclareQueue("orders-queue");
                t.DeclareSubscription("orders", "orders-queue", "first-name");
                t.DeclareSubscription("orders", "orders-queue", "second-name");
            });
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var subscriptions = topology.Subscriptions
            .Where(s => s.Source.Name == "orders" && s.Destination.Name == "orders-queue")
            .Select(s => new { SourceName = s.Source.Name, DestinationName = s.Destination.Name, s.Name })
            .ToList();

        // assert
        new { Count = subscriptions.Count, Subscriptions = subscriptions }.MatchInlineSnapshot(
            """
            {
              "Count": 1,
              "Subscriptions": [
                {
                  "SourceName": "orders",
                  "DestinationName": "orders-queue",
                  "Name": "second-name"
                }
              ]
            }
            """);
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
}

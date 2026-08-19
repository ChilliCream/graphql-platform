using CookieCrumble;
using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Routing;

public class AzureServiceBusProvisioningConvergenceTests
{
    [Fact]
    public void BuildRuntime_Should_RetainDeclaredAutoProvision_When_QueueAndDispatchEndpointBothTargetIt()
    {
        // arrange
        // Configuration statement order does not affect topology build order: OnAfterInitialized
        // copies all declared queues into the topology before any endpoint topology discovery runs,
        // so DeclareQueue and DispatchEndpoint can be written in either order here. The direct-URI
        // test above covers the order runtime discovery actually resolves them in.

        // act
        var runtime = CreateRuntime(
            _ => { },
            t =>
            {
                t.DeclareQueue("orders").AutoProvision(false);
                t.DispatchEndpoint("to-orders").ToQueue("orders").Send<OrderCreated>();
            });

        // assert
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var endpoint = transport.DispatchEndpoints
            .OfType<AzureServiceBusDispatchEndpoint>()
            .Single(e => e.Queue?.Name == "orders");

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
    public void GetDispatchEndpoint_Should_RetainDeclaredAutoProvision_When_QueueAndDirectUriAddressBothTargetIt()
    {
        // arrange
        var runtime = CreateRuntime(
            _ => { },
            t => t.DeclareQueue("orders").AutoProvision(false));
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;

        // act
        var endpoint = (AzureServiceBusDispatchEndpoint)runtime.GetDispatchEndpoint(new Uri("azuresb:q/orders"));

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
              "EndpointName": "q/orders",
              "EndpointQueueName": "orders"
            }
            """);
    }

    [Fact]
    public void BuildRuntime_Should_UseLastSuppliedSubscriptionName_When_SubscriptionDeclaredRepeatedlyForSameTopicAndQueue()
    {
        // act
        var runtime = CreateRuntime(
            _ => { },
            t =>
            {
                t.DeclareTopic("orders");
                t.DeclareQueue("orders-queue");
                t.DeclareSubscription("orders", "orders-queue", "first-name");
                t.DeclareSubscription("orders", "orders-queue", "second-name");
            });

        // assert
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        var topology = (AzureServiceBusMessagingTopology)transport.Topology;
        var subscriptions = topology.Subscriptions
            .Where(s => s.Source.Name == "orders" && s.Destination.Name == "orders-queue")
            .Select(s => new { SourceName = s.Source.Name, DestinationName = s.Destination.Name, s.Name })
            .ToList();

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

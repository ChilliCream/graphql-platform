using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureServiceBus.Tests.Helpers;

namespace Mocha.Transport.AzureServiceBus.Tests.Topology;

public class AzureServiceBusMessagingTopologyTests
{
    [Fact]
    public void Topology_Should_UseConnectionStringNamespace_When_ConnectionStringConfigured()
    {
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddAzureServiceBus(t => t.ConnectionString(
                "Endpoint=sb://orders.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test"))
            .BuildRuntime();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();

        Assert.Equal("orders.servicebus.windows.net", transport.Topology.Address.Host);
    }

    [Fact]
    public void AddTopic_Should_ReturnExistingUnchanged_When_NameIsDuplicate()
    {
        var (_, _, topology) = CreateTopology();
        var first = topology.AddTopic(new AzureServiceBusTopicConfiguration
        {
            Name = "orders",
            AutoProvision = false,
            Origin = TopologyOrigin.Declared
        });

        var result = topology.AddTopic(new AzureServiceBusTopicConfiguration
        {
            Name = "orders",
            AutoProvision = true,
            Origin = TopologyOrigin.Convention
        });

        Assert.Same(first, result);
        Assert.Single(topology.Topics, t => t.Name == "orders");
        Assert.False(result.AutoProvision);
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
    }

    [Fact]
    public void AddQueue_Should_ReturnExistingUnchanged_When_NameIsDuplicate()
    {
        var (_, _, topology) = CreateTopology();
        var first = topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "orders",
            RequiresSession = true,
            Origin = TopologyOrigin.Declared
        });

        var result = topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "orders",
            RequiresSession = false,
            Origin = TopologyOrigin.Convention
        });

        Assert.Same(first, result);
        Assert.Single(topology.Queues, q => q.Name == "orders");
        Assert.True(result.RequiresSession);
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
    }

    [Fact]
    public void AddQueue_Should_SuppressAutoDeleteOnIdle_When_AutoDeleteIsDisabled()
    {
        var (_, _, topology) = CreateTopology();

        var queue = topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "orders",
            AutoDelete = false,
            AutoDeleteOnIdle = TimeSpan.FromMinutes(5)
        });

        Assert.False(queue.AutoDelete);
        Assert.Null(queue.AutoDeleteOnIdle);
    }

    [Fact]
    public void AddQueue_Should_NotInventIdlePolicy_When_AutoDeleteIsEnabledAlone()
    {
        var (_, _, topology) = CreateTopology();

        var queue = topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "orders",
            AutoDelete = true
        });

        Assert.True(queue.AutoDelete);
        Assert.Null(queue.AutoDeleteOnIdle);
    }

    [Fact]
    public void AddQueue_Should_Throw_When_SessionQueueAutoForwards()
    {
        var (_, _, topology) = CreateTopology();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            topology.AddQueue(new AzureServiceBusQueueConfiguration
            {
                Name = "orders",
                RequiresSession = true,
                ForwardTo = "archive"
            }));

        Assert.Equal(
            "Azure Service Bus queue 'orders' cannot require sessions and auto-forward messages.",
            exception.Message);
    }

    [Fact]
    public void AddSubscription_Should_Throw_When_SubscriptionRequiresSession()
    {
        var (_, _, topology) = CreateTopology();
        topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = "orders" });
        topology.AddQueue(new AzureServiceBusQueueConfiguration { Name = "accounting" });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
            {
                Source = "orders",
                Destination = "accounting",
                RequiresSession = true
            }));

        Assert.Equal(
            "Azure Service Bus subscription from 'orders' to 'accounting' cannot require sessions "
            + "because modeled subscriptions auto-forward messages.",
            exception.Message);
    }

    [Fact]
    public void AddSubscription_Should_Throw_When_DestinationRequiresSession()
    {
        var (_, _, topology) = CreateTopology();
        topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = "orders" });
        topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "accounting",
            RequiresSession = true
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
            {
                Source = "orders",
                Destination = "accounting"
            }));

        Assert.Equal(
            "Azure Service Bus subscription from 'orders' cannot auto-forward to session-enabled "
            + "queue 'accounting'.",
            exception.Message);
    }

    [Fact]
    public void AddSubscription_Should_ReturnExistingUnchanged_When_LinkIsDuplicate()
    {
        var (_, _, topology) = CreateTopology();
        topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = "orders" });
        topology.AddQueue(new AzureServiceBusQueueConfiguration { Name = "accounting" });
        var first = topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
        {
            Source = "orders",
            Destination = "accounting",
            AutoProvision = false,
            Origin = TopologyOrigin.Declared
        });

        var result = topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
        {
            Source = "orders",
            Destination = "accounting",
            AutoProvision = true,
            Origin = TopologyOrigin.Convention
        });

        Assert.Same(first, result);
        Assert.Single(
            topology.Subscriptions,
            s => s.Source.Name == "orders" && s.Destination.Name == "accounting");
        Assert.False(result.AutoProvision);
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
    }

    [Fact]
    public void SubscriptionName_Should_RemainUniqueAndBounded_When_DestinationNamesAreLong()
    {
        var (_, _, topology) = CreateTopology();
        var prefix = new string('a', 60);
        var firstQueueName = prefix + "-first";
        var secondQueueName = prefix + "-second";
        topology.AddTopic(new AzureServiceBusTopicConfiguration { Name = "orders" });
        topology.AddQueue(new AzureServiceBusQueueConfiguration { Name = firstQueueName });
        topology.AddQueue(new AzureServiceBusQueueConfiguration { Name = secondQueueName });

        var first = topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
        {
            Source = "orders",
            Destination = firstQueueName
        });
        var second = topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
        {
            Source = "orders",
            Destination = secondQueueName
        });

        Assert.Equal(50, first.Name.Length);
        Assert.Equal(50, second.Name.Length);
        Assert.NotEqual(first.Name, second.Name);
    }

    [Fact]
    public void Describe_Should_UseStableIdsAndOrigins_When_TopologyIsDeclared()
    {
        var (_, transport, topology) = CreateTopology();
        var topic = topology.AddTopic(new AzureServiceBusTopicConfiguration
        {
            Name = "orders",
            Origin = TopologyOrigin.Declared
        });
        var queue = topology.AddQueue(new AzureServiceBusQueueConfiguration
        {
            Name = "accounting",
            Origin = TopologyOrigin.Endpoint
        });
        var subscription = topology.AddSubscription(new AzureServiceBusSubscriptionConfiguration
        {
            Source = topic.Name,
            Destination = queue.Name,
            Origin = TopologyOrigin.Convention
        });

        var description = transport.Describe();

        var describedTopic = description.Topology!.Entities.Single(e => e.Address == topic.Address.ToString());
        var describedQueue = description.Topology.Entities.Single(e => e.Address == queue.Address.ToString());
        var describedSubscription = description.Topology.Links.Single(e =>
            e.Address == subscription.Address.ToString());
        Assert.Equal($"urn:mocha:topology:{topic.Address}", describedTopic.Id);
        Assert.Equal($"urn:mocha:topology:{queue.Address}", describedQueue.Id);
        Assert.Equal($"urn:mocha:link:{subscription.Address}", describedSubscription.Id);
        Assert.Equal(TopologyOrigin.Declared, describedTopic.Properties!["origin"]);
        Assert.Equal(TopologyOrigin.Endpoint, describedQueue.Properties!["origin"]);
        Assert.Equal(TopologyOrigin.Convention, describedSubscription.Properties!["origin"]);
        Assert.Equal("both", describedTopic.Flow);
        Assert.Equal("both", describedQueue.Flow);
    }

    private static (
        MessagingRuntime Runtime,
        AzureServiceBusMessagingTransport Transport,
        AzureServiceBusMessagingTopology Topology) CreateTopology()
    {
        var services = new ServiceCollection();
        var runtime = services
            .AddMessageBus()
            .AddAzureServiceBus(t => t.ConnectionString(DummyConnectionString))
            .BuildRuntime();
        var transport = runtime.Transports.OfType<AzureServiceBusMessagingTransport>().Single();
        return (runtime, transport, (AzureServiceBusMessagingTopology)transport.Topology);
    }

    private const string DummyConnectionString =
        "Endpoint=sb://localhost/;SharedAccessKeyName=test;SharedAccessKey=test";
}

using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.AzureEventHub.Tests.Helpers;

namespace Mocha.Transport.AzureEventHub.Tests.Topology;

public class EventHubTopologyTests
{
    [Fact]
    public void AddTopic_Should_CreateTopic_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new EventHubTopicConfiguration { Name = "test-hub" };

        // act
        var topic = topology.AddTopic(config);

        // assert
        Assert.Equal("test-hub", topic.Name);
        Assert.Contains(topic, topology.Topics);
    }

    [Fact]
    public void AddTopic_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddTopic(new EventHubTopicConfiguration { Name = "duplicate-hub" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddTopic(new EventHubTopicConfiguration { Name = "duplicate-hub" })
            );
        Assert.Contains("duplicate-hub", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void AddTopic_Should_SetPartitionCount_When_Specified()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new EventHubTopicConfiguration { Name = "partitioned-hub", PartitionCount = 8 });

        // assert
        Assert.Equal(8, topic.PartitionCount);
    }

    [Fact]
    public void AddTopic_Should_GenerateAddress_When_Completed()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new EventHubTopicConfiguration { Name = "address-hub" });

        // assert
        Assert.NotNull(topic.Address);
        Assert.Contains("/h/address-hub", topic.Address.ToString());
    }

    [Fact]
    public void AddSubscription_Should_CreateSubscription_When_TopicAndGroupProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddTopic(new EventHubTopicConfiguration { Name = "sub-hub" });

        var config = new EventHubSubscriptionConfiguration
        {
            TopicName = "sub-hub",
            ConsumerGroup = "my-group"
        };

        // act
        var subscription = topology.AddSubscription(config);

        // assert
        Assert.Equal("sub-hub", subscription.TopicName);
        Assert.Equal("my-group", subscription.ConsumerGroup);
        Assert.Contains(subscription, topology.Subscriptions);
    }

    [Fact]
    public void AddSubscription_Should_DefaultToDefaultGroup_When_ConsumerGroupNotSpecified()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddTopic(new EventHubTopicConfiguration { Name = "default-group-hub" });

        // act
        var subscription = topology.AddSubscription(new EventHubSubscriptionConfiguration
        {
            TopicName = "default-group-hub"
        });

        // assert
        Assert.Equal("$Default", subscription.ConsumerGroup);
    }

    [Fact]
    public void AddSubscription_Should_GenerateAddress_When_Completed()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        topology.AddTopic(new EventHubTopicConfiguration { Name = "addr-hub" });

        // act
        var subscription = topology.AddSubscription(new EventHubSubscriptionConfiguration
        {
            TopicName = "addr-hub",
            ConsumerGroup = "test-group"
        });

        // assert
        Assert.NotNull(subscription.Address);
        Assert.Contains("/h/addr-hub/cg/test-group", subscription.Address.ToString());
    }

    [Fact]
    public async Task AddTopicAndSubscription_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var initialTopicCount = topology.Topics.Count;
        const int operationCount = 100;

        // act
        var allTasks = Enumerable
            .Range(0, operationCount)
            .Select(i =>
                Task.Run(() => topology.AddTopic(new EventHubTopicConfiguration { Name = $"hub-{i}" }))
            )
            .ToList();

        await Task.WhenAll(allTasks);

        // assert
        Assert.Equal(initialTopicCount + operationCount, topology.Topics.Count);

        var topicNames = topology.Topics.Select(t => t.Name).ToList();
        Assert.Equal(topicNames.Count, topicNames.Distinct().Count());

        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Topics, t => t.Name == $"hub-{i}");
        }
    }

    private static (
        MessagingRuntime Runtime,
        EventHubMessagingTransport Transport,
        EventHubMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddEventHub(t => t.ConnectionProvider(_ => new StubConnectionProvider()))
            .BuildRuntime();
        var transport = runtime.Transports.OfType<EventHubMessagingTransport>().Single();
        var topology = (EventHubMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}

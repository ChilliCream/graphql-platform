using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Topology;

public class PostgresMessagingTopologyTests
{
    [Fact]
    public void AddTopic_Should_CreateTopic_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "test-topic" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "test-topic");
        Assert.Equal("test-topic", topic.Name);
    }

    [Fact]
    public void AddTopic_Should_Throw_When_DuplicateNameAdded()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "dup-topic" });

        // assert
        Assert.Throws<InvalidOperationException>(() =>
            topology.AddTopic(new PostgresTopicConfiguration { Name = "dup-topic" }));
    }

    [Fact]
    public void AddTopic_Should_SetAutoProvision_When_Configured()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "topic-ap", AutoProvision = false });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "topic-ap");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void AddQueue_Should_CreateQueue_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "test-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "test-queue");
        Assert.Equal("test-queue", queue.Name);
    }

    [Fact]
    public void AddQueue_Should_Throw_When_DuplicateNameAdded()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dup-queue" });

        // assert
        Assert.Throws<InvalidOperationException>(() =>
            topology.AddQueue(new PostgresQueueConfiguration { Name = "dup-queue" }));
    }

    [Fact]
    public void AddQueue_Should_SetAutoDelete_When_Configured()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "ad-queue", AutoDelete = true });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "ad-queue");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void AddQueue_Should_SetAutoProvision_When_Configured()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "ap-queue", AutoProvision = false });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "ap-queue");
        Assert.False(queue.AutoProvision);
    }

    [Fact]
    public void AddSubscription_Should_ConnectTopicToQueue_When_BothExist()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "src-topic" });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dst-queue" });

        // act
        topology.AddSubscription(new PostgresSubscriptionConfiguration
        {
            Source = "src-topic",
            Destination = "dst-queue"
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.Equal("src-topic", subscription.Source.Name);
        Assert.Equal("dst-queue", subscription.Destination.Name);
    }

    [Fact]
    public void AddSubscription_Should_Throw_When_TopicNotFound()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dst-queue" });

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            topology.AddSubscription(new PostgresSubscriptionConfiguration
            {
                Source = "nonexistent-topic",
                Destination = "dst-queue"
            }));
        Assert.Contains("nonexistent-topic", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddSubscription_Should_Throw_When_QueueNotFound()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "src-topic" });

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            topology.AddSubscription(new PostgresSubscriptionConfiguration
            {
                Source = "src-topic",
                Destination = "nonexistent-queue"
            }));
        Assert.Contains("nonexistent-queue", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddSubscription_Should_Throw_When_DuplicateSourceAndDestination()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "src" });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dst" });

        // act
        topology.AddSubscription(new PostgresSubscriptionConfiguration { Source = "src", Destination = "dst" });

        // assert
        Assert.Throws<InvalidOperationException>(() =>
            topology.AddSubscription(new PostgresSubscriptionConfiguration { Source = "src", Destination = "dst" }));
    }

    [Fact]
    public void AddSubscription_Should_AddToTopicSubscriptions_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "src-topic" });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dst-queue" });

        // act
        topology.AddSubscription(new PostgresSubscriptionConfiguration
        {
            Source = "src-topic",
            Destination = "dst-queue"
        });

        // assert
        var topic = topology.Topics.Single(t => t.Name == "src-topic");
        Assert.Single(topic.Subscriptions);
        Assert.Equal("dst-queue", topic.Subscriptions[0].Destination.Name);
    }

    [Fact]
    public void AddSubscription_Should_AddToQueueSubscriptions_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "src-topic" });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dst-queue" });

        // act
        topology.AddSubscription(new PostgresSubscriptionConfiguration
        {
            Source = "src-topic",
            Destination = "dst-queue"
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "dst-queue");
        Assert.Single(queue.Subscriptions);
        Assert.Equal("src-topic", queue.Subscriptions[0].Source.Name);
    }

    [Fact]
    public async Task AddTopicAndQueue_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        var initialTopicCount = topology.Topics.Count;
        var initialQueueCount = topology.Queues.Count;

        const int operationCount = 100;

        // act
        var allTasks = Enumerable
            .Range(0, operationCount)
            .SelectMany(i =>
                new Task[]
                {
                    Task.Run(() => topology.AddTopic(new PostgresTopicConfiguration { Name = $"topic-{i}" })),
                    Task.Run(() => topology.AddQueue(new PostgresQueueConfiguration { Name = $"queue-{i}" }))
                })
            .ToList();

        await Task.WhenAll(allTasks);

        // assert
        Assert.Equal(initialTopicCount + operationCount, topology.Topics.Count);
        Assert.Equal(initialQueueCount + operationCount, topology.Queues.Count);

        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Topics, t => t.Name == $"topic-{i}");
            Assert.Contains(topology.Queues, q => q.Name == $"queue-{i}");
        }
    }
}

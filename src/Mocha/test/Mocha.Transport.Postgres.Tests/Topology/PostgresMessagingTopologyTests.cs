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
    public void AddTopic_Should_StrengthenAutoProvision_When_DuplicateNameAndAutoProvisionTrue()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "ap-topic", AutoProvision = false });
        var countAfterFirst = topology.Topics.Count;

        // act: second add with AutoProvision = true strengthens; count does not increase
        topology.AddTopic(new PostgresTopicConfiguration { Name = "ap-topic", AutoProvision = true });

        // assert
        Assert.Equal(countAfterFirst, topology.Topics.Count);
        var topic = Assert.Single(topology.Topics, t => t.Name == "ap-topic");
        Assert.True(topic.AutoProvision);
    }

    [Fact]
    public void AddQueue_Should_StrengthenAutoProvision_When_DuplicateNameAndAutoProvisionTrue()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "ap-queue", AutoProvision = false });
        var countAfterFirst = topology.Queues.Count;

        // act: second add with AutoProvision = true strengthens; count does not increase
        topology.AddQueue(new PostgresQueueConfiguration { Name = "ap-queue", AutoProvision = true });

        // assert
        Assert.Equal(countAfterFirst, topology.Queues.Count);
        var queue = Assert.Single(topology.Queues, q => q.Name == "ap-queue");
        Assert.True(queue.AutoProvision);
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
    public void AddQueue_Should_ReturnExisting_When_DuplicateNameNoopShape()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "dup-queue" });
        var countAfterFirst = topology.Queues.Count;

        // act
        var merged = topology.AddQueue(new PostgresQueueConfiguration { Name = "dup-queue" });

        // assert: count does not increase; same instance is returned
        Assert.Equal(countAfterFirst, topology.Queues.Count);
        Assert.Equal("dup-queue", merged.Name);
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
    public void AddTopic_Should_ReturnExisting_When_DuplicateNameNoopShape()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "dup-topic" });
        var countAfterFirst = topology.Topics.Count;

        // act
        var merged = topology.AddTopic(new PostgresTopicConfiguration { Name = "dup-topic" });

        // assert: count does not increase; same name is returned
        Assert.Equal(countAfterFirst, topology.Topics.Count);
        Assert.Equal("dup-topic", merged.Name);
    }

    [Fact]
    public void AddTopic_Should_FillAutoProvision_When_ExistingIsNullAndIncomingHasValue()
    {
        // arrange: first add leaves AutoProvision null
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddTopic(new PostgresTopicConfiguration { Name = "fill-topic" });
        var countAfterFirst = topology.Topics.Count;

        // act: second add provides AutoProvision = false (fills the null)
        topology.AddTopic(new PostgresTopicConfiguration { Name = "fill-topic", AutoProvision = false });

        // assert: count does not grow; AutoProvision is now filled
        Assert.Equal(countAfterFirst, topology.Topics.Count);
        var topic = Assert.Single(topology.Topics, t => t.Name == "fill-topic");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void AddQueue_Should_FillAutoProvision_When_ExistingIsNullAndIncomingHasValue()
    {
        // arrange: first add leaves AutoProvision null
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });
        topology.AddQueue(new PostgresQueueConfiguration { Name = "fill-queue" });
        var countAfterFirst = topology.Queues.Count;

        // act: second add provides AutoProvision = false (fills the null)
        topology.AddQueue(new PostgresQueueConfiguration { Name = "fill-queue", AutoProvision = false });

        // assert: count does not grow; AutoProvision is now filled
        Assert.Equal(countAfterFirst, topology.Queues.Count);
        var queue = Assert.Single(topology.Queues, q => q.Name == "fill-queue");
        Assert.False(queue.AutoProvision);
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

    [Fact]
    public async Task AddTopicAndQueue_Should_NotCorrupt_When_ConcurrentMergeAdds()
    {
        // arrange: pre-create entities that will be merged into concurrently
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        const int entityCount = 50;

        for (var i = 0; i < entityCount; i++)
        {
            topology.AddTopic(new PostgresTopicConfiguration { Name = $"merge-topic-{i}" });
            topology.AddQueue(new PostgresQueueConfiguration { Name = $"merge-queue-{i}" });
        }

        var topicCountBeforeMerge = topology.Topics.Count;
        var queueCountBeforeMerge = topology.Queues.Count;

        // act: flood the same names from multiple threads simultaneously
        const int threadsPerEntity = 4;

        var allTasks = Enumerable
            .Range(0, entityCount)
            .SelectMany(i =>
                Enumerable.Range(0, threadsPerEntity).SelectMany(_ =>
                    new Task[]
                    {
                        Task.Run(() => topology.AddTopic(new PostgresTopicConfiguration
                        {
                            Name = $"merge-topic-{i}",
                            AutoProvision = true
                        })),
                        Task.Run(() => topology.AddQueue(new PostgresQueueConfiguration
                        {
                            Name = $"merge-queue-{i}",
                            AutoProvision = true
                        }))
                    }))
            .ToList();

        await Task.WhenAll(allTasks);

        // assert: no new entities created; no duplicates; all original entities still intact
        Assert.Equal(topicCountBeforeMerge, topology.Topics.Count);
        Assert.Equal(queueCountBeforeMerge, topology.Queues.Count);

        var topicNames = topology.Topics.Select(t => t.Name).ToList();
        Assert.Equal(topicNames.Count, topicNames.Distinct().Count());

        var queueNames = topology.Queues.Select(q => q.Name).ToList();
        Assert.Equal(queueNames.Count, queueNames.Distinct().Count());
    }
}

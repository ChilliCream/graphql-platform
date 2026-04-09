using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Topology;

public class KafkaMessagingTopologyTests
{
    [Fact]
    public void AddTopic_Should_CreateTopic_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });
        var config = new KafkaTopicConfiguration { Name = "test-topic" };

        // act
        var topic = topology.AddTopic(config);

        // assert
        Assert.Equal("test-topic", topic.Name);
        Assert.Contains(topic, topology.Topics);
    }

    [Fact]
    public void AddTopic_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        topology.AddTopic(new KafkaTopicConfiguration { Name = "duplicate-topic" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddTopic(new KafkaTopicConfiguration { Name = "duplicate-topic" })
            );
        Assert.Contains("duplicate-topic", exception.Message);
        Assert.Contains("already exists", exception.Message);
    }

    [Fact]
    public void AddTopic_Should_ApplyBusDefaults_When_PartitionsNotSet()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d => d.Topic.Partitions = 6));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "defaulted-topic" });

        // assert
        Assert.Equal(6, topic.Partitions);
    }

    [Fact]
    public void AddTopic_Should_ApplyBusDefaults_When_ReplicationFactorNotSet()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopologyWithTransport(t =>
            t.ConfigureDefaults(d => d.Topic.ReplicationFactor = 3));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "replicated-topic" });

        // assert
        Assert.Equal(3, topic.ReplicationFactor);
    }

    [Fact]
    public void AddTopic_Should_DefaultPartitionsToOne_When_NoDefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "default-partitions" });

        // assert
        Assert.Equal(1, topic.Partitions);
    }

    [Fact]
    public void AddTopic_Should_DefaultReplicationToOne_When_NoDefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "default-replication" });

        // assert
        Assert.Equal(1, topic.ReplicationFactor);
    }

    [Fact]
    public void Topics_Should_ReturnAllAddedTopics()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });
        var initialCount = topology.Topics.Count;

        // act
        topology.AddTopic(new KafkaTopicConfiguration { Name = "topic-1" });
        topology.AddTopic(new KafkaTopicConfiguration { Name = "topic-2" });
        topology.AddTopic(new KafkaTopicConfiguration { Name = "topic-3" });

        // assert
        Assert.Equal(initialCount + 3, topology.Topics.Count);
        Assert.Contains(topology.Topics, t => t.Name == "topic-1");
        Assert.Contains(topology.Topics, t => t.Name == "topic-2");
        Assert.Contains(topology.Topics, t => t.Name == "topic-3");
    }

    [Fact]
    public void AutoProvision_Should_Propagate_When_SetOnTransport()
    {
        // arrange & act
        var (_, _, topology) = KafkaBusFixture.CreateTopologyWithTransport(
            t => t.AutoProvision(true));

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void AutoProvision_Should_DefaultToTrue_When_NotExplicitlySet()
    {
        // arrange & act
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void AddTopic_Should_SetTopicAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "addressed-topic" });

        // assert
        Assert.NotNull(topic.Address);
        Assert.Contains("t/addressed-topic", topic.Address.ToString());
    }

    [Fact]
    public async Task AddTopic_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var (_, _, topology) = KafkaBusFixture.CreateTopology(_ => { });

        var initialCount = topology.Topics.Count;
        const int operationCount = 100;

        // act
        var tasks = Enumerable
            .Range(0, operationCount)
            .Select(i => Task.Run(() =>
                topology.AddTopic(new KafkaTopicConfiguration { Name = $"topic-{i}" })))
            .ToList();

        await Task.WhenAll(tasks);

        // assert
        Assert.Equal(initialCount + operationCount, topology.Topics.Count);

        var topicNames = topology.Topics.Select(t => t.Name).ToList();
        Assert.Equal(topicNames.Count, topicNames.Distinct().Count());

        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Topics, t => t.Name == $"topic-{i}");
        }
    }
}

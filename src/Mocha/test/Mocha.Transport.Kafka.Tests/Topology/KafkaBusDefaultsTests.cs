using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Topology;

public class KafkaBusDefaultsTests
{
    [Fact]
    public void AddTopic_Should_ApplyDefaultPartitions_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.Partitions = 12));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert
        Assert.Equal(12, topic.Partitions);
    }

    [Fact]
    public void AddTopic_Should_NotOverridePartitions_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.Partitions = 12));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration
        {
            Name = "test-topic",
            Partitions = 3
        });

        // assert
        Assert.Equal(3, topic.Partitions);
    }

    [Fact]
    public void AddTopic_Should_ApplyDefaultReplicationFactor_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.ReplicationFactor = 3));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert
        Assert.Equal(3, topic.ReplicationFactor);
    }

    [Fact]
    public void AddTopic_Should_NotOverrideReplicationFactor_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.ReplicationFactor = 3));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration
        {
            Name = "test-topic",
            ReplicationFactor = 1
        });

        // assert
        Assert.Equal(1, topic.ReplicationFactor);
    }

    [Fact]
    public void AddTopic_Should_ApplyDefaultTopicConfigs_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.TopicConfigs = new Dictionary<string, string>
            {
                ["retention.ms"] = "86400000"
            }));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert
        Assert.NotNull(topic.TopicConfigs);
        Assert.Equal("86400000", topic.TopicConfigs!["retention.ms"]);
    }

    [Fact]
    public void AddTopic_Should_NotOverrideTopicConfigs_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.TopicConfigs = new Dictionary<string, string>
            {
                ["retention.ms"] = "86400000"
            }));

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration
        {
            Name = "test-topic",
            TopicConfigs = new Dictionary<string, string>
            {
                ["retention.ms"] = "3600000"
            }
        });

        // assert
        Assert.Equal("3600000", topic.TopicConfigs!["retention.ms"]);
    }

    [Fact]
    public void AddTopic_Should_ApplyNoDefaults_When_NoDefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert
        Assert.Equal(1, topic.Partitions);
        Assert.Equal(1, topic.ReplicationFactor);
        Assert.Null(topic.TopicConfigs);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyToExplicitlyDeclaredTopics()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Topic.Partitions = 12);
            t.DeclareTopic("my-topic");
        });

        // assert
        var topic = topology.Topics.Single(t => t.Name == "my-topic");
        Assert.Equal(12, topic.Partitions);
    }

    [Fact]
    public void ConfigureDefaults_Should_NotOverrideDeclaredTopicSettings()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Topic.Partitions = 12);
            t.DeclareTopic("my-topic").Partitions(3);
        });

        // assert
        var topic = topology.Topics.Single(t => t.Name == "my-topic");
        Assert.Equal(3, topic.Partitions);
    }

    [Fact]
    public void ConfigureDefaults_Should_AllowMultipleConfigureCalls()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Topic.Partitions = 12);
            t.ConfigureDefaults(d => d.Topic.ReplicationFactor = 3);
        });

        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert - both calls are applied
        Assert.Equal(12, topic.Partitions);
        Assert.Equal(3, topic.ReplicationFactor);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyLastValue_When_SamePropertySetMultipleTimes()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Topic.Partitions = 6);
            t.ConfigureDefaults(d => d.Topic.Partitions = 12);
        });

        var topic = topology.AddTopic(new KafkaTopicConfiguration { Name = "test-topic" });

        // assert - last write wins
        Assert.Equal(12, topic.Partitions);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyToAllTopics()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Topic.Partitions = 6));

        var topic1 = topology.AddTopic(new KafkaTopicConfiguration { Name = "topic-1" });
        var topic2 = topology.AddTopic(new KafkaTopicConfiguration { Name = "topic-2" });

        // assert
        Assert.Equal(6, topic1.Partitions);
        Assert.Equal(6, topic2.Partitions);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static (
        MessagingRuntime Runtime,
        KafkaMessagingTransport Transport,
        KafkaMessagingTopology Topology) CreateTopology(Action<IKafkaMessagingTransportDescriptor> configure)
        => KafkaBusFixture.CreateTopologyWithTransport(configure);
}

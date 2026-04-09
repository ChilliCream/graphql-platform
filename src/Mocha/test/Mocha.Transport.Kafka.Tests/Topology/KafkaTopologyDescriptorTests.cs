using Mocha.Transport.Kafka.Tests.Helpers;

namespace Mocha.Transport.Kafka.Tests.Topology;

public class KafkaTopologyDescriptorTests
{
    [Fact]
    public void DeclareTopic_Should_SetName_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("my-topic"));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal("my-topic", topic.Name);
    }

    [Fact]
    public void DeclareTopic_Should_SetPartitions_When_PartitionsCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("my-topic").Partitions(12));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal(12, topic.Partitions);
    }

    [Fact]
    public void DeclareTopic_Should_SetReplicationFactor_When_ReplicationFactorCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("my-topic").ReplicationFactor(3));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal(3, topic.ReplicationFactor);
    }

    [Fact]
    public void DeclareTopic_Should_SetAutoProvision_When_AutoProvisionCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("my-topic").AutoProvision(true));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.True(topic.AutoProvision);
    }

    [Fact]
    public void DeclareTopic_Should_SetAutoProvisionFalse_When_AutoProvisionCalledWithFalse()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("my-topic").AutoProvision(false));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void DeclareTopic_Should_SetConfig_When_WithConfigCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
            t.DeclareTopic("my-topic").WithConfig("retention.ms", "86400000"));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.NotNull(topic.TopicConfigs);
        Assert.Equal("86400000", topic.TopicConfigs!["retention.ms"]);
    }

    [Fact]
    public void DeclareTopic_Should_SupportFluentChaining_When_MultiplePropsCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
            t.DeclareTopic("my-topic")
                .Partitions(6)
                .ReplicationFactor(3)
                .AutoProvision(true)
                .WithConfig("cleanup.policy", "compact"));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal(6, topic.Partitions);
        Assert.Equal(3, topic.ReplicationFactor);
        Assert.True(topic.AutoProvision);
        Assert.Equal("compact", topic.TopicConfigs!["cleanup.policy"]);
    }

    [Fact]
    public void DeclareTopic_Should_DeclareMultipleTopics_When_CalledMultipleTimes()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("topic-a").Partitions(3);
            t.DeclareTopic("topic-b").Partitions(6);
        });

        // assert
        Assert.Contains(topology.Topics, t => t.Name == "topic-a" && t.Partitions == 3);
        Assert.Contains(topology.Topics, t => t.Name == "topic-b" && t.Partitions == 6);
    }

    [Fact]
    public void DeclareTopic_Should_OverrideDefaults_When_ExplicitValueProvided()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Topic.Partitions = 12);
            t.DeclareTopic("my-topic").Partitions(3);
        });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal(3, topic.Partitions);
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

using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Topology;

public class PostgresTopicTests
{
    [Fact]
    public void Topic_Should_HaveCorrectName_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "my-topic" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "my-topic");
        Assert.Equal("my-topic", topic.Name);
    }

    [Fact]
    public void Topic_Should_HaveAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "addr-topic" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "addr-topic");
        Assert.NotNull(topic.Address);
        Assert.Contains("/t/addr-topic", topic.Address!.AbsolutePath);
    }

    [Fact]
    public void Topic_Should_DefaultToAutoProvision_When_NotSpecified()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "default-topic" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "default-topic");
        Assert.NotEqual(false, topic.AutoProvision);
    }

    [Fact]
    public void Topic_Should_HaveEmptySubscriptions_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "no-sub-topic" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "no-sub-topic");
        Assert.Empty(topic.Subscriptions);
    }

    [Fact]
    public void Topic_Should_UseTopologyBaseAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddTopic(new PostgresTopicConfiguration { Name = "base-addr" });

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "base-addr");
        Assert.True(topology.Address.IsBaseOf(topic.Address!));
    }
}

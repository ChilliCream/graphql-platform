using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Topology;

public class PostgresSubscriptionTests
{
    [Fact]
    public void Subscription_Should_HaveCorrectSource_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopologyWithSubscription("src-topic", "dst-queue");

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.Equal("src-topic", subscription.Source.Name);
    }

    [Fact]
    public void Subscription_Should_HaveCorrectDestination_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopologyWithSubscription("src-topic", "dst-queue");

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.Equal("dst-queue", subscription.Destination.Name);
    }

    [Fact]
    public void Subscription_Should_HaveAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopologyWithSubscription("addr-src", "addr-dst");

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.NotNull(subscription.Address);
        Assert.Contains("/b/t/addr-src/q/addr-dst", subscription.Address!.AbsolutePath);
    }

    [Fact]
    public void Subscription_Should_DefaultToAutoProvision_When_NotSpecified()
    {
        // arrange
        var (_, _, topology) = CreateTopologyWithSubscription("topic", "queue");

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.NotEqual(false, subscription.AutoProvision);
    }

    [Fact]
    public void Subscription_Should_UseTopologyBaseAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = CreateTopologyWithSubscription("base-src", "base-dst");

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.True(topology.Address.IsBaseOf(subscription.Address!));
    }

    private static (
        MessagingRuntime Runtime,
        PostgresMessagingTransport Transport,
        PostgresMessagingTopology Topology) CreateTopologyWithSubscription(string topicName, string queueName)
    {
        var result = PostgresBusFixture.CreateTopology(_ => { });
        result.Topology.AddTopic(new PostgresTopicConfiguration { Name = topicName });
        result.Topology.AddQueue(new PostgresQueueConfiguration { Name = queueName });
        result.Topology.AddSubscription(new PostgresSubscriptionConfiguration
        {
            Source = topicName,
            Destination = queueName
        });
        return result;
    }
}

using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Topology;

public class PostgresQueueTests
{
    [Fact]
    public void Queue_Should_HaveCorrectName_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "my-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "my-queue");
        Assert.Equal("my-queue", queue.Name);
    }

    [Fact]
    public void Queue_Should_HaveAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "addr-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "addr-queue");
        Assert.NotNull(queue.Address);
        Assert.Contains("/q/addr-queue", queue.Address!.AbsolutePath);
    }

    [Fact]
    public void Queue_Should_DefaultToAutoProvision_When_NotSpecified()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "default-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "default-queue");
        Assert.NotEqual(false, queue.AutoProvision);
    }

    [Fact]
    public void Queue_Should_DefaultToNoAutoDelete_When_NotSpecified()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "no-ad-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "no-ad-queue");
        Assert.NotEqual(true, queue.AutoDelete);
    }

    [Fact]
    public void Queue_Should_HaveEmptySubscriptions_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "no-sub-queue" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "no-sub-queue");
        Assert.Empty(queue.Subscriptions);
    }

    [Fact]
    public void Queue_Should_UseTopologyBaseAddress_When_Created()
    {
        // arrange
        var (_, _, topology) = PostgresBusFixture.CreateTopology(_ => { });

        // act
        topology.AddQueue(new PostgresQueueConfiguration { Name = "base-addr" });

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "base-addr");
        Assert.True(topology.Address.IsBaseOf(queue.Address!));
    }
}

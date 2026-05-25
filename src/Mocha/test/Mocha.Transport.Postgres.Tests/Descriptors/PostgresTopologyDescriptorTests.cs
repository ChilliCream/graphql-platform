using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests.Descriptors;

public class PostgresTopologyDescriptorTests
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
    public void DeclareTopic_Should_DefaultToAutoProvision_When_NotSpecified()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareTopic("ap-topic"));

        // assert
        var topic = Assert.Single(topology.Topics, t => t.Name == "ap-topic");
        Assert.NotEqual(false, topic.AutoProvision);
    }

    [Fact]
    public void DeclareQueue_Should_SetName_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue"));

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "my-queue");
        Assert.Equal("my-queue", queue.Name);
    }

    [Fact]
    public void DeclareQueue_Should_DefaultToAutoProvision_When_NotSpecified()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("ap-queue"));

        // assert
        var queue = Assert.Single(topology.Queues, q => q.Name == "ap-queue");
        Assert.NotEqual(false, queue.AutoProvision);
    }

    [Fact]
    public void DeclareSubscription_Should_SetSourceAndDestination_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("src-topic");
            t.DeclareQueue("dst-queue");
            t.DeclareSubscription("src-topic", "dst-queue");
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.Equal("src-topic", subscription.Source.Name);
        Assert.Equal("dst-queue", subscription.Destination.Name);
    }

    [Fact]
    public void DeclareSubscription_Should_HaveCorrectAddress_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("addr-src");
            t.DeclareQueue("addr-dst");
            t.DeclareSubscription("addr-src", "addr-dst");
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.NotNull(subscription.Address);
        Assert.Contains("/b/t/addr-src/q/addr-dst", subscription.Address!.AbsolutePath);
    }

    [Fact]
    public void MultipleDeclares_Should_CreateFullTopology_When_Combined()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareTopic("events");
            t.DeclareQueue("q1");
            t.DeclareQueue("q2");
            t.DeclareSubscription("events", "q1");
            t.DeclareSubscription("events", "q2");
        });

        // assert
        Assert.Single(topology.Topics);
        Assert.Equal(2, topology.Queues.Count(q => q.Name is "q1" or "q2"));
        Assert.Equal(2, topology.Subscriptions.Count);
    }

    [Fact]
    public void Endpoint_Should_CreateReceiveEndpoint_When_Configured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("ep-queue");
            t.Endpoint("my-ep").Queue("ep-queue");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.ReceiveEndpoints
            .OfType<PostgresReceiveEndpoint>()
            .Single(e => e.Name == "my-ep");
        Assert.NotNull(endpoint);
        Assert.Equal("ep-queue", endpoint.Queue.Name);
    }

    [Fact]
    public void DispatchEndpoint_Should_CreateDispatchEndpoint_When_Configured()
    {
        // arrange & act
        var runtime = PostgresBusFixture.CreateRuntime(t =>
        {
            t.DeclareQueue("dispatch-q");
            t.DispatchEndpoint("my-dispatch").ToQueue("dispatch-q");
        });
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // assert
        var endpoint = transport.DispatchEndpoints
            .OfType<PostgresDispatchEndpoint>()
            .Single(e => e.Name == "my-dispatch");
        Assert.NotNull(endpoint);
        Assert.Equal("dispatch-q", endpoint.Queue!.Name);
    }

    private static (
        MessagingRuntime Runtime,
        PostgresMessagingTransport Transport,
        PostgresMessagingTopology Topology) CreateTopology(
        Action<IPostgresMessagingTransportDescriptor> configure)
    {
        return PostgresBusFixture.CreateTopologyWithTransport(configure);
    }
}

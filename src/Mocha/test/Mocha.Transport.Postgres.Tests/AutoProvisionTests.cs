using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.Postgres.Tests.Helpers;

namespace Mocha.Transport.Postgres.Tests;

public class AutoProvisionTests
{
    [Fact]
    public void AutoProvision_Should_BeTrue_When_NotConfigured()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(_ => { });

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void AutoProvision_Should_BeFalse_When_AutoProvisionSetToFalse()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t => t.AutoProvision(false));

        // assert
        Assert.False(topology.AutoProvision);
    }

    [Fact]
    public void AutoProvision_Should_BeTrue_When_AutoProvisionSetToTrue()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t => t.AutoProvision(true));

        // assert
        Assert.True(topology.AutoProvision);
    }

    [Fact]
    public void Topic_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t => t.DeclareTopic("t1"));

        // assert - null means it inherits the topology default
        var topic = topology.Topics.Single(e => e.Name == "t1");
        Assert.Null(topic.AutoProvision);
    }

    [Fact]
    public void Topic_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(false);
            t.DeclareTopic("t1").AutoProvision(true);
        });

        // assert - explicit true overrides topology false
        var topic = topology.Topics.Single(e => e.Name == "t1");
        Assert.True(topic.AutoProvision);
    }

    [Fact]
    public void Topic_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(true);
            t.DeclareTopic("t1").AutoProvision(false);
        });

        // assert - explicit false overrides topology true
        var topic = topology.Topics.Single(e => e.Name == "t1");
        Assert.False(topic.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t => t.DeclareQueue("q1"));

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.Null(queue.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(false);
            t.DeclareQueue("q1").AutoProvision(true);
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.True(queue.AutoProvision);
    }

    [Fact]
    public void Queue_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(true);
            t.DeclareQueue("q1").AutoProvision(false);
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "q1");
        Assert.False(queue.AutoProvision);
    }

    [Fact]
    public void Subscription_AutoProvision_Should_BeNull_When_NotSet()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.DeclareTopic("t1");
            t.DeclareQueue("q1");
            t.DeclareSubscription("t1", "q1");
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.Null(subscription.AutoProvision);
    }

    [Fact]
    public void Subscription_AutoProvision_Should_BeTrue_When_ExplicitlyEnabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(false);
            t.DeclareTopic("t1");
            t.DeclareQueue("q1");
            t.DeclareSubscription("t1", "q1").AutoProvision(true);
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.True(subscription.AutoProvision);
    }

    [Fact]
    public void Subscription_AutoProvision_Should_BeFalse_When_ExplicitlyDisabled()
    {
        // arrange & act
        var (_, _, topology) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(true);
            t.DeclareTopic("t1");
            t.DeclareQueue("q1");
            t.DeclareSubscription("t1", "q1").AutoProvision(false);
        });

        // assert
        var subscription = Assert.Single(topology.Subscriptions);
        Assert.False(subscription.AutoProvision);
    }

    [Fact]
    public void Describe_Should_IncludeAutoProvisionProperty_When_TransportEnabledByDefault()
    {
        // arrange
        var runtime = CreateRuntime(b => b.AddEventHandler<OrderCreatedHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();

        // act
        var description = transport.Describe();

        // assert - all entities should have autoProvision in their properties
        Assert.NotNull(description.Topology);
        foreach (var entity in description.Topology!.Entities)
        {
            Assert.True(
                entity.Properties!.ContainsKey("autoProvision"),
                $"Entity '{entity.Name}' should have autoProvision in properties");
        }

        // topics declared via convention get null AutoProvision → inherit topology default (true)
        var topics = description.Topology.Entities.Where(e => e.Kind == "topic");
        Assert.All(topics, e =>
        {
            var value = e.Properties!["autoProvision"];
            Assert.True(
                value is null or true,
                $"Topic '{e.Name}' should report autoProvision=null (inherited) or true");
        });
    }

    [Fact]
    public void Describe_Should_ReportAutoProvisionFalse_When_TopologyDisabled()
    {
        // arrange & act
        var (_, transport, _) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(false);
            t.DeclareTopic("t1");
            t.DeclareQueue("q1");
            t.DeclareSubscription("t1", "q1");
        });

        var description = transport.Describe();

        // assert - entities with no explicit override report null (inheriting topology's false)
        Assert.NotNull(description.Topology);
        foreach (var entity in description.Topology!.Entities.Where(e => e.Name is "t1" or "q1"))
        {
            Assert.Null(entity.Properties!["autoProvision"]);
        }

        foreach (var link in description.Topology.Links)
        {
            Assert.Null(link.Properties!["autoProvision"]);
        }
    }

    [Fact]
    public void Describe_Should_ReportMixedAutoProvision_When_ResourceOverridesTopology()
    {
        // arrange
        var (_, transport, _) = PostgresBusFixture.CreateTopologyWithTransport(t =>
        {
            t.AutoProvision(false);
            t.DeclareTopic("t1").AutoProvision(true);
            t.DeclareQueue("q1");
            t.DeclareSubscription("t1", "q1");
        });

        // act
        var description = transport.Describe();

        // assert
        Assert.NotNull(description.Topology);

        var topicEntity = description.Topology!.Entities.Single(e => e.Name == "t1");
        Assert.True(
            (bool)topicEntity.Properties!["autoProvision"]!,
            "Topic with explicit AutoProvision(true) should report true");

        var queueEntity = description.Topology.Entities.Single(e => e.Name == "q1");
        Assert.Null(queueEntity.Properties!["autoProvision"]);
    }

    [Fact]
    public void Convention_Should_PropagateAutoProvision_When_ReceiveEndpointCreated()
    {
        // arrange - the reply endpoint always sets AutoProvision = true
        var runtime = CreateRuntime(b =>
            b.AddRequestHandler<GetOrderStatusHandler>());
        var transport = runtime.Transports.OfType<PostgresMessagingTransport>().Single();
        var topology = (PostgresMessagingTopology)transport.Topology;

        // act - find the reply queue (temporary, auto-provisioned)
        var replyEndpoint = transport.ReceiveEndpoints
            .FirstOrDefault(e => e.Kind == ReceiveEndpointKind.Reply);

        // assert
        Assert.NotNull(replyEndpoint);
        var replyQueueName = ((PostgresReceiveEndpoint)replyEndpoint).Queue?.Name;
        Assert.NotNull(replyQueueName);

        var replyQueue = topology.Queues.Single(q => q.Name == replyQueueName);
        Assert.True(replyQueue.AutoProvision);
    }

    [Fact]
    public void Descriptor_AutoProvision_Should_ReturnSelf_When_Chaining()
    {
        // arrange & act - just verify the builder compiles and chains correctly
        var (_, transport, _) = PostgresBusFixture.CreateTopologyWithTransport(t =>
            t.AutoProvision(false)
             .DeclareTopic("t1")
             .AutoProvision(true));

        // assert - the last call wins
        var topic = ((PostgresMessagingTopology)transport.Topology)
            .Topics.Single(e => e.Name == "t1");
        Assert.True(topic.AutoProvision);
    }

    private static MessagingRuntime CreateRuntime(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        services.AddSingleton(new MessageRecorder());
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder
            .AddPostgres(t => t.ConnectionString("Host=localhost;Database=test;Username=test;Password=test"))
            .BuildRuntime();
        return runtime;
    }
}

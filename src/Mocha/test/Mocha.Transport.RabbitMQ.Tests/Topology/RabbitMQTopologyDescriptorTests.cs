using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQTopologyDescriptorTests
{
    [Fact]
    public void DeclareExchange_Should_SetName_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange"));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.Equal("my-exchange", exchange.Name);
    }

    [Fact]
    public void DeclareExchange_Should_SetType_When_TypeCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").Type("direct"));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.Equal("direct", exchange.Type);
    }

    [Fact]
    public void DeclareExchange_Should_SetDurable_When_DurableCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").Durable(false));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.False(exchange.Durable);
    }

    [Fact]
    public void DeclareExchange_Should_SetAutoDelete_When_AutoDeleteCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").AutoDelete(true));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.True(exchange.AutoDelete);
    }

    [Fact]
    public void DeclareExchange_Should_SetAutoProvision_When_AutoProvisionCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").AutoProvision(true));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.True(exchange.AutoProvision);
    }

    [Fact]
    public void DeclareExchange_Should_SetArgument_When_WithArgumentCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").WithArgument("x-custom", "value"));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.Equal("value", exchange.Arguments["x-custom"]);
    }

    [Fact]
    public void DeclareExchange_Should_SetAlternateExchange_When_ExtensionCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("my-exchange").AlternateExchange("alt"));

        // assert
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "my-exchange");
        Assert.Equal("alt", exchange.Arguments["alternate-exchange"]);
    }

    [Fact]
    public void DeclareExchange_Should_OverrideName_When_NameCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareExchange("original").Name("renamed"));

        // assert
        Assert.DoesNotContain(topology.Exchanges, e => e.Name == "original");
        var exchange = Assert.Single(topology.Exchanges, e => e.Name == "renamed");
        Assert.Equal("renamed", exchange.Name);
    }

    [Fact]
    public void DeclareQueue_Should_SetName_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue"));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal("my-queue", queue.Name);
    }

    [Fact]
    public void DeclareQueue_Should_SetDurable_When_DurableCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").Durable(false));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.False(queue.Durable);
    }

    [Fact]
    public void DeclareQueue_Should_SetExclusive_When_ExclusiveCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").Exclusive(true));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.True(queue.Exclusive);
    }

    [Fact]
    public void DeclareQueue_Should_SetAutoDelete_When_AutoDeleteCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").AutoDelete(true));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void DeclareQueue_Should_SetAutoProvision_When_AutoProvisionCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").AutoProvision(true));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.True(queue.AutoProvision);
    }

    [Fact]
    public void DeclareQueue_Should_SetMessageTTL_When_MessageTimeToLiveCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").MessageTimeToLive(TimeSpan.FromSeconds(30)));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(30000, (int)queue.Arguments["x-message-ttl"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetExpires_When_ExpiresCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").Expires(TimeSpan.FromMinutes(5)));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(300000, (int)queue.Arguments["x-expires"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetMaxLength_When_MaxLengthCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").MaxLength(1000));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(1000, (int)queue.Arguments["x-max-length"]!);
        Assert.Equal("drop-head", (string)queue.Arguments["x-overflow"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetMaxLengthBytes_When_MaxLengthBytesCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").MaxLengthBytes(1048576));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(1048576L, (long)queue.Arguments["x-max-length-bytes"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetDeadLetter_When_DeadLetterCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").DeadLetter("dlx", "dlq"));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal("dlx", (string)queue.Arguments["x-dead-letter-exchange"]!);
        Assert.Equal("dlq", (string)queue.Arguments["x-dead-letter-routing-key"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetQueueType_When_QueueTypeCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").QueueType("quorum"));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal("quorum", (string)queue.Arguments["x-queue-type"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetQueueMode_When_QueueModeCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").QueueMode(RabbitMQQueueMode.Lazy));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal("lazy", (string)queue.Arguments["x-queue-mode"]!);
    }

    [Fact]
    public void DeclareQueue_Should_EnableSingleActive_When_SingleActiveConsumerCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").SingleActiveConsumer());

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.True((bool)queue.Arguments["x-single-active-consumer"]!);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(255)]
    public void DeclareQueue_Should_SetMaxPriority_When_MaxPriorityCalled(int priority)
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").MaxPriority(priority));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(priority, (int)queue.Arguments["x-max-priority"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetQuorumSize_When_QuorumInitialGroupSizeCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").QuorumInitialGroupSize(3));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(3, (int)queue.Arguments["x-quorum-initial-group-size"]!);
    }

    [Fact]
    public void DeclareQueue_Should_SetDeliveryLimit_When_MaxDeliveryLimitCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("my-queue").MaxDeliveryLimit(5));

        // assert
        var queue = FindQueue(topology, "my-queue");
        Assert.Equal(5, (int)queue.Arguments["x-delivery-limit"]!);
    }

    [Fact]
    public void DeclareQueue_Should_OverrideName_When_NameCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t => t.DeclareQueue("original").Name("renamed"));

        // assert
        Assert.DoesNotContain(topology.Queues, q => q.Name == "original");
        var queue = FindQueue(topology, "renamed");
        Assert.Equal("renamed", queue.Name);
    }

    [Fact]
    public void DeclareBinding_Should_SetSourceAndDestination_When_Created()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue");
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.Equal("src-exchange", binding.Source.Name);
        var queueBinding = Assert.IsType<RabbitMQQueueBinding>(binding);
        Assert.Equal("dest-queue", queueBinding.Destination.Name);
    }

    [Fact]
    public void DeclareBinding_Should_SetRoutingKey_When_RoutingKeyCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue").RoutingKey("my.key");
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.Equal("my.key", binding.RoutingKey);
    }

    [Fact]
    public void DeclareBinding_Should_SetDestinationKindQueue_When_ToQueueCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue").ToQueue("dest-queue");
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.IsType<RabbitMQQueueBinding>(binding);
    }

    [Fact]
    public void DeclareBinding_Should_SetDestinationKindExchange_When_ToExchangeCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareExchange("dest-exchange");
            t.DeclareBinding("src-exchange", "dest-exchange").ToExchange("dest-exchange");
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        var exchangeBinding = Assert.IsType<RabbitMQExchangeBinding>(binding);
        Assert.Equal("dest-exchange", exchangeBinding.Destination.Name);
    }

    [Fact]
    public void DeclareBinding_Should_SetAutoProvision_When_AutoProvisionCalled()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue").AutoProvision(true);
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.True(binding.AutoProvision);
    }

    [Fact]
    public void DeclareBinding_Should_SetMatchAll_When_MatchCalledWithAll()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue").Match(RabbitMQBindingMatchType.All);
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.Equal("all", (string)binding.Arguments["x-match"]!);
    }

    [Fact]
    public void DeclareBinding_Should_SetMatchAny_When_MatchCalledWithAny()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.DeclareExchange("src-exchange");
            t.DeclareQueue("dest-queue");
            t.DeclareBinding("src-exchange", "dest-queue").Match(RabbitMQBindingMatchType.Any);
        });

        // assert
        var binding = Assert.Single(topology.Bindings);
        Assert.Equal("any", (string)binding.Arguments["x-match"]!);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

    private static RabbitMQQueue FindQueue(RabbitMQMessagingTopology topology, string name)
    {
        var queue = topology.Queues.SingleOrDefault(q => q.Name == name);
        Assert.NotNull(queue);
        return queue;
    }

    private static (
        MessagingRuntime Runtime,
        RabbitMQMessagingTransport Transport,
        RabbitMQMessagingTopology Topology) CreateTopology(Action<IRabbitMQMessagingTransportDescriptor> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        var runtime = builder
            .AddRabbitMQ(t =>
            {
                t.ConnectionProvider(_ => new StubConnectionProvider());
                configure(t);
            })
            .BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}

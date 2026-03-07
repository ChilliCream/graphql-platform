using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQBusDefaultsTests
{
    [Fact]
    public void AddQueue_Should_ApplyDefaultQueueType_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "test-queue" });

        // assert
        Assert.Equal("quorum", queue.Arguments["x-queue-type"]);
    }

    [Fact]
    public void AddQueue_Should_NotOverrideQueueType_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "test-queue",
            Arguments = new Dictionary<string, object> { ["x-queue-type"] = "stream" }
        });

        // assert
        Assert.Equal("stream", queue.Arguments["x-queue-type"]);
    }

    [Fact]
    public void AddQueue_Should_ApplyDefaultAutoDelete_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "test-queue",
            AutoDelete = null
        });

        // assert
        Assert.True(queue.AutoDelete);
    }

    [Fact]
    public void AddQueue_Should_NotOverrideAutoDelete_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.AutoDelete = true));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "test-queue",
            AutoDelete = false
        });

        // assert
        Assert.False(queue.AutoDelete);
    }

    [Fact]
    public void AddQueue_Should_MergeDefaultArguments_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.Arguments["x-delivery-limit"] = 5));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "test-queue" });

        // assert
        Assert.Equal(5, queue.Arguments["x-delivery-limit"]);
    }

    [Fact]
    public void AddQueue_Should_NotOverrideArguments_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.Arguments["x-delivery-limit"] = 5));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "test-queue",
            Arguments = new Dictionary<string, object> { ["x-delivery-limit"] = 10 }
        });

        // assert
        Assert.Equal(10, queue.Arguments["x-delivery-limit"]);
    }

    [Fact]
    public void AddQueue_Should_MergeMultipleArguments_When_DefaultsAndExplicitCombined()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d =>
            {
                d.Queue.Arguments["x-delivery-limit"] = 5;
                d.Queue.Arguments["x-max-priority"] = 10;
            }));

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "test-queue",
            Arguments = new Dictionary<string, object> { ["x-delivery-limit"] = 3 }
        });

        // assert — explicit argument wins, default argument is added
        Assert.Equal(3, queue.Arguments["x-delivery-limit"]);
        Assert.Equal(10, queue.Arguments["x-max-priority"]);
    }

    [Fact]
    public void AddQueue_Should_ApplyNoDefaults_When_NoDefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "test-queue" });

        // assert — only standard defaults, no x-queue-type argument
        Assert.True(queue.Durable);
        Assert.False(queue.AutoDelete);
        Assert.DoesNotContain("x-queue-type", queue.Arguments.Keys);
    }

    [Fact]
    public void AddExchange_Should_ApplyDefaultType_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Type = RabbitMQExchangeType.Topic));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "test-exchange" });

        // assert
        Assert.Equal("topic", exchange.Type);
    }

    [Fact]
    public void AddExchange_Should_NotOverrideType_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Type = RabbitMQExchangeType.Topic));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "test-exchange",
            Type = RabbitMQExchangeType.Direct
        });

        // assert
        Assert.Equal("direct", exchange.Type);
    }

    [Fact]
    public void AddExchange_Should_ApplyDefaultDurable_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Durable = false));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "test-exchange" });

        // assert
        Assert.False(exchange.Durable);
    }

    [Fact]
    public void AddExchange_Should_NotOverrideDurable_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Durable = false));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "test-exchange",
            Durable = true
        });

        // assert
        Assert.True(exchange.Durable);
    }

    [Fact]
    public void AddExchange_Should_ApplyDefaultAutoDelete_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.AutoDelete = true));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "test-exchange" });

        // assert
        Assert.True(exchange.AutoDelete);
    }

    [Fact]
    public void AddExchange_Should_NotOverrideAutoDelete_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.AutoDelete = true));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "test-exchange",
            AutoDelete = false
        });

        // assert
        Assert.False(exchange.AutoDelete);
    }

    [Fact]
    public void AddExchange_Should_MergeDefaultArguments_When_DefaultsConfigured()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Arguments["alternate-exchange"] = "alt-exchange"));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "test-exchange" });

        // assert
        Assert.Equal("alt-exchange", exchange.Arguments["alternate-exchange"]);
    }

    [Fact]
    public void AddExchange_Should_NotOverrideArguments_When_ExplicitlySet()
    {
        // arrange
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Arguments["alternate-exchange"] = "default-alt"));

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "test-exchange",
            Arguments = new Dictionary<string, object> { ["alternate-exchange"] = "explicit-alt" }
        });

        // assert
        Assert.Equal("explicit-alt", exchange.Arguments["alternate-exchange"]);
    }

    [Fact]
    public void ConfigureDefaults_Should_SkipQueueType_When_AutoDeleteIsTrue()
    {
        // arrange — auto-delete queues (e.g. reply queues) are incompatible with quorum
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum));
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "reply-queue",
            AutoDelete = true
        });

        // assert
        Assert.DoesNotContain("x-queue-type", queue.Arguments.Keys);
    }

    [Fact]
    public void ConfigureDefaults_Should_SkipQueueType_When_ExclusiveIsTrue()
    {
        // arrange — exclusive queues are incompatible with quorum
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum));
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "exclusive-queue",
            Exclusive = true
        });

        // assert
        Assert.DoesNotContain("x-queue-type", queue.Arguments.Keys);
    }

    [Fact]
    public void ConfigureDefaults_Should_SkipStreamQueueType_When_AutoDeleteIsTrue()
    {
        // arrange — auto-delete queues are incompatible with stream
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Stream));
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "reply-queue",
            AutoDelete = true
        });

        // assert
        Assert.DoesNotContain("x-queue-type", queue.Arguments.Keys);
    }

    [Fact]
    public void ConfigureDefaults_Should_SkipAllDefaults_When_QueueIsIncompatible()
    {
        // arrange — both queue type and arguments are skipped for incompatible queues
        // since default arguments may be queue-type-specific (e.g. x-delivery-limit)
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d =>
            {
                d.Queue.QueueType = RabbitMQQueueType.Quorum;
                d.Queue.Arguments["x-delivery-limit"] = 5;
            }));

        var queue = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "reply-queue",
            AutoDelete = true
        });

        // assert — both queue type and arguments are skipped
        Assert.DoesNotContain("x-queue-type", queue.Arguments.Keys);
        Assert.DoesNotContain("x-delivery-limit", queue.Arguments.Keys);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyToExplicitlyDeclaredQueues()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
            t.DeclareQueue("my-queue");
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "my-queue");
        Assert.Equal("quorum", queue.Arguments["x-queue-type"]);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyToExplicitlyDeclaredExchanges()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Exchange.Type = RabbitMQExchangeType.Topic);
            t.DeclareExchange("my-exchange");
        });

        // assert
        var exchange = topology.Exchanges.Single(e => e.Name == "my-exchange");
        Assert.Equal("topic", exchange.Type);
    }

    [Fact]
    public void ConfigureDefaults_Should_NotOverrideDeclaredQueueSettings()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
            t.DeclareQueue("my-queue").QueueType(RabbitMQQueueType.Stream);
        });

        // assert
        var queue = topology.Queues.Single(q => q.Name == "my-queue");
        Assert.Equal("stream", queue.Arguments["x-queue-type"]);
    }

    [Fact]
    public void ConfigureDefaults_Should_NotOverrideDeclaredExchangeType()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Exchange.Type = RabbitMQExchangeType.Topic);
            t.DeclareExchange("my-exchange").Type(RabbitMQExchangeType.Direct);
        });

        // assert
        var exchange = topology.Exchanges.Single(e => e.Name == "my-exchange");
        Assert.Equal("direct", exchange.Type);
    }

    [Fact]
    public void ConfigureDefaults_Should_AllowMultipleConfigureCalls()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
            t.ConfigureDefaults(d => d.Queue.Arguments["x-delivery-limit"] = 5);
        });

        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "test-queue" });

        // assert — both calls are applied
        Assert.Equal("quorum", queue.Arguments["x-queue-type"]);
        Assert.Equal(5, queue.Arguments["x-delivery-limit"]);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyLastValue_When_SamePropertySetMultipleTimes()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
        {
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum);
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Stream);
        });

        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "test-queue" });

        // assert — last write wins
        Assert.Equal("stream", queue.Arguments["x-queue-type"]);
    }

    // ──────────────────────────────────────────────
    // Defaults apply to multiple resources
    // ──────────────────────────────────────────────

    [Fact]
    public void ConfigureDefaults_Should_ApplyToAllQueues()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Queue.QueueType = RabbitMQQueueType.Quorum));

        var queue1 = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "queue-1" });
        var queue2 = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "queue-2" });

        // assert
        Assert.Equal("quorum", queue1.Arguments["x-queue-type"]);
        Assert.Equal("quorum", queue2.Arguments["x-queue-type"]);
    }

    [Fact]
    public void ConfigureDefaults_Should_ApplyToAllExchanges()
    {
        // arrange & act
        var (_, _, topology) = CreateTopology(t =>
            t.ConfigureDefaults(d => d.Exchange.Type = RabbitMQExchangeType.Topic));

        var exchange1 = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "exchange-1" });
        var exchange2 = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "exchange-2" });

        // assert
        Assert.Equal("topic", exchange1.Type);
        Assert.Equal("topic", exchange2.Type);
    }

    // ──────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────

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

using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.RabbitMQ.Tests.Helpers;

namespace Mocha.Transport.RabbitMQ.Tests.Topology;

public class RabbitMQMessagingTopologyTests
{
    [Fact]
    public void AddExchange_Should_CreateExchange_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new RabbitMQExchangeConfiguration { Name = "test-exchange" };

        // act
        var exchange = topology.AddExchange(config);

        // assert
        Assert.Equal("test-exchange", exchange.Name);
        Assert.Contains(exchange, topology.Exchanges);
    }

    [Fact]
    public void AddExchange_Should_MergeProperties_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "shared-exchange",
            Type = "direct",
            Origin = TopologyOrigin.Convention
        });

        var countAfterFirst = topology.Exchanges.Count;

        // act
        var merged = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "shared-exchange",
            Durable = false,
            Origin = TopologyOrigin.Declared
        });

        // assert: count does not increase; merged properties reflect the rules
        Assert.Equal(countAfterFirst, topology.Exchanges.Count);
        Assert.Equal("direct", merged.Type);
        Assert.False(merged.Durable);
        Assert.Equal(TopologyOrigin.Declared, merged.Origin);
    }

    [Fact]
    public void AddExchange_Should_DefaultToFanout_When_TypeNotSpecified()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "fanout-exchange" });

        // assert
        Assert.Equal("fanout", exchange.Type);
    }

    [Fact]
    public void AddExchange_Should_DefaultToDurable_When_DurabilityNotSpecified()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var exchange = topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "durable-exchange" });

        // assert
        Assert.True(exchange.Durable);
    }

    [Fact]
    public void AddQueue_Should_CreateQueue_When_NameProvided()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });
        var config = new RabbitMQQueueConfiguration { Name = "test-queue" };

        // act
        var queue = topology.AddQueue(config);

        // assert
        Assert.Equal("test-queue", queue.Name);
        Assert.Contains(queue, topology.Queues);
    }

    [Fact]
    public void AddQueue_Should_MergeProperties_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "shared-queue",
            Durable = true,
            Origin = TopologyOrigin.Convention
        });

        var countAfterFirst = topology.Queues.Count;

        // act
        var merged = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "shared-queue",
            AutoDelete = true,
            Origin = TopologyOrigin.Declared
        });

        // assert: count does not increase; merged properties reflect the rules
        Assert.Equal(countAfterFirst, topology.Queues.Count);
        Assert.True(merged.Durable);
        Assert.True(merged.AutoDelete);
        Assert.Equal(TopologyOrigin.Declared, merged.Origin);
    }

    [Fact]
    public void AddQueue_Should_DefaultToDurable_When_DurabilityNotSpecified()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        // act
        var queue = topology.AddQueue(new RabbitMQQueueConfiguration { Name = "durable-queue" });

        // assert
        Assert.True(queue.Durable);
    }

    [Fact]
    public void AddBinding_Should_ConnectExchangeToQueue_When_QueueDestination()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "source-exchange" });
        topology.AddQueue(new RabbitMQQueueConfiguration { Name = "destination-queue" });

        var bindingConfig = new RabbitMQBindingConfiguration
        {
            Source = "source-exchange",
            Destination = "destination-queue",
            DestinationKind = RabbitMQDestinationKind.Queue
        };

        // act
        var binding = topology.AddBinding(bindingConfig);

        // assert
        Assert.NotNull(binding);
        Assert.Equal("source-exchange", binding.Source.Name);
        Assert.Contains(binding, topology.Bindings);

        var queueBinding = Assert.IsType<RabbitMQQueueBinding>(binding);
        Assert.Equal("destination-queue", queueBinding.Destination.Name);
    }

    [Fact]
    public void AddBinding_Should_ConnectExchangeToExchange_When_ExchangeDestination()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "source-exchange" });
        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "destination-exchange" });

        var bindingConfig = new RabbitMQBindingConfiguration
        {
            Source = "source-exchange",
            Destination = "destination-exchange",
            DestinationKind = RabbitMQDestinationKind.Exchange
        };

        // act
        var binding = topology.AddBinding(bindingConfig);

        // assert
        Assert.NotNull(binding);
        Assert.Equal("source-exchange", binding.Source.Name);
        Assert.Contains(binding, topology.Bindings);

        var exchangeBinding = Assert.IsType<RabbitMQExchangeBinding>(binding);
        Assert.Equal("destination-exchange", exchangeBinding.Destination.Name);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_SourceExchangeNotFound()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddQueue(new RabbitMQQueueConfiguration { Name = "destination-queue" });

        var bindingConfig = new RabbitMQBindingConfiguration
        {
            Source = "nonexistent-exchange",
            Destination = "destination-queue",
            DestinationKind = RabbitMQDestinationKind.Queue
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("nonexistent-exchange", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_DestinationQueueNotFound()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "source-exchange" });

        var bindingConfig = new RabbitMQBindingConfiguration
        {
            Source = "source-exchange",
            Destination = "nonexistent-queue",
            DestinationKind = RabbitMQDestinationKind.Queue
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("nonexistent-queue", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_UnknownDestinationKind()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "source-exchange" });

        var bindingConfig = new RabbitMQBindingConfiguration
        {
            Source = "source-exchange",
            Destination = "some-dest",
            DestinationKind = (RabbitMQDestinationKind)99
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("Unknown destination kind", exception.Message);
    }

    [Fact]
    public async Task AddExchangeAndQueue_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        var initialExchangeCount = topology.Exchanges.Count;
        var initialQueueCount = topology.Queues.Count;

        const int operationCount = 100;

        // act
        var allTasks = Enumerable
            .Range(0, operationCount)
            .SelectMany(i =>
                new Task[]
                {
                    Task.Run(() => topology.AddExchange(new RabbitMQExchangeConfiguration { Name = $"exchange-{i}" })),
                    Task.Run(() => topology.AddQueue(new RabbitMQQueueConfiguration { Name = $"queue-{i}" }))
                }
            )
            .ToList();

        await Task.WhenAll(allTasks);

        // assert
        Assert.Equal(initialExchangeCount + operationCount, topology.Exchanges.Count);
        Assert.Equal(initialQueueCount + operationCount, topology.Queues.Count);

        var exchangeNames = topology.Exchanges.Select(e => e.Name).ToList();
        Assert.Equal(exchangeNames.Count, exchangeNames.Distinct().Count());

        var queueNames = topology.Queues.Select(q => q.Name).ToList();
        Assert.Equal(queueNames.Count, queueNames.Distinct().Count());

        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Exchanges, e => e.Name == $"exchange-{i}");
            Assert.Contains(topology.Queues, q => q.Name == $"queue-{i}");
        }
    }

    // Merge-matrix tests (8 cases, covering plan section 3.5 rules)

    [Fact]
    public void AddExchange_Merge_Should_WinDeclaredOverConvention_When_ScalarsConflict()
    {
        // arrange: convention creates a fanout exchange first
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "orders",
            Type = "fanout",
            Durable = true,
            Origin = TopologyOrigin.Convention
        });

        var countAfterFirst = topology.Exchanges.Count;

        // act: declared configuration comes in with different type
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "orders",
            Type = "topic",
            Origin = TopologyOrigin.Declared
        });

        // assert: declared wins; durable stays from convention fill; count unchanged
        Assert.Equal("topic", result.Type);
        Assert.True(result.Durable);
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
        Assert.Equal(countAfterFirst, topology.Exchanges.Count);
    }

    [Fact]
    public void AddExchange_Merge_Should_UnionArguments_When_KeysAreDifferent()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "events",
            Arguments = new Dictionary<string, object> { ["alternate-exchange"] = "ae" },
            Origin = TopologyOrigin.Convention
        });

        // act
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "events",
            Arguments = new Dictionary<string, object> { ["x-delayed-type"] = "direct" },
            Origin = TopologyOrigin.Declared
        });

        // assert: both argument keys are present
        Assert.True(result.Arguments.ContainsKey("alternate-exchange"));
        Assert.True(result.Arguments.ContainsKey("x-delayed-type"));
    }

    [Fact]
    public void AddExchange_Merge_Should_ReturnSameInstance_When_ExactDuplicate()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        var first = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "noop",
            Type = "fanout",
            Durable = true,
            Origin = TopologyOrigin.Convention
        });

        var countAfterFirst = topology.Exchanges.Count;

        // act: identical second declaration (no-op merge)
        var second = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "noop",
            Type = "fanout",
            Durable = true,
            Origin = TopologyOrigin.Convention
        });

        // assert: same object returned; count unchanged
        Assert.Same(first, second);
        Assert.Equal(countAfterFirst, topology.Exchanges.Count);
    }

    [Fact]
    public void AddExchange_Merge_Should_StrengthenAutoProvision_When_IncomingIsTrue()
    {
        // arrange: first entry has AutoProvision null
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "ap-exchange",
            AutoProvision = null,
            Origin = TopologyOrigin.Convention
        });

        // act: endpoint brings AutoProvision true
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "ap-exchange",
            AutoProvision = true,
            Origin = TopologyOrigin.Endpoint
        });

        // assert: true wins
        Assert.True(result.AutoProvision);
    }

    [Fact]
    public void AddExchange_Merge_Should_UpgradeOrigin_When_EndpointMergesIntoConvention()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "prov-exchange",
            Origin = TopologyOrigin.Convention
        });

        // act: endpoint-origin arrives
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "prov-exchange",
            Origin = TopologyOrigin.Endpoint
        });

        // assert: origin upgraded from convention to endpoint
        Assert.Equal(TopologyOrigin.Endpoint, result.Origin);
    }

    [Fact]
    public void AddExchange_Merge_Should_NeverDowngradeOrigin_When_DeclaredIsFollowedByConvention()
    {
        // arrange: declared entity exists
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "order-exchange",
            Origin = TopologyOrigin.Declared
        });

        // act: convention comes in later (e.g., from a convention pass)
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "order-exchange",
            Origin = TopologyOrigin.Convention
        });

        // assert: origin never downgrades
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
    }

    [Fact]
    public void AddExchange_Merge_Should_RespectEndpointOrdering_When_EndpointThenDeclared()
    {
        // arrange: endpoint adds exchange first, declared adds it second
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "mixed-exchange",
            Type = "fanout",
            Origin = TopologyOrigin.Endpoint
        });

        // act: declared arrives with a different type, which wins
        var result = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "mixed-exchange",
            Type = "topic",
            Origin = TopologyOrigin.Declared
        });

        // assert: declared type wins; origin upgraded to declared
        Assert.Equal("topic", result.Type);
        Assert.Equal(TopologyOrigin.Declared, result.Origin);
    }

    [Fact]
    public void AddExchange_Merge_Should_ThrowShapeConflict_When_BothDeclaredWithDifferentTypes()
    {
        // arrange: first declared exchange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "conflict-exchange",
            Type = "direct",
            Origin = TopologyOrigin.Declared
        });

        // act: second declared exchange with different type
        var ex = Assert.Throws<RabbitMQTopologyShapeConflictException>(() =>
            topology.AddExchange(new RabbitMQExchangeConfiguration
            {
                Name = "conflict-exchange",
                Type = "topic",
                Origin = TopologyOrigin.Declared
            })
        );

        // assert
        Assert.Equal("exchange", ex.EntityType);
        Assert.Equal("conflict-exchange", ex.EntityName);
        Assert.Equal("Type", ex.PropertyName);
    }

    [Fact]
    public void AddQueue_Merge_Should_UnionArguments_When_ConventionAndDeclaredEachAddDifferentKeys()
    {
        // arrange: convention queue with one argument
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "work-queue",
            Arguments = new Dictionary<string, object> { ["x-message-ttl"] = 60000 },
            Origin = TopologyOrigin.Convention
        });

        // act: declared adds a different argument key
        var result = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "work-queue",
            Arguments = new Dictionary<string, object> { ["x-dead-letter-exchange"] = "dlx" },
            Origin = TopologyOrigin.Declared
        });

        // assert: both keys present after union
        Assert.True(result.Arguments.ContainsKey("x-message-ttl"));
        Assert.True(result.Arguments.ContainsKey("x-dead-letter-exchange"));
    }

    [Fact]
    public void AddExchange_Should_ReturnExisting_When_DuplicateNameNoopShape()
    {
        // arrange: create an exchange with full configuration
        var (_, _, topology) = CreateTopology(_ => { });

        var first = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "noop-exchange",
            Type = "direct",
            Durable = true,
            Origin = TopologyOrigin.Declared
        });

        var countAfterFirst = topology.Exchanges.Count;

        // act: add a configuration with only the name set (no-op shape: all scalars null)
        var second = topology.AddExchange(new RabbitMQExchangeConfiguration
        {
            Name = "noop-exchange",
            Origin = TopologyOrigin.Convention
        });

        // assert: same instance returned; existing properties unchanged; count stable
        Assert.Same(first, second);
        Assert.Equal("direct", second.Type);
        Assert.True(second.Durable);
        Assert.Equal(TopologyOrigin.Declared, second.Origin);
        Assert.Equal(countAfterFirst, topology.Exchanges.Count);
    }

    [Fact]
    public void AddQueue_Should_ReturnExisting_When_DuplicateNameNoopShape()
    {
        // arrange: create a queue with full configuration
        var (_, _, topology) = CreateTopology(_ => { });

        var first = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "noop-queue",
            Durable = true,
            AutoDelete = false,
            Origin = TopologyOrigin.Declared
        });

        var countAfterFirst = topology.Queues.Count;

        // act: add a configuration with only the name set (no-op shape: all scalars null)
        var second = topology.AddQueue(new RabbitMQQueueConfiguration
        {
            Name = "noop-queue",
            Origin = TopologyOrigin.Convention
        });

        // assert: same instance returned; existing properties unchanged; count stable
        Assert.Same(first, second);
        Assert.True(second.Durable);
        Assert.False(second.AutoDelete);
        Assert.Equal(TopologyOrigin.Declared, second.Origin);
        Assert.Equal(countAfterFirst, topology.Queues.Count);
    }

    [Fact]
    public async Task AddExchangeAndQueue_Should_NotCorrupt_When_ConcurrentMergeAdds()
    {
        // arrange: pre-create entities that will be merged into concurrently
        var (_, _, topology) = CreateTopology(_ => { });

        const int entityCount = 50;

        for (var i = 0; i < entityCount; i++)
        {
            topology.AddExchange(new RabbitMQExchangeConfiguration { Name = $"merge-exchange-{i}", Origin = TopologyOrigin.Convention });
            topology.AddQueue(new RabbitMQQueueConfiguration { Name = $"merge-queue-{i}", Origin = TopologyOrigin.Convention });
        }

        var countBeforeMerge = topology.Exchanges.Count;
        var queueCountBeforeMerge = topology.Queues.Count;

        // act: flood the same names from multiple threads simultaneously
        const int threadsPerEntity = 4;

        var allTasks = Enumerable
            .Range(0, entityCount)
            .SelectMany(i =>
                Enumerable.Range(0, threadsPerEntity).SelectMany(_ =>
                    new Task[]
                    {
                        Task.Run(() => topology.AddExchange(new RabbitMQExchangeConfiguration
                        {
                            Name = $"merge-exchange-{i}",
                            Type = "fanout",
                            Origin = TopologyOrigin.Convention
                        })),
                        Task.Run(() => topology.AddQueue(new RabbitMQQueueConfiguration
                        {
                            Name = $"merge-queue-{i}",
                            Durable = true,
                            Origin = TopologyOrigin.Convention
                        }))
                    })
            )
            .ToList();

        await Task.WhenAll(allTasks);

        // assert: no new entities created; no duplicates; all original entities still intact
        Assert.Equal(countBeforeMerge, topology.Exchanges.Count);
        Assert.Equal(queueCountBeforeMerge, topology.Queues.Count);

        var exchangeNames = topology.Exchanges.Select(e => e.Name).ToList();
        Assert.Equal(exchangeNames.Count, exchangeNames.Distinct().Count());

        var queueNames = topology.Queues.Select(q => q.Name).ToList();
        Assert.Equal(queueNames.Count, queueNames.Distinct().Count());
    }

    private static (
        MessagingRuntime Runtime,
        RabbitMQMessagingTransport Transport,
        RabbitMQMessagingTopology Topology) CreateTopology(Action<IMessageBusHostBuilder> configure)
    {
        var services = new ServiceCollection();
        var builder = services.AddMessageBus();
        configure(builder);
        var runtime = builder.AddRabbitMQ(t => t.ConnectionProvider(_ => new StubConnectionProvider())).BuildRuntime();
        var transport = runtime.Transports.OfType<RabbitMQMessagingTransport>().Single();
        var topology = (RabbitMQMessagingTopology)transport.Topology;
        return (runtime, transport, topology);
    }
}

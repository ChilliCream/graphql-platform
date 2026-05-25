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
    public void AddExchange_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "duplicate-exchange" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddExchange(new RabbitMQExchangeConfiguration { Name = "duplicate-exchange" })
            );
        Assert.Contains("duplicate-exchange", exception.Message);
        Assert.Contains("already exists", exception.Message);
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
    public void AddQueue_Should_Throw_When_DuplicateName()
    {
        // arrange
        var (_, _, topology) = CreateTopology(_ => { });

        topology.AddQueue(new RabbitMQQueueConfiguration { Name = "duplicate-queue" });

        // act & assert
        var exception =
            Assert.Throws<InvalidOperationException>(() =>
                topology.AddQueue(new RabbitMQQueueConfiguration { Name = "duplicate-queue" })
            );
        Assert.Contains("duplicate-queue", exception.Message);
        Assert.Contains("already exists", exception.Message);
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

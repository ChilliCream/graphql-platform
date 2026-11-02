using Microsoft.Extensions.DependencyInjection;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryMessagingTopologyTests
{
    [Fact]
    public void AddTopic_Should_CreateTopic_When_NameProvided()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var config = new InMemoryTopicConfiguration { Name = "test-topic" };

        // act
        var topic = topology.AddTopic(config);

        // assert
        Assert.Equal("test-topic", topic.Name);
        Assert.Contains(topic, topology.Topics);
    }

    [Fact]
    public void AddTopic_Should_ReturnExisting_When_DuplicateName()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var config1 = new InMemoryTopicConfiguration { Name = "duplicate-topic" };
        var config2 = new InMemoryTopicConfiguration { Name = "duplicate-topic" };

        var first = topology.AddTopic(config1);
        var countAfterFirst = topology.Topics.Count;

        // act
        var second = topology.AddTopic(config2);

        // assert
        Assert.Same(first, second);
        Assert.Equal(countAfterFirst, topology.Topics.Count);
    }

    [Fact]
    public void AddQueue_Should_CreateQueue_When_NameProvided()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var config = new InMemoryQueueConfiguration { Name = "test-queue" };

        // act
        var queue = topology.AddQueue(config);

        // assert
        Assert.Equal("test-queue", queue.Name);
        Assert.Contains(queue, topology.Queues);
    }

    [Fact]
    public void AddQueue_Should_ReturnExisting_When_DuplicateName()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var config1 = new InMemoryQueueConfiguration { Name = "duplicate-queue" };
        var config2 = new InMemoryQueueConfiguration { Name = "duplicate-queue" };

        var first = topology.AddQueue(config1);
        var countAfterFirst = topology.Queues.Count;

        // act
        var second = topology.AddQueue(config2);

        // assert
        Assert.Same(first, second);
        Assert.Equal(countAfterFirst, topology.Queues.Count);
    }

    [Fact]
    public void AddBinding_Should_ConnectTopicToQueue_When_QueueDestination()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });
        topology.AddQueue(new InMemoryQueueConfiguration { Name = "destination-queue" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "destination-queue",
            DestinationKind = InMemoryDestinationKind.Queue
        };

        // act
        var binding = topology.AddBinding(bindingConfig);

        // assert
        Assert.NotNull(binding);
        Assert.Equal("source-topic", binding.Source.Name);
        Assert.Contains(binding, topology.Bindings);

        // Verify it's a queue binding
        var queueBinding = Assert.IsType<InMemoryQueueBinding>(binding);
        Assert.Equal("destination-queue", queueBinding.Destination.Name);
    }

    [Fact]
    public void Describe_Should_UseAddressBasedIds_When_TopologyHasEntitiesAndLinks()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var topic = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });
        var queue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "destination-queue" });
        var binding = topology.AddBinding(new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "destination-queue",
            DestinationKind = InMemoryDestinationKind.Queue
        });

        // act
        var description = transport.Describe();

        // assert
        var describedTopic = description.Topology!.Entities.Single(e => e.Address == topic.Address.ToString());
        var describedQueue = description.Topology.Entities.Single(e => e.Address == queue.Address.ToString());
        var describedBinding = description.Topology.Links.Single();
        Assert.Equal($"urn:mocha:topology:{topic.Address}", describedTopic.Id);
        Assert.Equal($"urn:mocha:topology:{queue.Address}", describedQueue.Id);
        Assert.Equal($"urn:mocha:link:{binding.Address}", describedBinding.Id);
    }

    [Fact]
    public void AddBinding_Should_ConnectTopicToTopic_When_TopicDestination()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "destination-topic" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "destination-topic",
            DestinationKind = InMemoryDestinationKind.Topic
        };

        // act
        var binding = topology.AddBinding(bindingConfig);

        // assert
        Assert.NotNull(binding);
        Assert.Equal("source-topic", binding.Source.Name);
        Assert.Contains(binding, topology.Bindings);

        // Verify it's a topic binding
        var topicBinding = Assert.IsType<InMemoryTopicBinding>(binding);
        Assert.Equal("destination-topic", topicBinding.Destination.Name);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_SourceNotFound()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddQueue(new InMemoryQueueConfiguration { Name = "destination-queue" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "nonexistent-topic",
            Destination = "destination-queue",
            DestinationKind = InMemoryDestinationKind.Queue
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("nonexistent-topic", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void AddBinding_Should_Throw_When_DestinationQueueNotFound()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });

        var bindingConfig = new InMemoryBindingConfiguration
        {
            Source = "source-topic",
            Destination = "nonexistent-queue",
            DestinationKind = InMemoryDestinationKind.Queue
        };

        // act & assert
        var exception = Assert.Throws<InvalidOperationException>(() => topology.AddBinding(bindingConfig));
        Assert.Contains("nonexistent-queue", exception.Message);
        Assert.Contains("not found", exception.Message);
    }

    [Fact]
    public void GetTopic_Should_ReturnNull_When_NameNotFound()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // act
        var topic = topology.GetTopic("nonexistent-topic");

        // assert
        Assert.Null(topic);
    }

    [Fact]
    public void GetTopic_Should_ReturnTopic_When_NameExists()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var addedTopic = topology.AddTopic(new InMemoryTopicConfiguration { Name = "my-topic" });

        // act
        var foundTopic = topology.GetTopic("my-topic");

        // assert
        Assert.NotNull(foundTopic);
        Assert.Same(addedTopic, foundTopic);
    }

    [Fact]
    public void GetQueue_Should_ReturnNull_When_NameNotFound()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // act
        var queue = topology.GetQueue("nonexistent-queue");

        // assert
        Assert.Null(queue);
    }

    [Fact]
    public void GetQueue_Should_ReturnQueue_When_NameExists()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        var addedQueue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "my-queue" });

        // act
        var foundQueue = topology.GetQueue("my-queue");

        // assert
        Assert.NotNull(foundQueue);
        Assert.Same(addedQueue, foundQueue);
    }

    [Fact]
    public async Task AddTopicAndQueue_Should_NotCorrupt_When_ConcurrentAdds()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        // Record initial state - the runtime may pre-create some resources
        var initialTopicCount = topology.Topics.Count;
        var initialQueueCount = topology.Queues.Count;

        const int operationCount = 100;

        // act - add topics and queues concurrently from multiple threads
        // Each thread tries to add topics with unique names
        var allTasks = Enumerable
            .Range(0, operationCount)
            .SelectMany(i =>
                new Task[]
                {
                    Task.Run(() => topology.AddTopic(new InMemoryTopicConfiguration { Name = $"topic-{i}" })),
                    Task.Run(() => topology.AddQueue(new InMemoryQueueConfiguration { Name = $"queue-{i}" }))
                }
            )
            .ToList();

        await Task.WhenAll(allTasks);

        // assert - verify all items were added without corruption
        Assert.Equal(initialTopicCount + operationCount, topology.Topics.Count);
        Assert.Equal(initialQueueCount + operationCount, topology.Queues.Count);

        // Verify no duplicates in the collections (most important test for thread safety)
        var topicNames = topology.Topics.Select(t => t.Name).ToList();
        Assert.Equal(topicNames.Count, topicNames.Distinct().Count());

        var queueNames = topology.Queues.Select(q => q.Name).ToList();
        Assert.Equal(queueNames.Count, queueNames.Distinct().Count());

        // Verify all expected names are present
        for (var i = 0; i < operationCount; i++)
        {
            Assert.Contains(topology.Topics, t => t.Name == $"topic-{i}");
            Assert.Contains(topology.Queues, q => q.Name == $"queue-{i}");
        }
    }
}

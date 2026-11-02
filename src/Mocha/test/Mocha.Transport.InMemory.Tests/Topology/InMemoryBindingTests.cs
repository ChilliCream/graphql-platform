using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryBindingTests
{
    [Fact]
    public async Task QueueBinding_Should_Send_To_Destination_Queue()
    {
        // Arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = (InMemoryMessagingTransport)runtime.Transports.Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "test-topic" });
        var queue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });
        var binding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "test-topic",
                Destination = "test-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { MessageId = "test-message-1" };
        var ct = CancellationToken.None;

        // Act
        await binding.SendAsync(envelope, ct);

        // Assert
        await foreach (var receivedItem in queue.ConsumeAsync(ct))
        {
            Assert.Equal("test-message-1", receivedItem.Envelope.MessageId);
            receivedItem.Dispose();
            break;
        }
    }

    [Fact]
    public async Task TopicBinding_Should_Send_To_Destination_Topic()
    {
        // Arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = (InMemoryMessagingTransport)runtime.Transports.Single();
        var topology = (InMemoryMessagingTopology)transport.Topology;

        topology.AddTopic(new InMemoryTopicConfiguration { Name = "source-topic" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "dest-topic" });
        var destinationQueue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "dest-queue" });

        // Create binding from source -> destination topic
        var topicBinding = topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "source-topic",
                Destination = "dest-topic",
                DestinationKind = InMemoryDestinationKind.Topic
            });

        // Create binding from destination topic -> queue
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "dest-topic",
                Destination = "dest-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { MessageId = "test-message-2" };
        var ct = CancellationToken.None;

        // Act
        await topicBinding.SendAsync(envelope, ct);

        // Assert
        await foreach (var receivedItem in destinationQueue.ConsumeAsync(ct))
        {
            Assert.Equal("test-message-2", receivedItem.Envelope.MessageId);
            receivedItem.Dispose();
            break;
        }
    }
}

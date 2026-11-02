using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryTopicTests
{
    [Fact]
    public async Task Send_delivers_envelope_to_bound_queue()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var topic = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "test-topic" });
        var queue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "test-topic",
                Destination = "test-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([1, 2, 3]) };

        // act
        await topic.SendAsync(envelope, CancellationToken.None);

        // assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        InMemoryQueueItem? queueItem = null;

        await foreach (var item in queue.ConsumeAsync(cts.Token))
        {
            queueItem = item;
            break;
        }

        try
        {
            Assert.NotNull(queueItem);
            Assert.Equal(new byte[] { 1, 2, 3 }, queueItem.Envelope.Body.Span.ToArray());
        }
        finally
        {
            queueItem?.Dispose();
        }
    }

    [Fact]
    public async Task Send_fans_out_to_all_bound_queues()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var topic = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "test-topic" });
        var queue1 = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue-1" });
        var queue2 = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue-2" });
        var queue3 = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue-3" });

        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "test-topic",
                Destination = "test-queue-1",
                DestinationKind = InMemoryDestinationKind.Queue
            });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "test-topic",
                Destination = "test-queue-2",
                DestinationKind = InMemoryDestinationKind.Queue
            });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "test-topic",
                Destination = "test-queue-3",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([42]) };

        // act
        await topic.SendAsync(envelope, CancellationToken.None);

        // assert - verify all three queues received the message
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        InMemoryQueueItem? item1 = null;
        await foreach (var item in queue1.ConsumeAsync(cts.Token))
        {
            item1 = item;
            break;
        }

        InMemoryQueueItem? item2 = null;
        await foreach (var item in queue2.ConsumeAsync(cts.Token))
        {
            item2 = item;
            break;
        }

        InMemoryQueueItem? item3 = null;
        await foreach (var item in queue3.ConsumeAsync(cts.Token))
        {
            item3 = item;
            break;
        }

        try
        {
            Assert.NotNull(item1);
            Assert.NotNull(item2);
            Assert.NotNull(item3);
            Assert.Equal(new byte[] { 42 }, item1.Envelope.Body.Span.ToArray());
            Assert.Equal(new byte[] { 42 }, item2.Envelope.Body.Span.ToArray());
            Assert.Equal(new byte[] { 42 }, item3.Envelope.Body.Span.ToArray());
        }
        finally
        {
            item1?.Dispose();
            item2?.Dispose();
            item3?.Dispose();
        }
    }

    [Fact]
    public async Task Send_with_no_bindings_completes_without_error()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var topic = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "test-topic" });
        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([1, 2, 3]) };

        // act & assert - should complete without throwing
        await topic.SendAsync(envelope, CancellationToken.None);
    }

    [Fact]
    public async Task Send_through_chained_topics_delivers_to_final_queue()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var topicA = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "topic-a" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "topic-b" });
        var queue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });

        // Chain: Topic A -> Topic B -> Queue
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "topic-a",
                Destination = "topic-b",
                DestinationKind = InMemoryDestinationKind.Topic
            });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "topic-b",
                Destination = "test-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([99]) };

        // act
        await topicA.SendAsync(envelope, CancellationToken.None);

        // assert
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        InMemoryQueueItem? queueItem = null;

        await foreach (var item in queue.ConsumeAsync(cts.Token))
        {
            queueItem = item;
            break;
        }

        try
        {
            Assert.NotNull(queueItem);
            Assert.Equal(new byte[] { 99 }, queueItem.Envelope.Body.Span.ToArray());
        }
        finally
        {
            queueItem?.Dispose();
        }
    }

    [Fact]
    public async Task Send_with_cyclic_bindings_does_not_loop()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var topicA = topology!.AddTopic(new InMemoryTopicConfiguration { Name = "topic-a" });
        topology.AddTopic(new InMemoryTopicConfiguration { Name = "topic-b" });
        var queue = topology.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });

        // Create cycle: Topic A -> Topic B -> Topic A (cycle)
        // Also add: Topic A -> Queue (so we can verify message was delivered once)
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "topic-a",
                Destination = "topic-b",
                DestinationKind = InMemoryDestinationKind.Topic
            });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "topic-b",
                Destination = "topic-a",
                DestinationKind = InMemoryDestinationKind.Topic
            });
        topology.AddBinding(
            new InMemoryBindingConfiguration
            {
                Source = "topic-a",
                Destination = "test-queue",
                DestinationKind = InMemoryDestinationKind.Queue
            });

        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([123]) };

        // act - should complete without infinite loop
        await topicA.SendAsync(envelope, CancellationToken.None);

        // assert - verify message was delivered to queue exactly once
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var receivedCount = 0;
        var receivedItems = new List<InMemoryQueueItem>();

        try
        {
            await foreach (var item in queue.ConsumeAsync(cts.Token))
            {
                receivedItems.Add(item);
                receivedCount++;

                if (receivedCount == 1)
                {
                    // Wait a bit to ensure no duplicate deliveries
                    await Task.Delay(100, cts.Token);
                    break;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected if timeout occurs
        }

        try
        {
            Assert.Equal(1, receivedCount);
            Assert.Equal(new byte[] { 123 }, receivedItems[0].Envelope.Body.Span.ToArray());
        }
        finally
        {
            foreach (var item in receivedItems)
            {
                item.Dispose();
            }
        }
    }
}

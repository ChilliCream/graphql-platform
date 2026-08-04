using Microsoft.Extensions.DependencyInjection;
using Mocha.Middlewares;
using Mocha.Transport.InMemory.Tests.Helpers;

namespace Mocha.Transport.InMemory.Tests;

public class InMemoryQueueTests
{
    [Fact]
    public async Task Messages_Should_Be_Consumed_In_Send_Order()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var queue = topology!.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });

        var envelope1 = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([1]) };
        var envelope2 = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([2]) };
        var envelope3 = new MessageEnvelope { Body = new ReadOnlyMemory<byte>([3]) };

        // act
        await queue.SendAsync(envelope1, CancellationToken.None);
        await queue.SendAsync(envelope2, CancellationToken.None);
        await queue.SendAsync(envelope3, CancellationToken.None);

        // assert
        var items = new List<byte>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var count = 0;

        try
        {
            await foreach (var item in queue.ConsumeAsync(cts.Token))
            {
                items.Add(item.Envelope.Body.Span[0]);
                item.Dispose();

                count++;
                if (count == 3)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when we cancel after reading all messages
        }

        Assert.Equal(new byte[] { 1, 2, 3 }, items);
    }

    [Fact]
    public async Task Queue_Item_Body_Should_Be_Independent_Copy()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var queue = topology!.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });

        var originalData = "*+,"u8.ToArray();
        var envelope = new MessageEnvelope { Body = new ReadOnlyMemory<byte>(originalData) };

        // act
        await queue.SendAsync(envelope, CancellationToken.None);

        // Modify the original data
        originalData[0] = 99;
        originalData[1] = 98;
        originalData[2] = 97;

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
            var queueItemBody = queueItem.Envelope.Body.Span;

            // Original data was modified to [99, 98, 97]
            // But queue item should still have [42, 43, 44]
            Assert.Equal("*+,"u8.ToArray(), queueItemBody.ToArray());
        }
        finally
        {
            queueItem?.Dispose();
        }
    }

    [Fact]
    public async Task Concurrent_Sends_Should_Not_Lose_Messages()
    {
        // arrange
        var runtime = new ServiceCollection().AddMessageBus().AddInMemory().BuildRuntime();
        var transport = runtime.Transports.OfType<InMemoryMessagingTransport>().Single();
        var topology = transport.Topology as InMemoryMessagingTopology;

        var queue = topology!.AddQueue(new InMemoryQueueConfiguration { Name = "test-queue" });

        const int messageCount = 100;
        var envelopes = Enumerable
            .Range(0, messageCount)
            .Select(i => new MessageEnvelope { Body = new ReadOnlyMemory<byte>([(byte)(i % 256)]) })
            .ToList();

        // act - send messages concurrently from multiple tasks
        var sendTasks = envelopes.Select(
            (envelope, _) => Task.Run(async () => await queue.SendAsync(envelope, CancellationToken.None)));

        await Task.WhenAll(sendTasks);

        // assert - consume all messages and verify count
        var receivedCount = 0;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

        try
        {
            await foreach (var item in queue.ConsumeAsync(cts.Token))
            {
                item.Dispose();
                receivedCount++;

                if (receivedCount == messageCount)
                {
                    await cts.CancelAsync();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when we cancel after reading all messages
        }

        Assert.Equal(messageCount, receivedCount);
    }
}

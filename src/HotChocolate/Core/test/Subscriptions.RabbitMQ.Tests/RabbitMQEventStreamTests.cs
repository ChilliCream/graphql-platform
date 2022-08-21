using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HotChocolate.Execution;
using HotChocolate.Subscriptions.RabbitMQ.Serialization;
using Moq;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using Xunit;

namespace HotChocolate.Subscriptions.RabbitMQ;

public class RabbitMQEventStreamTests
{
    [Fact]
    public async Task Dispose()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);

        bool empty = false;
        ActiveConsumer activeConsumer = new(consumer, () => empty = true);

        ISourceStream<string> sut1 = new RabbitMQEventStream<string>(new JsonSerializer(), activeConsumer);
        ISourceStream<string> sut2 = new RabbitMQEventStream<string>(new JsonSerializer(), activeConsumer);

        Assert.False(empty);

        await sut2.DisposeAsync();
        await sut1.DisposeAsync();

        Assert.True(empty);
    }

    [Fact]
    public async Task ReadEventsAsync_DisposeMidway()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);

        bool empty = false;
        ActiveConsumer activeConsumer = new(consumer, () => empty = true);

        ISourceStream<string> sut = new RabbitMQEventStream<string>(new JsonSerializer(), activeConsumer);

        ValueTask<string[]> task = sut.ReadEventsAsync().ToArrayAsync();

        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("first"));
        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("second"));

        Assert.False(empty);
        await sut.DisposeAsync();
        Assert.True(empty);

        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("won't be seen"));
        
        Assert.Equal(new[] { "first", "second" }, await task);
    }

    [Fact]
    public async Task ReadEventsAsync_Completed()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);

        bool empty = false;
        ActiveConsumer activeConsumer = new(consumer, () => empty = true);

        ISourceStream<string> sut = new RabbitMQEventStream<string>(new JsonSerializer(), activeConsumer);

        ValueTask<string[]> task = sut.ReadEventsAsync().ToArrayAsync();

        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("first"));
        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("second"));

        Assert.False(empty);
        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes(WellKnownMessages.Completed));
        Assert.True(empty);

        await consumer.HandleBasicDeliver("test", 1, false, "test-ex", "", null, Encoding.UTF8.GetBytes("won't be seen"));
        
        Assert.Equal(new[] { "first", "second" }, await task);
    }

    [Fact]
    public async Task ReadEventsAsync_Cancel()
    {
        Mock<IModel> channel = new();
        AsyncEventingBasicConsumer consumer = new(channel.Object);

        bool empty = false;
        ActiveConsumer activeConsumer = new(consumer, () => empty = true);

        ISourceStream<string> sut = new RabbitMQEventStream<string>(new JsonSerializer(), activeConsumer);

        CancellationTokenSource src1 = new();
        src1.Cancel();

        await Assert.ThrowsAsync<TaskCanceledException>(async () =>
        {
            await foreach (string msg in sut.ReadEventsAsync().WithCancellation(src1.Token))
            {

            }
        });
        Assert.False(empty);
    }
}

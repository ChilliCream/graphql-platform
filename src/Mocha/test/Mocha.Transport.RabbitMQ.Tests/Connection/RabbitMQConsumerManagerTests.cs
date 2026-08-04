using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public class RabbitMQConsumerManagerTests
{
    [Fact]
    public async Task RegisterConsumerAsync_Should_TrackConsumer_When_Called()
    {
        // arrange
        await using var manager = CreateManager();

        // act
        var registration = await manager.RegisterConsumerAsync(
            "test-queue",
            (_, _, _) => default,
            prefetchCount: 10,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        // assert
        Assert.NotNull(registration);
        Assert.IsAssignableFrom<IAsyncDisposable>(registration);
    }

    [Fact]
    public async Task AddConsumerAsync_Should_BeThreadSafe_When_ConcurrentCalls()
    {
        // arrange
        await using var manager = CreateManager();
        var tasks = new Task<IAsyncDisposable>[10];

        // act
        for (var i = 0; i < 10; i++)
        {
            var queueName = $"queue-{i}";
            tasks[i] = manager.RegisterConsumerAsync(
                queueName,
                (_, _, _) => default,
                prefetchCount: 10,
                consumerDispatchConcurrency: 1,
                CancellationToken.None);
        }

        var results = await Task.WhenAll(tasks);

        // assert
        Assert.Equal(10, results.Length);
        Assert.All(results, Assert.NotNull);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeAllConsumers_When_Called()
    {
        // arrange
        var manager = CreateManager();

        var registrations = new IAsyncDisposable[3];
        for (var i = 0; i < 3; i++)
        {
            registrations[i] = await manager.RegisterConsumerAsync(
                $"queue-{i}",
                (_, _, _) => default,
                prefetchCount: 10,
                consumerDispatchConcurrency: 1,
                CancellationToken.None);
        }

        // act
        await manager.DisposeAsync();

        // assert - registering after dispose via the internal method should still work
        // (the manager clears its list during dispose).
        // We verify dispose completes without error.
        // The consumers' internal state (ConsumerTag/Channel) should be null after dispose.
        foreach (var reg in registrations)
        {
            var consumer = (RabbitMQConsumerManager.RegisteredConsumer)reg;
            Assert.Null(consumer.ConsumerTag);
            Assert.Null(consumer.Channel);
        }
    }

    [Fact]
    public async Task DisposeAsync_Should_HandleConsumerDisposeFailure_When_ConsumerThrows()
    {
        // arrange
        var channelMock = new Mock<IChannel>();
        channelMock.SetupGet(c => c.IsOpen).Returns(true);
        channelMock
            .Setup(c => c.BasicCancelAsync(It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromException(new InvalidOperationException("cancel failed")));

        var manager = CreateManager();

        var registration = await manager.RegisterConsumerAsync(
            "test-queue",
            (_, _, _) => default,
            prefetchCount: 10,
            consumerDispatchConcurrency: 1,
            CancellationToken.None);

        // Manually set channel to simulate a connected consumer
        var consumer = (RabbitMQConsumerManager.RegisteredConsumer)registration;
        consumer.Channel = channelMock.Object;
        consumer.ConsumerTag = "tag-1";

        // act - should not throw despite consumer dispose failure
        var exception = await Record.ExceptionAsync(() => manager.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }

    private static RabbitMQConsumerManager CreateManager()
    {
        var connectionMock = new Mock<IConnection>();
        connectionMock.SetupGet(c => c.IsOpen).Returns(false);

        return new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            _ => new ValueTask<IConnection>(connectionMock.Object));
    }
}

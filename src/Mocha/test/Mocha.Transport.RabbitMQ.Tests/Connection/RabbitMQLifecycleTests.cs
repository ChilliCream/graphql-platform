using System.Diagnostics;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using Moq;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQLifecycleTests
{
    [Fact]
    public async Task DisposeAsync_Should_FailWaitingRents_When_ConnectionAttemptIsPending()
    {
        // arrange
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcher = TestManagers.CreateDispatcher(
            async ct =>
            {
                entered.TrySetResult();
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                finally
                {
                    await release.Task;
                }

                throw new UnreachableException();
            },
            static (_, _) => Task.CompletedTask);
        var starting = dispatcher.StartAsync(TestContext.Current.CancellationToken);
        var first = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();
        await entered.Task.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        var second = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();

        // act
        var disposing = dispatcher.DisposeAsync().AsTask();
        bool disposeWaitedForAttempt;
        try
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => second.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));
            disposeWaitedForAttempt = !disposing.IsCompleted;
        }
        finally
        {
            release.TrySetResult();
            await disposing.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.True(disposeWaitedForAttempt);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => first);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => starting);
        Assert.False(dispatcher.IsConnected);
    }

    [Fact]
    public async Task RentChannelAsync_Should_ThrowObjectDisposed_When_Disposed()
    {
        // arrange
        var dispatcher = TestManagers.CreateDispatcher(NeverConnectAsync, static (_, _) => Task.CompletedTask);
        await dispatcher.DisposeAsync();

        // act
        var exception = await Record.ExceptionAsync(
            () => dispatcher.RentChannelAsync(TestContext.Current.CancellationToken).AsTask());

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_RejectZeroConcurrency_When_NoHandlerCouldRun()
    {
        // arrange
        await using var manager = TestManagers.CreateConsumerManager(NeverConnectAsync);

        // act
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => manager.RegisterConsumerAsync(
            "orders",
            static (_, _, _) => default,
            1,
            0,
            TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("maxConcurrency", exception.ParamName);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_RejectRegistration_When_ManagerDisposed()
    {
        // arrange
        var manager = TestManagers.CreateConsumerManager(NeverConnectAsync);
        await manager.DisposeAsync();

        // act
        var exception = await Record.ExceptionAsync(() => manager.RegisterConsumerAsync(
            "orders",
            static (_, _, _) => default,
            1,
            1,
            TestContext.Current.CancellationToken));

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
        Assert.Equal("The consumer connection is unavailable.", manager.HealthFailureReason);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_ReturnDetachedConsumer_When_NoConnectionIsReady()
    {
        // arrange
        await using var manager = TestManagers.CreateConsumerManager(NeverConnectAsync);

        // act
        var consumer = await manager.RegisterConsumerAsync(
            "orders",
            static (_, _, _) => default,
            1,
            1,
            TestContext.Current.CancellationToken);

        // assert
        Assert.False(consumer.IsAttached);
        Assert.Equal("The consumer is not attached.", consumer.HealthFailureReason);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_FailTheFirstAttempt_When_ItsRequestsTogetherExceedAttachTimeout()
    {
        // arrange
        // the preparation and the request that follows each take less than the timeout, and together more
        var requestDuration = TimeSpan.FromSeconds(2);
        var channel = new Mock<IChannel>();
        channel.SetupGet(c => c.IsOpen).Returns(true);
        channel
            .Setup(c => c.BasicQosAsync(
                It.IsAny<uint>(),
                It.IsAny<ushort>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>()))
            .Returns(() => Task.Delay(requestDuration));
        var connection = new Mock<IConnection>();
        connection.SetupGet(c => c.IsOpen).Returns(true);
        connection
            .Setup(c => c.CreateChannelAsync(It.IsAny<CreateChannelOptions?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(channel.Object);
        await using var manager = TestManagers.CreateConsumerManager(
            _ => new ValueTask<IConnection>(connection.Object));
        manager.AttachTimeout = TimeSpan.FromSeconds(3);
        await manager.StartAsync(TestContext.Current.CancellationToken);

        // act
        var consumer = await manager.RegisterConsumerAsync(
            "orders",
            (_, _, ct) => Task.Delay(requestDuration, ct),
            static (_, _, _) => default,
            maxPrefetch: 1,
            maxConcurrency: 1,
            TestContext.Current.CancellationToken);
        var failure = await consumer.FirstAttempt;

        // assert
        var timeout = Assert.IsType<TimeoutException>(failure);
        Assert.Equal("Attaching the consumer for queue 'orders' did not complete within 00:00:03.", timeout.Message);
    }

    [Fact]
    public async Task WaitUntilAttachedAsync_Should_EndEveryWait_When_ConsumerStops()
    {
        // arrange
        await using var manager = TestManagers.CreateConsumerManager(NeverConnectAsync);
        var consumer = await manager.RegisterConsumerAsync(
            "orders",
            static (_, _, _) => default,
            1,
            1,
            TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        var first = consumer.WaitUntilAttachedAsync(cancelled.Token).AsTask();
        var second = consumer.WaitUntilAttachedAsync(CancellationToken.None).AsTask();

        // act
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var otherWaiterPending = !second.IsCompleted;
        await consumer.StopAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(otherWaiterPending);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => second.WaitAsync(TestTimeouts.Signal, TestContext.Current.CancellationToken));
        Assert.Equal("The consumer for queue 'orders' is stopped.", exception.Message);
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_ConsumerWasStopped()
    {
        // arrange
        await using var manager = TestManagers.CreateConsumerManager(NeverConnectAsync);
        var consumer = await manager.RegisterConsumerAsync(
            "orders",
            static (_, _, _) => default,
            1,
            1,
            TestContext.Current.CancellationToken);
        await consumer.StopAsync(TestContext.Current.CancellationToken);

        // act
        var exception = await Record.ExceptionAsync(() => consumer.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
        Assert.Equal("The consumer is stopped.", consumer.HealthFailureReason);
    }

    private static async ValueTask<IConnection> NeverConnectAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
        throw new UnreachableException();
    }
}

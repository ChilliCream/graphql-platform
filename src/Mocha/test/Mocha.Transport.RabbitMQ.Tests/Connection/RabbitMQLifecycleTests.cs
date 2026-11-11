using System.Diagnostics;
using Mocha.Transport.RabbitMQ.Tests.Helpers;
using RabbitMQ.Client;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQLifecycleTests
{
    private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(5);

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
        var first = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();
        await entered.Task.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        var second = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();

        // act
        var disposing = dispatcher.DisposeAsync().AsTask();
        bool disposeWaitedForAttempt;
        try
        {
            await Assert.ThrowsAsync<ObjectDisposedException>(
                () => second.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
            disposeWaitedForAttempt = !disposing.IsCompleted;
        }
        finally
        {
            release.TrySetResult();
            await disposing.WaitAsync(s_timeout, TestContext.Current.CancellationToken);
        }

        // assert
        Assert.True(disposeWaitedForAttempt);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => first);
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
            () => second.WaitAsync(s_timeout, TestContext.Current.CancellationToken));
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

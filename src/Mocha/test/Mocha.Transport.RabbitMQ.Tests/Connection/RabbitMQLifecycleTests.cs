using Microsoft.Extensions.Logging.Abstractions;

namespace Mocha.Transport.RabbitMQ.Tests.Connection;

public sealed class RabbitMQLifecycleTests
{
    [Fact]
    public async Task DisposeAsync_Should_CompleteWaitingRents_When_ConnectionFactoryIsCancelled()
    {
        // arrange
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var dispatcher = new RabbitMQDispatcher(
            NullLogger<RabbitMQDispatcher>.Instance,
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

                throw new InvalidOperationException("Unreachable.");
            },
            static (_, _) => Task.CompletedTask);
        var first = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        var second = dispatcher.RentChannelAsync(CancellationToken.None).AsTask();

        // act
        var disposing = dispatcher.DisposeAsync().AsTask();
        try
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(
                () => second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
            Assert.False(disposing.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await disposing.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        }

        // assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        Assert.False(dispatcher.IsConnected);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_RejectZeroConcurrency_When_NoHandlerCouldRun()
    {
        // arrange
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            static _ => throw new InvalidOperationException("No connection expected."));

        // act
        var exception = await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => manager.RegisterConsumerAsync(
            "orders", static (_, _, _) => default, 1, 0, TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("consumerDispatchConcurrency", exception.ParamName);
    }

    [Fact]
    public async Task RegisterConsumerAsync_Should_RejectRegistration_When_ManagerDisposed()
    {
        // arrange
        var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            static _ => throw new InvalidOperationException("No connection expected."));
        await manager.DisposeAsync();

        // act
        var exception = await Record.ExceptionAsync(() => manager.RegisterConsumerAsync(
            "orders", static (_, _, _) => default, 1, 1, TestContext.Current.CancellationToken));

        // assert
        Assert.IsType<ObjectDisposedException>(exception);
        Assert.False(manager.IsHealthy);
    }

    [Fact]
    public async Task WaitUntilAttachedAsync_Should_CancelAllWaiters_When_RegistrationStops()
    {
        // arrange
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            static _ => throw new InvalidOperationException("No connection expected."));
        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            "orders", static (_, _, _) => default, 1, 1, TestContext.Current.CancellationToken);
        using var cancelled = new CancellationTokenSource();
        var first = registration.WaitUntilAttachedAsync(cancelled.Token).AsTask();
        var second = registration.WaitUntilAttachedAsync(CancellationToken.None).AsTask();

        // act
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => first);
        var otherWaiterPending = !second.IsCompleted;
        await registration.StopAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(otherWaiterPending);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => second.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_When_RegistrationWasStopped()
    {
        // arrange
        await using var manager = new RabbitMQConsumerManager(
            NullLogger<RabbitMQConsumerManager>.Instance,
            static _ => throw new InvalidOperationException("No connection expected."));
        var registration = (RabbitMQConsumerManager.RegisteredConsumer)await manager.RegisterConsumerAsync(
            "orders", static (_, _, _) => default, 1, 1, TestContext.Current.CancellationToken);
        await registration.StopAsync(TestContext.Current.CancellationToken);

        // act
        var exception = await Record.ExceptionAsync(() => registration.DisposeAsync().AsTask());

        // assert
        Assert.Null(exception);
    }
}

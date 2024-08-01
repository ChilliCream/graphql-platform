using System;
using System.Threading;
using System.Threading.Tasks;
using Moq;

namespace HotChocolate.Subscriptions.Postgres;

public class BackgroundTaskTests
{
    [Fact]
    public async Task DisposeAsync_Should_CancelAndDisposeCompletion_When_Invoked()
    {
        // Arrange
        CancellationToken cancellationRequested = default;
        var handler = new Func<CancellationToken, Task>((token) =>
        {
            cancellationRequested = token;
            return Task.CompletedTask;
        });

        var backgroundTask = new ContinuousTask(handler, TimeProvider.System);

        // Act
        await backgroundTask.DisposeAsync();

        // Assert
        Assert.True(cancellationRequested.IsCancellationRequested);
    }

    [Fact]
    public async Task DisposeAsync_Should_ExecuteTask_When_Initialized()
    {
        // Arrange
        var handlerCalled = false;
        var handler = new Func<CancellationToken, Task>(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        var backgroundTask = new ContinuousTask(handler, TimeProvider.System);

        // Act
        await backgroundTask.DisposeAsync();

        // Assert
        Assert.True(handlerCalled);
    }

    [Fact]
    public async Task RunContinuously_Should_ExecuteHandler_When_NotCancelled()
    {
        // Arrange
        var handlerCalled = false;
        var handler = new Func<CancellationToken, Task>(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        var backgroundTask = new ContinuousTask(handler, TimeProvider.System);

        // Act
        SpinWait.SpinUntil(() => handlerCalled, TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(handlerCalled);

        // Cleanup
        await backgroundTask.DisposeAsync();
    }

    [Fact]
    public async Task RunContinuously_Should_NotExecuteHandler_When_Cancelled()
    {
        // Arrange
        var handlerCalled = false;
        var handler = new Func<CancellationToken, Task>(_ =>
        {
            handlerCalled = true;
            return Task.CompletedTask;
        });

        var backgroundTask = new ContinuousTask(handler, TimeProvider.System);
        await backgroundTask.DisposeAsync();

        handlerCalled = false;

        // Act
        await Task.Delay(100); // Give some time to start the background task

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task RunContinuously_Should_WaitForASecond_When_HandlerThrowsException()
    {
        // Arrange
        var hitCount = 0;
        var handler = new Func<CancellationToken, Task>(_ =>
        {
            hitCount++;

            throw new Exception("First call exception");
        });

        var mockTimeProvider = new Mock<TimeProvider>();
        var backgroundTask = new ContinuousTask(handler, mockTimeProvider.Object);

        // Act
        SpinWait.SpinUntil(() => hitCount == 1, TimeSpan.FromSeconds(5));

        // Assert
        mockTimeProvider.Verify(
            tp => tp.CreateTimer(
                It.IsAny<TimerCallback>(),
                It.IsAny<object>(),
                TimeSpan.FromSeconds(1),
                Timeout.InfiniteTimeSpan),
            Times.Once);

        // Cleanup
        await backgroundTask.DisposeAsync();
    }
}

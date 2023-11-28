using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

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

        var backgroundTask = new ContinuousTask(handler);

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

        var backgroundTask = new ContinuousTask(handler);

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

        var backgroundTask = new ContinuousTask(handler);

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

        var backgroundTask = new ContinuousTask(handler);
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
        var stopwatch = new Stopwatch();
        var handler = new Func<CancellationToken, Task>(async (token) =>
        {
            hitCount++;

            if (hitCount == 1)
            {
                stopwatch.Start();
                throw new Exception("First call exception");
            }

            if (hitCount == 2)
            {
                stopwatch.Stop();
            }

            if (hitCount > 2)
            {
                await Task.Delay(-1, token); // Wait indefinitely
            }
        });

        var backgroundTask = new ContinuousTask(handler);

        // Act
        SpinWait.SpinUntil(() => hitCount == 2, TimeSpan.FromSeconds(5));

        // Assert
        Assert.InRange(stopwatch.ElapsedMilliseconds, 1000, 2000);

        // Cleanup
        await backgroundTask.DisposeAsync();
    }
}

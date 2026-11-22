using Microsoft.Extensions.Time.Testing;

namespace HotChocolate.Subscriptions.Postgres;

public class BackgroundTaskTests
{
    [Fact]
    public async Task DisposeAsync_Should_CancelAndDisposeCompletion_When_Invoked()
    {
        // Arrange
        var cancellationRequested = CancellationToken.None;
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
    public async Task DisposeAsync_Should_WaitForHandler_When_HandlerIsRunning()
    {
        // arrange
        var handlerStopped = false;
        var handler = new Func<CancellationToken, Task>(async cancellationToken =>
        {
            try
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // the handler stays busy for a moment after it was cancelled
                await Task.Delay(100, CancellationToken.None);
                handlerStopped = true;
            }
        });
        var backgroundTask = new ContinuousTask(handler, TimeProvider.System);

        // act
        await backgroundTask.DisposeAsync().AsTask().WaitAsync(
            TimeSpan.FromSeconds(10),
            TestContext.Current.CancellationToken);

        // assert
        Assert.True(handlerStopped);
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
        await Task.Delay(100, TestContext.Current.CancellationToken); // Give some time to start the background task

        // Assert
        Assert.False(handlerCalled);
    }

    [Fact]
    public async Task RunContinuously_Should_WaitForASecond_When_HandlerThrowsException()
    {
        // arrange
        var hitCount = 0;
        var secondAttempt = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handler = new Func<CancellationToken, Task>(_ =>
        {
            if (Interlocked.Increment(ref hitCount) == 2)
            {
                secondAttempt.TrySetResult();
            }

            throw new Exception("First call exception");
        });
        var timeProvider = new FakeTimeProvider();
        await using var backgroundTask = new ContinuousTask(handler, timeProvider);

        // act
        timeProvider.Advance(TimeSpan.FromMilliseconds(999));
        var attemptsBeforeRetry = Volatile.Read(ref hitCount);
        timeProvider.Advance(TimeSpan.FromMilliseconds(1));
        await secondAttempt.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(1, attemptsBeforeRetry);
        Assert.Equal(2, Volatile.Read(ref hitCount));
    }
}

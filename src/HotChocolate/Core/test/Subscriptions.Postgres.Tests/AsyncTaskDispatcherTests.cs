namespace HotChocolate.Subscriptions.Postgres;

public class AsyncTaskDispatcherTests
{
    [Fact]
    public async Task Dispatch_Should_InvokeHandler()
    {
        // Arrange
        var wasHandlerCalled = false;
        Func<CancellationToken, Task> handler = _ =>
        {
            wasHandlerCalled = true;
            return Task.CompletedTask;
        };

        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        await asyncEventHandler.Initialize(CancellationToken.None);
        wasHandlerCalled = false;

        // Act
        asyncEventHandler.Dispatch();

        SpinWait.SpinUntil(() => wasHandlerCalled, TimeSpan.FromSeconds(1));

        // Assert
        Assert.True(wasHandlerCalled);
    }

    [Fact]
    public async Task Dispatch_Should_Throw_When_CalledAfterDispose()
    {
        // Arrange
        var asyncEventHandler = new AsyncTaskDispatcher(_ => Task.CompletedTask);
        await asyncEventHandler.Initialize(CancellationToken.None);

        // Act
        await asyncEventHandler.DisposeAsync();

        // Assert
        Assert.Throws<ObjectDisposedException>(() => asyncEventHandler.Dispatch());
    }

    [Fact]
    public async Task DisposeAsync_Should_CancelAndDisposeCompletion()
    {
        // Arrange
        Func<CancellationToken, Task> handler = _ => Task.CompletedTask;

        var asyncEventHandler = new AsyncTaskDispatcher(handler);
        await asyncEventHandler.Initialize(CancellationToken.None);

        // Act
        // Assert
        await asyncEventHandler.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_Should_CancelHandlerTask()
    {
        // Arrange
        var taskCompletionSource = new TaskCompletionSource<bool>();
        Func<CancellationToken, Task> handler = _ => taskCompletionSource.Task;
        var asyncEventHandler = new AsyncTaskDispatcher(handler);
        var handlerTask = asyncEventHandler.Initialize(CancellationToken.None);

        // Act
        await asyncEventHandler.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await handlerTask);
    }

    [Fact]
    public async Task Initialize_Should_Throw_When_CalledAfterDispose()
    {
        // Arrange
        var asyncEventHandler = new AsyncTaskDispatcher(_ => Task.CompletedTask);

        // Act
        await asyncEventHandler.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await asyncEventHandler.Initialize(CancellationToken.None));
    }

    [Fact]
    public async Task Initialize_Should_CompleteInitialization()
    {
        // Arrange
        var wasHandlerCalled = false;
        Func<CancellationToken, Task> handler = _ =>
        {
            wasHandlerCalled = true;
            return Task.CompletedTask;
        };

        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        // Act
        await asyncEventHandler.Initialize(CancellationToken.None);

        // Assert
        Assert.True(wasHandlerCalled);
    }

    [Fact]
    public async Task Initialize_Should_Throw_When_CalledAfterInitAndDispose()
    {
        // Arrange
        var asyncEventHandler = new AsyncTaskDispatcher(_ => Task.CompletedTask);
        await asyncEventHandler.Initialize(CancellationToken.None);

        // Act
        await asyncEventHandler.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            async () => await asyncEventHandler.Initialize(CancellationToken.None));
    }

    [Fact]
    public async Task Initialize_Should_NotThrowException_When_CalledMultipleTimes()
    {
        // Arrange
        Func<CancellationToken, Task> handler = _ => Task.CompletedTask;
        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        // Act
        await asyncEventHandler.Initialize(CancellationToken.None);

        // Assert
        var exception = await Record.ExceptionAsync(async ()
            => await asyncEventHandler.Initialize(CancellationToken.None));
        Assert.Null(exception);
    }

    [Fact]
    public async Task Initialize_Should_OnlyInitializeOnce_When_InitializeCalledConcurrently()
    {
        // Arrange
        var initializeCount = 0;
        Func<CancellationToken, Task> handler = _ =>
        {
            Interlocked.Increment(ref initializeCount);
            return Task.CompletedTask;
        };
        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        // Act
        var task1 = asyncEventHandler.Initialize(CancellationToken.None);
        var task2 = asyncEventHandler.Initialize(CancellationToken.None);
        await Task.WhenAll(task1, task2);

        // Assert
        Assert.Equal(1, initializeCount);
    }

    [Fact]
    public async Task Initialize_Should_Complete_When_CancellationTokenCancelled()
    {
        // Arrange
        var neverEnding = new TaskCompletionSource();
        var cancellationTokenSource = new CancellationTokenSource();
        Func<CancellationToken, Task> handler = _ => neverEnding.Task;
        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        // Act
        cancellationTokenSource.Cancel();
        var task = asyncEventHandler.Initialize(cancellationTokenSource.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }

    [Fact]
    public async Task Initialize_Should_Complete_When_Disposed()
    {
        // Arrange
        var neverEnding = new TaskCompletionSource();
        Func<CancellationToken, Task> handler = _ => neverEnding.Task;
        var asyncEventHandler = new AsyncTaskDispatcher(handler);

        // Act
        var task = asyncEventHandler.Initialize(CancellationToken.None);
        await asyncEventHandler.DisposeAsync();

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(async () => await task);
    }
}

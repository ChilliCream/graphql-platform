namespace HotChocolate.Subscriptions.Postgres;

public class AsyncAutoResetEventTests
{
    [Fact]
    public async Task Set_Should_SetResult_When_Called()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();

        // Act
        var waitTask = autoResetEvent.WaitAsync(CancellationToken.None);
        autoResetEvent.Set();
        await waitTask;

        // Assert
        Assert.True(waitTask.IsCompleted);
    }

    [Fact]
    public void Set_Should_Throw_When_Disposed()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();
        autoResetEvent.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => autoResetEvent.Set());
    }

    [Fact]
    public async Task WaitAsync_Should_Throw_When_Disposed()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();
        autoResetEvent.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(()
            => autoResetEvent.WaitAsync(CancellationToken.None));
    }

    [Fact]
    public void WaitAsync_Should_ReturnCompletedTask_When_PreviouslySet()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();
        autoResetEvent.Set();

        // Act
        var task = autoResetEvent.WaitAsync(CancellationToken.None);

        // Assert
        Assert.Equal(TaskStatus.RanToCompletion, task.Status);
    }

    [Fact]
    public void Set_Should_NotThrow_When_MultipleSetsCalled()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();

        // Act & Assert
        autoResetEvent.Set();
        autoResetEvent.Set();
    }

    [Fact]
    public async Task Set_Should_OnlyAllowOneTaskToComplete_When_MultipleWaitsAndSingleSet()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();

        // Act
        var task1 = autoResetEvent.WaitAsync(CancellationToken.None);
        var task2 = autoResetEvent.WaitAsync(CancellationToken.None);
        autoResetEvent.Set();
        await Task.Delay(100); // Delay to allow the event to propagate

        // Assert
        Assert.True(task1.IsCompleted ^ task2.IsCompleted);
    }

    [Fact]
    public async Task Set_Should_AllowMultipleTasksToComplete_When_MultipleWaitsAndMultipleSets()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();

        // Act
        var task1 = autoResetEvent.WaitAsync(CancellationToken.None);
        var task2 = autoResetEvent.WaitAsync(CancellationToken.None);
        autoResetEvent.Set();
        autoResetEvent.Set();
        await Task.Delay(100); // Delay to allow the event to propagate

        // Assert
        Assert.True(task1.IsCompleted && task2.IsCompleted);
    }

    [Fact]
    public async Task Dispose_Should_CancelTask_When_DisposedDuringWait()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();

        // Act
        var task = autoResetEvent.WaitAsync(CancellationToken.None);
        autoResetEvent.Dispose();
        await Task.Delay(100); // Delay to allow the event to propagate

        // Assert
        Assert.True(task.IsCanceled);
    }

    [Fact]
    public async Task WaitAsync_Should_ReturnFalse_When_CancellationTokenCancelledBeforeSet()
    {
        // Arrange
        var autoResetEvent = new AsyncAutoResetEvent();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        // Act
        var task = autoResetEvent.WaitAsync(cancellationTokenSource.Token);

        // Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
    }
}

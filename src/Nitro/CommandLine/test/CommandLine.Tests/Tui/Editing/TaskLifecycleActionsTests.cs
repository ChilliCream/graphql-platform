using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class TaskLifecycleActionsTests
{
    [Fact]
    public async Task CloseAsync_Should_CallStoreWithReason_And_ReturnSucceeded()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var store = new FakeTaskStore { ResultTask = TaskItemBuilder.Create("a1", "Title", TaskStates.Closed) };

        // act
        var outcome = await TaskLifecycleActions.CloseAsync(store, task, "done", "me", CancellationToken.None);

        // assert
        Assert.Equal(["a1"], store.ClosedIds);
        Assert.Equal("done", store.ClosedReason);
        var succeeded = Assert.IsType<TaskLifecycleOutcome.Succeeded>(outcome);
        Assert.Equal(TaskLifecycleAction.Close, succeeded.Action);
        Assert.False(succeeded.ShouldPopDetail);
    }

    [Fact]
    public async Task CloseAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("already closed") };

        // act
        var outcome = await TaskLifecycleActions.CloseAsync(store, task, "", "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskLifecycleOutcome.Failed>(outcome);
        Assert.Equal(TaskLifecycleAction.Close, failed.Action);
        Assert.Equal("already closed", failed.ToastText);
    }

    [Fact]
    public async Task ReopenAsync_Should_ReturnFailedWithoutCallingStore_When_TaskNotClosed()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", TaskStates.Open);
        var store = new FakeTaskStore();

        // act
        var outcome = await TaskLifecycleActions.ReopenAsync(store, task, "", "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskLifecycleOutcome.Failed>(outcome);
        Assert.Equal(TaskLifecycleAction.Reopen, failed.Action);
        Assert.Null(store.ReopenedId);
    }

    [Fact]
    public async Task ReopenAsync_Should_CallStoreWithReason_And_ReturnSucceeded_When_TaskClosed()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", TaskStates.Closed);
        var store = new FakeTaskStore { ResultTask = TaskItemBuilder.Create("a1", "Title", TaskStates.Open) };

        // act
        var outcome = await TaskLifecycleActions.ReopenAsync(store, task, "reconsidered", "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.ReopenedId);
        Assert.Equal("reconsidered", store.ReopenedReason);
        var succeeded = Assert.IsType<TaskLifecycleOutcome.Succeeded>(outcome);
        Assert.Equal(TaskLifecycleAction.Reopen, succeeded.Action);
        Assert.False(succeeded.ShouldPopDetail);
    }

    [Fact]
    public async Task ReopenAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", TaskStates.Closed);
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("not closed") };

        // act
        var outcome = await TaskLifecycleActions.ReopenAsync(store, task, "", "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskLifecycleOutcome.Failed>(outcome);
        Assert.Equal("not closed", failed.ToastText);
    }

    [Fact]
    public async Task DeleteAsync_Should_CallStoreWithReason_And_ReturnSucceeded_WithDetailPop()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var store = new FakeTaskStore { ResultTask = TaskItemBuilder.Create("a1", "Title", TaskStates.Tombstone) };

        // act
        var outcome = await TaskLifecycleActions.DeleteAsync(store, task, "obsolete", "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.DeletedId);
        Assert.Equal("obsolete", store.DeletedReason);
        var succeeded = Assert.IsType<TaskLifecycleOutcome.Succeeded>(outcome);
        Assert.Equal(TaskLifecycleAction.Delete, succeeded.Action);
        Assert.True(succeeded.ShouldPopDetail);
    }

    [Fact]
    public async Task DeleteAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title");
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("not found") };

        // act
        var outcome = await TaskLifecycleActions.DeleteAsync(store, task, "", "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskLifecycleOutcome.Failed>(outcome);
        Assert.Equal("not found", failed.ToastText);
    }

    [Theory]
    [InlineData(TaskStates.Open, false)]
    [InlineData(TaskStates.InProgress, false)]
    [InlineData(TaskStates.Closed, true)]
    public void CanReopen_Should_ReturnTrue_OnlyWhenClosed(string status, bool expected)
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", "Title", status);

        // act
        var canReopen = TaskLifecycleActions.CanReopen(task);

        // assert
        Assert.Equal(expected, canReopen);
    }

    [Fact]
    public void ToShowToast_Should_UseSuccessStyle_ForSucceeded()
    {
        // arrange
        var outcome = new TaskLifecycleOutcome.Succeeded(
            TaskLifecycleAction.Close, TaskItemBuilder.Create("a1"), "Closed task 'a1'.");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("Closed task 'a1'.", toast.Text);
        Assert.Equal(ToastStyle.Success, toast.Style);
    }

    [Fact]
    public void ToShowToast_Should_UseErrorStyle_ForFailed()
    {
        // arrange
        var outcome = new TaskLifecycleOutcome.Failed(TaskLifecycleAction.Delete, "not found");

        // act
        var toast = outcome.ToShowToast();

        // assert
        Assert.Equal("not found", toast.Text);
        Assert.Equal(ToastStyle.Error, toast.Style);
    }
}

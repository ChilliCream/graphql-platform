using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class StatusPickerTests
{
    [Fact]
    public void Create_Should_PreSelectTasksCurrentStatus()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Blocked);

        // act
        var picker = StatusPicker.Create(task);
        var applied = Assert.IsType<QuickPickerResult.Applied>(
            picker.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)));

        // assert
        Assert.Equal(TaskStates.Blocked, applied.SelectedId);
    }

    [Fact]
    public async Task ApplyAsync_Should_CallUpdateTaskAsync_WithStatusGiven()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Open);
        var store = new FakeTaskStore { UpdateResult = new TaskUpdateResult { ChangedFields = ["status"] } };

        // act
        var outcome = await StatusPicker.ApplyAsync(
            store, task, TaskStates.InProgress, "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.UpdatedId);
        Assert.True(store.UpdateReceived!.StatusGiven);
        Assert.Equal(TaskStates.InProgress, store.UpdateReceived.Status);
        Assert.False(store.UpdateReceived.TitleGiven);
        Assert.Equal("me", store.Actor);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Contains("in_progress", succeeded.ToastText);
    }

    [Fact]
    public async Task ApplyAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Closed);
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("Use `nitro task reopen` to reopen a task.") };

        // act
        var outcome = await StatusPicker.ApplyAsync(
            store, task, TaskStates.Open, "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskEditorOutcome.Failed>(outcome);
        Assert.Equal("Use `nitro task reopen` to reopen a task.", failed.ToastText);
    }
}

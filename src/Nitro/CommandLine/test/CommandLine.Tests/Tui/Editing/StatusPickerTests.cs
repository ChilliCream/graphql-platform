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
    public async Task ApplyAsync_Should_ReportSuccess_When_StatusDiffersFromTask()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Open);
        var store = new FakeTaskStore();

        // act
        var outcome = await StatusPicker.ApplyAsync(
            store, task, TaskStates.InProgress, "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.UpdatedId);
        Assert.True(store.UpdateReceived!.StatusGiven);
        Assert.Equal(TaskStates.InProgress, store.UpdateReceived.Status);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal("Status set to 'in_progress' for task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task ApplyAsync_Should_ReportNoChanges_When_StatusMatchesTask()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Open);
        var store = new FakeTaskStore();

        // act
        var outcome = await StatusPicker.ApplyAsync(
            store, task, TaskStates.Open, "me", CancellationToken.None);

        // assert
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Equal("No changes to task 'a1'.", succeeded.ToastText);
    }

    [Fact]
    public async Task ApplyAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", status: TaskStates.Closed);
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("Use `nitro agent tasks reopen` to reopen a task.") };

        // act
        var outcome = await StatusPicker.ApplyAsync(
            store, task, TaskStates.Open, "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskEditorOutcome.Failed>(outcome);
        Assert.Equal("Use `nitro agent tasks reopen` to reopen a task.", failed.ToastText);
    }
}

using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Editing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Editing;

public sealed class PriorityPickerTests
{
    [Fact]
    public void Create_Should_PreSelectTasksCurrentPriority()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", priority: TaskPriorities.High);

        // act
        var picker = PriorityPicker.Create(task);
        var applied = Assert.IsType<QuickPickerResult.Applied>(
            picker.HandleKey(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)));

        // assert
        Assert.Equal("1", applied.SelectedId);
    }

    [Fact]
    public async Task ApplyAsync_Should_CallUpdateTaskAsync_WithPriorityGiven()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1", priority: TaskPriorities.Medium);
        var store = new FakeTaskStore { UpdateResult = new TaskUpdateResult { ChangedFields = ["priority"] } };

        // act
        var outcome = await PriorityPicker.ApplyAsync(
            store, task, TaskPriorities.Critical, "me", CancellationToken.None);

        // assert
        Assert.Equal("a1", store.UpdatedId);
        Assert.True(store.UpdateReceived!.PriorityGiven);
        Assert.Equal(TaskPriorities.Critical, store.UpdateReceived.Priority);
        Assert.False(store.UpdateReceived.StatusGiven);
        Assert.Equal("me", store.Actor);
        var succeeded = Assert.IsType<TaskEditorOutcome.Succeeded>(outcome);
        Assert.Contains("P0", succeeded.ToastText);
    }

    [Fact]
    public async Task ApplyAsync_Should_ReturnFailed_When_StoreThrows()
    {
        // arrange
        var task = TaskItemBuilder.Create("a1");
        var store = new FakeTaskStore { ThrowOnWrite = new ExitException("not found") };

        // act
        var outcome = await PriorityPicker.ApplyAsync(
            store, task, TaskPriorities.Low, "me", CancellationToken.None);

        // assert
        var failed = Assert.IsType<TaskEditorOutcome.Failed>(outcome);
        Assert.Equal("not found", failed.ToastText);
    }
}

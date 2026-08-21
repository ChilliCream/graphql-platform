using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using ChilliCream.Nitro.CommandLine.Tui.Input;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed class TaskDetailModelTests
{
    [Fact]
    public void Constructor_Should_Throw_When_StoreIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new TaskDetailModel(null!));
    }

    [Fact]
    public async Task LoadAsync_Should_PopulateTaskAndRelatedData_When_TaskExists()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1", title: "First task");
        store.Labels["t-1"] = ["backend", "urgent"];
        store.Comments["t-1"] = [new TaskComment
        {
            TaskId = "t-1", Author = "alice", Text = "hi", CreatedAt = DateTimeOffset.UnixEpoch
        }];
        store.Blocked["t-1"] = ["t-0:not closed"];
        var model = new TaskDetailModel(store);

        // act
        var loaded = await model.LoadAsync("t-1", CancellationToken.None);

        // assert
        Assert.True(loaded);
        Assert.Equal("t-1", model.CurrentTaskId);
        Assert.Equal("First task", model.Task!.Title);
        Assert.Equal(["backend", "urgent"], model.Labels);
        Assert.Single(model.Comments);
        Assert.Equal(["t-0:not closed"], model.BlockedBy);
    }

    [Fact]
    public async Task LoadAsync_Should_ClearState_When_TaskDoesNotExist()
    {
        // arrange
        var store = new FakeTaskStore();
        var model = new TaskDetailModel(store);

        // act
        var loaded = await model.LoadAsync("missing", CancellationToken.None);

        // assert
        Assert.False(loaded);
        Assert.Null(model.Task);
        Assert.Equal("missing", model.CurrentTaskId);
        Assert.Empty(model.Rows);
    }

    [Fact]
    public async Task LoadAsync_Should_BuildRows_DependenciesThenBlocks()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Blocks, DependsOnId = "t-2", Status = TaskStates.Open, Title = "Dep" }
        ];
        store.Dependents["t-1"] =
        [
            new TaskDependentDetail { Type = TaskDependencyTypes.Related, TaskId = "t-3", Status = TaskStates.Open, Title = "Blocked task" }
        ];
        var model = new TaskDetailModel(store);

        // act
        await model.LoadAsync("t-1", CancellationToken.None);

        // assert
        Assert.Equal(2, model.Rows.Count);
        Assert.Equal(TaskDetailRowKind.Dependency, model.Rows[0].Kind);
        Assert.Equal("t-2", model.Rows[0].TargetId);
        Assert.True(model.Rows[0].IsBlocking);
        Assert.Equal(TaskDetailRowKind.Blocks, model.Rows[1].Kind);
        Assert.Equal("t-3", model.Rows[1].TargetId);
        Assert.False(model.Rows[1].IsBlocking);
    }

    [Fact]
    public async Task MoveRowCursor_Should_ClampToRowBounds()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-2", Status = TaskStates.Open, Title = "A" },
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-3", Status = TaskStates.Open, Title = "B" }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act & assert
        Assert.Equal(0, model.SelectedRowIndex);

        model.MoveRowCursor(CursorDirection.Up);
        Assert.Equal(0, model.SelectedRowIndex);

        model.MoveRowCursor(CursorDirection.Down);
        Assert.Equal(1, model.SelectedRowIndex);

        model.MoveRowCursor(CursorDirection.Down);
        Assert.Equal(1, model.SelectedRowIndex);

        model.MoveRowCursor(CursorDirection.Up);
        Assert.Equal(0, model.SelectedRowIndex);
    }

    [Fact]
    public async Task OpenSelectedRowAsync_Should_NavigateAndPushBackStack_When_TargetExists()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1", title: "Origin");
        store.Tasks["t-2"] = TaskItemBuilder.Create("t-2", title: "Target");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-2", Status = TaskStates.Open, Title = "Target" }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var navigated = await model.OpenSelectedRowAsync(CancellationToken.None);

        // assert
        Assert.True(navigated);
        Assert.Equal("t-2", model.Task!.Id);
        Assert.True(model.CanGoBack);
    }

    [Fact]
    public async Task OpenSelectedRowAsync_Should_NotNavigate_When_TargetNoLongerExists()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-2", Status = null, Title = null }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var navigated = await model.OpenSelectedRowAsync(CancellationToken.None);

        // assert
        Assert.False(navigated);
        Assert.Equal("t-1", model.Task!.Id);
        Assert.False(model.CanGoBack);
    }

    [Fact]
    public async Task OpenSelectedRowAsync_Should_ReturnFalse_When_NoRowsExist()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var navigated = await model.OpenSelectedRowAsync(CancellationToken.None);

        // assert
        Assert.False(navigated);
    }

    [Fact]
    public async Task GoBackAsync_Should_PopStackAndReloadPreviousTask()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1", title: "Origin");
        store.Tasks["t-2"] = TaskItemBuilder.Create("t-2", title: "Target");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-2", Status = TaskStates.Open, Title = "Target" }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);
        await model.OpenSelectedRowAsync(CancellationToken.None);
        Assert.Equal("t-2", model.Task!.Id);

        // act
        var wentBack = await model.GoBackAsync(CancellationToken.None);

        // assert
        Assert.True(wentBack);
        Assert.Equal("t-1", model.Task!.Id);
        Assert.False(model.CanGoBack);
    }

    [Fact]
    public async Task GoBackAsync_Should_ReturnFalse_When_StackIsEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        var wentBack = await model.GoBackAsync(CancellationToken.None);

        // assert
        Assert.False(wentBack);
        Assert.Equal("t-1", model.Task!.Id);
    }

    [Fact]
    public async Task NavigationStack_Should_SupportMultipleHops()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks["t-1"] = TaskItemBuilder.Create("t-1");
        store.Tasks["t-2"] = TaskItemBuilder.Create("t-2");
        store.Tasks["t-3"] = TaskItemBuilder.Create("t-3");
        store.Dependencies["t-1"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-2", Status = TaskStates.Open, Title = "t2" }
        ];
        store.Dependencies["t-2"] =
        [
            new TaskDependencyDetail { Type = TaskDependencyTypes.Related, DependsOnId = "t-3", Status = TaskStates.Open, Title = "t3" }
        ];
        var model = new TaskDetailModel(store);
        await model.LoadAsync("t-1", CancellationToken.None);

        // act
        await model.OpenSelectedRowAsync(CancellationToken.None);
        await model.OpenSelectedRowAsync(CancellationToken.None);
        Assert.Equal("t-3", model.Task!.Id);

        // assert: pops back through both hops in reverse order
        await model.GoBackAsync(CancellationToken.None);
        Assert.Equal("t-2", model.Task!.Id);

        await model.GoBackAsync(CancellationToken.None);
        Assert.Equal("t-1", model.Task!.Id);
        Assert.False(model.CanGoBack);
    }
}

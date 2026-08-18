using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Tree;

public sealed class DependencyTreeViewTests
{
    [Fact]
    public void Constructor_Should_Throw_When_StoreIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new DependencyTreeView(null!, "a"));
    }

    [Fact]
    public void Constructor_Should_Throw_When_RootIdIsNull()
    {
        // act & assert
        Assert.Throws<ArgumentNullException>(() => new DependencyTreeView(new FakeTaskStore(), null!));
    }

    [Fact]
    public void OnEnter_Should_LoadOnlyRoot_When_NoEdgesExist()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");

        // act
        view.OnEnter();

        // assert
        Assert.Equal(["a"], view.Rows.Select(r => r.TaskId));
        Assert.Equal("a", view.SelectedTaskId);
    }

    [Fact]
    public void OnEnter_Should_LoadDependencyChildren_When_EdgesExist()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Edges.Add(Edge("a", "b"));
        var view = new DependencyTreeView(store, "a");

        // act
        view.OnEnter();

        // assert
        Assert.Equal(["a", "b"], view.Rows.Select(r => r.TaskId));
    }

    [Fact]
    public void SelectedTaskId_Should_BeNull_When_NotYetEntered()
    {
        // arrange
        var view = new DependencyTreeView(new FakeTaskStore(), "a");

        // assert
        Assert.Null(view.SelectedTaskId);
    }

    [Fact]
    public void Handle_MoveCursorDown_Should_AdvanceSelection()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("c"));
        store.Edges.Add(Edge("a", "b"));
        store.Edges.Add(Edge("a", "c"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        Assert.Equal("a", view.SelectedTaskId);

        // act & assert
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal("b", view.SelectedTaskId);

        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal("c", view.SelectedTaskId);
    }

    [Fact]
    public void Handle_MoveCursorUp_Should_ClampAtFirstRow()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Up));

        // assert
        Assert.Equal("a", view.SelectedTaskId);
    }

    [Fact]
    public void Handle_MoveToEdge_Should_JumpToTopAndBottom()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("c"));
        store.Edges.Add(Edge("a", "b"));
        store.Edges.Add(Edge("a", "c"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act & assert
        view.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Bottom));
        Assert.Equal("c", view.SelectedTaskId);

        view.Handle(new TuiMessage.MoveToEdge(EdgeTarget.Top));
        Assert.Equal("a", view.SelectedTaskId);
    }

    [Fact]
    public void Handle_OpenSelected_Should_RefocusOnCursorNode_And_PushBreadcrumb()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("c"));
        store.Edges.Add(Edge("a", "b"));
        store.Edges.Add(Edge("b", "c"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        Assert.Equal("b", view.SelectedTaskId);

        // act
        view.Handle(new TuiMessage.OpenSelected());

        // assert
        Assert.Equal("b", view.RootId);
        Assert.True(view.CanNavigateBack);
        Assert.Equal(["b", "c"], view.Rows.Select(r => r.TaskId));
    }

    [Fact]
    public void Refocus_Should_HaveNoEffect_When_CursorIsOnRoot()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        view.Refocus();

        // assert
        Assert.Equal("a", view.RootId);
        Assert.False(view.CanNavigateBack);
    }

    [Fact]
    public void NavigateBack_Should_PopBreadcrumb_And_RestorePreviousRoot()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Edges.Add(Edge("a", "b"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        view.Refocus();
        Assert.Equal("b", view.RootId);

        // act
        var popped = view.NavigateBack();

        // assert
        Assert.True(popped);
        Assert.Equal("a", view.RootId);
        Assert.False(view.CanNavigateBack);
    }

    [Fact]
    public void NavigateBack_Should_ReturnFalse_When_BreadcrumbsAreEmpty()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        var popped = view.NavigateBack();

        // assert
        Assert.False(popped);
        Assert.Equal("a", view.RootId);
    }

    [Fact]
    public void EnterOnTask_Should_ResetBreadcrumbsAndCursor()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("z"));
        store.Edges.Add(Edge("a", "b"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        view.Refocus();
        Assert.True(view.CanNavigateBack);

        // act
        view.EnterOnTask("z");

        // assert
        Assert.Equal("z", view.RootId);
        Assert.False(view.CanNavigateBack);
        Assert.Equal("z", view.SelectedTaskId);
    }

    [Fact]
    public void ToggleEdgeMode_Should_SwitchBetweenBlockingAndParentChild_WithoutRefetching()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("c"));
        store.Edges.Add(Edge("a", "b", TaskDependencyTypes.ParentChild));
        store.Edges.Add(Edge("a", "c", TaskDependencyTypes.Blocks));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        Assert.Equal(TreeEdgeMode.Blocking, view.EdgeMode);
        Assert.Equal(["a", "b", "c"], view.Rows.Select(r => r.TaskId));
        var queriesAfterEnter = store.EdgeQueryCount;

        // act
        view.ToggleEdgeMode();

        // assert
        Assert.Equal(TreeEdgeMode.ParentChild, view.EdgeMode);
        Assert.Equal(["a", "b"], view.Rows.Select(r => r.TaskId));
        Assert.Equal(queriesAfterEnter, store.EdgeQueryCount);
    }

    [Fact]
    public void ToggleDirection_Should_SwitchBetweenUpAndDown_WithoutRefetching()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Tasks.Add(TaskItemBuilder.Create("c"));
        store.Edges.Add(Edge("a", "b"));
        store.Edges.Add(Edge("c", "a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        Assert.Equal(TreeDirection.Up, view.Direction);
        Assert.Equal(["a", "b"], view.Rows.Select(r => r.TaskId));
        var queriesAfterEnter = store.EdgeQueryCount;

        // act
        view.ToggleDirection();

        // assert
        Assert.Equal(TreeDirection.Down, view.Direction);
        Assert.Equal(["a", "c"], view.Rows.Select(r => r.TaskId));
        Assert.Equal(queriesAfterEnter, store.EdgeQueryCount);
    }

    [Fact]
    public void Handle_RefreshRequested_Should_ReloadFromStore()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();
        var queriesAfterEnter = store.EdgeQueryCount;

        // act
        view.Handle(new TuiMessage.RefreshRequested());

        // assert
        Assert.Equal(queriesAfterEnter + 1, store.EdgeQueryCount);
    }

    [Fact]
    public void KeyMap_Should_ResolveF_To_OpenSelected()
    {
        // arrange
        var view = new DependencyTreeView(new FakeTaskStore(), "a");

        // act
        var resolved = view.KeyMap!.TryResolve(
            new KeyChord(ConsoleKey.F, ConsoleModifiers.None, 'f'), out var message);

        // assert
        Assert.True(resolved);
        Assert.Equal(new TuiMessage.OpenSelected(), message);
    }

    private static TaskDependency Edge(string taskId, string dependsOnId, string type = TaskDependencyTypes.Blocks)
        => new()
        {
            TaskId = taskId,
            DependsOnId = dependsOnId,
            Type = type,
            CreatedAt = DateTimeOffset.UnixEpoch
        };
}

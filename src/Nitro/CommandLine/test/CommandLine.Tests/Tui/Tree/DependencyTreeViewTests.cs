using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Input;
using ChilliCream.Nitro.CommandLine.Tui.Tree;
using Spectre.Console;
using Spectre.Console.Testing;
using CursorDirection = ChilliCream.Nitro.CommandLine.Tui.Input.CursorDirection;

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

    [Fact]
    public void KeyMap_Should_ResolveM_D_U_To_TreeGestures()
    {
        // arrange
        var view = new DependencyTreeView(new FakeTaskStore(), "a");

        // act & assert
        Assert.True(view.KeyMap!.TryResolve(new KeyChord(ConsoleKey.M, ConsoleModifiers.None, 'm'), out var m));
        Assert.Equal(new TuiMessage.ToggleTreeEdgeMode(), m);

        Assert.True(view.KeyMap!.TryResolve(new KeyChord(ConsoleKey.D, ConsoleModifiers.None, 'd'), out var d));
        Assert.Equal(new TuiMessage.ToggleTreeDirection(), d);

        Assert.True(view.KeyMap!.TryResolve(new KeyChord(ConsoleKey.U, ConsoleModifiers.None, 'u'), out var u));
        Assert.Equal(new TuiMessage.NavigateTreeBack(), u);
    }

    [Fact]
    public void Handle_ToggleTreeEdgeMode_Should_ToggleEdgeMode()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        store.Tasks.Add(TaskItemBuilder.Create("b"));
        store.Edges.Add(Edge("a", "b", TaskDependencyTypes.ParentChild));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        view.Handle(new TuiMessage.ToggleTreeEdgeMode());

        // assert
        Assert.Equal(TreeEdgeMode.ParentChild, view.EdgeMode);
    }

    [Fact]
    public void Handle_ToggleTreeDirection_Should_ToggleDirection()
    {
        // arrange
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        view.Handle(new TuiMessage.ToggleTreeDirection());

        // assert
        Assert.Equal(TreeDirection.Down, view.Direction);
    }

    [Fact]
    public void Handle_NavigateTreeBack_Should_PopBreadcrumb()
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
        view.Handle(new TuiMessage.NavigateTreeBack());

        // assert
        Assert.Equal("a", view.RootId);
    }

    [Theory]
    [InlineData(64, "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaa > z · blocking · depends on")]
    [InlineData(44, "aaaaaaaa…aaa > z · blocking · depends on")]
    [InlineData(24, "aaa…> z · depends on")]
    [InlineData(10, "aa… z")]
    public void Render_Should_DegradeBreadcrumbHeader_When_PanelIsNarrow(int width, string expectedHeader)
    {
        // arrange: a two-id breadcrumb (a pushed root plus the current root)
        // wide enough on its own to overflow the panel at the narrower
        // widths below. The sole visible row (the tree has no edges once
        // refocused on "z") carries a long title so the panel's own,
        // content-driven width never becomes the binding constraint on the
        // header budget at any of the widths under test, matching the
        // pre-refactor header sizing exactly.
        var longRootId = new string('a', 30);
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create(longRootId));
        store.Tasks.Add(TaskItemBuilder.Create("z", title: new string('x', 60)));
        store.Edges.Add(Edge(longRootId, "z"));
        var view = new DependencyTreeView(store, longRootId);
        view.OnEnter();
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        view.Refocus();
        Assert.Equal("z", view.RootId);

        // act
        var panel = Assert.IsType<Panel>(view.Render(width, 10));
        var console = new TestConsole().Width(120);
        console.Write(panel);

        // assert: the header degrades deterministically (middle-truncating
        // the id chain, then dropping the edge mode, then the direction)
        // instead of being cut wherever Spectre's own panel-header ellipsis
        // lands. Asserted against the rendered top border line rather than
        // panel.Header.Text, since Spectre may still re-truncate a header
        // that does not fit the panel's actual (content-driven) width.
        Assert.Contains(expectedHeader, console.Lines[0]);
    }

    [Fact]
    public void Render_Should_AlwaysUseARoundedBorder()
    {
        // arrange: this view is the tree mode's only pane - it never loses
        // focus to a sibling pane - so it deliberately keeps the plain
        // Rounded box from PaneBorders' shared focus language rather than
        // the Heavy one; only its accent color marks it as the active view.
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("a"));
        var view = new DependencyTreeView(store, "a");
        view.OnEnter();

        // act
        var panel = Assert.IsType<Panel>(view.Render(80, 10));

        // assert
        Assert.Equal(BoxBorder.Rounded, panel.Border);
    }

    [Fact]
    public void Render_Should_TruncateHeaderToPanelWidth_When_ContentRowsAreNarrowerThanHeader()
    {
        // arrange: a two-id breadcrumb (53 chars
        // fully expanded: "acme-epic1 > acme-epic1.1 · blocking · depended
        // on by"), but the only visible row after refocusing is the short,
        // untitled "acme-epic1.1" node itself, so the panel's actual
        // (content-driven) width is only 36 columns, well under the full
        // header. The header must degrade to fit that panel width via this
        // view's own deterministic middle-truncation, not via Spectre's
        // PanelHeader ellipsis, which would otherwise cut it mid-word before
        // " by".
        var store = new FakeTaskStore();
        store.Tasks.Add(TaskItemBuilder.Create("acme-epic1"));
        store.Tasks.Add(TaskItemBuilder.Create("acme-epic1.1"));
        store.Edges.Add(Edge("acme-epic1", "acme-epic1.1"));
        var view = new DependencyTreeView(store, "acme-epic1");
        view.OnEnter();
        view.Handle(new TuiMessage.MoveCursor(CursorDirection.Down));
        view.Refocus();
        Assert.Equal("acme-epic1.1", view.RootId);
        view.Handle(new TuiMessage.ToggleTreeDirection());
        Assert.Equal(TreeDirection.Down, view.Direction);

        // act
        var panel = Assert.IsType<Panel>(view.Render(95, 10));
        var console = new TestConsole().Width(120);
        console.Write(panel);

        // assert
        Assert.Contains("acme…1.1 · blocking · depended on by", console.Lines[0]);
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

using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.TreeView;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.TreeView;

public sealed class GraphTreeViewTests
{
    [Fact]
    public void Create_Should_BuildRootSpineForNestedEpicsAndOrphanTasks()
    {
        // arrange
        var model = Model(
            [
                Node("epic", type: TaskTypes.Epic, priority: 0),
                Node("nested", type: TaskTypes.Epic, priority: 1),
                Node("nested-task", priority: 1),
                Node("task", priority: 2),
                Node("orphan", priority: 3)
            ],
            [
                Edge("epic", "nested", GraphEdgeKind.ParentChild),
                Edge("nested", "nested-task", GraphEdgeKind.ParentChild),
                Edge("epic", "task", GraphEdgeKind.ParentChild)
            ]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        Assert.Equal([null, "epic", "nested", "nested-task", "task", "orphan"], view.Rows.Select(t => t.TaskId));
        Assert.Equal([0, 1, 2, 3, 2, 1], view.Rows.Select(t => t.Connector.Depth));
        Assert.Equal("├─ ", view.Rows[1].Connector.BuildConnector());
        Assert.Equal("│  ├─ ", view.Rows[2].Connector.BuildConnector());
        Assert.Equal("└─ ", view.Rows[5].Connector.BuildConnector());
    }

    [Fact]
    public void Create_Should_OrderEverySiblingByPriorityThenId()
    {
        // arrange
        var model = Model(
            [
                Node("root-b", type: TaskTypes.Epic, priority: 1),
                Node("root-a", type: TaskTypes.Epic, priority: 1),
                Node("root-c", priority: 2),
                Node("child-z", priority: 2),
                Node("child-a", priority: 1),
                Node("child-b", priority: 1)
            ],
            [
                Edge("root-a", "child-z", GraphEdgeKind.ParentChild),
                Edge("root-a", "child-a", GraphEdgeKind.ParentChild),
                Edge("root-a", "child-b", GraphEdgeKind.ParentChild)
            ]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        Assert.Equal([null, "root-a", "child-a", "child-b", "child-z", "root-b", "root-c"], view.Rows.Select(t => t.TaskId));
    }

    [Fact]
    public void Create_Should_UseTheCanonicalPriorityParent_When_AChildHasMultipleParents()
    {
        // arrange
        var model = Model(
            [
                Node("z", type: TaskTypes.Epic, priority: 0),
                Node("a", type: TaskTypes.Epic, priority: 4),
                Node("child", priority: 2)
            ],
            [
                Edge("a", "child", GraphEdgeKind.ParentChild),
                Edge("z", "child", GraphEdgeKind.ParentChild)
            ]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        Assert.Equal([null, "z", "child", "a"], view.Rows.Select(t => t.TaskId));
    }

    [Fact]
    public void Create_Should_HideTerminalTasksAndTheirBlockingEdgesByDefault()
    {
        // arrange
        var model = Model(
            [Node("open"), Node("closed", status: TaskStates.Closed)],
            [Edge("open", "closed")]);

        // act
        var view = new GraphTreeView(model, Set());

        // assert
        var open = Assert.Single(view.Rows, t => t.TaskId == "open");
        Assert.Equal([null, "open"], view.Rows.Select(t => t.TaskId));
        Assert.Equal((0, 0), (open.BlockedByCount, open.BlocksCount));
    }

    [Fact]
    public void SetHideClosed_Should_ShowTerminalTasksAndTheirBlockingEdges_When_Disabled()
    {
        // arrange
        var model = Model(
            [Node("open"), Node("closed", status: TaskStates.Closed)],
            [Edge("open", "closed")]);
        var view = new GraphTreeView(model, Set());

        // act
        view.SetHideClosed(false);

        // assert
        Assert.Equal([null, "closed", "open"], view.Rows.Select(t => t.TaskId));
        Assert.Equal(
            [("closed", 1, 0), ("open", 0, 1)],
            view.Rows.Where(t => !t.IsRoot).Select(t => (t.TaskId, t.BlockedByCount, t.BlocksCount)));
    }

    [Fact]
    public void Create_Should_CountVisibleBlockingEdgesAndHighlightSelectionRelationships()
    {
        // arrange
        var model = Model(
            [Node("blocker"), Node("selected"), Node("dependent")],
            [Edge("blocker", "selected"), Edge("selected", "dependent")]);
        var view = new GraphTreeView(model, Set());

        // act
        view.SelectTask("selected");

        // assert
        Assert.Equal(
            [
                ((string?)null, 0, 0, false, false),
                ("blocker", 0, 1, false, true),
                ("dependent", 1, 0, false, true),
                ("selected", 1, 1, true, false)
            ],
            view.Rows.Select(t => (t.TaskId, t.BlockedByCount, t.BlocksCount, t.IsSelected, t.IsRelatedToSelection)));
    }

    [Fact]
    public void Create_Should_KeepTaskBlockingBadgesWhenParentEpicIsCollapsed()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic, priority: 0), Node("blocker"), Node("child")],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("child", "blocker")
            ]);
        var view = new GraphTreeView(model, Set());
        var expandedState = view.Rows
            .Select(t => (t.TaskId, t.BlockedByCount, t.BlocksCount))
            .ToArray();

        // act
        view.SelectTask("epic");
        view.CollapseSelected();

        // assert
        Assert.Equal(
            [
                ((string?)null, 0, 0),
                ("epic", 0, 0),
                ("child", 0, 1),
                ("blocker", 1, 0)
            ],
            expandedState);
        Assert.Equal(
            [
                ((string?)null, 0, 0),
                ("epic", 0, 0),
                ("blocker", 1, 0)
            ],
            view.Rows.Select(t => (t.TaskId, t.BlockedByCount, t.BlocksCount)));
    }

    [Fact]
    public void Create_Should_AggregateHiddenBlockerRelationshipsOnCollapsedEpic()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("hidden"), Node("selected")],
            [
                Edge("epic", "hidden", GraphEdgeKind.ParentChild),
                Edge("hidden", "selected")
            ]);
        var view = new GraphTreeView(model, Set("epic"));

        // act
        view.SelectTask("selected");

        // assert
        Assert.Equal(
            [
                ((string?)null, 0, 0, 0, false, false),
                ("epic", 0, 0, 1, false, true),
                ("selected", 1, 0, 0, true, false)
            ],
            view.Rows.Select(t =>
                (t.TaskId,
                    t.BlockedByCount,
                    t.BlocksCount,
                    t.ContainedRelationshipCount,
                    t.IsSelected,
                    t.IsRelatedToSelection)));
        Assert.Equal(
            "Root\n├─ ▸ ○ [E] epic epic  blocked by 0 / blocks 0  1 related\n"
            + "└─   ○ [T] selected selected  blocked by 1 / blocks 0\n",
            Render(view));
    }

    [Fact]
    public void Create_Should_AggregateHiddenDependentRelationshipsOnCollapsedEpic()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("hidden"), Node("selected")],
            [
                Edge("epic", "hidden", GraphEdgeKind.ParentChild),
                Edge("selected", "hidden")
            ]);
        var view = new GraphTreeView(model, Set("epic"));

        // act
        view.SelectTask("selected");

        // assert
        Assert.Equal(
            [
                ((string?)null, 0, 0, 0, false, false),
                ("epic", 0, 0, 1, false, true),
                ("selected", 0, 1, 0, true, false)
            ],
            view.Rows.Select(t =>
                (t.TaskId,
                    t.BlockedByCount,
                    t.BlocksCount,
                    t.ContainedRelationshipCount,
                    t.IsSelected,
                    t.IsRelatedToSelection)));
        Assert.Equal(
            "Root\n├─ ▸ ○ [E] epic epic  blocked by 0 / blocks 0  1 related\n"
            + "└─   ○ [T] selected selected  blocked by 0 / blocks 1\n",
            Render(view));
    }

    [Fact]
    public void Create_Should_MarkCollapsedEpicWithContainedMatchesWithoutExpandingIt()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("first"), Node("second")],
            [
                Edge("epic", "first", GraphEdgeKind.ParentChild),
                Edge("epic", "second", GraphEdgeKind.ParentChild)
            ]);
        var view = new GraphTreeView(model, Set("epic"));

        // act
        view.SetMatchIds(["first", "second"]);
        view.SelectTask("first");

        // assert
        var epic = Assert.Single(view.Rows, t => t.TaskId == "epic");
        Assert.Equal([null, "epic"], view.Rows.Select(t => t.TaskId));
        Assert.False(epic.IsExpanded);
        Assert.Equal(2, epic.ContainedMatchCount);
        Assert.Equal(0, view.Rows.Count(t => t.IsSelected));
    }

    [Fact]
    public void Create_Should_ExpandAtTheAdaptiveThresholdAndCollapseAboveIt()
    {
        // arrange
        var atThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold - 1);
        var aboveThreshold = EpicWithChildren(GraphReductionOptions.AdaptiveCollapseThreshold);

        // act
        var expanded = new GraphTreeView(atThreshold);
        var collapsed = new GraphTreeView(aboveThreshold);

        // assert
        Assert.Equal(GraphReductionOptions.AdaptiveCollapseThreshold + 1, expanded.Rows.Count);
        Assert.True(Assert.Single(expanded.Rows, t => t.TaskId == "epic").IsExpanded);
        Assert.Equal(2, collapsed.Rows.Count);
        Assert.False(Assert.Single(collapsed.Rows, t => t.TaskId == "epic").IsExpanded);
    }

    [Fact]
    public void CollapseSelectedAndExpandSelected_Should_ToggleTheSelectedEpic()
    {
        // arrange
        var model = Model(
            [Node("epic", type: TaskTypes.Epic), Node("child")],
            [Edge("epic", "child", GraphEdgeKind.ParentChild)]);
        var view = new GraphTreeView(model, Set());

        // act
        view.CollapseSelected();
        var collapsedRows = view.Rows;
        view.ExpandSelected();

        // assert
        Assert.Equal([null, "epic"], collapsedRows.Select(t => t.TaskId));
        Assert.False(Assert.Single(collapsedRows, t => t.TaskId == "epic").IsExpanded);
        Assert.Equal([null, "epic", "child"], view.Rows.Select(t => t.TaskId));
        Assert.True(Assert.Single(view.Rows, t => t.TaskId == "epic").IsExpanded);
    }

    [Fact]
    public void Render_Should_ShowTitlesBeforeDimmedIdsAndDependencyBadges()
    {
        // arrange
        var model = Model(
            [Node("epic", title: "Ship the graph", type: TaskTypes.Epic), Node("child")],
            [
                Edge("epic", "child", GraphEdgeKind.ParentChild),
                Edge("epic", "child")
            ]);
        var view = new GraphTreeView(model, Set());
        var console = new TestConsole().Width(120).Height(10);

        // act
        console.Write(view.Render(120, 10));

        // assert
        Assert.Contains("▾ ○ [E] Ship the graph epic  blocked by 0 / blocks 1", console.Output);
        Assert.Contains("└─   ○ [T] child child  blocked by 1 / blocks 0", console.Output);
    }

    [Fact]
    public void Render_Should_TruncateTitlesWithinWidthAndRetainRowStylesForSelectedRelationships()
    {
        // arrange
        var model = Model(
            [
                Node("blocker", title: "Blocker title is deliberately long"),
                Node("selected", title: "Selected title is deliberately long")
            ],
            [Edge("blocker", "selected")]);
        var view = new GraphTreeView(model, Set());
        view.SelectTask("selected");
        var plainConsole = new TestConsole().Width(50).Height(10);
        var ansiConsole = new TestConsole()
            .Colors(ColorSystem.TrueColor)
            .EmitAnsiSequences()
            .Width(50)
            .Height(10);

        // act
        plainConsole.Write(view.Render(50, 10));
        ansiConsole.Write(view.Render(50, 10));

        // assert
        Assert.Equal(
            "Root\n├─   ○ [T] Block… blocker  blocked by 0 / blocks 1\n└─   ○ [T] Sele… selected  blocked by 1 / blocks 0\n",
            plainConsole.Output);
        AssertSelectedTokenPrefixesText(ansiConsole.Output, "status.glyph.open", "○");
        AssertSelectedTokenPrefixesText(ansiConsole.Output, "badge.type.task", "[T]");
        AssertSelectedTokenPrefixesText(ansiConsole.Output, "footer.key", "blocker");
        var selectionPrefix = GetAnsiPrefix(ThemeTokens.GetStyle("selection.highlight"));
        Assert.True(
            ansiConsole.Output.Contains(selectionPrefix + "├─", StringComparison.Ordinal)
            && ansiConsole.Output.Contains(selectionPrefix + "└─", StringComparison.Ordinal));
    }

    private static GraphModel EpicWithChildren(int childCount)
    {
        var nodes = new List<GraphNode> { Node("epic", type: TaskTypes.Epic) };
        var edges = new List<GraphEdge>();

        for (var index = 0; index < childCount; index++)
        {
            var childId = $"child-{index:D3}";
            nodes.Add(Node(childId));
            edges.Add(Edge("epic", childId, GraphEdgeKind.ParentChild));
        }

        return Model(nodes, edges);
    }

    private static GraphModel Model(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge> edges)
        => new(nodes, edges);

    private static GraphNode Node(
        string id,
        string? title = null,
        string status = TaskStates.Open,
        string type = TaskTypes.Task,
        int priority = 2)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Status = status,
            Type = type,
            Priority = priority
        };

    private static GraphEdge Edge(
        string fromId,
        string toId,
        GraphEdgeKind kind = GraphEdgeKind.Blocks)
        => new(fromId, toId, kind);

    private static IReadOnlySet<string> Set(params string[] ids)
        => ids.ToHashSet(StringComparer.Ordinal);

    private static string Render(GraphTreeView view)
    {
        var console = new TestConsole().Width(120).Height(10);
        console.Write(view.Render(120, 10));
        return console.Output;
    }

    private static void AssertSelectedTokenPrefixesText(string output, string token, string text)
    {
        var tokenStyle = ThemeTokens.GetStyle(token);
        var selectionStyle = ThemeTokens.GetStyle("selection.highlight");
        var composedStyle = new Style(tokenStyle.Foreground, selectionStyle.Background, tokenStyle.Decoration);
        Assert.Contains(GetAnsiPrefix(composedStyle) + text, output);
    }

    private static string GetAnsiPrefix(Style style)
    {
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        console.Write(new Markup("x", style));
        return console.Output[..console.Output.IndexOf('x')];
    }
}

using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tests.Tui;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Render;
using ChilliCream.Nitro.CommandLine.Tui.Theming;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Graph.CanvasView;

public sealed class GraphCanvasViewTests
{
    [Fact]
    public void MoveLeftAndRight_Should_CycleNearestBlockingRelationships_When_Repeated()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("target"), Node("c"), Node("d")],
            [Edge("a", "target"), Edge("b", "target"), Edge("target", "c"), Edge("target", "d")]));
        view.SelectTask("target");

        // act
        view.MoveLeft();
        var firstBlocker = view.SelectedTaskId;
        view.MoveLeft();
        var secondBlocker = view.SelectedTaskId;
        view.MoveLeft();
        var cycledBlocker = view.SelectedTaskId;
        view.SelectTask("target");
        view.MoveRight();
        var firstDependent = view.SelectedTaskId;
        view.MoveRight();
        var secondDependent = view.SelectedTaskId;
        view.MoveRight();
        var cycledDependent = view.SelectedTaskId;

        // assert
        $"{firstBlocker},{secondBlocker},{cycledBlocker}\n{firstDependent},{secondDependent},{cycledDependent}"
            .MatchInlineSnapshot("""
                a,b,a
                c,d,c
                """);
    }

    [Fact]
    public void MoveLeftAndRight_Should_FollowSingleCandidateChains_When_Repeated()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c")],
            [Edge("a", "b"), Edge("b", "c")]));
        view.SelectTask("a");

        // act
        view.MoveRight();
        var firstRight = view.SelectedTaskId;
        view.MoveRight();
        var secondRight = view.SelectedTaskId;
        view.MoveLeft();
        var firstLeft = view.SelectedTaskId;
        view.MoveLeft();
        var secondLeft = view.SelectedTaskId;

        // assert
        new[] { firstRight, secondRight, firstLeft, secondLeft }.MatchInlineSnapshots(
            ["b", "c", "b", "a"]);
    }

    [Fact]
    public void MoveUpAndDown_Should_StayWithinCurrentLayoutLayer()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c"), Node("target")],
            [Edge("a", "target"), Edge("b", "target"), Edge("c", "target")]));
        view.SelectTask("b");

        // act
        view.MoveUp();
        var previous = view.SelectedTaskId;
        view.MoveDown();
        view.MoveDown();
        var next = view.SelectedTaskId;

        // assert
        Assert.Equal(("a", "c"), (previous, next));
    }

    [Fact]
    public void SetModel_Should_RetainSelection_When_SelectedTaskStillExists()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("retained"), Node("removed")]));
        view.SelectTask("retained");

        // act
        view.SetModel(Model([Node("retained"), Node("added")]));

        // assert
        Assert.Equal("retained", view.SelectedTaskId);
    }

    [Fact]
    public void Render_Should_FollowSelectedNode_When_ViewportIsSmallerThanCanvas()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("source"), Node("target"), Node("dependent")],
            [Edge("source", "target"), Edge("target", "dependent")]));
        view.SelectTask("dependent");

        // act
        _ = view.Render(12, 3);
        var selected = view.Layout.FindNode("dependent")!;
        var centerX = selected.X + (selected.Width / 2);
        var centerY = selected.Y + (selected.Height / 2);

        // assert
        Assert.InRange(centerX, view.Viewport.X, view.Viewport.X + view.Viewport.Width - 1);
        Assert.InRange(centerY, view.Viewport.Y, view.Viewport.Y + view.Viewport.Height - 1);
    }

    [Fact]
    public void ToggleCompact_Should_UseSingleLineNodes_When_Enabled()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task", title: "A title that is intentionally too long for its node")]));
        var boxed = view.Layout.FindNode("task")!;

        // act
        view.ToggleCompact();
        var compact = view.Layout.FindNode("task")!;
        var result = view.CreateRenderResult();

        // assert
        Assert.Equal((4, 1), (boxed.Height, compact.Height));
        result.Buffer.ToText(result.Viewport).MatchInlineSnapshot("○ [T] task A title that is in…");
    }

    [Fact]
    public void CreateRenderResult_Should_RenderActorAndCollapsedEpicText_InBothNodeModes()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("task", title: "ignored", status: TaskStates.InProgress, assignee: "lucy", hiddenChildCount: 3)]));

        // act
        var boxedResult = view.CreateRenderResult();
        var boxed = boxedResult.Buffer.ToText(boxedResult.Viewport);
        view.ToggleCompact();
        var compactResult = view.CreateRenderResult();
        var compact = compactResult.Buffer.ToText(compactResult.Viewport);

        // assert
        new[] { boxed, compact }.MatchInlineSnapshots(
            [
                """
                ┌────────────────────────────┐
                │● [T] task @lucy            │
                │[epic +3]                   │
                └────────────────────────────┘
                """,
                "● [T] task @lucy [epic +3]    "
            ]);
    }

    [Fact]
    public void CreateRenderResult_Should_ApplySelectionAndTerminalDecoration_ToEveryClosedNodeSpan()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("closed", status: TaskStates.Closed)]));
        view.SelectTask("closed");
        var node = view.Layout.FindNode("closed")!;
        var selection = ThemeTokens.GetStyle("selection.highlight");
        var status = ThemeTokens.GetStyle("status.glyph.closed");
        var type = ThemeTokens.GetStyle("badge.type.task");
        var footer = ThemeTokens.GetStyle("footer.key");
        const Decoration terminal = Decoration.Dim;

        // act
        var buffer = view.CreateRenderResult().Buffer;
        var styles = new[]
        {
            buffer.Get(node.X + 1, node.Y + 1).Style,
            buffer.Get(node.X + 3, node.Y + 1).Style,
            buffer.Get(node.X + 7, node.Y + 1).Style,
            buffer.Get(node.X + 1, node.Y + 2).Style
        };

        // assert
        Assert.Equal(
            [
                new Style(status.Foreground, selection.Background, status.Decoration | selection.Decoration | terminal),
                new Style(type.Foreground, selection.Background, type.Decoration | selection.Decoration | terminal),
                new Style(footer.Foreground, selection.Background, footer.Decoration | selection.Decoration | terminal),
                new Style(Style.Plain.Foreground, selection.Background, selection.Decoration | terminal)
            ],
            styles);
    }

    [Fact]
    public void CreateRenderResult_Should_HighlightOnlyIncidentEdges_When_NodeIsSelected()
    {
        // arrange
        var selectedEdge = Edge("a", "b");
        var unrelatedEdge = Edge("c", "d");
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c"), Node("d")],
            [selectedEdge, unrelatedEdge]));
        view.SelectTask("a");

        // act
        var result = view.CreateRenderResult();
        var styles = result.Routes
            .GroupBy(route => route.Span.Edge)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(route => route.Points)
                    .Select(point => result.Buffer.Get(point.X, point.Y).Style)
                    .Distinct()
                    .ToArray());

        // assert
        Assert.Equal([ThemeTokens.GetStyle("selection.highlight")], styles[selectedEdge]);
        Assert.Equal([ThemeTokens.GetStyle("board.column.border")], styles[unrelatedEdge]);
    }

    [Fact]
    public void CreateRenderResult_Should_ExcludeParentChildEdges_UntilTheOverlayIsEnabled()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("parent"), Node("child")],
            [new GraphEdge("parent", "child", GraphEdgeKind.ParentChild)]));

        // act
        var withoutParentChild = view.CreateRenderResult().RenderedEdgeCount;
        view.ToggleParentChild();
        var withParentChild = view.CreateRenderResult().RenderedEdgeCount;

        // assert
        Assert.Equal((0, 1), (withoutParentChild, withParentChild));
    }

    [Fact]
    public void Render_Should_ShowReducedNotice_When_ModelWasReduced()
    {
        // arrange
        var view = new GraphCanvasView(new GraphModel([Node("task")], [], IsReduced: true, HiddenNodeCount: 401));

        // act
        var result = view.CreateRenderResult();
        var output = Render(view, 60, 6);

        // assert
        result.Buffer.ToText(result.Viewport).MatchInlineSnapshot("""
            ┌────────────────────────────┐
            │○ [T] task                  │
            │task                        │
            └────────────────────────────┘
            """);
        Assert.Contains("Graph reduced: 401 nodes hidden", output);
    }

    [Fact]
    public void Render_Should_ShowFullFooter_When_OnlyOneUnreducedRowIsAvailable()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task")]));
        var footer = GraphRenderFooter.CreateText(view.CreateRenderResult());

        // act
        var output = Render(view, 100, 1);

        // assert
        Assert.Contains(footer, output);
    }

    [Fact]
    public void Render_Should_PrioritizeNoticeCanvasAndFooter_When_HeightIsConstrained()
    {
        // arrange
        var view = new GraphCanvasView(new GraphModel([Node("task")], [], IsReduced: true, HiddenNodeCount: 401));

        // act
        var heightOne = Render(view, 6, 1).TrimEnd('\r', '\n');
        var heightTwo = Render(view, 6, 2).TrimEnd('\r', '\n');
        var heightThree = Render(view, 6, 3).TrimEnd('\r', '\n');

        // assert
        new[] { heightOne, heightTwo, heightThree }.MatchInlineSnapshots(
            [
                "Graph…",
                """
                Graph…
                nodes…
                """,
                "Graph…\n      \nnodes…"
            ]);
        Assert.Equal(new CanvasViewport(12, 2, 6, 1), view.Viewport);
    }

    [Fact]
    public void Render_Should_ResetViewport_When_DimensionsAreNonPositive()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task")]));
        _ = view.Render(30, 4);

        // act
        _ = view.Render(0, 4);

        // assert
        Assert.Equal(default, view.Viewport);
    }

    [Fact]
    public void Render_Should_DimClosedNodesAndInvertSelection_When_StylesAreApplied()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("closed", status: TaskStates.Closed), Node("open")]));
        view.SelectTask("open");

        // act
        var result = view.CreateRenderResult();
        var output = Render(view, 80, 8, ansi: true);
        var closed = view.Layout.FindNode("closed")!;

        // assert
        Assert.True((result.Buffer.Get(closed.X, closed.Y).Style.Decoration & Decoration.Dim) != 0);
        AnsiAssertions.AssertAnsiStyleApplied(output, "selection.highlight");
    }

    private static GraphModel Model(IReadOnlyList<GraphNode> nodes, IReadOnlyList<GraphEdge>? edges = null)
        => new(nodes, edges ?? []);

    private static GraphNode Node(
        string id,
        string? title = null,
        string status = TaskStates.Open,
        string type = TaskTypes.Task,
        string? assignee = null,
        int hiddenChildCount = 0)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Status = status,
            Type = type,
            Priority = 2,
            Assignee = assignee,
            HiddenChildCount = hiddenChildCount
        };

    private static GraphEdge Edge(string fromId, string toId)
        => new(fromId, toId, GraphEdgeKind.Blocks);

    private static string Render(GraphCanvasView view, int width, int height, bool ansi = false)
    {
        var console = new TestConsole().Width(width).Height(height);

        if (ansi)
        {
            console.Colors(ColorSystem.TrueColor).EmitAnsiSequences();
        }

        console.Write(view.Render(width, height));
        return console.Output;
    }
}

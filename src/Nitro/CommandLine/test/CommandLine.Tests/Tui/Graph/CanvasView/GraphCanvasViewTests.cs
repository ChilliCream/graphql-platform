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

        // assert
        Assert.InRange(selected.X, view.Viewport.X, view.Viewport.X + view.Viewport.Width - 1);
        Assert.InRange(selected.Y, view.Viewport.Y, view.Viewport.Y + view.Viewport.Height - 1);
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
        string type = TaskTypes.Task)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Status = status,
            Type = type,
            Priority = 2
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

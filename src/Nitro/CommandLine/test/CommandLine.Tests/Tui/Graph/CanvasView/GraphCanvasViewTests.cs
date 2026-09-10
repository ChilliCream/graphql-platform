using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Graph;
using ChilliCream.Nitro.CommandLine.Tui.Graph.CanvasView;
using ChilliCream.Nitro.CommandLine.Tui.Graph.Layout;
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
    public void Render_Should_AlignWaveCaptionsToLayerSpans_When_NodeModeChanges()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c")],
            [Edge("a", "b"), Edge("b", "c")]));

        // act
        var boxedPositions = GetWaveCaptionPositions(Render(view, 120, 7), 3);
        var boxedExpected = GetExpectedWaveCaptionPositions(view.Layout);
        view.ToggleCompact();
        var compactPositions = GetWaveCaptionPositions(Render(view, 120, 7), 3);
        var compactExpected = GetExpectedWaveCaptionPositions(view.Layout);

        // assert
        Assert.Equal(boxedExpected, boxedPositions);
        Assert.Equal(compactExpected, compactPositions);
    }

    [Fact]
    public void Render_Should_KeepWaveHeaderPinned_When_SelectedNodeScrollsVertically()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("a"), Node("b"), Node("c"), Node("d")]));
        view.SelectTask("d");

        // act
        var output = Render(view, 40, 4);
        var captionX = GetWaveCaptionPositions(output, 1)[0];
        var expectedCaptionX = GetExpectedWaveCaptionPositions(view.Layout)[0];

        // assert
        Assert.Equal((true, expectedCaptionX), (view.Viewport.Y > 0, captionX));
    }

    [Fact]
    public void Render_Should_ScrollWaveHeaderHorizontally_When_ViewportMovesHorizontally()
    {
        // arrange
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c")],
            [Edge("a", "b"), Edge("b", "c")]));
        view.SelectTask("c");

        // act
        var output = Render(view, 20, 4);
        var captionX = GetWaveCaptionPositions(output, 3)[2];
        var expectedCaptionX = GetExpectedWaveCaptionPositions(view.Layout)[2] - view.Viewport.X;

        // assert
        Assert.Equal((true, expectedCaptionX), (view.Viewport.X > 0, captionX));
    }

    [Fact]
    public void CreateRenderResult_Should_AddLayerSeparatorsOnlyToEmptyChannelCells_When_RouteCrossesSeparator()
    {
        // arrange
        var edge = Edge("source", "target");
        var view = new GraphCanvasView(Model([Node("source"), Node("target")], [edge]));
        view.SelectTask(null);
        var layers = view.Layout.Nodes.GroupBy(node => node.Layer).OrderBy(group => group.Key).ToArray();
        var leftRight = layers[0].Max(node => node.X + node.Width);
        var rightX = layers[1].Min(node => node.X);
        var separatorX = leftRight + ((rightX - leftRight) / 2);

        // act
        var result = view.CreateRenderResult();
        var edgePoint = result.Routes.SelectMany(route => route.Points).First(point => point.X == separatorX);
        var edgeCell = result.Buffer.Get(edgePoint.X, edgePoint.Y);
        var separatorCell = Enumerable.Range(0, result.Buffer.Height)
            .Select(y => result.Buffer.Get(separatorX, y))
            .First(cell => cell.Glyph == '│' && cell.Owners.Count == 0);
        var node = view.Layout.FindNode("source")!;

        // assert
        Assert.Equal((GraphEdgeStyles.Line, true), (edgeCell.Style, edgeCell.Owners.Contains(edge)));
        Assert.Equal(
            ('│', GraphEdgeStyles.Dim(GraphEdgeStyles.Line)),
            (separatorCell.Glyph, separatorCell.Style));
        Assert.Equal('┌', result.Buffer.Get(node.X, node.Y).Glyph);
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

    [Theory]
    [InlineData(TaskStates.Blocked, "board.column.status.blocked")]
    [InlineData(TaskStates.Deferred, "board.column.status.deferred")]
    [InlineData(TaskStates.Open, "board.column.status.ready")]
    [InlineData(TaskStates.InProgress, "board.column.status.inprogress")]
    [InlineData(TaskStates.Closed, "board.column.status.closed")]
    public void CreateRenderResult_Should_UseBoardStatusStyleForBoxBorderAndGlyph_When_StatusVaries(
        string status,
        string token)
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task", title: "Neutral title", status: status)]));
        view.SelectTask(null);
        var node = view.Layout.FindNode("task")!;
        var neutralTitle = status == TaskStates.Closed
            ? new Style(decoration: Decoration.Dim)
            : Style.Plain;
        var expectedNodeStyle = ExpectedNodeStyle(status, token);

        // act
        var buffer = view.CreateRenderResult().Buffer;

        // assert
        Assert.Equal(
            (expectedNodeStyle, expectedNodeStyle, neutralTitle),
            (buffer.Get(node.X, node.Y).Style,
                buffer.Get(node.X + 1, node.Y + 1).Style,
                buffer.Get(node.X + 1, node.Y + 2).Style));
    }

    [Theory]
    [InlineData(TaskStates.Blocked, "board.column.status.blocked", "⊘")]
    [InlineData(TaskStates.Deferred, "board.column.status.deferred", "⏸")]
    [InlineData(TaskStates.Open, "board.column.status.ready", "○")]
    [InlineData(TaskStates.InProgress, "board.column.status.inprogress", "●")]
    [InlineData(TaskStates.Closed, "board.column.status.closed", "✓")]
    public void Render_Should_EmitBoardStatusAnsiAndKeepGlyph_When_StatusVaries(
        string status,
        string token,
        string glyph)
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task", status: status)]));
        view.SelectTask(null);

        // act
        var output = Render(view, 80, 8, ansi: true);

        // assert
        var styleConsole = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(1).Height(1);
        styleConsole.Write(new Markup("x", ExpectedNodeStyle(status, token)));
        var ansiPrefix = styleConsole.Output[..styleConsole.Output.IndexOf('x')];
        Assert.Contains(ansiPrefix + "┌", output);
        Assert.Contains(glyph, output, StringComparison.Ordinal);
    }

    [Fact]
    public void CreateRenderResult_Should_ColorAnOpenNodeAsBlocked_When_ItsBoardStatusIsBlocked()
    {
        // arrange: an "open" task the Board would place in the Blocked column
        // (an unmet dependency, an epic with open children, ...) must render
        // with the same red the Board uses, while its glyph stays the open
        // circle -- the raw status, not the board status, drives the glyph.
        var view = new GraphCanvasView(Model(
            [Node("task", status: TaskStates.Open, boardStatus: TaskStates.Blocked)]));
        view.SelectTask(null);
        var node = view.Layout.FindNode("task")!;
        var blockedStyle = ThemeTokens.GetStyle("board.column.status.blocked");

        // act
        var buffer = view.CreateRenderResult().Buffer;
        var output = Render(view, 80, 8, ansi: true);

        // assert
        Assert.Equal(
            (blockedStyle, blockedStyle),
            (buffer.Get(node.X, node.Y).Style, buffer.Get(node.X + 1, node.Y + 1).Style));
        AnsiAssertions.AssertAnsiStylePrefixesText(output, "board.column.status.blocked", "┌");
        Assert.Contains("○", output, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(TaskStates.Blocked, "board.column.status.blocked")]
    [InlineData(TaskStates.Deferred, "board.column.status.deferred")]
    [InlineData(TaskStates.Open, "board.column.status.ready")]
    [InlineData(TaskStates.InProgress, "board.column.status.inprogress")]
    [InlineData(TaskStates.Closed, "board.column.status.closed")]
    public void CreateRenderResult_Should_UseBoardStatusStyleForCompactGlyphAndId_When_StatusVaries(
        string status,
        string token)
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("task", title: "Neutral title", status: status)]));
        view.ToggleCompact();
        view.SelectTask(null);
        var node = view.Layout.FindNode("task")!;
        var neutralTitle = status == TaskStates.Closed
            ? new Style(decoration: Decoration.Dim)
            : Style.Plain;
        var expectedNodeStyle = ExpectedNodeStyle(status, token);

        // act
        var buffer = view.CreateRenderResult().Buffer;

        // assert
        Assert.Equal(
            (expectedNodeStyle, expectedNodeStyle, neutralTitle),
            (buffer.Get(node.X, node.Y).Style,
                buffer.Get(node.X + 6, node.Y).Style,
                buffer.Get(node.X + 11, node.Y).Style));
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
    public void CreateRenderResult_Should_RenderContainedSearchHitsForSelectedCollapsedEpics_When_NodeModeChanges()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("epic", hiddenChildCount: 3)]));
        view.SelectTask("epic");
        view.SetMatchIds([], new Dictionary<string, int>(StringComparer.Ordinal) { ["epic"] = 2 });

        // act
        var boxed = view.CreateRenderResult();
        var boxedText = boxed.Buffer.ToText(boxed.Viewport);
        view.ToggleCompact();
        var compact = view.CreateRenderResult();
        var compactText = compact.Buffer.ToText(compact.Viewport);
        var node = view.Layout.FindNode("epic")!;

        // assert
        Assert.Contains("hits 2", boxedText, StringComparison.Ordinal);
        Assert.Contains("hits 2", compactText, StringComparison.Ordinal);
        Assert.Equal(ThemeTokens.GetStyle("selection.highlight").Background, compact.Buffer.Get(node.X, node.Y).Style.Background);
        Assert.Equal(ThemeTokens.GetStyle("selection.highlight").Foreground, compact.Buffer.Get(node.X, node.Y).Style.Foreground);
    }

    [Fact]
    public void CreateRenderResult_Should_ApplySelectionBeforeStatusAndKeepTerminalDim_When_ClosedNodeIsSelected()
    {
        // arrange
        var view = new GraphCanvasView(Model([Node("closed", status: TaskStates.Closed)]));
        view.SelectTask("closed");
        var node = view.Layout.FindNode("closed")!;
        var selection = ThemeTokens.GetStyle("selection.highlight");
        var expected = new Style(
            selection.Foreground,
            selection.Background,
            selection.Decoration | Decoration.Dim);

        // act
        var boxed = view.CreateRenderResult().Buffer;
        var styles = new[]
        {
            boxed.Get(node.X, node.Y).Style,
            boxed.Get(node.X + 1, node.Y + 1).Style,
            boxed.Get(node.X + 3, node.Y + 1).Style,
            boxed.Get(node.X + 7, node.Y + 1).Style,
            boxed.Get(node.X + 1, node.Y + 2).Style
        };
        view.ToggleCompact();
        node = view.Layout.FindNode("closed")!;
        var compact = view.CreateRenderResult().Buffer;
        styles =
        [
            .. styles,
            compact.Get(node.X, node.Y).Style,
            compact.Get(node.X + 2, node.Y).Style,
            compact.Get(node.X + 6, node.Y).Style,
            compact.Get(node.X + 13, node.Y).Style
        ];

        // assert
        Assert.Equal(Enumerable.Repeat(expected, styles.Length), styles);
    }

    [Theory]
    [InlineData(TaskStates.Blocked)]
    [InlineData(TaskStates.Deferred)]
    [InlineData(TaskStates.Open)]
    [InlineData(TaskStates.InProgress)]
    [InlineData(TaskStates.Closed)]
    public void CreateRenderResult_Should_DistinguishSelectedEdgesFromStatusBorders_When_StatusVaries(string status)
    {
        // arrange
        var selectedEdge = Edge("a", "b");
        var unrelatedEdge = Edge("c", "d");
        var view = new GraphCanvasView(Model(
            [Node("a"), Node("b"), Node("c"), Node("d"), Node("status", status: status)],
            [selectedEdge, unrelatedEdge]));
        view.SelectTask("a");

        // act
        var result = view.CreateRenderResult();
        var selectedStyle = new Style(
            GraphEdgeStyles.Line.Foreground,
            ThemeTokens.GetStyle("selection.highlight").Background,
            GraphEdgeStyles.Line.Decoration
                | ThemeTokens.GetStyle("selection.highlight").Decoration);
        var styles = result.Routes
            .GroupBy(route => route.Span.Edge)
            .ToDictionary(
                group => group.Key,
                group => group.SelectMany(route => route.Points)
                    .Select(point => result.Buffer.Get(point.X, point.Y).Style)
                    .Distinct()
                    .ToArray());

        // assert
        Assert.Equal([selectedStyle], styles[selectedEdge]);
        Assert.Equal([ThemeTokens.GetStyle("board.column.border")], styles[unrelatedEdge]);
        Assert.NotEqual(
            result.Buffer.Get(view.Layout.FindNode("status")!.X, view.Layout.FindNode("status")!.Y).Style,
            styles[selectedEdge][0]);
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
        string? boardStatus = null,
        string type = TaskTypes.Task,
        string? assignee = null,
        int hiddenChildCount = 0)
        => new()
        {
            Id = id,
            Title = title ?? id,
            Status = status,
            BoardStatus = boardStatus ?? status,
            Type = type,
            Priority = 2,
            Assignee = assignee,
            HiddenChildCount = hiddenChildCount
        };

    private static GraphEdge Edge(string fromId, string toId)
        => new(fromId, toId, GraphEdgeKind.Blocks);

    private static int[] GetExpectedWaveCaptionPositions(GraphLayoutResult layout)
        => layout.Nodes
            .GroupBy(node => node.Layer)
            .OrderBy(group => group.Key)
            .Select(group =>
            {
                var x = group.Min(node => node.X);
                var right = group.Max(node => node.X + node.Width);
                var captionLength = $"wave {group.Key + 1}".Length;
                return x + ((right - x - captionLength) / 2);
            })
            .ToArray();

    private static int[] GetWaveCaptionPositions(string output, int count)
    {
        var header = output.Split(Environment.NewLine, StringSplitOptions.None)[0];
        return Enumerable.Range(1, count)
            .Select(wave => header.IndexOf($"wave {wave}", StringComparison.Ordinal))
            .ToArray();
    }

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

    /// <summary>
    /// The board status style, adjusted for closed/terminal statuses to match
    /// <c>GraphCanvasNodeRenderer.Compose</c>, which dims every terminal-status
    /// node's border and glyph regardless of the theme token's own decoration.
    /// </summary>
    private static Style ExpectedNodeStyle(string status, string token)
    {
        var style = ThemeTokens.GetStyle(token);
        return TaskStates.IsTerminal(status)
            ? new Style(style.Foreground, style.Background, style.Decoration | Decoration.Dim)
            : style;
    }
}

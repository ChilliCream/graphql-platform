using ChilliCream.Nitro.Client.Telemetry.Models;
using ChilliCream.Nitro.CommandLine.Commands.Telemetry.Traces.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Commands.Telemetry.Traces.Tree;

public sealed class SpanSelectionTests
{
    [Fact]
    public void SelectOverview_Should_PutErrorSpansFirst_When_RootsHaveDifferentPriorities()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("slow", duration: 100),
            CreateSpan("error", status: "ERROR"),
            CreateSpan("fast")
        ]);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 3);

        // assert
        Assert.Equal(
            ["error", "slow", "fast"],
            selection.Spans.Select(static span => span.Node.Span.SpanId));
    }

    [Fact]
    public void SelectOverview_Should_RespectMaximumDepth_When_TreeIsDeeperThanLimit()
    {
        // arrange
        var spans = new List<TraceSpan>();
        for (var i = 0; i < 10; i++)
        {
            spans.Add(CreateSpan($"span-{i}", i == 0 ? "" : $"span-{i - 1}"));
        }

        var tree = SpanTreeBuilder.Build(spans);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumDepth: 3);

        // assert
        Assert.Equal(4, selection.Count);
        Assert.Equal(3, selection.Spans.Max(static span => span.Depth));
    }

    [Fact]
    public void SelectOverview_Should_PrioritizeNestedErrorBeforeChildCap_WhenParentIsFast()
    {
        // arrange
        var spans = new List<TraceSpan> { CreateSpan("root") };
        spans.AddRange(Enumerable.Range(0, 24).Select(i => CreateSpan($"slow-{i}", "root", 100)));
        spans.Add(CreateSpan("context", "root"));
        spans.Add(CreateSpan("nested-error", "context", status: "ERROR"));
        var tree = SpanTreeBuilder.Build(spans);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 3);

        // assert
        Assert.Equal(
            ["root", "context", "nested-error"],
            selection.Spans.Select(static span => span.Node.Span.SpanId).ToArray());
    }

    [Fact]
    public void SelectOverview_Should_RankTargetsByOwnPriority_When_NestedSlowSpanCompetesWithSlowRoot()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("slow-root", duration: 100),
            CreateSpan("context-root"),
            CreateSpan("nested-slow", "context-root", duration: 100)
        ]);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 3);

        // assert
        Assert.Equal(
            ["slow-root", "context-root", "nested-slow"],
            selection.Spans.Select(static span => span.Node.Span.SpanId).ToArray());
    }

    [Fact]
    public void SelectOverview_Should_RespectMaximumChildrenPerParent_When_ParentHasManyChildren()
    {
        // arrange
        var spans = new List<TraceSpan> { CreateSpan("root") };
        spans.AddRange(Enumerable.Range(0, 30).Select(i => CreateSpan($"child-{i}", "root")));
        var tree = SpanTreeBuilder.Build(spans);

        // act
        var selection = SpanSelection.SelectOverview(tree);

        // assert
        Assert.Equal(25, selection.Count);
        Assert.Equal(24, selection.Spans.Count(static span => span.Node.Parent?.Span.SpanId == "root"));
    }

    [Fact]
    public void SelectOverview_Should_CapOverview_When_TreeHasMoreThanNinetySixSpans()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
            Enumerable.Range(0, 120).Select(i => CreateSpan($"span-{i}")).ToArray());

        // act
        var selection = SpanSelection.SelectOverview(tree);

        // assert
        Assert.Equal(SpanSelection.MaximumOverviewSpans, selection.Count);
    }

    [Fact]
    public void SelectOverview_Should_SelectEqualPriorityErrorPathsIndependentlyOfSpanId_When_GlobalCapIsReached()
    {
        // arrange
        var firstTree = CreateErrorBranches(i => $"a{i:D3}", i => $"z{i:D3}");
        var secondTree = CreateErrorBranches(i => $"z{i:D3}", i => $"a{i:D3}");

        // act
        var firstSelection = SpanSelection.SelectOverview(firstTree);
        var secondSelection = SpanSelection.SelectOverview(secondTree);

        // assert
        Assert.Equal(SpanSelection.MaximumOverviewSpans, firstSelection.Count);
        Assert.Equal(48, firstSelection.Spans.Count(static entry => SpanSelection.IsError(entry.Node.Span)));
        Assert.Equal(
            firstSelection.Spans.Select(static entry => entry.Node.Span.SpanName),
            secondSelection.Spans.Select(static entry => entry.Node.Span.SpanName));
        Assert.Equal(
            Enumerable.Range(0, 48).SelectMany(i => new[] { $"root-{i}", $"error-{i}" }),
            firstSelection.Spans.Select(static entry => entry.Node.Span.SpanName));
    }

    [Theory]
    [InlineData(95, 47)]
    [InlineData(96, 48)]
    [InlineData(97, 48)]
    [InlineData(98, 49)]
    public void SelectOverview_Should_AdmitCompletePaths_When_GlobalCapacityIsAtBoundary(
        int maximumSpans,
        int expectedErrorCount)
    {
        // arrange
        var tree = CreateErrorBranches(i => $"root-{i}", i => $"error-{i}");

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: maximumSpans);
        var selectedNodes = selection.Spans.Select(static entry => entry.Node).ToArray();

        // assert
        Assert.Equal(maximumSpans, selection.Count);
        Assert.Equal(expectedErrorCount, selection.Spans.Count(static entry => SpanSelection.IsError(entry.Node.Span)));
        Assert.Equal(
            maximumSpans - expectedErrorCount,
            selection.Spans.Count(static entry => !SpanSelection.IsError(entry.Node.Span)));
        Assert.All(
            selection.Spans.Where(static entry => SpanSelection.IsError(entry.Node.Span)),
            entry => Assert.Contains(entry.Node.Parent!, selectedNodes));
        Assert.All(
            selection.Spans.Where(static entry => !SpanSelection.IsError(entry.Node.Span)),
            static entry => Assert.Null(entry.Node.Parent));
    }

    [Fact]
    public void SelectOverview_Should_ContinueToLaterTarget_When_HigherPriorityPathCannotFit()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("context-root"),
            CreateSpan("nested-error", "context-root", status: "ERROR"),
            CreateSpan("slow-root", duration: 100)
        ]);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 1);

        // assert
        Assert.Equal(["slow-root"], selection.Spans.Select(static entry => entry.Node.Span.SpanId));
    }

    [Fact]
    public void SelectOverview_Should_ChargeOverlappingPathsOnce_When_ErrorsShareAncestors()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("root"),
            CreateSpan("shared", "root"),
            CreateSpan("error-1", "shared", status: "ERROR"),
            CreateSpan("error-2", "shared", status: "ERROR")
        ]);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 4);

        // assert
        Assert.Equal(
            ["root", "shared", "error-1", "error-2"],
            selection.Spans.Select(static entry => entry.Node.Span.SpanId));
    }

    [Fact]
    public void SelectOverview_Should_IncludeOnlyEligibleDepth_When_ErrorIsAtOrBeyondDepthBoundary()
    {
        // arrange
        var depthEightTree = CreateDeepTree(8);
        var depthNineTree = CreateDeepTree(9);

        // act
        var depthEightSelection = SpanSelection.SelectOverview(depthEightTree, maximumSpans: 9, maximumDepth: 8);
        var depthNineSelection = SpanSelection.SelectOverview(depthNineTree, maximumSpans: 10, maximumDepth: 8);

        // assert
        Assert.Equal(9, depthEightSelection.Count);
        Assert.Equal("span-8", depthEightSelection.Spans[^1].Node.Span.SpanId);
        Assert.Equal(9, depthNineSelection.Count);
        Assert.All(
            depthNineSelection.Spans,
            static entry => Assert.True(entry.Depth <= 8 && entry.Node.Span.SpanId != "span-9"));
    }

    [Fact]
    public void SelectOverview_Should_UseEncounterOrderForEqualChildren_When_SpanIdsArePermuted()
    {
        // arrange
        var firstTree = CreateErrorChildren(i => $"a{i:D2}");
        var secondTree = CreateErrorChildren(i => $"z{24 - i:D2}");

        // act
        var firstSelection = SpanSelection.SelectOverview(firstTree, maximumSpans: 26);
        var secondSelection = SpanSelection.SelectOverview(secondTree, maximumSpans: 26);

        // assert
        var expected = Enumerable.Range(0, 24).Select(i => $"child-{i}").Prepend("root");
        Assert.Equal(expected, firstSelection.Spans.Select(static entry => entry.Node.Span.SpanName));
        Assert.Equal(expected, secondSelection.Spans.Select(static entry => entry.Node.Span.SpanName));
    }

    [Fact]
    public void SelectOverview_Should_DisplaceLowerPriorityChild_When_ChildLimitIsReached()
    {
        // arrange
        var tree = CreateErrorChildren(
            i => $"child-{i}",
            i => i == 24 ? 100 : 1);

        // act
        var selection = SpanSelection.SelectOverview(tree, maximumSpans: 26);

        // assert
        Assert.Equal(24, selection.Spans.Count(static entry => SpanSelection.IsError(entry.Node.Span)));
        Assert.Equal(
            Enumerable.Range(0, 23).Append(24),
            selection.Spans
                .Where(static entry => SpanSelection.IsError(entry.Node.Span))
                .Select(static entry => int.Parse(entry.Node.Span.SpanName.AsSpan("child-".Length)))
                .OrderBy(static i => i));
    }

    [Fact]
    public void SelectFocused_Should_AdmitErrorPathsUntilFocusedCap_When_BranchesShareFocusRoot()
    {
        // arrange
        var spans = new List<TraceSpan> { CreateSpan("focus") };
        for (var branchIndex = 0; branchIndex < 5; branchIndex++)
        {
            var branchId = $"branch-{branchIndex}";
            spans.Add(CreateSpan(branchId, "focus"));
            spans.AddRange(
                Enumerable.Range(0, 10).Select(errorIndex =>
                    CreateSpan($"error-{branchIndex}-{errorIndex}", branchId, status: "ERROR")));
        }

        var tree = SpanTreeBuilder.Build(spans);

        // act
        var selection = SpanSelection.SelectFocused(tree, "focus");
        var selectedNodes = selection.Spans.Select(static entry => entry.Node).ToArray();

        // assert
        Assert.Equal(SpanSelection.MaximumFocusedSpans, selection.Count);
        Assert.Equal(35, selection.Spans.Count(static entry => SpanSelection.IsError(entry.Node.Span)));
        Assert.Equal(0, selection.Spans[0].Depth);
        Assert.All(
            selection.Spans.Where(static entry => SpanSelection.IsError(entry.Node.Span)),
            entry => Assert.Contains(entry.Node.Parent!, selectedNodes));
        Assert.All(
            selection.Spans.GroupBy(static entry => entry.Node.Parent),
            static group => Assert.InRange(group.Count(), 1, SpanSelection.MaximumChildrenPerParent));
    }

    [Fact]
    public void SelectOverviewAndFocused_Should_Terminate_When_ParentLinksFormCycle()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("first", "second"),
            CreateSpan("second", "first")
        ]);

        // act
        var overview = SpanSelection.SelectOverview(tree);
        var focusedSelections = new[]
        {
            SpanSelection.SelectFocused(tree, "first"),
            SpanSelection.SelectFocused(tree, "second")
        };

        // assert
        Assert.Empty(overview.Spans);
        Assert.Equal(2, overview.TotalSpanCount);
        Assert.All(focusedSelections, static selection => Assert.Equal(2, selection.Count));
        Assert.All(focusedSelections, static selection => Assert.Equal(2, selection.TotalSpanCount));
    }

    [Fact]
    public void SelectFocused_Should_StartAtRequestedSpan_When_SpanIsNested()
    {
        // arrange
        var tree = SpanTreeBuilder.Build(
        [
            CreateSpan("root"),
            CreateSpan("focused", "root"),
            CreateSpan("child", "focused")
        ]);

        // act
        var selection = SpanSelection.SelectFocused(tree, "focused");

        // assert
        Assert.Equal(2, selection.Count);
        Assert.Equal("focused", selection.Spans[0].Node.Span.SpanId);
        Assert.Equal(0, selection.Spans[0].Depth);
        Assert.Equal("child", selection.Spans[1].Node.Span.SpanId);
    }

    private static SpanTree CreateErrorBranches(
        Func<int, string> rootId,
        Func<int, string> errorId)
    {
        var spans = new List<TraceSpan>();
        for (var i = 0; i < 96; i++)
        {
            var currentRootId = rootId(i);
            spans.Add(CreateSpan(currentRootId, name: $"root-{i}"));
            spans.Add(
                CreateSpan(
                    errorId(i),
                    currentRootId,
                    status: "ERROR",
                    name: $"error-{i}"));
        }

        return SpanTreeBuilder.Build(spans);
    }

    private static SpanTree CreateDeepTree(int errorDepth)
    {
        var spans = new List<TraceSpan>();
        for (var depth = 0; depth <= errorDepth; depth++)
        {
            spans.Add(
                CreateSpan(
                    $"span-{depth}",
                    depth == 0 ? "" : $"span-{depth - 1}",
                    status: depth == errorDepth ? "ERROR" : "OK"));
        }

        return SpanTreeBuilder.Build(spans);
    }

    private static SpanTree CreateErrorChildren(
        Func<int, string> childId,
        Func<int, double>? duration = null)
    {
        var spans = new List<TraceSpan> { CreateSpan("root", name: "root") };
        for (var i = 0; i < 25; i++)
        {
            spans.Add(
                CreateSpan(
                    childId(i),
                    "root",
                    duration: duration?.Invoke(i) ?? 1,
                    status: "ERROR",
                    name: $"child-{i}"));
        }

        return SpanTreeBuilder.Build(spans);
    }

    private static TraceSpan CreateSpan(
        string id,
        string parent = "",
        double duration = 1,
        string status = "OK",
        string? name = null)
        => new(
            id,
            parent,
            name ?? id,
            "SERVER",
            duration,
            0,
            status,
            string.Empty,
            [],
            [],
            [],
            null);
}

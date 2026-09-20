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
        Assert.Equal(3, selection.Count);
        Assert.Equal("error", selection.Spans[0].Node.Span.SpanId);
        Assert.Equal("slow", selection.Spans[1].Node.Span.SpanId);
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
    public void SelectOverview_Should_PrioritizeNestedSlowBranchBeforeGlobalCap_WhenParentIsFast()
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
            ["context-root", "nested-slow", "slow-root"],
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

    private static TraceSpan CreateSpan(
        string id,
        string parent = "",
        double duration = 1,
        string status = "OK")
        => new(
            id,
            parent,
            id,
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

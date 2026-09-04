using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console;
using Spectre.Console.Rendering;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets;

public sealed class TaskBadgeTests
{
    [Fact]
    public void Render_Should_BuildBadgeLine_When_Unselected()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Fix bug",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 80);

        // assert
        line.MatchInlineSnapshot(
            "  [grey70]○[/] [grey70][[T]][/] [yellow]P2[/] T-1 Fix bug");
    }

    [Fact]
    public void Render_Should_PrefixAndHighlight_When_Selected()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Fix bug",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: true,
            maxWidth: 80);

        // assert
        line.MatchInlineSnapshot(
            "[default on grey35]> [grey70]○[/] [grey70][[T]][/] [yellow]P2[/] T-1 Fix bug[/]");
    }

    [Theory]
    [InlineData(TaskStates.Closed, "✓")]
    [InlineData(TaskStates.InProgress, "●")]
    [InlineData(TaskStates.Open, "○")]
    [InlineData(TaskStates.Deferred, "⏸")]
    [InlineData(TaskStates.Blocked, "⊘")]
    public void Render_Should_UseGlyph_ForEachKnownStatus(string status, string expectedGlyph)
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: status,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 80);

        // assert
        Assert.Contains(expectedGlyph, line);
    }

    [Theory]
    [InlineData(TaskTypes.Bug, "B")]
    [InlineData(TaskTypes.Feature, "F")]
    [InlineData(TaskTypes.Task, "T")]
    [InlineData(TaskTypes.Epic, "E")]
    [InlineData(TaskTypes.Chore, "C")]
    [InlineData(TaskTypes.Docs, "D")]
    [InlineData(TaskTypes.Question, "Q")]
    public void Render_Should_UseTypeCode_ForEachKnownType(string type, string expectedCode)
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: type,
            selected: false,
            maxWidth: 80);

        // assert
        Assert.Contains($"[[{expectedCode}]]", line);
    }

    [Fact]
    public void Render_Should_FallBackToFirstLetter_When_TypeUnknown()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: "design",
            selected: false,
            maxWidth: 80);

        // assert
        Assert.Contains("[[D]]", line);
    }

    [Fact]
    public void Render_Should_TruncateTitleWithEllipsis_When_LineExceedsMaxWidth()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Fix bug and more stuff",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 20);

        // assert
        line.MatchInlineSnapshot(
            "  [grey70]○[/] [grey70][[T]][/] [yellow]P2[/] T-1 Fix …");
    }

    [Fact]
    public void Render_Should_EscapeTruncatedTitle_When_TruncationCutsThroughBracketChar()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "[URGENT] fix this",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 18);

        // assert
        line.MatchInlineSnapshot(
            "  [grey70]○[/] [grey70][[T]][/] [yellow]P2[/] T-1 [[U…");
    }

    [Fact]
    public void Render_Should_ReturnEmpty_When_MaxWidthIsZero()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 0);

        // assert
        Assert.Equal(string.Empty, line);
    }

    [Fact]
    public void Render_Should_RenderAsValidMarkup_When_TypeIsKnownAndSelected()
    {
        // arrange
        var console = new TestConsole().Width(80);
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Fix bug",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: true,
            maxWidth: 80);

        // act
        var exception = Record.Exception(() => console.Write(new Markup(line)));

        // assert
        Assert.Null(exception);
        Assert.Contains("Fix bug", console.Output);
    }

    [Fact]
    public void Render_Should_RenderAsValidMarkup_When_TypeIsUnknown()
    {
        // arrange
        var console = new TestConsole().Width(80);
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: "design",
            selected: false,
            maxWidth: 80);

        // act
        var exception = Record.Exception(() => console.Write(new Markup(line)));

        // assert
        Assert.Null(exception);
        Assert.Contains("D", console.Output);
    }

    [Fact]
    public void Render_Should_RenderAsValidMarkup_When_StatusIsTombstone()
    {
        // arrange
        var console = new TestConsole().Width(80);
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Tombstone,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 80);

        // act
        var exception = Record.Exception(() => console.Write(new Markup(line)));

        // assert
        Assert.Null(exception);
        Assert.Contains("Title", console.Output);
    }

    [Fact]
    public void Render_Should_RenderAsValidMarkup_When_PriorityIsOutOfRange()
    {
        // arrange
        var console = new TestConsole().Width(80);
        var line = TaskBadge.Render(
            id: "T-1",
            title: "Title",
            status: TaskStates.Open,
            priority: 7,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 80);

        // act
        var exception = Record.Exception(() => console.Write(new Markup(line)));

        // assert
        Assert.Null(exception);
        Assert.Contains("P7", console.Output);
    }

    [Fact]
    public void Render_Should_TruncateIdWithEllipsis_When_FixedSegmentsAloneOverflow()
    {
        // act: a 20-char id alone (with type and priority) already exceeds
        // maxWidth 14, so the title is dropped and the id is truncated.
        var line = TaskBadge.Render(
            id: "fusion-demo-yt-ezu.1",
            title: "Some title",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 14);

        // assert
        var plain = PlainCellLength(line);
        Assert.True(plain <= 14, $"Expected width <= 14 but was {plain}.");
        Assert.DoesNotContain("Some title", line);
        Assert.EndsWith("…", Markup.Remove(line));
    }

    [Theory]
    [InlineData(6)]
    [InlineData(1)]
    public void Render_Should_StayWithinMaxWidth_When_ExtremelyNarrow(int maxWidth)
    {
        // act
        var line = TaskBadge.Render(
            id: "fusion-demo-yt-ezu.1",
            title: "Some title that is quite long",
            status: TaskStates.Blocked,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: maxWidth);

        // assert
        Assert.False(string.IsNullOrEmpty(line));
        Assert.True(
            PlainCellLength(line) <= maxWidth,
            $"Expected width <= {maxWidth} but was {PlainCellLength(line)} ('{Markup.Remove(line)}').");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Render_Should_NeverExceedMaxWidth_When_SweepingAcrossNarrowWidths(bool selected)
    {
        // arrange: a long id and a long title so every degradation step
        // (title truncation, title drop, id truncation, priority/type drop)
        // gets exercised somewhere across the sweep.
        for (var maxWidth = 1; maxWidth <= 60; maxWidth++)
        {
            // act
            var line = TaskBadge.Render(
                id: "fusion-demo-yt-ezu.1",
                title: "A rather long title that will not fit in a narrow column",
                status: TaskStates.Blocked,
                priority: TaskPriorities.Critical,
                type: TaskTypes.Feature,
                selected: selected,
                maxWidth: maxWidth);

            // assert
            var plain = Markup.Remove(line);
            var width = PlainCellLength(line);
            Assert.True(
                width <= maxWidth,
                $"maxWidth {maxWidth}: expected width <= {maxWidth} but was {width} ('{plain}').");
        }
    }

    [Fact]
    public void Render_Should_NotSplitWideGrapheme_When_TitleIsCjkOrEmoji()
    {
        // act
        var line = TaskBadge.Render(
            id: "T-1",
            title: "中文標題😀更多文字",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: 20);

        // assert
        var width = PlainCellLength(line);
        Assert.True(width <= 20, $"Expected width <= 20 but was {width}.");
    }

    [Fact]
    public void Render_Should_DropTitleAndSeparatingSpace_When_TitleBudgetBelowTwoCells()
    {
        // arrange: id fits with type and priority, leaving exactly 1 cell of
        // title budget, which rule (b) says must drop the title entirely
        // rather than render a lone ellipsis. The fixed width is derived
        // from the same pieces Render composes (prefix, glyph, bracketed
        // type, priority, id, one separator each) rather than hard-coded,
        // so it stays correct if any glyph's display width changes.
        const string id = "T-1";
        var fixedWidthWithoutTitle =
            new Segment("  ").CellCount()
            + new Segment(TaskGlyphs.Status(TaskStates.Open)).CellCount() + 1
            + new Segment(TaskGlyphs.TypeCode(TaskTypes.Task)).CellCount() + 2 + 1
            + new Segment(TaskPriorities.Format(TaskPriorities.Medium)).CellCount() + 1
            + new Segment(id).CellCount();
        var maxWidth = fixedWidthWithoutTitle + 1;

        // act
        var line = TaskBadge.Render(
            id: id,
            title: "Anything",
            status: TaskStates.Open,
            priority: TaskPriorities.Medium,
            type: TaskTypes.Task,
            selected: false,
            maxWidth: maxWidth);

        // assert
        Assert.DoesNotContain("Anything", line);
        Assert.DoesNotContain("…", Markup.Remove(line));
        Assert.EndsWith(id, Markup.Remove(line));
    }

    private static int PlainCellLength(string line) => new Segment(Markup.Remove(line)).CellCount();
}

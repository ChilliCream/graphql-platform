using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console;
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
            "[default on aqua]> [grey70]○[/] [grey70][[T]][/] [yellow]P2[/] T-1 Fix bug[/]");
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
}

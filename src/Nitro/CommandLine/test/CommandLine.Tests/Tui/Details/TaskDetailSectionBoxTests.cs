using System.Text.RegularExpressions;
using ChilliCream.Nitro.CommandLine.Tui.Details;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Details;

public sealed partial class TaskDetailSectionBoxTests
{
    [Fact]
    public void Render_Should_ReturnEmpty_When_TextIsEmpty()
    {
        // act
        var lines = TaskDetailSectionBox.Render("Notes", "", 20);

        // assert
        Assert.Empty(lines);
    }

    [Fact]
    public void Render_Should_ProduceRoundedBoxWithTitleAndPadding()
    {
        // act
        var lines = TaskDetailSectionBox.Render("Notes", "hi", 10);

        // assert: a rounded border carries the title, one padded content row
        // holds the text, and the border closes with a matching bottom row.
        Assert.Equal(
            [
                new TaskDetailBodyLine("╭─Notes──╮", IsMarkup: true),
                new TaskDetailBodyLine("│ hi     │", IsMarkup: false),
                new TaskDetailBodyLine("╰────────╯", IsMarkup: true)
            ],
            StripStyle(lines));
    }

    [Fact]
    public void Render_Should_WrapContent_AtInteriorWidth_NotBoxWidth()
    {
        // act: at a 10-column box, the 4-column border-and-padding chrome
        // leaves a 6-column interior, so "alpha beta gamma" wraps to three
        // rows, not the two it would take if wrapped at the full box width.
        var lines = TaskDetailSectionBox.Render("Notes", "alpha beta gamma", 10);

        // assert
        Assert.Equal(
            [
                "╭─Notes──╮",
                "│ alpha  │",
                "│ beta   │",
                "│ gamma  │",
                "╰────────╯"
            ],
            StripStyle(lines).Select(l => l.Content));
    }

    [Fact]
    public void Render_Should_ProduceOneContentRow_ForShortNonEmptyText()
    {
        // act
        var lines = TaskDetailSectionBox.Render("Notes", "hi", 40);

        // assert: top border, one content row, bottom border.
        Assert.Equal(3, lines.Count);
    }

    [Fact]
    public void Render_Should_NotThrow_When_TitleIsLongerThanWidth()
    {
        // act
        var exception = Record.Exception(() => TaskDetailSectionBox.Render("A very long section title", "text", 5));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_NotThrow_When_WidthIsNarrowerThanChrome()
    {
        // act
        var exception = Record.Exception(() => TaskDetailSectionBox.Render("Notes", "text", 1));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_PreserveBracketsInContent_When_RenderedThroughSpectre()
    {
        // arrange: the content row is plain, unescaped text; Spectre.Console
        // escapes it at render time the same way it escapes any other
        // plain-text body line.
        var lines = TaskDetailSectionBox.Render("Notes", "[greeting]", 20);
        var console = new TestConsole().Width(20);

        // act
        console.Write(new Markup(Markup.Escape(lines[1].Content)));

        // assert
        Assert.Contains("[greeting]", console.Output);
    }

    private static IReadOnlyList<TaskDetailBodyLine> StripStyle(IReadOnlyList<TaskDetailBodyLine> lines)
        => lines.Select(l => l with { Content = StripMarkupTags(l.Content) }).ToList();

    private static string StripMarkupTags(string line) => MarkupTagPattern().Replace(line, "");

    [GeneratedRegex(@"\[[^\]]*\]")]
    private static partial Regex MarkupTagPattern();
}

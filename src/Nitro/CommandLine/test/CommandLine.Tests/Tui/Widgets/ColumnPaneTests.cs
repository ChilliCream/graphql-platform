using ChilliCream.Nitro.CommandLine.Tui.Theming;
using ChilliCream.Nitro.CommandLine.Tui.Widgets;
using Spectre.Console;
using Spectre.Console.Testing;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets;

public sealed class ColumnPaneTests
{
    [Fact]
    public void Render_Should_TitlePanelWithNameAndCount()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 3, ["one", "two"], focused: false);

        // assert
        Assert.Equal("Backlog (3)", panel.Header?.Text);
    }

    [Fact]
    public void Render_Should_UseRoundedBorder()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 0, [], focused: false);

        // assert
        Assert.Equal(BoxBorder.Rounded, panel.Border);
    }

    [Fact]
    public void Render_Should_UseFocusedBorderStyle_When_Focused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 0, [], focused: true);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.border.focused"), panel.BorderStyle);
    }

    [Fact]
    public void Render_Should_UseUnfocusedBorderStyle_When_NotFocused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 0, [], focused: false);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.border"), panel.BorderStyle);
    }

    [Fact]
    public void Render_Should_RenderContentLines()
    {
        // arrange
        var console = new TestConsole().Width(40);
        var panel = ColumnPane.Render("Backlog", 2, ["first row", "second row"], focused: false);

        // act
        console.Write(panel);

        // assert
        Assert.Contains("first row", console.Output);
        Assert.Contains("second row", console.Output);
    }

    [Fact]
    public void Render_Should_NotThrow_When_NoLines()
    {
        // arrange
        var console = new TestConsole().Width(40);
        var panel = ColumnPane.Render("Backlog", 0, [], focused: false);

        // act
        var exception = Record.Exception(() => console.Write(panel));

        // assert
        Assert.Null(exception);
    }

    [Fact]
    public void Render_Should_NotFoldRowOntoExtraLines_When_LineIsAnOverlongUnbreakableToken()
    {
        // arrange: TaskBadge.Render always budgets its line to the column
        // width, so this only guards a caller that (by mistake, or in the
        // future) hands ColumnPane a raw, un-budgeted single token with no
        // spaces to wrap on - the same shape as the reported bug's overlong
        // task id. Without an overflow mode, Spectre folds such a token
        // across as many extra lines as it takes, silently pushing the
        // panel past the height the board budgeted for it.
        var overlongToken = new string('a', 60);
        var panel = ColumnPane.Render("Blocked", 1, [overlongToken], focused: false);
        var console = new TestConsole().Width(20);

        // act
        console.Write(panel);

        // assert: exactly one content row between the panel's top and
        // bottom border, ellipsis-cropped rather than wrapped.
        var lines = TrimTrailingNewline(console.Output.Split('\n'));
        Assert.Equal(3, lines.Length);
        Assert.Contains('…', lines[1]);
    }

    /// <summary>
    /// Spectre appends a trailing line break after a bare panel, so
    /// splitting console output on '\n' can leave one extra empty entry at
    /// the end.
    /// </summary>
    private static string[] TrimTrailingNewline(string[] lines) =>
        lines.Length > 0 && lines[^1].Length == 0 ? lines[..^1] : lines;
}

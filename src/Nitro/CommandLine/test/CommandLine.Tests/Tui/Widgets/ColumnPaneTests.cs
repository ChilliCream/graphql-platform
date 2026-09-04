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
    public void Render_Should_UseRoundedBorder_When_NotFocused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 0, [], focused: false);

        // assert
        Assert.Equal(BoxBorder.Rounded, panel.Border);
    }

    [Fact]
    public void Render_Should_UseHeavyBorder_When_Focused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 0, [], focused: true);

        // assert: a heavier box, not bold text, is what marks focus - see
        // https://github.com/ (hc-11-04h): bold box-drawing glyphs render
        // misaligned in some terminals.
        Assert.Equal(BoxBorder.Heavy, panel.Border);
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
    public void Render_Should_NeverBoldTheBorderStyle_RegardlessOfFocus()
    {
        // act
        var focused = ColumnPane.Render("Backlog", 0, [], focused: true);
        var unfocused = ColumnPane.Render("Backlog", 0, [], focused: false);

        // assert
        Assert.Equal(Decoration.None, focused.BorderStyle?.Decoration);
        Assert.Equal(Decoration.None, unfocused.BorderStyle?.Decoration);
    }

    [Fact]
    public void Render_Should_BoldTheHeaderText_When_Focused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 3, [], focused: true);

        // assert: PanelHeader.SetStyle is a no-op stub in this Spectre
        // version - a panel's header is always painted with the panel's own
        // BorderStyle, so the header can only be bolded independently of the
        // (never-bold) border by wrapping the header markup itself.
        Assert.Equal("[bold]Backlog (3)[/]", panel.Header?.Text);
    }

    [Fact]
    public void Render_Should_NotBoldTheHeaderText_When_NotFocused()
    {
        // act
        var panel = ColumnPane.Render("Backlog", 3, [], focused: false);

        // assert
        Assert.Equal("Backlog (3)", panel.Header?.Text);
    }

    [Fact]
    public void Render_Should_RenderTheHeaderTextBold_When_Focused()
    {
        // arrange: content wide enough that the panel isn't squeezed down to
        // a width too narrow for Spectre to draw the header title at all.
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(40);
        var panel = ColumnPane.Render("Backlog", 3, ["a task row wide enough to size the panel"], focused: true);

        // act
        console.Write(panel);

        // assert: an SGR run opening with the bold parameter (1;...) starts
        // right at the header text and is reset again right after it, so
        // only the title - never the frame's box-drawing glyphs - draws
        // with the bold font face.
        var boldOn = console.Output.IndexOf("[1;", StringComparison.Ordinal);
        var titleIndex = console.Output.IndexOf("Backlog (3)", StringComparison.Ordinal);
        Assert.True(titleIndex >= 0, "Expected the header title to actually render.");
        Assert.True(boldOn >= 0 && boldOn < titleIndex, "Expected bold to turn on before the header title.");
    }

    [Fact]
    public void Render_Should_NotRenderTheHeaderTextBold_When_NotFocused()
    {
        // arrange
        var console = new TestConsole().Colors(ColorSystem.TrueColor).EmitAnsiSequences().Width(40);
        var panel = ColumnPane.Render("Backlog", 3, ["a task row wide enough to size the panel"], focused: false);

        // act
        console.Write(panel);

        // assert
        Assert.Contains("Backlog (3)", console.Output);
        Assert.DoesNotContain("[1;", console.Output);
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

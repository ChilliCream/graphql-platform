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
}

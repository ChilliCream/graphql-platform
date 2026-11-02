using ChilliCream.Nitro.CommandLine.Tui.Board;
using ChilliCream.Nitro.CommandLine.Tui.Theming;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class ColumnDefinitionTests
{
    [Fact]
    public void ResolveBorderStyle_Should_UseNeutralBorderToken_When_NoBorderTokenAssigned_And_NotFocused()
    {
        // arrange
        var definition = new ColumnDefinition { Name = "Backlog" };

        // act
        var style = definition.ResolveBorderStyle(focused: false);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.border"), style);
    }

    [Fact]
    public void ResolveBorderStyle_Should_UseNeutralFocusedBorderToken_When_NoBorderTokenAssigned_And_Focused()
    {
        // arrange
        var definition = new ColumnDefinition { Name = "Backlog" };

        // act
        var style = definition.ResolveBorderStyle(focused: true);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.border.focused"), style);
    }

    [Fact]
    public void ResolveBorderStyle_Should_UseOwnAccentToken_When_BorderTokenAssigned_And_NotFocused()
    {
        // arrange
        var definition = new ColumnDefinition
        {
            Name = "Blocked",
            BorderToken = "board.column.status.blocked"
        };

        // act
        var style = definition.ResolveBorderStyle(focused: false);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.status.blocked"), style);
    }

    [Fact]
    public void ResolveBorderStyle_Should_UseOwnFocusedAccentToken_When_BorderTokenAssigned_And_Focused()
    {
        // arrange
        var definition = new ColumnDefinition
        {
            Name = "Blocked",
            BorderToken = "board.column.status.blocked"
        };

        // act
        var style = definition.ResolveBorderStyle(focused: true);

        // assert
        Assert.Equal(ThemeTokens.GetStyle("board.column.status.blocked.focused"), style);
        Assert.NotEqual(ThemeTokens.GetStyle("board.column.border.focused"), style);
    }
}

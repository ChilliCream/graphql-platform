using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardLayoutTests
{
    [Theory]
    [InlineData(200, 5)]
    [InlineData(24 * 5, 5)]
    public void Decide_Should_ReturnGrid_When_WidthPerColumnMeetsThreshold(int width, int columnCount)
    {
        // act
        var decision = BoardLayout.Decide(width, 20, columnCount, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Grid, decision.Kind);
    }

    [Theory]
    [InlineData(119, 5)]
    [InlineData(23 * 5, 5)]
    public void Decide_Should_ReturnStacked_When_WidthPerColumnBelowThreshold(int width, int columnCount)
    {
        // act
        var decision = BoardLayout.Decide(width, 20, columnCount, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
    }

    [Fact]
    public void Decide_Should_ReturnMaximized_When_MaximizedFlagSet_EvenAtWideWidth()
    {
        // act
        var decision = BoardLayout.Decide(200, 20, 5, focusedColumnIndex: 2, maximized: true);

        // assert
        Assert.Equal(BoardLayoutKind.Maximized, decision.Kind);
    }

    [Fact]
    public void Decide_Should_SumColumnWidthsToTotalWidth_When_Grid()
    {
        // act
        var decision = BoardLayout.Decide(203, 20, 7, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(203, decision.Columns.Sum(c => c.Width));
    }

    [Fact]
    public void Decide_Should_RouteToStacked_When_WidthIsLessThanColumnCount()
    {
        // act: width below the column count routes to stacked instead of grid
        var decision = BoardLayout.Decide(3, 20, 5, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
    }

    [Fact]
    public void Decide_Should_GiveOnlyFocusedColumnFullWidth_When_Maximized()
    {
        // act
        var decision = BoardLayout.Decide(100, 20, 4, focusedColumnIndex: 2, maximized: true);

        // assert
        Assert.Equal(100, decision.Columns[2].Width);
        Assert.True(decision.Columns[2].Expanded);
        Assert.Equal(0, decision.Columns[0].Width);
        Assert.False(decision.Columns[0].Expanded);
        Assert.Equal(0, decision.Columns[1].Width);
        Assert.Equal(0, decision.Columns[3].Width);
    }

    [Fact]
    public void Decide_Should_ExpandEveryColumn_When_StackedWithRoomToShareEqually()
    {
        // act: 30 rows over 3 columns clears the equal-share minimum, so every column expands
        var decision = BoardLayout.Decide(40, 30, 3, focusedColumnIndex: 1, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
        Assert.All(decision.Columns, c => Assert.True(c.Expanded));
    }

    [Fact]
    public void Decide_Should_ShareHeightEqually_When_StackedWithRoomToShareEqually()
    {
        // act: 32 rows minus 2 separator rows leaves 30, which divides evenly across 3 columns
        var decision = BoardLayout.Decide(40, 32, 3, focusedColumnIndex: 1, maximized: false);

        // assert
        Assert.All(decision.Columns, c => Assert.Equal(10, c.Height));
        Assert.Equal(30, decision.Columns.Sum(c => c.Height));
    }

    [Fact]
    public void Decide_Should_GiveRemainderRowsToFirstColumns_When_StackedHeightDoesNotDivideEvenly()
    {
        // act: 31 rows minus 2 separator rows leaves 29, a remainder of 2 across 3 columns
        var decision = BoardLayout.Decide(40, 31, 3, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(10, decision.Columns[0].Height);
        Assert.Equal(10, decision.Columns[1].Height);
        Assert.Equal(9, decision.Columns[2].Height);
        Assert.Equal(29, decision.Columns.Sum(c => c.Height));
    }

    [Fact]
    public void Decide_Should_ExpandFocusedColumnOnly_When_StackedTooShortToShareEqually()
    {
        // act: 12 rows over 3 columns falls below the 5-row equal-share minimum, so only the focused column expands
        var decision = BoardLayout.Decide(40, 12, 3, focusedColumnIndex: 1, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
        Assert.False(decision.Columns[0].Expanded);
        Assert.True(decision.Columns[1].Expanded);
        Assert.False(decision.Columns[2].Expanded);
        Assert.True(decision.Columns[1].Height > decision.Columns[0].Height);
    }

    [Fact]
    public void Decide_Should_FallBackDeterministically_When_SeparatorRowsMakeFiveColumnsTooTightAt24Rows()
    {
        // act: 24 rows is below what 5 columns need to share equally, so only the focused column expands
        var decision = BoardLayout.Decide(40, 24, 5, focusedColumnIndex: 2, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
        Assert.True(decision.Columns[2].Expanded);
        Assert.All(decision.Columns, c => Assert.True(c.Height >= 1));
    }

    [Fact]
    public void Decide_Should_ClampFocusedColumnIndex_When_OutOfRange()
    {
        // act
        var decision = BoardLayout.Decide(100, 20, 3, focusedColumnIndex: 99, maximized: true);

        // assert
        Assert.True(decision.Columns[2].Expanded);
    }

    [Fact]
    public void Decide_Should_ReturnNoColumns_When_ColumnCountIsZero()
    {
        // act
        var decision = BoardLayout.Decide(100, 20, 0, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Empty(decision.Columns);
    }

    [Fact]
    public void Decide_Should_NotThrow_And_KeepHeightsAtLeastOne_When_FrameIsVerySmall()
    {
        // act: a "title plus one row" minimum sane frame.
        var exception = Record.Exception(() =>
        {
            var decision = BoardLayout.Decide(5, 1, 4, focusedColumnIndex: 0, maximized: false);
            Assert.All(decision.Columns, c => Assert.True(c.Height >= 1));
        });

        // assert
        Assert.Null(exception);
    }
}

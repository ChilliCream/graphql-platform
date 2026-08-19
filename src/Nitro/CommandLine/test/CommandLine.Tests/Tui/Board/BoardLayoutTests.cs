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
        // act: width below the column count used to make grid distribution
        // report widths summing above the requested total; routing to
        // stacked instead avoids that entirely.
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
        // act: 30 rows over 3 columns clears the equal-share minimum
        // (3 columns * 5 rows), so every column expands rather than only
        // the focused one.
        var decision = BoardLayout.Decide(40, 30, 3, focusedColumnIndex: 1, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
        Assert.All(decision.Columns, c => Assert.True(c.Expanded));
    }

    [Fact]
    public void Decide_Should_ShareHeightEqually_When_StackedWithRoomToShareEqually()
    {
        // act
        var decision = BoardLayout.Decide(40, 30, 3, focusedColumnIndex: 1, maximized: false);

        // assert: 30 / 3 divides evenly, so every column gets the same slice.
        Assert.All(decision.Columns, c => Assert.Equal(10, c.Height));
        Assert.Equal(30, decision.Columns.Sum(c => c.Height));
    }

    [Fact]
    public void Decide_Should_GiveRemainderRowsToFirstColumns_When_StackedHeightDoesNotDivideEvenly()
    {
        // act: 31 rows over 3 columns leaves a remainder of 1.
        var decision = BoardLayout.Decide(40, 31, 3, focusedColumnIndex: 0, maximized: false);

        // assert
        Assert.Equal(11, decision.Columns[0].Height);
        Assert.Equal(10, decision.Columns[1].Height);
        Assert.Equal(10, decision.Columns[2].Height);
        Assert.Equal(31, decision.Columns.Sum(c => c.Height));
    }

    [Fact]
    public void Decide_Should_ExpandFocusedColumnOnly_When_StackedTooShortToShareEqually()
    {
        // act: 12 rows over 3 columns (4 rows each) is below the 5-row-per
        // column minimum an equal share needs to stay usable, so this falls
        // back to expanding only the focused column.
        var decision = BoardLayout.Decide(40, 12, 3, focusedColumnIndex: 1, maximized: false);

        // assert
        Assert.Equal(BoardLayoutKind.Stacked, decision.Kind);
        Assert.False(decision.Columns[0].Expanded);
        Assert.True(decision.Columns[1].Expanded);
        Assert.False(decision.Columns[2].Expanded);
        Assert.True(decision.Columns[1].Height > decision.Columns[0].Height);
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

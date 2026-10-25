using ChilliCream.Nitro.CommandLine.Tui.Board;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Board;

public sealed class BoardViewTests
{
    [Fact]
    public void Default_Should_DeclareBlockedDeferredReadyInProgressAndClosedColumns_InOrder()
    {
        // act
        var columns = BoardView.Default.Columns.Select(c => c.Name);

        // assert
        Assert.Equal(["Blocked", "Deferred", "Ready", "In Progress", "Closed"], columns);
    }

    [Fact]
    public void Default_Should_AssignDistinctBorderTokenPerColumn()
    {
        // act
        var borderTokens = BoardView.Default.Columns.Select(c => c.BorderToken);

        // assert
        Assert.Equal(
            [
                "board.column.status.blocked",
                "board.column.status.deferred",
                "board.column.status.ready",
                "board.column.status.inprogress",
                "board.column.status.closed"
            ],
            borderTokens);
    }
}

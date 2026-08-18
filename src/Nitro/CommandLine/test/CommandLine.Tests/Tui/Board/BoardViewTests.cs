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
}

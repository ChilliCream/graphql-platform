using ChilliCream.Nitro.CommandLine.Tui.Tree;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Tree;

public sealed class TreeNodeRowTests
{
    [Fact]
    public void BuildConnector_Should_ReturnEmpty_When_DepthIsZero()
    {
        // arrange
        var row = new TreeNodeRow { TaskId = "root", Depth = 0 };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("", connector);
    }

    [Fact]
    public void BuildConnector_Should_UseBranchGlyph_When_DepthOneAndNotLastChild()
    {
        // arrange
        var row = new TreeNodeRow { TaskId = "a", Depth = 1, IsLastChild = false };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("├─ ", connector);
    }

    [Fact]
    public void BuildConnector_Should_UseCornerGlyph_When_DepthOneAndLastChild()
    {
        // arrange
        var row = new TreeNodeRow { TaskId = "a", Depth = 1, IsLastChild = true };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("└─ ", connector);
    }

    [Fact]
    public void BuildConnector_Should_DrawContinuationBar_When_AncestorHasMoreSiblings()
    {
        // arrange
        var row = new TreeNodeRow
        {
            TaskId = "a",
            Depth = 2,
            IsLastChild = true,
            AncestorIsLastChild = [false]
        };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("│  └─ ", connector);
    }

    [Fact]
    public void BuildConnector_Should_DrawBlankSegment_When_AncestorIsLastChild()
    {
        // arrange
        var row = new TreeNodeRow
        {
            TaskId = "a",
            Depth = 2,
            IsLastChild = false,
            AncestorIsLastChild = [true]
        };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("   ├─ ", connector);
    }

    [Fact]
    public void BuildConnector_Should_ChainMultipleAncestorSegments_When_DepthIsThree()
    {
        // arrange
        var row = new TreeNodeRow
        {
            TaskId = "a",
            Depth = 3,
            IsLastChild = true,
            AncestorIsLastChild = [true, false]
        };

        // act
        var connector = row.BuildConnector();

        // assert
        Assert.Equal("   │  └─ ", connector);
    }
}

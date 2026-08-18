using ChilliCream.Nitro.CommandLine.Tui.Widgets;

namespace ChilliCream.Nitro.CommandLine.Tests.Tui.Widgets;

public sealed class ViewportTests
{
    [Fact]
    public void Slice_Should_ReturnEmptyRange_When_Empty()
    {
        // arrange
        var viewport = new Viewport(totalCount: 0, windowHeight: 5);

        // act
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(0, start);
        Assert.Equal(0, count);
        Assert.Equal(0, viewport.HiddenAbove);
        Assert.Equal(0, viewport.HiddenBelow);
    }

    [Fact]
    public void Slice_Should_ReturnWholeList_When_WindowLargerThanContent()
    {
        // arrange
        var viewport = new Viewport(totalCount: 3, windowHeight: 10);

        // act
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(0, start);
        Assert.Equal(3, count);
        Assert.Equal(0, viewport.HiddenAbove);
        Assert.Equal(0, viewport.HiddenBelow);
    }

    [Fact]
    public void EnsureVisible_Should_ClampToLastIndex_When_IndexBeyondEnd()
    {
        // arrange
        var viewport = new Viewport(totalCount: 5, windowHeight: 2);

        // act
        viewport.EnsureVisible(99);
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(3, start);
        Assert.Equal(2, count);
        Assert.Equal(3, viewport.HiddenAbove);
        Assert.Equal(0, viewport.HiddenBelow);
    }

    [Fact]
    public void EnsureVisible_Should_ClampToFirstIndex_When_IndexBeforeStart()
    {
        // arrange
        var viewport = new Viewport(totalCount: 5, windowHeight: 2);
        viewport.EnsureVisible(4);

        // act
        viewport.EnsureVisible(-10);
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(0, start);
        Assert.Equal(2, count);
    }

    [Fact]
    public void EnsureVisible_Should_ScrollDown_When_IndexBelowWindow()
    {
        // arrange
        var viewport = new Viewport(totalCount: 10, windowHeight: 3);

        // act
        viewport.EnsureVisible(5);
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(3, start);
        Assert.Equal(3, count);
        Assert.Equal(3, viewport.HiddenAbove);
        Assert.Equal(4, viewport.HiddenBelow);
    }

    [Fact]
    public void EnsureVisible_Should_ScrollUp_When_IndexAboveWindow()
    {
        // arrange
        var viewport = new Viewport(totalCount: 10, windowHeight: 3);
        viewport.EnsureVisible(9);

        // act
        viewport.EnsureVisible(1);
        var (start, _) = viewport.Slice();

        // assert
        Assert.Equal(1, start);
    }

    [Fact]
    public void Update_Should_ClampOffset_When_ContentShrinks()
    {
        // arrange
        var viewport = new Viewport(totalCount: 20, windowHeight: 5);
        viewport.EnsureVisible(19);

        // act
        viewport.Update(totalCount: 3, windowHeight: 5);
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(0, start);
        Assert.Equal(3, count);
    }

    [Fact]
    public void EnsureVisible_Should_NotThrow_When_WindowHeightIsZero()
    {
        // arrange
        var viewport = new Viewport(totalCount: 5, windowHeight: 0);

        // act
        viewport.EnsureVisible(2);
        var (start, count) = viewport.Slice();

        // assert
        Assert.Equal(0, start);
        Assert.Equal(0, count);
    }
}

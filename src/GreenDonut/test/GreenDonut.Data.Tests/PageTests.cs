using System.Collections.Immutable;

namespace GreenDonut.Data;

public class PageTests
{
    [Fact]
    public void CreateCursor_UsesMatchingElementIndex_WhenValuesRepeat()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: element => element.ToString());

        // act
        var first = page.CreateStartCursor();
        var second = page.CreateEndCursor();

        // assert
        Assert.Equal(0, page.First!.Value.Index);
        Assert.Equal(1, page.Last!.Value.Index);
        Assert.Equal("1", first);
        Assert.Equal("2", second);
    }

    [Fact]
    public void CreateCursor_Throws_WhenIndexIsNegative()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["a"],
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: static value => value);

        // act
        void Action() => page.CreateCursor(new PageEntry<string>("a", -1));

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public void CreateCursor_Throws_WhenIndexIsOutsidePage()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["a"],
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: static value => value);

        // act
        void Action() => page.CreateCursor(new PageEntry<string>("a", 1));

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public void FirstAndLast_AreNull_OnEmptyPage()
    {
        // arrange
        var page = Page<string>.Empty;

        // assert
        Assert.Null(page.First);
        Assert.Null(page.Last);
        Assert.Null(page.CreateStartCursor());
        Assert.Null(page.CreateEndCursor());
    }

    [Fact]
    public void CreateRelativeBackwardCursors_UsesFirstPageIndex()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: true,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 3,
            requestedPageSize: 2,
            totalCount: 10);

        // act
        var cursors = page.CreateRelativeBackwardCursors(2);

        // assert
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("1:-1:3:10", 1), cursor),
            cursor => Assert.Equal(new PageCursor("1:0:3:10", 2), cursor));
    }

    [Fact]
    public void CreateRelativeForwardCursors_UsesLastPageIndex()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: true,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 1,
            requestedPageSize: 2,
            totalCount: 10);

        // act
        var cursors = page.CreateRelativeForwardCursors(2);

        // assert
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("2:0:1:10", 2), cursor),
            cursor => Assert.Equal(new PageCursor("2:1:1:10", 3), cursor));
    }
}

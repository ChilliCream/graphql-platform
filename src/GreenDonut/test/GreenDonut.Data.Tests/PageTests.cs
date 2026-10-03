using System.Collections.Immutable;
using System.Linq.Expressions;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.Cursors.Serializers;

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
    public void TotalCount_Should_BeZero_When_PageIsEmpty()
    {
        // arrange
        var page = Page<string>.Empty;

        // act
        var totalCount = page.TotalCount;

        // assert
        Assert.Equal(0, totalCount);
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

    [Fact]
    public void CreateRelativeForwardCursors_Should_ReturnEmpty_When_MaxCursorsIsZero()
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
        var cursors = page.CreateRelativeForwardCursors(0);

        // assert
        Assert.Empty(cursors);
    }

    [Fact]
    public void CreateRelativeLastPageCursors_ReturnsPagesUpToLast_WhenNotOnLastPage()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        var cursors = page.CreateRelativeLastPageCursors(5);

        // assert
        Assert.Collection(
            cursors,
            cursor => AssertEndCursor(cursor, page: 2, offset: -1, totalCount: 25),
            cursor => AssertEndCursor(cursor, page: 3, offset: 0, totalCount: 25));
    }

    [Fact]
    public void CreateRelativeLastPageCursors_ReturnsEmpty_WhenOnLastPage()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: false,
            hasPreviousPage: true,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 3,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        var cursors = page.CreateRelativeLastPageCursors(5);

        // assert
        Assert.Empty(cursors);
    }

    [Fact]
    public void CreateLastPageCursor_ReturnsLastPage_WhenOffsetIsZero()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        var cursor = page.CreateLastPageCursor();

        // assert
        AssertEndCursor(cursor, page: 3, offset: 0, totalCount: 25);
    }

    [Fact]
    public void CreateLastPageCursor_Throws_WhenOffsetIsPositive()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        void Action() => page.CreateLastPageCursor(1);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public void CreateLastPageCursor_Throws_WhenOffsetMovesBeforeFirstPage()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["duplicate", "duplicate"],
            elements: ImmutableArray.Create(1, 2),
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        void Action() => page.CreateLastPageCursor(-3);

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public void CreateLastPageCursor_Throws_WhenTotalCountIsUnknown()
    {
        // arrange
        var page = Page<string>.Create(
            items: ["a"],
            hasNextPage: false,
            hasPreviousPage: false,
            createCursor: static value => value);

        // act
        void Action() => page.CreateLastPageCursor();

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Action);
        Assert.Equal("This page does not allow relative cursors.", exception.Message);
    }

    private static void AssertEndCursor(PageCursor cursor, int page, int offset, int totalCount)
    {
        Assert.Equal(page, cursor.Page);

        var parsed = CursorParser.Parse(cursor.Cursor, CreateKeys());

        Assert.True(parsed.IsEndCursor);
        Assert.Equal(offset, parsed.Offset);
        Assert.Equal(totalCount, parsed.TotalCount);
    }

    private static CursorKey[] CreateKeys()
    {
        Expression<Func<string, object?>> selector = static value => value;
        return [new CursorKey(selector, new StringCursorKeySerializer())];
    }
}

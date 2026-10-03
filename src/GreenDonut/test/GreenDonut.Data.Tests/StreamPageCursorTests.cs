using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamPageCursorTests
{
    [Fact]
    public async Task CreateStartCursorAsync_Should_ReturnFirstEntryCursor_WithoutDraining()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2, 3);
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 3));

        // act
        var cursor = await page.CreateStartCursorAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("1:0:0:0", cursor);
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateStartCursorAsync_Should_ReturnNull_When_PageIsEmpty()
    {
        // arrange
        var source = new ScriptedRowSource<int>();
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 3));

        // act
        var cursor = await page.CreateStartCursorAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Null(cursor);
    }

    [Fact]
    public async Task CreateStartCursorAsync_Should_CompleteSynchronously_When_SinglePageAlreadyPrimed()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2, 3);
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 3));

        // act
        var cursorTask = page.CreateStartCursorAsync(TestContext.Current.CancellationToken);
        var completedSynchronously = cursorTask.IsCompletedSuccessfully;
        var cursor = await cursorTask;

        // assert
        Assert.True(completedSynchronously);
        Assert.Equal("1:0:0:0", cursor);
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateStartCursorAsync_Should_CompleteOnlyAfterThePumpReachedTheKey_When_BatchPageIsNotYetPrimed()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, int>>(Row("A", 1), Row("A", 2), Row("B", 3));
        var gate = source.GateBeforeItem(2);
        var pump = (await StreamBatchPump<string, int>.CreateAsync(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken), ["A", "B"]))!;
        _ = pump.CreatePage(
            "A",
            Definition(requestedCount: 1),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");
        var pageB = pump.CreatePage(
            "B",
            Definition(requestedCount: 1),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");

        // act
        var cursorTask = pageB.CreateStartCursorAsync(TestContext.Current.CancellationToken);
        var pendingWhileGated = !cursorTask.IsCompleted;
        gate.SetResult();
        var cursor = await cursorTask;

        // assert
        Assert.True(pendingWhileGated);
        Assert.Equal("3:0:0:0", cursor);
        Assert.Equal(3, source.Yielded.Count);
    }

    [Fact]
    public async Task CreateEndCursorAsync_Should_DrainToLastEntry()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2, 3);
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 3));

        // act
        var cursor = await page.CreateEndCursorAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal("3:0:0:0", cursor);
        Assert.Equal(3, source.RowsRead);
    }

    [Fact]
    public async Task CreateEndCursorAsync_Should_ReturnNull_When_PageIsEmpty()
    {
        // arrange
        var source = new ScriptedRowSource<int>();
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 3));

        // act
        var cursor = await page.CreateEndCursorAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Null(cursor);
    }

    [Fact]
    public async Task CreateRelativeBackwardCursorsAsync_Should_CreateCursorsFromFirstEntry_When_IndexIsAfterFirstPage()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(
            source,
            Definition(requestedCount: 2, index: 3, requestedSize: 2, totalCount: 10));

        // act
        var cursors = await page.CreateRelativeBackwardCursorsAsync(2, TestContext.Current.CancellationToken);

        // assert
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("1:-1:3:10", 1), cursor),
            cursor => Assert.Equal(new PageCursor("1:0:3:10", 2), cursor));
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateRelativeBackwardCursorsAsync_Should_CompleteSynchronously_When_SinglePageAlreadyPrimed()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(
            source,
            Definition(requestedCount: 2, index: 3, requestedSize: 2, totalCount: 10));

        // act
        var cursorsTask = page.CreateRelativeBackwardCursorsAsync(2, TestContext.Current.CancellationToken);
        var completedSynchronously = cursorsTask.IsCompletedSuccessfully;
        var cursors = await cursorsTask;

        // assert
        Assert.True(completedSynchronously);
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("1:-1:3:10", 1), cursor),
            cursor => Assert.Equal(new PageCursor("1:0:3:10", 2), cursor));
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateRelativeBackwardCursorsAsync_Should_CompleteOnlyAfterThePumpReachedTheKey_When_BatchPageIsNotYetPrimed()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, int>>(Row("A", 1), Row("A", 2), Row("B", 3));
        var gate = source.GateBeforeItem(2);
        var pump = (await StreamBatchPump<string, int>.CreateAsync(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken), ["A", "B"]))!;
        _ = pump.CreatePage(
            "A",
            Definition(requestedCount: 1),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");
        var pageB = pump.CreatePage(
            "B",
            Definition(requestedCount: 1, index: 3, requestedSize: 2, totalCount: 10),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");

        // act
        var cursorsTask = pageB.CreateRelativeBackwardCursorsAsync(2, TestContext.Current.CancellationToken);
        var pendingWhileGated = !cursorsTask.IsCompleted;
        gate.SetResult();
        var cursors = await cursorsTask;

        // assert
        Assert.True(pendingWhileGated);
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("3:-1:3:10", 1), cursor),
            cursor => Assert.Equal(new PageCursor("3:0:3:10", 2), cursor));
        Assert.Equal(3, source.Yielded.Count);
    }

    [Fact]
    public async Task CreateRelativeBackwardCursorsAsync_Should_ReturnEmptyWithoutReading_When_BatchPageIsOnFirstPageAndNotYetPrimed()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, int>>(Row("A", 1), Row("A", 2), Row("B", 3));
        var gate = source.GateBeforeItem(2);
        var pump = (await StreamBatchPump<string, int>.CreateAsync(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken), ["A", "B"]))!;
        _ = pump.CreatePage(
            "A",
            Definition(requestedCount: 1),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");
        var pageB = pump.CreatePage(
            "B",
            Definition(requestedCount: 1, index: 1, requestedSize: 2, totalCount: 10),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");

        // act
        var cursorsTask = pageB.CreateRelativeBackwardCursorsAsync(2, TestContext.Current.CancellationToken);
        var completedSynchronously = cursorsTask.IsCompletedSuccessfully;
        var cursors = await cursorsTask;

        // assert
        Assert.True(completedSynchronously);
        Assert.Empty(cursors);
        Assert.Single(source.Yielded);
    }

    [Fact]
    public async Task CreateRelativeForwardCursorsAsync_Should_CreateCursorsFromLastEntry_When_PagesRemain()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(
            source,
            Definition(requestedCount: 2, index: 1, requestedSize: 2, totalCount: 10));

        // act
        var cursors = await page.CreateRelativeForwardCursorsAsync(2, TestContext.Current.CancellationToken);

        // assert
        Assert.Collection(
            cursors,
            cursor => Assert.Equal(new PageCursor("2:0:1:10", 2), cursor),
            cursor => Assert.Equal(new PageCursor("2:1:1:10", 3), cursor));
        Assert.Equal(2, source.RowsRead);
    }

    [Fact]
    public async Task CreateRelativeForwardCursorsAsync_Should_ReturnEmpty_When_MaxCursorsIsZero()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(
            source,
            Definition(requestedCount: 2, index: 1, requestedSize: 2, totalCount: 10));

        // act
        var cursors = await page.CreateRelativeForwardCursorsAsync(0, TestContext.Current.CancellationToken);

        // assert: checked before any read, so priming is the only row ever read
        Assert.Empty(cursors);
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateRelativeForwardCursorsAsync_Should_ReturnEmptyWithoutDraining_When_TotalCountIsUnknown()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(source, Definition(requestedCount: 2, index: 1));

        // act
        var cursors = await page.CreateRelativeForwardCursorsAsync(2, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(cursors);
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateRelativeForwardCursorsAsync_Should_ReturnEmptyWithoutDraining_When_PageIsAlreadyTheLastPage()
    {
        // arrange
        var source = new ScriptedRowSource<int>(1, 2);
        var page = await CreatePrimedPageAsync(
            source,
            Definition(requestedCount: 2, index: 1, requestedSize: 2, totalCount: 2));

        // act
        var cursors = await page.CreateRelativeForwardCursorsAsync(2, TestContext.Current.CancellationToken);

        // assert
        Assert.Empty(cursors);
        Assert.Equal(1, source.RowsRead);
    }

    [Fact]
    public async Task CreateLastPageCursor_Should_MatchPageHelper_ForTheSameTotalSizeAndIndex()
    {
        // arrange
        var streamPage = await CreatePage(
            new ScriptedRowSource<int>(1, 2),
            Definition(requestedCount: 2, index: 1, requestedSize: 10, totalCount: 25));
        var page = Page<int>.Create(
            items: [1, 2],
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => entry.Node.ToString(),
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        var streamPageCursor = streamPage.CreateLastPageCursor();
        var pageCursor = page.CreateLastPageCursor();

        // assert
        Assert.Equal(pageCursor, streamPageCursor);
    }

    [Fact]
    public void CreateLastPageCursor_Should_Throw_When_TotalCountIsUnknown()
    {
        // arrange
        var page = StreamPage<int>.Empty;

        // act
        void Action() => page.CreateLastPageCursor();

        // assert
        var exception = Assert.Throws<InvalidOperationException>(Action);
        Assert.Equal("This page does not allow relative cursors.", exception.Message);
    }

    [Fact]
    public async Task CreateRelativeLastPageCursors_Should_MatchPageHelper_ForTheSameTotalSizeAndIndex()
    {
        // arrange
        var streamPage = await CreatePage(
            new ScriptedRowSource<int>(1, 2),
            Definition(requestedCount: 2, index: 1, requestedSize: 10, totalCount: 25));
        var page = Page<int>.Create(
            items: [1, 2],
            hasNextPage: true,
            hasPreviousPage: false,
            createCursor: static entry => entry.Node.ToString(),
            index: 1,
            requestedPageSize: 10,
            totalCount: 25);

        // act
        var streamPageCursors = streamPage.CreateRelativeLastPageCursors(5);
        var pageCursors = page.CreateRelativeLastPageCursors(5);

        // assert
        Assert.Equal(pageCursors.ToArray(), streamPageCursors.ToArray());
    }

    [Fact]
    public async Task CreateRelativeLastPageCursors_Should_ReturnEmpty_When_OnLastPage()
    {
        // arrange
        var streamPage = await CreatePage(
            new ScriptedRowSource<int>(1, 2),
            Definition(requestedCount: 2, index: 3, requestedSize: 10, totalCount: 25));

        // act
        var cursors = streamPage.CreateRelativeLastPageCursors(5);

        // assert
        Assert.Empty(cursors);
    }

    private static StreamBatchRow<TKey, TElement> Row<TKey, TElement>(TKey key, TElement item)
        where TKey : notnull
        => new() { Key = key, Item = item };

    private static StreamPageDefinition<int> Definition(
        int requestedCount,
        int? index = null,
        int? requestedSize = null,
        int? totalCount = null)
        => new(
            RequestedCount: requestedCount,
            Forward: true,
            TrailingSentinel: false,
            SkipFront: 0,
            SkipFrontFromCount: null,
            Index: index,
            RequestedSize: requestedSize,
            TotalCount: totalCount,
            HasNextPage: null,
            HasPreviousPage: null,
            FlagsFromFirstRow: null);

    private static Task<StreamPage<int>> CreatePage(
        ScriptedRowSource<int> source,
        StreamPageDefinition<int> definition)
    {
        var pump = new StreamPagePump<int>(source.GetAsyncEnumerator(), pageCount: 1);
        return ValueCursorStreamPage<int>.CreatePrimedAsync(
            pump,
            definition,
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}",
            TestContext.Current.CancellationToken).AsTask();
    }

    // Builds a page whose first row is already buffered.
    private static Task<StreamPage<int>> CreatePrimedPageAsync(
        ScriptedRowSource<int> source,
        StreamPageDefinition<int> definition)
        => CreatePage(source, definition);

    // A minimal scripted async source for these tests.
    private sealed class ScriptedRowSource<T> : IAsyncEnumerable<StreamRow<T>>
    {
        private readonly StreamRow<T>[] _rows;

        public ScriptedRowSource(params T[] items)
            => _rows = [.. items.Select(item => new StreamRow<T> { Item = item })];

        public int RowsRead { get; private set; }

        public IAsyncEnumerator<StreamRow<T>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(this);

        private sealed class Enumerator(ScriptedRowSource<T> owner) : IAsyncEnumerator<StreamRow<T>>
        {
            private int _index = -1;

            public StreamRow<T> Current => owner._rows[_index];

            public ValueTask<bool> MoveNextAsync()
            {
                _index++;

                if (_index >= owner._rows.Length)
                {
                    return new ValueTask<bool>(false);
                }

                owner.RowsRead++;
                return new ValueTask<bool>(true);
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}

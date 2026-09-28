using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamPageTests
{
    [Fact]
    public async Task Replay_Should_YieldIdenticalSequence_When_EnumeratedTwice()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var first = await CollectAsync(page);
        var second = await CollectAsync(page);

        // assert
        Assert.Equal(["a", "b", "c"], first);
        Assert.Equal(first, second);
        Assert.Equal(3, source.Yielded.Count);
    }

    [Fact]
    public async Task EnumerateEntriesAsync_Should_ProduceZeroBasedIndices_InServedOrder()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var entries = new List<PageEntry<string>>();
        await foreach (var entry in page.EnumerateEntriesAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        // assert
        Assert.Collection(
            entries,
            e => Assert.Equal(("a", 0), (e.Item, e.Index)),
            e => Assert.Equal(("b", 1), (e.Item, e.Index)),
            e => Assert.Equal(("c", 2), (e.Item, e.Index)));
    }

    [Fact]
    public async Task Enumerators_Should_Interleave_Over_TheSharedBuffer()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        await using var first = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await using var second = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var r1 = (await first.MoveNextAsync(), first.Current);
        var r2 = (await second.MoveNextAsync(), second.Current);
        var r3 = (await second.MoveNextAsync(), second.Current);
        var r4 = (await first.MoveNextAsync(), first.Current);

        // assert: only two physical rows were needed for the four MoveNextAsync calls above
        Assert.Equal([(true, "a"), (true, "a"), (true, "b"), (true, "b")], [r1, r2, r3, r4]);
        Assert.Equal(2, source.Yielded.Count);
    }

    [Fact]
    public async Task TotalCountAsync_Should_ParkExactlyOneRow_And_ItStillYieldsFirst_When_CalledBeforeIteration()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a", totalCount: 5), Row("b"), Row("c"));
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var totalCount = await page.TotalCountAsync(TestContext.Current.CancellationToken);
        var rowsReadForCount = source.Yielded.Count;
        var items = await CollectAsync(page);

        // assert
        Assert.Equal(5, totalCount);
        Assert.Equal(1, rowsReadForCount);
        Assert.Equal(["a", "b", "c"], items);
    }

    [Fact]
    public async Task HasNextPageAsync_Should_ForceDrainThroughSentinel_Before_AnyItemIsConsumed()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"), Row("d"));
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

        // act: ask for the flag before touching any item
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);
        var yieldedWhileAsking = source.Yielded.Count;
        var items = await CollectAsync(page);

        // assert: the pump had to read the sentinel to answer the flag, but it was never yielded
        Assert.True(hasNextPage);
        Assert.Equal(3, yieldedWhileAsking);
        Assert.Equal(["a", "b"], items);
    }

    [Fact]
    public async Task TrailingSentinel_Should_ReportNoNextPage_When_SourceEndsWithoutIt()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

        // act
        var items = await CollectAsync(page);
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["a", "b"], items);
        Assert.False(hasNextPage);
    }

    [Fact]
    public async Task HasPreviousPageAsync_Should_ReturnTheFixedValueFromTheDefinition()
    {
        // arrange: a plain "last: N" page with no cursor carries fixed flags computed up front
        // from the inlined total, never derived from a row
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        var page = CreatePage(
            source,
            Definition<string>(requestedCount: 2, forward: false) with
            {
                HasPreviousPage = true,
                HasNextPage = false
            });

        // act
        var hasPreviousPage = await page.HasPreviousPageAsync(TestContext.Current.CancellationToken);
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(hasPreviousPage);
        Assert.False(hasNextPage);
    }

    [Fact]
    public async Task HasPreviousPageAsync_Should_DeriveFromFirstRow_When_FlagsRideAlongWithIt()
    {
        // arrange: a plain "last: N before: X" page carries its previous-page flag on the first row
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a", hasMore: true), Row("b"));
        var page = CreatePage(
            source,
            Definition<string>(requestedCount: 2, forward: false) with
            {
                FlagsFromFirstRow = static row => (HasNextPage: true, HasPreviousPage: row.HasMore)
            });

        // act
        var hasPreviousPage = await page.HasPreviousPageAsync(TestContext.Current.CancellationToken);
        var rowsReadForFlag = source.Yielded.Count;

        // assert
        Assert.True(hasPreviousPage);
        Assert.Equal(1, rowsReadForFlag);
    }

    [Fact]
    public async Task SkipFront_Should_NeverYieldTheSkippedRows()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"), Row("d"));
        var page = CreatePage(source, Definition<string>(requestedCount: 4, forward: true, skipFront: 2));

        // act
        var items = await CollectAsync(page);

        // assert
        Assert.Equal(["c", "d"], items);
        Assert.Equal(4, source.Yielded.Count);
    }

    [Theory]
    [InlineData(25, 10, 5)]
    [InlineData(30, 10, 0)]
    [InlineData(7, 10, 0)]
    public async Task SkipFront_Should_MatchEndCursorAlignment_ForTheLastPage(int total, int pageSize, int expectedSkip)
    {
        // arrange: the skip ToStreamPageAsync computes for an end-cursor's last page (m89.11): the
        // dataset's trailing remainder survives at the front, everything before it is read but
        // never yielded
        var rowCount = Math.Min(total, pageSize);
        var source = new ScriptedAsyncSource<StreamRow<int>>(
            Enumerable.Range(1, rowCount).Select(i => Row(i)).ToArray());
        var page = CreatePage(source, Definition<int>(requestedCount: pageSize, forward: true, skipFront: expectedSkip));

        // act
        var items = await CollectAsync(page);

        // assert
        Assert.Equal(Enumerable.Range(expectedSkip + 1, rowCount - expectedSkip), items);
        Assert.Equal(rowCount, source.Yielded.Count);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeSourceAndLifetimeOnce_When_CalledMidStream()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var lifetime = new ScriptedAsyncDisposable();
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true), lifetime);

        await using var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();

        // act
        await page.DisposeAsync();
        await page.DisposeAsync();
        var replay = await CollectAsync(page);

        // assert
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.True(page.IsCompleted);
        Assert.Equal(["a"], replay);
    }

    [Fact]
    public async Task Completion_Should_DisposeSourceAndLifetimeOnce_And_StayReplayable_When_PageIsDisposedAgain()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        var lifetime = new ScriptedAsyncDisposable();
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true), lifetime);

        // act: draining completes the page naturally; disposing it again afterward is a no-op
        var first = await CollectAsync(page);
        await page.DisposeAsync();
        await page.DisposeAsync();
        var replay = await CollectAsync(page);

        // assert
        Assert.Equal(first, replay);
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.True(page.IsCompleted);
    }

    [Fact]
    public async Task SourceException_Should_SurfaceToTheEnumerator_And_ReleaseTheLifetime_When_ThrownMidStream()
    {
        // arrange
        var exception = new InvalidOperationException("boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        source.ThrowAt(1, exception);
        var lifetime = new ScriptedAsyncDisposable();
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));
        var replayed = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));
        var flagged = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.HasNextPageAsync(TestContext.Current.CancellationToken).AsTask());

        // assert: the fault still releases the source and the lifetime, exactly like completion
        // does, and every later call on the faulted page rethrows the same exception with no
        // further reads (folded into one tuple equality to stay within the 5-Assert-call limit)
        Assert.Equal(
            (exception, exception, exception, 2, 1, 1),
            (thrown, replayed, flagged, source.MoveNextCount, source.DisposeCount, lifetime.DisposeCount));
    }

    [Fact]
    public async Task PrimeAsync_Should_BufferFirstRowAndResolveCount_When_AwaitedBeforeHandOff()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a", totalCount: 5), Row("b"), Row("c"));
        var pump = new StreamPagePump<string>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var definition = Definition<string>(requestedCount: 3, forward: true) with { Index = 1 };
        var page = new ValueCursorStreamPage<string>(pump, definition, static entry => entry.Node!);

        // act
        await page.PrimeAsync(TestContext.Current.CancellationToken);
        var entry = page.GetBufferedEntry(0);
        var cursor = page.CreateCursor(entry);
        var relativeCursor = page.CreateCursor(entry, 0);
        var rowsReadAfterPrime = source.Yielded.Count;
        var replay = await CollectAsync(page);

        // assert
        Assert.Equal((5, "a", "a", "a", 1), (page.TotalCount, entry.Item, cursor, relativeCursor, rowsReadAfterPrime));
        Assert.Equal(["a", "b", "c"], replay);
    }

    [Fact]
    public async Task PrimeAsync_Should_CompleteAndDisposeOnce_When_SourceIsEmpty()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>();
        var lifetime = new ScriptedAsyncDisposable();
        var pump = new StreamPagePump<string>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1, lifetime: lifetime);
        var definition = Definition<string>(requestedCount: 3, forward: true);
        var page = new ValueCursorStreamPage<string>(pump, definition, static entry => entry.Node!);

        // act
        await page.PrimeAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (true, 1, 1, 0),
            (page.IsCompleted, lifetime.DisposeCount, source.DisposeCount, source.Yielded.Count));
    }

    [Fact]
    public async Task ElementCursorStreamPage_Should_ProjectEachRowOnce_When_EnumeratedTwice()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<int>>(Row(1), Row(2), Row(3));
        var pump = new StreamPagePump<int>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var selectorCalls = 0;
        var page = new ElementCursorStreamPage<int, string>(
            pump,
            Definition<int>(requestedCount: 3, forward: true),
            valueSelector: element =>
            {
                selectorCalls++;
                return $"v{element}";
            },
            createCursor: static entry => $"elem:{entry.Node}");

        // act
        var first = await CollectAsync(page);
        var second = await CollectAsync(page);
        var cursor = page.CreateCursor(new PageEntry<string>(first[0], 0));

        // assert
        Assert.Equal(3, selectorCalls);
        Assert.All(Enumerable.Range(0, first.Count), i => Assert.Same(first[i], second[i]));
        Assert.Equal("elem:1", cursor);
    }

    [Fact]
    public async Task CreateCursor_Should_UseMatchingElementIndex_When_ValuesRepeat()
    {
        // arrange: two rows project to the same value, so the cursor can only be right if it is
        // read from the entry's index, not looked up by the value itself
        var source = new ScriptedAsyncSource<StreamRow<int>>(Row(1), Row(2));
        var pump = new StreamPagePump<int>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var page = new ElementCursorStreamPage<int, string>(
            pump,
            Definition<int>(requestedCount: 2, forward: true),
            valueSelector: static _ => "duplicate",
            createCursor: static entry => entry.Node.ToString());

        // act
        var items = await CollectAsync(page);
        var startCursor = page.CreateCursor(new PageEntry<string>(items[0], 0));
        var endCursor = page.CreateCursor(new PageEntry<string>(items[1], 1));

        // assert
        Assert.Equal(["duplicate", "duplicate"], items);
        Assert.Equal("1", startCursor);
        Assert.Equal("2", endCursor);
    }

    [Fact]
    public async Task CreateCursor_Should_Throw_When_IndexIsNegative()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var page = CreatePage(source, Definition<string>(requestedCount: 1, forward: true));
        await page.PrimeAsync(TestContext.Current.CancellationToken);

        // act
        void Action() => page.CreateCursor(new PageEntry<string>("a", -1));

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public async Task CreateCursor_Should_Throw_When_IndexIsOutsidePage()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var page = CreatePage(source, Definition<string>(requestedCount: 1, forward: true));
        await page.PrimeAsync(TestContext.Current.CancellationToken);

        // act
        void Action() => page.CreateCursor(new PageEntry<string>("a", 1));

        // assert
        Assert.Throws<ArgumentOutOfRangeException>(Action);
    }

    [Fact]
    public async Task Empty_Should_BeCompleted_With_NoRowsAndFalseFlags()
    {
        // arrange
        var page = StreamPage<string>.Empty;

        // act
        var items = await CollectAsync(page);
        var totalCount = await page.TotalCountAsync(TestContext.Current.CancellationToken);
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);
        var hasPreviousPage = await page.HasPreviousPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.True(page.IsCompleted);
        Assert.Empty(items);
        Assert.Equal(0, totalCount);
        Assert.False(hasNextPage);
        Assert.False(hasPreviousPage);
    }

    private static StreamRow<T> Row<T>(T item, int? totalCount = null, bool? hasMore = null)
        => new() { Item = item, TotalCount = totalCount, HasMore = hasMore };

    private static StreamPageDefinition<T> Definition<T>(
        int requestedCount,
        bool forward,
        bool trailingSentinel = false,
        int skipFront = 0)
        => new(
            RequestedCount: requestedCount,
            Forward: forward,
            TrailingSentinel: trailingSentinel,
            SkipFront: skipFront,
            SkipFrontFromCount: null,
            Index: null,
            RequestedSize: requestedCount,
            TotalCount: null,
            HasNextPage: null,
            HasPreviousPage: null,
            FlagsFromFirstRow: null);

    private static ValueCursorStreamPage<T> CreatePage<T>(
        ScriptedAsyncSource<StreamRow<T>> source,
        StreamPageDefinition<T> definition,
        IAsyncDisposable? lifetime = null)
    {
        var pump = new StreamPagePump<T>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1, lifetime: lifetime);
        return new ValueCursorStreamPage<T>(pump, definition, static entry => entry.Node!.ToString()!);
    }

    private static async Task<List<T>> CollectAsync<T>(StreamPage<T> page)
    {
        var items = new List<T>();

        await foreach (var item in page)
        {
            items.Add(item);
        }

        return items;
    }
}

using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamPageBasicsTests
{
    [Fact]
    public async Task Replay_Should_YieldIdenticalSequence_When_EnumeratedTwice()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b", "c");
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var first = await CollectAsync(page);
        var second = await CollectAsync(page);

        // assert
        Assert.Equal(["a", "b", "c"], first);
        Assert.Equal(first, second);
        Assert.Equal(3, source.RowsRead);
    }

    [Fact]
    public async Task EnumerateEntriesAsync_Should_ProduceZeroBasedIndices_InServedOrder()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b", "c");
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
        var source = new ScriptedRowSource<string>("a", "b", "c");
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
        Assert.Equal(2, source.RowsRead);
    }

    [Fact]
    public async Task TotalCountAsync_Should_ParkExactlyOneRow_When_CalledBeforeIteration()
    {
        // arrange
        var source = new ScriptedRowSource<string>([Row("a", totalCount: 5), Row("b"), Row("c")]);
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var totalCount = await page.TotalCountAsync(TestContext.Current.CancellationToken);
        var rowsReadForCount = source.RowsRead;
        var items = await CollectAsync(page);

        // assert
        Assert.Equal(5, totalCount);
        Assert.Equal(1, rowsReadForCount);
        Assert.Equal(["a", "b", "c"], items);
    }

    [Fact]
    public async Task TrailingSentinel_Should_NeverBeYielded_And_SetHasNextPage()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b", "c", "d");
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

        // act
        var items = await CollectAsync(page);
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["a", "b"], items);
        Assert.True(hasNextPage);
        Assert.Equal(3, source.RowsRead);
    }

    [Fact]
    public async Task TrailingSentinel_Should_ReportNoNextPage_When_SourceEndsWithoutIt()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b");
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

        // act
        var items = await CollectAsync(page);
        var hasNextPage = await page.HasNextPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["a", "b"], items);
        Assert.False(hasNextPage);
    }

    [Fact]
    public async Task SkipFront_Should_NeverYieldTheSkippedRows()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b", "c", "d");
        var page = CreatePage(source, Definition<string>(requestedCount: 4, forward: true, skipFront: 2));

        // act
        var items = await CollectAsync(page);

        // assert
        Assert.Equal(["c", "d"], items);
        Assert.Equal(4, source.RowsRead);
    }

    [Fact]
    public async Task DisposeAsync_Should_DisposeSourceAndLifetimeOnce_When_CalledMidStream()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b", "c");
        var lifetime = new RecordingLifetime();
        var page = CreatePage(source, Definition<string>(requestedCount: 3, forward: true), lifetime);

        await using var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();

        // act
        await page.DisposeAsync();
        await page.DisposeAsync();
        var replay = await CollectAsync(page);

        // assert
        Assert.True(source.Disposed);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.True(page.IsCompleted);
        Assert.Equal(["a"], replay);
    }

    [Fact]
    public async Task Completion_Should_DisposeSourceAndLifetimeOnce_When_SourceIsExhausted()
    {
        // arrange
        var source = new ScriptedRowSource<string>("a", "b");
        var lifetime = new RecordingLifetime();
        var page = CreatePage(source, Definition<string>(requestedCount: 2, forward: true), lifetime);

        // act
        await CollectAsync(page);
        await page.DisposeAsync();

        // assert
        Assert.True(source.Disposed);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.True(page.IsCompleted);
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
        Assert.Null(totalCount);
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
        ScriptedRowSource<T> source,
        StreamPageDefinition<T> definition,
        IAsyncDisposable? lifetime = null)
    {
        var pump = new StreamPagePump<T>(source.GetAsyncEnumerator(), pageCount: 1, lifetime: lifetime);
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

    private sealed class RecordingLifetime : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public ValueTask DisposeAsync()
        {
            DisposeCount++;
            return ValueTask.CompletedTask;
        }
    }

    // A minimal hand-rolled async source for these smoke tests. The shared, reusable scripted
    // source lives in a later test-infrastructure task.
    private sealed class ScriptedRowSource<T> : IAsyncEnumerable<StreamRow<T>>
    {
        private readonly StreamRow<T>[] _rows;

        public ScriptedRowSource(params T[] items)
            : this([.. items.Select(item => new StreamRow<T> { Item = item })])
        {
        }

        public ScriptedRowSource(IEnumerable<StreamRow<T>> rows) => _rows = [.. rows];

        public int RowsRead { get; private set; }

        public bool Disposed { get; private set; }

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

            public ValueTask DisposeAsync()
            {
                owner.Disposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }
}

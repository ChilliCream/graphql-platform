using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamBatchPumpTests
{
    [Fact]
    public async Task Rows_Should_RouteToTheirKeysPage()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(
            Row("A", "a1"), Row("A", "a2"), Row("B", "b1"), Row("B", "b2"));
        var pump = await CreatePump(source, ["A", "B"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 2, forward: true));

        // act
        var itemsA = await CollectAsync(pageA);
        var itemsB = await CollectAsync(pageB);

        // assert
        Assert.Equal(["a1", "a2"], itemsA);
        Assert.Equal(["b1", "b2"], itemsB);
    }

    [Fact]
    public async Task KeyChange_Should_CompleteThePreviousKeysPage()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"), Row("A", "a2"), Row("B", "b1"));
        var pump = await CreatePump(source, ["A", "B"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 2, forward: true));

        // act: pulling on B alone must drive the pump through every one of A's rows first, and
        // buffer them, before A is ever touched directly
        await using var enumeratorB = pageB.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumeratorB.MoveNextAsync();
        var rowsReadAfterB = source.Yielded.Count;
        var itemsA = await CollectAsync(pageA);

        // assert: A's key change was already detected, so completing A needed no further reads
        Assert.Equal(3, rowsReadAfterB);
        Assert.Equal(["a1", "a2"], itemsA);
        Assert.True(pageA.IsCompleted);
        Assert.False(pageB.IsCompleted);
        Assert.Equal(3, source.Yielded.Count);
    }

    [Fact]
    public async Task TrailingSentinel_Should_ApplyPerKey()
    {
        // arrange: A's second row is its sentinel and must never be yielded; B is unaffected.
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"), Row("A", "a2"), Row("B", "b1"));
        var pump = await CreatePump(source, ["A", "B"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 1, forward: true, trailingSentinel: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 1, forward: true));

        // act
        var itemsA = await CollectAsync(pageA);
        var hasNextA = await pageA.HasNextPageAsync(TestContext.Current.CancellationToken);
        var itemsB = await CollectAsync(pageB);

        // assert
        Assert.Equal(["a1"], itemsA);
        Assert.True(hasNextA);
        Assert.Equal(["b1"], itemsB);
    }

    [Fact]
    public async Task BackwardPages_Should_UsePerKeyFixedFlagsAndTotalCount()
    {
        // arrange: backward pages never over-fetch, so each key's flags and total come from the
        // per-key definition, exactly as the EF layer would supply them.
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"), Row("B", "b1"), Row("B", "b2"));
        var pump = await CreatePump(source, ["A", "B"]);
        var definitionA = Definition<string>(requestedCount: 1, forward: false) with
        {
            TotalCount = 3,
            HasNextPage = true,
            HasPreviousPage = false
        };
        var definitionB = Definition<string>(requestedCount: 2, forward: false) with
        {
            TotalCount = 9,
            HasNextPage = false,
            HasPreviousPage = true
        };
        var pageA = CreatePage(pump, "A", definitionA);
        var pageB = CreatePage(pump, "B", definitionB);

        // act
        var itemsA = await CollectAsync(pageA);
        var itemsB = await CollectAsync(pageB);
        var totalA = await pageA.TotalCountAsync(TestContext.Current.CancellationToken);
        var totalB = await pageB.TotalCountAsync(TestContext.Current.CancellationToken);
        var hasNextA = await pageA.HasNextPageAsync(TestContext.Current.CancellationToken);
        var hasPreviousB = await pageB.HasPreviousPageAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["a1"], itemsA);
        Assert.Equal(["b1", "b2"], itemsB);
        Assert.Equal((3, 9), (totalA, totalB));
        Assert.True(hasNextA);
        Assert.True(hasPreviousB);
    }

    [Fact]
    public async Task UnseenKey_Should_CompleteAsEmptyPage_When_SourceReachesEnd()
    {
        // arrange: "B" is requested but never appears in the source.
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"));
        var pump = await CreatePump(source, ["A", "B"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 1, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 1, forward: true));

        // act
        var itemsB = await CollectAsync(pageB);
        var itemsA = await CollectAsync(pageA);

        // assert
        Assert.Empty(itemsB);
        Assert.True(pageB.IsCompleted);
        Assert.Equal(["a1"], itemsA);
    }

    [Fact]
    public async Task ConsumingLastKeyFirst_Should_BufferEarlierKeys()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(
            Row("A", "a1"), Row("A", "a2"), Row("B", "b1"), Row("B", "b2"), Row("C", "c1"), Row("C", "c2"));
        var pump = await CreatePump(source, ["A", "B", "C"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 2, forward: true));
        var pageC = CreatePage(pump, "C", Definition<string>(requestedCount: 2, forward: true));

        // act: draining the last key first must buffer every earlier key's rows along the way
        var itemsC = await CollectAsync(pageC);
        var rowsReadAfterC = source.Yielded.Count;
        var itemsA = await CollectAsync(pageA);
        var itemsB = await CollectAsync(pageB);

        // assert: no further physical reads were needed once C had been drained
        Assert.Equal(["c1", "c2"], itemsC);
        Assert.Equal(6, rowsReadAfterC);
        Assert.Equal(["a1", "a2"], itemsA);
        Assert.Equal(["b1", "b2"], itemsB);
        Assert.Equal(6, source.Yielded.Count);
    }

    [Fact]
    public async Task Enumerators_Should_Interleave_AcrossKeys_OverTheSharedPump()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(
            Row("A", "a1"), Row("A", "a2"), Row("B", "b1"), Row("B", "b2"));
        var pump = await CreatePump(source, ["A", "B"]);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 2, forward: true));

        // act
        await using var enumeratorA = pageA.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await using var enumeratorB = pageB.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        var r1 = (await enumeratorA.MoveNextAsync(), enumeratorA.Current);
        var r2 = (await enumeratorB.MoveNextAsync(), enumeratorB.Current);
        var r3 = (await enumeratorA.MoveNextAsync(), enumeratorA.Current);
        var r4 = (await enumeratorB.MoveNextAsync(), enumeratorB.Current);

        // assert: B's first pull already had to drive the pump through A's second row
        Assert.Equal([(true, "a1"), (true, "b1"), (true, "a2"), (true, "b2")], [r1, r2, r3, r4]);
        Assert.Equal(4, source.Yielded.Count);
    }

    [Fact]
    public async Task EmptyKeySet_Should_DisposeSourceAndLifetime_Immediately()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"));

        // act
        var pump = await StreamBatchPump<string, string>.CreateAsync(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken), [], lifetime);

        // assert
        Assert.Null(pump);
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Empty(source.Yielded);
    }

    [Fact]
    public async Task DuplicateKey_Should_Throw_ArgumentException_NamingTheKey()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"));

        // act
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => StreamBatchPump<string, string>.CreateAsync(
                source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
                ["A", "B", "A"]).AsTask());

        // assert
        Assert.Equal("keys", exception.ParamName);
        Assert.Equal(
            "The requested keys contain a duplicate: 'A'. (Parameter 'keys')",
            exception.Message);
    }

    [Fact]
    public async Task Counter_Should_ReachZero_Only_When_EveryKeyCompletesOrIsDisposed()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"), Row("B", "b1"));
        var pump = await CreatePump(source, ["A", "B"], lifetime);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 1, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 1, forward: true));

        // act
        await CollectAsync(pageA);
        var lifetimeDisposedAfterA = lifetime.DisposeCount;
        await CollectAsync(pageB);

        // assert
        Assert.Equal(0, lifetimeDisposedAfterA);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task DisposeAsync_Should_BeIdempotent_And_NotDoubleReleaseTheCounter()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(
            Row("A", "a1"), Row("A", "a2"), Row("B", "b1"), Row("B", "b2"));
        var pump = await CreatePump(source, ["A", "B"], lifetime);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 2, forward: true));

        await using var enumeratorA = pageA.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumeratorA.MoveNextAsync();

        // act: abandon A early, twice, then drain B to the end
        await pageA.DisposeAsync();
        await pageA.DisposeAsync();
        var lifetimeDisposedAfterA = lifetime.DisposeCount;
        await CollectAsync(pageB);
        await pageB.DisposeAsync(); // no-op: B already completed naturally

        // assert
        Assert.True(pageA.IsCompleted);
        Assert.Equal(0, lifetimeDisposedAfterA);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task SourceException_Should_SurfaceToTheEnumerator_And_ReleaseTheLifetime_When_ThrownMidStream()
    {
        // arrange
        var exception = new InvalidOperationException("boom");
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(
            Row("A", "a1"), Row("A", "a2"), Row("B", "b1"));
        source.ThrowAt(1, exception);
        var lifetime = new ScriptedAsyncDisposable();
        var pump = await CreatePump(source, ["A", "B"], lifetime);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 1, forward: true));

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(pageA));
        var thrownB = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(pageB));

        // assert: the fault releases the shared source and the lifetime once, and the sibling
        // page rethrows the same fault instead of reading the now-disposed source
        Assert.Same(exception, thrown);
        Assert.Same(exception, thrownB);
        Assert.Equal((2, 1, 1), (source.MoveNextCount, source.DisposeCount, lifetime.DisposeCount));
    }

    [Fact]
    public async Task SourceEof_Should_CompleteEveryPage_And_ReleaseOnce_When_OnlyOnePageIsDrained()
    {
        // arrange
        var lifetime = new ScriptedAsyncDisposable();
        var source = new ScriptedAsyncSource<StreamBatchRow<string, string>>(Row("A", "a1"), Row("A", "a2"), Row("B", "b1"));
        var pump = await CreatePump(source, ["A", "B"], lifetime);
        var pageA = CreatePage(pump, "A", Definition<string>(requestedCount: 2, forward: true));
        var pageB = CreatePage(pump, "B", Definition<string>(requestedCount: 1, forward: true));

        // act: prime both pages, then drain only B; reaching source EOF must still complete A
        await pageA.PrimeAsync(TestContext.Current.CancellationToken);
        await pageB.PrimeAsync(TestContext.Current.CancellationToken);
        var itemsB = await CollectAsync(pageB);

        // assert: A completed and everything released as soon as the source ran out, with no
        // consumer ever pulling on A directly
        Assert.Equal(["b1"], itemsB);
        Assert.Equal((true, 1, 1), (pageA.IsCompleted, source.DisposeCount, lifetime.DisposeCount));

        // A's buffered rows still replay with no further physical reads
        Assert.Equal(["a1", "a2"], await CollectAsync(pageA));
        Assert.Equal(3, source.Yielded.Count);
    }

    private static StreamBatchRow<TKey, TElement> Row<TKey, TElement>(TKey key, TElement item)
        where TKey : notnull
        => new() { Key = key, Item = item };

    private static async Task<StreamBatchPump<TKey, TElement>> CreatePump<TKey, TElement>(
        ScriptedAsyncSource<StreamBatchRow<TKey, TElement>> source,
        IReadOnlyCollection<TKey> keys,
        IAsyncDisposable? lifetime = null)
        where TKey : notnull
    {
        var pump = await StreamBatchPump<TKey, TElement>.CreateAsync(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken), keys, lifetime);

        Assert.NotNull(pump);
        return pump;
    }

    private static StreamPageDefinition<T> Definition<T>(
        int requestedCount,
        bool forward,
        bool trailingSentinel = false)
        => new(
            RequestedCount: requestedCount,
            Forward: forward,
            TrailingSentinel: trailingSentinel,
            SkipFront: 0,
            SkipFrontFromCount: null,
            Index: null,
            RequestedSize: requestedCount,
            TotalCount: null,
            HasNextPage: null,
            HasPreviousPage: null,
            FlagsFromFirstRow: null);

    private static StreamPage<T> CreatePage<TKey, T>(
        StreamBatchPump<TKey, T> pump,
        TKey key,
        StreamPageDefinition<T> definition)
        where TKey : notnull
        => pump.CreatePage(
            key,
            keyPump => new ValueCursorStreamPage<T>(keyPump, definition, static entry => entry.Node!.ToString()!));

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

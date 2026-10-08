using System.Reflection;
using GreenDonut.Data.Internal;

namespace GreenDonut.Data;

public class StreamPageTests
{
    [Fact]
    public async Task Replay_Should_YieldIdenticalSequence_When_EnumeratedTwice()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var first = await CollectAsync(page);
        var second = await CollectAsync(page);

        // assert
        Assert.Equal(["a", "b", "c"], first);
        Assert.Equal(first, second);
        Assert.Equal(3, source.Yielded.Count);
    }

    [Fact]
    public async Task GetEntriesAsync_Should_ProduceZeroBasedIndices_InServedOrder()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var entries = new List<PageEntry<string>>();
        await foreach (var entry in page.GetEntriesAsync(TestContext.Current.CancellationToken))
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
    public async Task GetEntriesAsync_And_Values_Should_ObserveSameRows_InSameOrder_FromTheSharedLoop()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

        // act
        var entries = new List<PageEntry<string>>();
        await foreach (var entry in page.GetEntriesAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        var values = await CollectAsync(page);

        // assert
        Assert.Equal(["a", "b", "c"], values);
        Assert.Equal(values, entries.Select(e => e.Item));
        Assert.Equal(entries.Select(e => e.Index), Enumerable.Range(0, values.Count));
    }

    [Fact]
    public async Task ElementProjectingSource_Should_ObserveSameProjectedRows_InSameOrder_FromTheSharedLoop()
    {
        // arrange: entries are read first and values second, from the same shared loop
        var source = new ScriptedAsyncSource<StreamRow<int>>(Row(1), Row(2), Row(3));
        var pump = new StreamPagePump<int>(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1);
        var selectorCalls = 0;
        var page = ElementCursorStreamPage<int, string>.CreateForBatch(
            pump,
            Definition<int>(requestedCount: 3, forward: true),
            valueSelector: element =>
            {
                selectorCalls++;
                return $"v{element}";
            },
            createCursor: static entry => $"elem:{entry.Node}");

        // act
        var entries = new List<PageEntry<string>>();
        await foreach (var entry in page.GetEntriesAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(entry);
        }

        var values = await CollectAsync(page);

        // assert
        Assert.Equal(["v1", "v2", "v3"], values);
        Assert.Equal(values, entries.Select(e => e.Item));
        Assert.Equal(3, selectorCalls);
    }

    [Fact]
    public void StreamPageSources_Should_NotImplementIAsyncEnumerable()
    {
        // arrange: every non-abstract subclass of StreamPageSource<T> in this assembly
        var sourceTypes = typeof(StreamPage<>).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract)
            .Where(IsStreamPageSourceSubclass)
            .ToArray();

        // act
        var typesImplementingBoth = sourceTypes
            .Where(t => t.GetInterfaces().Any(
                i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAsyncEnumerable<>)))
            .ToArray();

        // assert
        Assert.NotEmpty(sourceTypes);
        Assert.Empty(typesImplementingBoth);
    }

    [Fact]
    public void StreamPage_Should_DeclareNoNonPublicMembers_Besides_TotalCountRequestedSizeAndCreateCursor()
    {
        // arrange: every non-public property and non-accessor method declared directly on the base page
        var members = typeof(StreamPage<>)
            .GetMembers(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(member => member is PropertyInfo or MethodInfo { IsSpecialName: false })
            .Select(member => member.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal);

        // assert
        Assert.Equal(["CreateCursor", "RequestedSize", "TotalCount"], members);
    }

    [Fact]
    public void StreamPageSource_Should_NotDeclareDrainAsync()
    {
        // arrange
        var methodNames = typeof(StreamPageSource<>)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(member => !member.IsSpecialName)
            .Select(member => member.Name)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal);

        // assert
        Assert.Equal(
            [
                "DisposeAsync",
                "GetBufferedEntry",
                "GetEntriesAsync",
                "GetValuesAsync",
                "HasNextPageAsync",
                "HasPreviousPageAsync",
                "PrimeAsync",
                "TotalCountAsync"
            ],
            methodNames);
    }

    [Fact]
    public void StreamPage_And_Subclasses_Should_ExposeNoConstructorsAccessibleOutsidePrimitives()
    {
        // arrange: the abstract base and every concrete page type derived from it in this assembly
        var pageTypes = typeof(StreamPage<>).Assembly
            .GetTypes()
            .Where(IsStreamPageOrSubclass)
            .ToArray();

        // act
        var constructors = pageTypes
            .SelectMany(t => t.GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            .ToArray();

        // assert: construction outside Primitives only ever goes through a primed factory or CreatePage
        Assert.NotEmpty(pageTypes);
        Assert.NotEmpty(constructors);
        Assert.All(constructors, c => Assert.True(c.IsPrivate || c.IsFamilyAndAssembly));
    }

    [Fact]
    public async Task ValueCursorStreamPage_And_ElementCursorStreamPage_Should_BothConstructFromAPrimedFactory()
    {
        // arrange: the same construction path is available for both subclasses
        var valueSource = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var valuePump = new StreamPagePump<string>(
            valueSource.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1);
        var valueDefinition = Definition<string>(requestedCount: 1, forward: true);

        var elementSource = new ScriptedAsyncSource<StreamRow<int>>(Row(1));
        var elementPump = new StreamPagePump<int>(
            elementSource.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1);
        var elementDefinition = Definition<int>(requestedCount: 1, forward: true);

        // act
        var valuePage = await ValueCursorStreamPage<string>.CreatePrimedAsync(
            valuePump,
            valueDefinition,
            static entry => entry.Node!,
            TestContext.Current.CancellationToken);
        var elementPage = await ElementCursorStreamPage<int, string>.CreatePrimedAsync(
            elementPump,
            elementDefinition,
            static element => $"v{element}",
            static entry => entry.Node.ToString(),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["a"], await CollectAsync(valuePage));
        Assert.Equal(["v1"], await CollectAsync(elementPage));
    }

    [Fact]
    public void PageFactories_Should_BeInternalAndLimitedToCreateForBatchAndCreatePrimedAsync()
    {
        // arrange: the only ways to construct a page from outside Primitives are these two factories
        var factoryTypes = new[] { typeof(ValueCursorStreamPage<>), typeof(ElementCursorStreamPage<,>) };

        // act
        var methods = factoryTypes
            .SelectMany(t => t.GetMethods(
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Where(m => !m.IsSpecialName)
            .ToArray();

        // assert
        Assert.NotEmpty(methods);
        Assert.Equal(
            ["CreateForBatch", "CreatePrimedAsync"],
            methods.Select(m => m.Name).Distinct().OrderBy(name => name, StringComparer.Ordinal));
        Assert.All(methods, m => Assert.True(m.IsAssembly && !m.IsPublic));
    }

    private static bool IsStreamPageOrSubclass(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var candidate = current.IsGenericType ? current.GetGenericTypeDefinition() : current;

            if (candidate == typeof(StreamPage<>))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsStreamPageSourceSubclass(Type type)
    {
        for (var current = type; current is not null; current = current.BaseType)
        {
            var candidate = current.IsGenericType ? current.GetGenericTypeDefinition() : current;

            if (candidate == typeof(StreamPageSource<>))
            {
                return true;
            }
        }

        return false;
    }

    [Fact]
    public async Task Enumerators_Should_Interleave_Over_TheSharedBuffer()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

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
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true));

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
        var page = await CreatePage(
            source,
            Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

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
        var page = await CreatePage(
            source,
            Definition<string>(requestedCount: 2, forward: true, trailingSentinel: true));

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
        // arrange: a plain "last: N" page with no cursor carries fixed flags from the inlined total
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        var page = await CreatePage(
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
        var page = await CreatePage(
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
        var page = await CreatePage(source, Definition<string>(requestedCount: 4, forward: true, skipFront: 2));

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
        // arrange: the skip ToStreamPageAsync computes for an end-cursor's last page
        var rowCount = Math.Min(total, pageSize);
        var source = new ScriptedAsyncSource<StreamRow<int>>(
            Enumerable.Range(1, rowCount).Select(i => Row(i)).ToArray());
        var page = await CreatePage(
            source,
            Definition<int>(requestedCount: pageSize, forward: true, skipFront: expectedSkip));

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
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true), lifetime);

        await using var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);
        await enumerator.MoveNextAsync();

        // act
        await page.DisposeAsync();
        await page.DisposeAsync();
        var replay = await CollectAsync(page);

        // assert
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, lifetime.DisposeCount);
        Assert.Equal(["a"], replay);
    }

    [Fact]
    public async Task Completion_Should_DisposeSourceAndLifetimeOnce_And_StayReplayable_When_PageIsDisposedAgain()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        var lifetime = new ScriptedAsyncDisposable();
        var page = await CreatePage(source, Definition<string>(requestedCount: 2, forward: true), lifetime);

        // act: draining completes the page naturally; disposing it again afterward is a no-op
        var first = await CollectAsync(page);
        await page.DisposeAsync();
        await page.DisposeAsync();
        var replay = await CollectAsync(page);

        // assert
        Assert.Equal(first, replay);
        Assert.Equal(1, source.DisposeCount);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task SourceException_Should_SurfaceToTheEnumerator_And_ReleaseTheLifetime_When_ThrownMidStream()
    {
        // arrange
        var exception = new InvalidOperationException("boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"), Row("c"));
        source.ThrowAt(1, exception);
        var lifetime = new ScriptedAsyncDisposable();
        var page = await CreatePage(source, Definition<string>(requestedCount: 3, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));
        var replayed = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));
        var flagged = await Assert.ThrowsAsync<InvalidOperationException>(
            () => page.HasNextPageAsync(TestContext.Current.CancellationToken).AsTask());

        // assert: the fault releases the source and the lifetime, and every later call rethrows it
        Assert.Equal(
            (exception, exception, exception, 2, 1, 1),
            (thrown, replayed, flagged, source.MoveNextCount, source.DisposeCount, lifetime.DisposeCount));
    }

    [Fact]
    public async Task SourceException_Should_StillReleaseTheLifetime_And_PreserveTheOriginalFault_When_SourceDisposeAsyncThrows()
    {
        // arrange: the source faults mid-stream, and its own DisposeAsync then fails too
        var faultException = new InvalidOperationException("boom");
        var disposeException = new InvalidOperationException("dispose boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        source.ThrowAt(1, faultException);
        source.ThrowOnDispose(disposeException);
        var lifetime = new ScriptedAsyncDisposable();
        var page = await CreatePage(source, Definition<string>(requestedCount: 2, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));

        // assert: the mid-stream fault surfaces, and the lifetime is still released
        Assert.Same(faultException, thrown);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task SourceException_Should_AttachLifetimeDisposalFailure_Not_ReplaceIt_When_LifetimeDisposeAsyncThrows()
    {
        // arrange: the source faults mid-stream, and the lifetime's own DisposeAsync then fails
        var faultException = new InvalidOperationException("boom");
        var disposeException = new InvalidOperationException("lifetime boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"), Row("b"));
        source.ThrowAt(1, faultException);
        var lifetime = new ScriptedAsyncDisposable();
        lifetime.ThrowOnDispose(disposeException);
        var page = await CreatePage(source, Definition<string>(requestedCount: 2, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));

        // assert: the mid-stream fault surfaces, with the lifetime's disposal failure attached
        Assert.Same(faultException, thrown);
        Assert.Equal([disposeException], OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task Completion_Should_SurfaceLifetimeDisposalFailure_Once_When_DrainedNaturally()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var lifetime = new ScriptedAsyncDisposable();
        var disposeException = new InvalidOperationException("lifetime boom");
        lifetime.ThrowOnDispose(disposeException);
        var page = await CreatePage(source, Definition<string>(requestedCount: 1, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));

        // assert: the disposal failure surfaces once, and a later replay is clean and complete
        Assert.Same(disposeException, thrown);
        Assert.Equal(["a"], await CollectAsync(page));
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task Completion_Should_ThrowSourceException_With_LifetimeAttached_When_BothDisposalsFail_AndNoFaultWasRecorded()
    {
        // arrange: natural completion; both the source's and the lifetime's DisposeAsync fail
        var sourceDisposeException = new InvalidOperationException("source boom");
        var lifetimeDisposeException = new InvalidOperationException("lifetime boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        source.ThrowOnDispose(sourceDisposeException);
        var lifetime = new ScriptedAsyncDisposable();
        lifetime.ThrowOnDispose(lifetimeDisposeException);
        var page = await CreatePage(source, Definition<string>(requestedCount: 1, forward: true), lifetime);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() => CollectAsync(page));

        // assert: the source's exception wins the race, with the lifetime's attached
        Assert.Same(sourceDisposeException, thrown);
        Assert.Equal([lifetimeDisposeException], OrderedDisposal.GetAttached(thrown));
    }

    [Fact]
    public async Task PrimeAsync_Should_BufferFirstRowAndResolveCount_When_AwaitedBeforeHandOff()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a", totalCount: 5), Row("b"), Row("c"));
        var pump = new StreamPagePump<string>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var definition = Definition<string>(requestedCount: 3, forward: true) with { Index = 1 };

        // act: the primed factory buffers the first row and resolves the count as part of creation
        var page = await ValueCursorStreamPage<string>.CreatePrimedAsync(
            pump,
            definition,
            static entry => entry.Node!,
            TestContext.Current.CancellationToken);
        var rowsReadAfterPrime = source.Yielded.Count;
        var entries = new List<PageEntry<string>>();
        await foreach (var pageEntry in page.GetEntriesAsync(TestContext.Current.CancellationToken))
        {
            entries.Add(pageEntry);
        }
        var entry = entries[0];
        var cursor = page.CreateCursor(entry);
        var relativeCursor = page.CreateCursor(entry, 0);
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
        var buffer = new StreamPageBuffer<string>(pump, definition);

        // act
        await buffer.PrimeAsync(TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(
            (true, 1, 1, 0),
            (buffer.IsCompleted, lifetime.DisposeCount, source.DisposeCount, source.Yielded.Count));
    }

    [Fact]
    public async Task CreatePrimedAsync_Should_DisposeSourceAndLifetime_When_PrimingMoveNextAsyncThrows()
    {
        // arrange
        var exception = new InvalidOperationException("boom");
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        source.ThrowAt(0, exception);
        var lifetime = new ScriptedAsyncDisposable();
        var pump = new StreamPagePump<string>(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1,
            lifetime: lifetime);
        var definition = Definition<string>(requestedCount: 3, forward: true);

        // act
        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(
            () => StreamPageBuffer<string>.CreatePrimedAsync(
                pump,
                definition,
                TestContext.Current.CancellationToken).AsTask());

        // assert: the creating call observes the priming fault with everything already released
        Assert.Same(exception, thrown);
        Assert.Equal((1, 1), (source.DisposeCount, lifetime.DisposeCount));
    }

    [Fact]
    public async Task CreatePrimedAsync_Should_DisposeSourceAndLifetime_When_PrimingIsCancelled()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var lifetime = new ScriptedAsyncDisposable();
        var pump = new StreamPagePump<string>(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1,
            lifetime: lifetime);
        var definition = Definition<string>(requestedCount: 3, forward: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => StreamPageBuffer<string>.CreatePrimedAsync(pump, definition, cts.Token).AsTask());

        // assert: a cancelled priming call still disposes the source and the lifetime once each
        Assert.Equal((1, 1), (source.DisposeCount, lifetime.DisposeCount));
    }

    [Fact]
    public async Task CreatePrimedAsync_Should_AttachCleanupFailure_Not_ReplaceCancellation_When_LifetimeDisposeAsyncThrows()
    {
        // arrange: priming is cancelled, and cleaning up afterward fails to dispose the lifetime
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a"));
        var lifetime = new ScriptedAsyncDisposable();
        var disposeException = new InvalidOperationException("lifetime boom");
        lifetime.ThrowOnDispose(disposeException);
        var pump = new StreamPagePump<string>(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1,
            lifetime: lifetime);
        var definition = Definition<string>(requestedCount: 3, forward: true);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        // act
        var thrown = await Assert.ThrowsAsync<OperationCanceledException>(
            () => StreamPageBuffer<string>.CreatePrimedAsync(pump, definition, cts.Token).AsTask());

        // assert: the cancellation surfaces, with the cleanup failure attached
        Assert.Equal([disposeException], OrderedDisposal.GetAttached(thrown));
        Assert.Equal(1, source.DisposeCount);
    }

    [Fact]
    public async Task CreatePrimedAsync_Should_ConstructAndPrimeAtomically()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<string>>(Row("a", totalCount: 5), Row("b"));
        var pump = new StreamPagePump<string>(
            source.GetAsyncEnumerator(TestContext.Current.CancellationToken),
            pageCount: 1);
        var definition = Definition<string>(requestedCount: 2, forward: true) with { Index = 1 };

        // act
        var buffer = await StreamPageBuffer<string>.CreatePrimedAsync(
            pump,
            definition,
            TestContext.Current.CancellationToken);

        // assert: the first row is already buffered, so the buffer is never handed to a page unprimed
        Assert.Equal(1, buffer.BufferedCount);
        Assert.Equal("a", buffer.GetBufferedEntry(0).Item);
    }

    [Fact]
    public async Task ElementCursorStreamPage_Should_ProjectEachRowOnce_When_EnumeratedTwice()
    {
        // arrange
        var source = new ScriptedAsyncSource<StreamRow<int>>(Row(1), Row(2), Row(3));
        var pump = new StreamPagePump<int>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var selectorCalls = 0;
        var page = ElementCursorStreamPage<int, string>.CreateForBatch(
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
        // arrange: two rows project to the same value, so the cursor must be read from the entry's index
        var source = new ScriptedAsyncSource<StreamRow<int>>(Row(1), Row(2));
        var pump = new StreamPagePump<int>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1);
        var page = ElementCursorStreamPage<int, string>.CreateForBatch(
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
        var page = await CreatePrimedPageAsync(source, Definition<string>(requestedCount: 1, forward: true));

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
        var page = await CreatePrimedPageAsync(source, Definition<string>(requestedCount: 1, forward: true));

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

        // assert: an empty page has nothing left to stream
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

    private static async Task<StreamPage<T>> CreatePage<T>(
        ScriptedAsyncSource<StreamRow<T>> source,
        StreamPageDefinition<T> definition,
        IAsyncDisposable? lifetime = null)
    {
        var pump = new StreamPagePump<T>(source.GetAsyncEnumerator(TestContext.Current.CancellationToken), pageCount: 1, lifetime: lifetime);
        return await ValueCursorStreamPage<T>.CreatePrimedAsync(
            pump,
            definition,
            static entry => entry.Node!.ToString()!,
            TestContext.Current.CancellationToken);
    }

    // Builds a page whose first row is already buffered.
    private static Task<StreamPage<T>> CreatePrimedPageAsync<T>(
        ScriptedAsyncSource<StreamRow<T>> source,
        StreamPageDefinition<T> definition,
        IAsyncDisposable? lifetime = null)
        => CreatePage(source, definition, lifetime);

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

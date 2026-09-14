using System.Runtime.ExceptionServices;

namespace GreenDonut.Data;

public class StreamPageTests
{
    [Fact]
    public async Task Enumerate_Should_YieldItemsWithCursors_When_StreamContainsItems()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a", "b", "c"),
            new PagingArguments(first: 2),
            static entry => $"{entry.Node}:{entry.Offset}:{entry.PageIndex}:{entry.TotalCount}");
        List<StreamPageEdge<string>> edges = [];

        // act
        await foreach (var edge in page)
        {
            edges.Add(edge);
        }

        // assert
        Assert.Collection(
            edges,
            edge => Assert.Equal(new StreamPageEdge<string>("a", "a:0:0:0"), edge),
            edge => Assert.Equal(new StreamPageEdge<string>("b", "b:0:0:0"), edge));
    }

    [Fact]
    public async Task Completion_Should_ExposePageFacts_When_StreamIsFullyEnumerated()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a", "b", "c"),
            new PagingArguments(first: 2, after: "before"),
            static item => item,
            totalCount: 3);

        // act
        await foreach (var _ in page)
        {
        }

        var completion = await page.Completion;
        var totalCount = await page.TotalCount;

        // assert
        Assert.True(completion.HasNextPage);
        Assert.True(completion.HasPreviousPage);
        Assert.Equal("a", completion.StartCursor);
        Assert.Equal("b", completion.EndCursor);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task GetAsyncEnumerator_Should_Throw_When_StreamIsEnumeratedMoreThanOnce()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item);

        await foreach (var _ in page)
        {
        }

        // act
        void Action() => page.GetAsyncEnumerator();

        // assert
        Assert.Throws<InvalidOperationException>(Action);
    }

    [Fact]
    public async Task Items_Should_ProjectNodes_When_ItemsAreEnumerated()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a", "b", "c"),
            new PagingArguments(first: 2),
            static item => item);
        List<string> items = [];

        // act
        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        // assert
        Assert.Equal(["a", "b"], items);
    }

    [Fact]
    public async Task Items_Should_CancelCompletionAndClaimPage_When_DisposedBeforeItStarts()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item);
        var enumerator = page.Items.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        await enumerator.DisposeAsync();
        var edgeException = Assert.Throws<InvalidOperationException>(
            () => page.GetAsyncEnumerator(TestContext.Current.CancellationToken));
        var itemsException = Assert.Throws<InvalidOperationException>(
            () => page.Items.GetAsyncEnumerator(TestContext.Current.CancellationToken));

        // assert
        Assert.Equal("A streamed page can only be enumerated once.", edgeException.Message);
        Assert.Equal(edgeException.Message, itemsException.Message);
        await Assert.ThrowsAsync<TaskCanceledException>(() => page.Completion);
    }

    [Fact]
    public async Task Items_Should_Throw_When_PageIsEnumeratedAsEdges()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item);

        // act
        await foreach (var _ in page)
        {
        }

        // assert
        var exception = Assert.Throws<InvalidOperationException>(
            () => page.Items.GetAsyncEnumerator(TestContext.Current.CancellationToken));
        Assert.Equal("A streamed page can only be enumerated once.", exception.Message);
    }
    [Fact]
    public async Task Completion_Should_FaultWithSourceException_When_SourceEnumerationFails()

    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(moveNextException: expectedException),
            new PagingArguments(first: 1),
            static item => item);

        // act
        var enumerationException = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));
        var completionException = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task EnumerateAsync_Should_PropagateSourceException_When_SourceCurrentThrows()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);

        // act
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));

        // assert
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task Completion_Should_FaultWithSourceException_When_SourceCurrentThrows()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);

        // act
        await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, exception);
    }

    [Fact]
    public async Task Completion_Should_PreserveCurrentException_When_SourceDisposalAlsoFails()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(
                disposeException: new InvalidOperationException(),
                currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);

        // act
        var enumerationException = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));
        var completionException = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task Completion_Should_FaultWithCurrentException_When_CurrentThrowsUncanceledOperationCanceledException()
    {
        // arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        var expectedException = new OperationCanceledException(cancellationTokenSource.Token);
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);
        await using var enumerator = page.GetAsyncEnumerator(cancellationTokenSource.Token);

        // act
        var enumerationException = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await enumerator.MoveNextAsync());
        var completionException = await Assert.ThrowsAsync<OperationCanceledException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task Completion_Should_FaultWithCurrentException_When_CurrentThrowsOperationCanceledExceptionForDifferentToken()
    {
        // arrange
        using var enumerationCancellationTokenSource = new CancellationTokenSource();
        using var sourceCancellationTokenSource = new CancellationTokenSource();
        enumerationCancellationTokenSource.Cancel();
        sourceCancellationTokenSource.Cancel();
        var expectedException = new OperationCanceledException(sourceCancellationTokenSource.Token);
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);
        await using var enumerator = page.GetAsyncEnumerator(enumerationCancellationTokenSource.Token);

        // act
        var enumerationException = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await enumerator.MoveNextAsync());
        var completionException = await Assert.ThrowsAsync<OperationCanceledException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task Completion_Should_BeCanceled_When_CurrentThrowsOperationCanceledExceptionForCanceledEnumerationToken()
    {
        // arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var expectedException = new OperationCanceledException(cancellationTokenSource.Token);
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(currentException: expectedException),
            new PagingArguments(first: 1),
            static item => item);
        await using var enumerator = page.GetAsyncEnumerator(cancellationTokenSource.Token);

        // act
        var enumerationException = await Assert.ThrowsAsync<OperationCanceledException>(
            async () => await enumerator.MoveNextAsync());

        // assert
        Assert.Same(expectedException, enumerationException);
        await Assert.ThrowsAsync<TaskCanceledException>(() => page.Completion);
    }

    [Fact]
    public async Task Completion_Should_FaultWithCursorException_When_CursorCreationFails()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            (string _) => throw expectedException);

        // act
        var enumerationException = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));
        var completionException = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task Completion_Should_FaultWithSourceException_When_SourceDisposalFails()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        var page = new StreamPage<string>(
            new ThrowingAsyncEnumerable(disposeException: expectedException),
            new PagingArguments(first: 1),
            static item => item);

        // act
        var enumerationException = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));
        var completionException = await Assert.ThrowsAsync<InvalidOperationException>(() => page.Completion);

        // assert
        Assert.Same(expectedException, enumerationException);
        Assert.Same(enumerationException, completionException);
    }

    [Fact]
    public async Task Completion_Should_BeCanceled_When_EnumerationIsCanceled()
    {
        // arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        var page = new StreamPage<string>(
            new CancellableAsyncEnumerable(),
            new PagingArguments(first: 1),
            static item => item);
        await using var enumerator = page.GetAsyncEnumerator(cancellationTokenSource.Token);

        // act
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await enumerator.MoveNextAsync());

        // assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => page.Completion);
    }

    [Fact]
    public async Task Completion_Should_BeCanceled_When_EnumerationIsDisposedEarly()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a", "b"),
            new PagingArguments(first: 2),
            static item => item);

        // act
        await using (var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken))
        {
            await enumerator.MoveNextAsync();
        }

        // assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => page.Completion);
    }

    [Fact]
    public async Task Completion_Should_BeCanceled_When_EnumerationIsDisposedBeforeItStarts()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item);
        var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        // act
        await enumerator.DisposeAsync();

        // assert
        await Assert.ThrowsAsync<TaskCanceledException>(() => page.Completion);
    }

    [Theory]
    [MemberData(nameof(UnsupportedPagingArguments))]
    public void Constructor_Should_Throw_When_BackwardPagingArgumentsAreSpecified(
        PagingArguments arguments)
    {
        // act
        var exception = Assert.Throws<ArgumentException>(
            () => new StreamPage<string>(CreateItems("a"), arguments, static item => item));

        // assert
        Assert.Equal("arguments", exception.ParamName);
    }

    [Fact]
    public async Task Completion_Should_NotReportPreviousPage_When_AfterIsSpecifiedButSourceIsEmpty()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems(),
            new PagingArguments(first: 1, after: "before"),
            static item => item);

        // act
        await EnumerateAsync(page);
        var completion = await page.Completion;

        // assert
        Assert.False(completion.HasNextPage);
        Assert.False(completion.HasPreviousPage);
    }

    [Fact]
    public async Task Completion_Should_ReportBoundaryFacts_When_FirstIsZero()
    {
        // arrange
        var emptyPage = new StreamPage<string>(
            CreateItems(),
            new PagingArguments(first: 0, after: "before"),
            static item => item);
        var overfetchPage = new StreamPage<string>(
            CreateItems("a", "b"),
            new PagingArguments(first: 0, after: "before"),
            static item => item);

        // act
        await EnumerateAsync(emptyPage);
        await EnumerateAsync(overfetchPage);
        var emptyCompletion = await emptyPage.Completion;
        var overfetchCompletion = await overfetchPage.Completion;

        // assert
        Assert.False(emptyCompletion.HasNextPage);
        Assert.False(emptyCompletion.HasPreviousPage);
        Assert.True(overfetchCompletion.HasNextPage);
        Assert.True(overfetchCompletion.HasPreviousPage);
    }

    [Fact]
    public async Task TotalCount_Should_BeCompleted_When_CountIsKnownUpFront()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item,
            totalCount: 1);

        // act
        var isCompleted = page.TotalCount.IsCompletedSuccessfully;
        var totalCount = await page.TotalCount;

        // assert
        Assert.True(isCompleted);
        Assert.Equal(1, totalCount);
    }

    [Fact]
    public void Constructor_Should_Throw_When_TotalCountTaskIsNull()
    {
        // act
        var exception = Assert.Throws<ArgumentNullException>(
            () => new StreamPage<string>(
                CreateItems("a"),
                new PagingArguments(first: 1),
                static item => item,
                (Task<int?>)null!));

        // assert
        Assert.Equal("totalCount", exception.ParamName);
    }

    [Fact]
    public async Task TotalCount_Should_SettleFromFirstRow_When_CountIsAwaitedBeforeItems()
    {
        // arrange
        await using var source = new CombinedCountSource(CreateItems("a", "b", "c"), 3);
        var page = CreatePage(source);
        List<string> items = [];

        // act
        var totalCount = await page.TotalCount;

        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        // assert
        Assert.Equal(3, totalCount);
        Assert.Equal(["a", "b"], items);
        Assert.True(source.SourceDisposed);
    }

    [Fact]
    public async Task TotalCount_Should_DisposeSource_When_CountIsAwaitedWithoutEnumeration()
    {
        // arrange
        var source = new CombinedCountSource(CreateItems("a", "b", "c"), 3);
        var page = CreatePage(source);

        // act
        var totalCount = await page.TotalCount;
        await source.DisposeAsync();

        // assert
        Assert.Equal(3, totalCount);
        Assert.True(source.SourceDisposed);
    }

    [Fact]
    public async Task TotalCount_Should_Settle_When_ItemsAreEnumeratedWithoutAwaitingCount()
    {
        // arrange
        await using var source = new CombinedCountSource(CreateItems("a", "b", "c"), 3);
        var page = CreatePage(source);
        List<string> items = [];

        // act
        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        // assert
        Assert.Equal(["a", "b"], items);
        Assert.True(page.TotalCount.IsCompletedSuccessfully);
        Assert.True(source.SourceDisposed);
    }

    [Fact]
    public async Task TotalCount_Should_ReportCount_When_CountIsAwaitedAfterItems()
    {
        // arrange
        await using var source = new CombinedCountSource(CreateItems("a", "b", "c"), 3);
        var page = CreatePage(source);
        List<string> items = [];

        // act
        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        var totalCount = await page.TotalCount;

        // assert
        Assert.Equal(["a", "b"], items);
        Assert.Equal(3, totalCount);
    }

    [Fact]
    public async Task TotalCount_Should_FallBackToCountQuery_When_SourceIsEmpty()
    {
        // arrange
        await using var source = new CombinedCountSource(CreateItems(), 0);
        var page = CreatePage(source);
        List<string> items = [];

        // act
        var totalCount = await page.TotalCount;

        await foreach (var item in page.Items)
        {
            items.Add(item);
        }

        // assert
        Assert.Equal(0, totalCount);
        Assert.Empty(items);
        Assert.True(source.UsedCountQuery);
        Assert.True(source.SourceDisposed);
    }

    [Fact]
    public async Task TotalCount_Should_Fault_When_SourceFailsBeforeTheFirstRow()
    {
        // arrange
        var expectedException = new InvalidOperationException();
        await using var source = new CombinedCountSource(
            new ThrowingAsyncEnumerable(moveNextException: expectedException),
            null);
        var page = CreatePage(source);

        // act
        var countException = await Assert.ThrowsAsync<InvalidOperationException>(() => page.TotalCount);
        var enumerationException = await Assert.ThrowsAsync<InvalidOperationException>(() => EnumerateAsync(page));

        // assert
        Assert.Same(expectedException, countException);
        Assert.Same(expectedException, enumerationException);
    }

    [Fact]
    public async Task TotalCount_Should_BeCanceled_When_SourceIsCanceledBeforeTheFirstRow()
    {
        // arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();
        await using var source = new CombinedCountSource(
            new CancellableAsyncEnumerable(),
            null,
            cancellationTokenSource.Token);
        var page = CreatePage(source);

        // act
        var exception = await Assert.ThrowsAnyAsync<OperationCanceledException>(() => page.TotalCount);

        // assert
        Assert.Equal(cancellationTokenSource.Token, exception.CancellationToken);
        Assert.True(page.TotalCount.IsCanceled);
    }

    public static IEnumerable<object[]> UnsupportedPagingArguments()
    {
        yield return [new PagingArguments(last: 1)];
        yield return [new PagingArguments(before: "before")];
        yield return [new PagingArguments(first: 1, after: "after", last: 1, before: "before")];
    }

    private static StreamPage<string> CreatePage(CombinedCountSource source)
        => new(source, new PagingArguments(first: 2), static item => item, source.TotalCount);

    private static async Task EnumerateAsync(StreamPage<string> page)
    {
        await foreach (var _ in page)
        {
        }
    }

    private static async IAsyncEnumerable<string> CreateItems(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    private sealed class ThrowingAsyncEnumerable(
        Exception? moveNextException = null,
        Exception? disposeException = null,
        Exception? currentException = null) : IAsyncEnumerable<string>
    {
        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new ThrowingAsyncEnumerator(moveNextException, disposeException, currentException);
    }

    private sealed class ThrowingAsyncEnumerator(
        Exception? moveNextException,
        Exception? disposeException,
        Exception? currentException) : IAsyncEnumerator<string>
    {
        public string Current => currentException is null ? string.Empty : throw currentException;

        public ValueTask DisposeAsync()
            => disposeException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(disposeException);

        public ValueTask<bool> MoveNextAsync()
            => currentException is not null
                ? ValueTask.FromResult(true)
                : moveNextException is null
                    ? ValueTask.FromResult(false)
                    : ValueTask.FromException<bool>(moveNextException);
    }

    private sealed class CancellableAsyncEnumerable : IAsyncEnumerable<string>
    {
        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new CancellableAsyncEnumerator(cancellationToken);
    }

    private sealed class CancellableAsyncEnumerator(CancellationToken cancellationToken)
        : IAsyncEnumerator<string>
    {
        public string Current => string.Empty;

        public ValueTask DisposeAsync()
            => ValueTask.CompletedTask;

        public ValueTask<bool> MoveNextAsync()
            => ValueTask.FromCanceled<bool>(cancellationToken);
    }

    // Models the provider wrapper contract that StreamPage<T>.TotalCount documents: one combined
    // count and item query whose count settles from the first row, whose first read is shared by
    // the count branch and the stream branch, and which falls back to a count query when the
    // source is empty.
    private sealed class CombinedCountSource : IAsyncEnumerable<string>, IAsyncDisposable
    {
        private readonly TaskCompletionSource<int?> _totalCount =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly IAsyncEnumerable<string> _source;
        private readonly int? _count;
        private readonly CancellationToken _cancellationToken;
        private readonly Task _firstRow;
        private IAsyncEnumerator<string>? _enumerator;
        private string? _parkedRow;
        private bool _hasParkedRow;
        private bool _exhausted;
        private Exception? _failure;

        public CombinedCountSource(
            IAsyncEnumerable<string> source,
            int? count,
            CancellationToken cancellationToken = default)
        {
            _source = source;
            _count = count;
            _cancellationToken = cancellationToken;
            _firstRow = ReadFirstRowAsync();
        }

        public Task<int?> TotalCount => _totalCount.Task;

        public bool SourceDisposed { get; private set; }

        public bool UsedCountQuery { get; private set; }

        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new Enumerator(this);

        public ValueTask DisposeAsync()
            => DisposeSourceAsync();

        private async Task ReadFirstRowAsync()
        {
            try
            {
                _enumerator = _source.GetAsyncEnumerator(_cancellationToken);

                if (await _enumerator.MoveNextAsync())
                {
                    _parkedRow = _enumerator.Current;
                    _hasParkedRow = true;
                    _totalCount.TrySetResult(_count);
                    return;
                }

                _exhausted = true;
                UsedCountQuery = true;
                await DisposeSourceAsync();
                _totalCount.TrySetResult(_count);
            }
            catch (OperationCanceledException exception)
            {
                _failure = exception;
                await DisposeSourceAsync();
                _totalCount.TrySetCanceled(exception.CancellationToken);
            }
            catch (Exception exception)
            {
                _failure = exception;
                await DisposeSourceAsync();
                _totalCount.TrySetException(exception);
            }
        }

        private async ValueTask EnsureFirstRowAsync()
        {
            await _firstRow;

            if (_failure is { } failure)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private async ValueTask DisposeSourceAsync()
        {
            if (_enumerator is { } enumerator)
            {
                _enumerator = null;
                await enumerator.DisposeAsync();
                SourceDisposed = true;
            }
        }

        private sealed class Enumerator(CombinedCountSource source) : IAsyncEnumerator<string>
        {
            public string Current { get; private set; } = string.Empty;

            public async ValueTask<bool> MoveNextAsync()
            {
                await source.EnsureFirstRowAsync();

                if (source._hasParkedRow)
                {
                    source._hasParkedRow = false;
                    Current = source._parkedRow!;
                    source._parkedRow = null;
                    return true;
                }

                if (source._exhausted || source._enumerator is not { } enumerator)
                {
                    return false;
                }

                if (!await enumerator.MoveNextAsync())
                {
                    source._exhausted = true;
                    return false;
                }

                Current = enumerator.Current;
                return true;
            }

            public ValueTask DisposeAsync()
                => source.DisposeSourceAsync();
        }
    }
}

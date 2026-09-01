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

        // assert
        Assert.True(completion.HasNextPage);
        Assert.True(completion.HasPreviousPage);
        Assert.Equal("a", completion.StartCursor);
        Assert.Equal("b", completion.EndCursor);
        Assert.Equal(3, page.TotalCount);
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

    public static IEnumerable<object[]> UnsupportedPagingArguments()
    {
        yield return [new PagingArguments(last: 1)];
        yield return [new PagingArguments(before: "before")];
        yield return [new PagingArguments(first: 1, after: "after", last: 1, before: "before")];
    }

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
}

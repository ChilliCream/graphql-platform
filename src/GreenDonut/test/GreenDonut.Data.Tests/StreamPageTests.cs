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
            new ThrowingAsyncEnumerable(
                moveNextException: new OperationCanceledException(cancellationTokenSource.Token)),
            new PagingArguments(first: 1),
            static item => item);
        await using var enumerator = page.GetAsyncEnumerator(TestContext.Current.CancellationToken);

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
        Exception? disposeException = null) : IAsyncEnumerable<string>
    {
        public IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new ThrowingAsyncEnumerator(moveNextException, disposeException);
    }

    private sealed class ThrowingAsyncEnumerator(
        Exception? moveNextException,
        Exception? disposeException) : IAsyncEnumerator<string>
    {
        public string Current => string.Empty;

        public ValueTask DisposeAsync()
            => disposeException is null
                ? ValueTask.CompletedTask
                : ValueTask.FromException(disposeException);

        public ValueTask<bool> MoveNextAsync()
            => moveNextException is null
                ? ValueTask.FromResult(false)
                : ValueTask.FromException<bool>(moveNextException);
    }
}

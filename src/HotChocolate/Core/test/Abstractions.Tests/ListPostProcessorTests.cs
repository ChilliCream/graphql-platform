using HotChocolate.Execution;

namespace HotChocolate;

public static class ListPostProcessorTests
{
    public static TheoryData<string> NonStreamableResults { get; } =
    [
        "list",
        "array",
        "queryable",
        "enumerable",
        "executable"
    ];

    [Theory]
    [MemberData(nameof(NonStreamableResults))]
    public static void ToStreamResultAsync_Should_ReturnNull_When_ResultIsNotAsyncEnumerable(string kind)
    {
        // arrange
        object result = kind switch
        {
            "list" => new List<int> { 1, 2, 3 },
            "array" => new[] { 1, 2, 3 },
            "queryable" => new[] { 1, 2, 3 }.AsQueryable(),
            "enumerable" => Enumerable.Range(1, 3),
            _ => Executable.From(new[] { 1, 2, 3 }.AsQueryable())
        };

        // act
        var stream = ListPostProcessor<int>.Default.ToStreamResultAsync(result, CancellationToken.None);

        // assert
        Assert.Null(stream);
    }

    [Fact]
    public static async Task ToCompletionResultAsync_Should_ReturnList_When_ResultIsQueryable()
    {
        // arrange
        object result = new[] { 1, 2, 3 }.AsQueryable();

        // act
        var completed = await ListPostProcessor<int>.Default.ToCompletionResultAsync(
            result,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(new List<int> { 1, 2, 3 }, Assert.IsType<List<int>>(completed));
    }

    [Fact]
    public static async Task ToStreamResultAsync_Should_StreamElements_When_ResultIsAsyncEnumerable()
    {
        // arrange
        object result = new TokenCapturingAsyncEnumerable(1, 2, 3);

        // act
        var stream = ListPostProcessor<int>.Default.ToStreamResultAsync(result, CancellationToken.None);

        // assert
        Assert.NotNull(stream);

        var elements = new List<object?>();

        await foreach (var element in stream.WithCancellation(TestContext.Current.CancellationToken))
        {
            elements.Add(element);
        }

        Assert.Equal(new object?[] { 1, 2, 3 }, elements);
    }

    [Fact]
    public static async Task ToStreamResultAsync_Should_PassCancellationToken_When_ResultIsAsyncEnumerable()
    {
        // arrange
        using var cancellation = new CancellationTokenSource();
        var source = new TokenCapturingAsyncEnumerable(1);
        await cancellation.CancelAsync();

        // act
        var stream = ListPostProcessor<int>.Default.ToStreamResultAsync(source, cancellation.Token);
        await using var enumerator = stream?.GetAsyncEnumerator(TestContext.Current.CancellationToken);

        if (enumerator is not null)
        {
            await enumerator.MoveNextAsync();
        }

        // assert
        Assert.NotNull(stream);
        Assert.True(source.WasCancelled);
    }

    [Fact]
    public static void ToStreamResultAsync_Should_ReturnSource_When_ResultIsAsyncEnumerableOfReferenceType()
    {
        // arrange
        var source = new StringAsyncEnumerable("a");

        // act
        var stream = ListPostProcessor<string>.Default.ToStreamResultAsync(source, CancellationToken.None);

        // assert
        Assert.Same(source, stream);
    }

    private sealed class TokenCapturingAsyncEnumerable(params int[] items) : IAsyncEnumerable<int>
    {
        public bool WasCancelled { get; private set; }

        public async IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            WasCancelled = cancellationToken.IsCancellationRequested;

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    private sealed class StringAsyncEnumerable(params string[] items) : IAsyncEnumerable<string>
    {
        public async IAsyncEnumerator<string> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}

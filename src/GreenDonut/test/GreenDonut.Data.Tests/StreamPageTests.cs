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

    private static async IAsyncEnumerable<string> CreateItems(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }
}

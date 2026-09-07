using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class StreamConnectionTests
{
    [Fact]
    public async Task Connection_Should_StreamEdgesAndDeferPageInfo_When_EdgesAreStreamed()
    {
        // arrange
        var executor = await CreateExecutorAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                items(first: 2) {
                    edges @stream(initialCount: 1) { node { name } }
                    ... @defer { pageInfo { hasNextPage endCursor } }
                }
            }
            """,
            cts.Token);

        var delivery = await ReadDeliveryAsync(result, cts.Token);

        // assert
        delivery.MatchInlineSnapshots(
        [
            """initial:{"items":{"edges":[{"node":{"name":"a"}}]}}""",
            """items:[{"node":{"name":"b"}}]""",
            """deferred:{"pageInfo":{"hasNextPage":true,"endCursor":"b"}}""",
            "end"
        ]);
    }

    [Fact]
    public async Task Connection_Should_StreamNodesAndDeferPageInfo_When_NodesAreStreamed()
    {
        // arrange
        var executor = await CreateExecutorAsync();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                items(first: 2) {
                    nodes @stream(initialCount: 1) { name }
                    ... @defer { pageInfo { hasNextPage endCursor } }
                }
            }
            """,
            cts.Token);

        var delivery = await ReadDeliveryAsync(result, cts.Token);

        // assert
        delivery.MatchInlineSnapshots(
        [
            """initial:{"items":{"nodes":[{"name":"a"}]}}""",
            """items:[{"name":"b"}]""",
            """deferred:{"pageInfo":{"hasNextPage":true,"endCursor":"b"}}""",
            "end"
        ]);
    }

    [Fact]
    public async Task Connection_Should_DeliverInline_When_EdgesAndNodesConsumeTheItemSource()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var streamed = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges @stream(initialCount: 1) { node { name } } nodes { name } } }");
        var expected = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } nodes { name } } }");

        // assert
        // the item source is one-shot, so the second consumer fails in both deliveries alike.
        Assert.Equal(expected, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"errors":[{"message":"Unexpected Execution Error","path":["items","nodes"]}],"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"nodes":null}}}
            """);
    }

    [Fact]
    public async Task Connection_Should_DeliverInline_When_EdgesAreSelectedTwice()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var streamed = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges @stream(initialCount: 1) { node { name } } other: edges { cursor } } }");
        var expected = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } other: edges { cursor } } }");

        // assert
        // the item source is one-shot, so the aliased consumer fails in both deliveries alike.
        Assert.Equal(expected, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"errors":[{"message":"Unexpected Execution Error","path":["items","other"]}],"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"other":null}}}
            """);
    }

    [Fact]
    public async Task Connection_Should_DeliverInline_When_PageInfoIsNotDeferred()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var streamed = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges @stream(initialCount: 1) { node { name } } pageInfo { hasNextPage endCursor } } }");
        var expected = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } pageInfo { hasNextPage endCursor } } }");

        // assert
        Assert.Equal(expected, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"pageInfo":{"hasNextPage":true,"endCursor":"b"}}}}
            """);
    }

    [Fact]
    public async Task Connection_Should_DeliverInline_When_TotalCountIsNotDeferred()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var streamed = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges @stream(initialCount: 1) { node { name } } totalCount } }");
        var expected = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } totalCount } }");

        // assert
        Assert.Equal(expected, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"totalCount":3}}}
            """);
    }

    [Fact]
    public async Task Connection_Should_MatchPageConnection_When_StreamIsNotRequested()
    {
        // arrange
        const string query =
            "{ items(first: 2) { edges { node { name } } pageInfo { hasNextPage endCursor } totalCount } }";
        var streamExecutor = await CreateExecutorAsync();
        var pageExecutor = await CreateExecutorAsync<PageQuery>();

        // act
        var streamed = await ExecuteInlineAsync(streamExecutor, query);
        var paged = await ExecuteInlineAsync(pageExecutor, query);

        // assert
        Assert.Equal(paged, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"pageInfo":{"hasNextPage":true,"endCursor":"b"},"totalCount":3}}}
            """);
    }

    private static async Task<string> ExecuteInlineAsync(IRequestExecutor executor, string query)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var result = await executor.ExecuteAsync(query, cts.Token);
        return Assert.IsType<OperationResult>(result).ToJson(withIndentations: false);
    }

    // Projects the response stream to the ordered delivery events it carries. The events are
    // independent of the payload boundaries that the delivery coalescing produces.
    private static async Task<List<string>> ReadDeliveryAsync(
        IExecutionResult result,
        CancellationToken cancellationToken)
    {
        var events = new List<string>();

        await foreach (var payload in
            Assert.IsType<ResponseStream>(result).ReadResultsAsync().WithCancellation(cancellationToken))
        {
            await using var current = payload;
            using var document = JsonDocument.Parse(current.ToJson(withIndentations: false));
            var root = document.RootElement;

            if (root.TryGetProperty("data", out var data))
            {
                events.Add($"initial:{data.GetRawText()}");
            }

            if (root.TryGetProperty("incremental", out var incremental))
            {
                foreach (var entry in incremental.EnumerateArray())
                {
                    if (entry.TryGetProperty("items", out var items))
                    {
                        events.Add($"items:{items.GetRawText()}");
                    }
                    else if (entry.TryGetProperty("data", out var deferred))
                    {
                        events.Add($"deferred:{deferred.GetRawText()}");
                    }
                }
            }

            if (root.TryGetProperty("hasNext", out var hasNext) && !hasNext.GetBoolean())
            {
                events.Add("end");
            }
        }

        return events;
    }

    private static Task<IRequestExecutor> CreateExecutorAsync()
        => CreateExecutorAsync<Query>();

    private static async Task<IRequestExecutor> CreateExecutorAsync<TQuery>() where TQuery : class
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<TQuery>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

    public sealed class Query
    {
        public StreamPageConnection<Item> GetItems(int first)
            => new(
                new StreamPage<Item>(
                    CreateItems(),
                    new PagingArguments(first: first),
                    static item => item.Name,
                    totalCount: 3));

        private static async IAsyncEnumerable<Item> CreateItems()
        {
            foreach (var name in new[] { "a", "b", "c" })
            {
                await Task.Yield();
                yield return new Item(name);
            }
        }
    }

    [GraphQLName("Query")]
    public sealed class PageQuery
    {
        [GraphQLType<NonNullType<ItemPageConnectionType>>]
        public PageConnection<Item> GetItems(int first)
            => new(
                Page<Item>.Create(
                    [new Item("a"), new Item("b")],
                    hasNextPage: true,
                    hasPreviousPage: false,
                    static item => item.Name,
                    totalCount: 3));
    }

    public sealed class ItemPageConnectionType : ObjectType<PageConnection<Item>>;

    public sealed class Item(string name)
    {
        public string Name { get; } = name;
    }
}

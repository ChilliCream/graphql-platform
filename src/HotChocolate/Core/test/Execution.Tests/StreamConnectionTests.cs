using GreenDonut.Data;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Execution;

/// <summary>
/// Covers the delivery of a streamed connection end to end: the Relay pattern that streams the
/// edges and defers the page information, and the selections that fall back to an inlined
/// connection because the connection item source is one-shot.
/// </summary>
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
                    edges @stream(initialCount: 1) { node { name } cursor }
                    ... @defer { pageInfo { hasNextPage endCursor } }
                }
            }
            """,
            cts.Token);

        // assert
        var delivery = await IncrementalDeliveryReader.ReadDeliveryAsync(result, cts.Token);
        delivery.MatchInlineSnapshots(
        [
            """initial:{"items":{"edges":[{"node":{"name":"a"},"cursor":"a"}]}}""",
            """items:[{"node":{"name":"b"},"cursor":"b"}]""",
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

        // assert
        var delivery = await IncrementalDeliveryReader.ReadDeliveryAsync(result, cts.Token);
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
        var inlined = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } nodes { name } } }");

        // assert
        // the item source is one-shot, so the second consumer fails with and without the directive.
        Assert.Equal(inlined, streamed);
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
        var inlined = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } other: edges { cursor } } }");

        // assert
        // the aliased selection consumes the same one-shot source and fails in both deliveries.
        Assert.Equal(inlined, streamed);
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
            "{ items(first: 2) { edges @stream(initialCount: 1) { node { name } } pageInfo { hasNextPage } } }");
        var inlined = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } pageInfo { hasNextPage } } }");

        // assert
        // page information that is delivered with the initial response blocks the stream.
        Assert.Equal(inlined, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"pageInfo":{"hasNextPage":true}}}}
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
        var inlined = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } totalCount } }");

        // assert
        // a total count that is delivered with the initial response blocks the stream.
        Assert.Equal(inlined, streamed);
        streamed.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"totalCount":3}}}
            """);
    }

    [Fact]
    public async Task Connection_Should_DeliverInline_When_StreamIsNotRequested()
    {
        // arrange
        var executor = await CreateExecutorAsync();

        // act
        var result = await ExecuteInlineAsync(
            executor,
            "{ items(first: 2) { edges { node { name } } pageInfo { hasNextPage endCursor } totalCount } }");

        // assert
        result.MatchInlineSnapshot(
            """
            {"data":{"items":{"edges":[{"node":{"name":"a"}},{"node":{"name":"b"}}],"pageInfo":{"hasNextPage":true,"endCursor":"b"},"totalCount":3}}}
            """);
    }

    private static async Task<string> ExecuteInlineAsync(IRequestExecutor executor, string query)
    {
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        return Assert.IsType<OperationResult>(result).ToJson(withIndentations: false);
    }

    private static async Task<IRequestExecutor> CreateExecutorAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<Query>()
            .ModifyOptions(
                o =>
                {
                    o.EnableDefer = true;
                    o.EnableStream = true;
                })
            .BuildRequestExecutorAsync();

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

    public sealed class Item(string name)
    {
        public string Name { get; } = name;
    }
}

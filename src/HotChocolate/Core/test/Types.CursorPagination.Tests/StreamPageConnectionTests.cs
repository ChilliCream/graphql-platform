using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Types.Pagination;

public class StreamPageConnectionTests
{
    [Fact]
    public async Task Edges_Should_ExposeCursorsAndCompletePageInfo_When_StreamPageIsEnumerated()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a", "b", "c"),
            new PagingArguments(first: 2, after: "before"),
            static item => item,
            totalCount: 3);
        var connection = new StreamPageConnection<string>(page);
        var pageInfo = connection.PageInfo;
        List<StreamEdge<string>> edges = [];

        // act
        await foreach (var edge in connection.Edges!)
        {
            edges.Add(edge);
        }

        var info = await pageInfo;

        // assert
        Assert.True(pageInfo.IsCompleted);
        Assert.Equal(["a:a", "b:b"], edges.Select(t => $"{t.Node}:{t.Cursor}"));
        Assert.Equal(
            (true, true, "a", "b", (int?)3),
            (info.HasNextPage, info.HasPreviousPage, info.StartCursor, info.EndCursor, connection.TotalCount));
    }

    [Fact]
    public async Task Nodes_Should_DeriveNodes_When_EdgesAreStreamed()
    {
        // arrange
        var connection = new StreamPageConnection<string>(
            new StreamPage<string>(
                CreateItems("a", "b", "c"),
                new PagingArguments(first: 2),
                static item => item));
        List<string> nodes = [];

        // act
        await foreach (var node in connection.Nodes!)
        {
            nodes.Add(node);
        }

        // assert
        Assert.Equal(["a", "b"], nodes);
    }

    [Fact]
    public async Task Edges_Should_ExposeEmptyConnectionFacts_When_StreamPageIsEmpty()
    {
        // arrange
        var connection = new StreamPageConnection<string>(
            new StreamPage<string>(
                CreateItems(),
                new PagingArguments(first: 2),
                static item => item,
                totalCount: 0));
        List<StreamEdge<string>> edges = [];

        // act
        await foreach (var edge in connection.Edges!)
        {
            edges.Add(edge);
        }

        var info = await connection.PageInfo;

        // assert
        Assert.Empty(edges);
        Assert.Equal(
            (false, false, (string?)null, (string?)null, (int?)0),
            (info.HasNextPage, info.HasPreviousPage, info.StartCursor, info.EndCursor, connection.TotalCount));
    }

    [Fact]
    public async Task ImplicitConversion_Should_WrapStreamPage_When_ConvertingStreamPageToConnection()
    {
        // arrange
        var page = new StreamPage<string>(
            CreateItems("a"),
            new PagingArguments(first: 1),
            static item => item);
        List<string> nodes = [];

        // act
        StreamPageConnection<string> connection = page;

        await foreach (var node in connection.Nodes!)
        {
            nodes.Add(node);
        }

        // assert
        Assert.Equal(["a"], nodes);
    }

    [Fact]
    public async Task Schema_Should_MatchPageConnection_When_StreamPageConnectionIsUsed()
    {
        // arrange
        var pageExecutor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<PageQueryType>()
            .AddType<StringPageConnectionType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);
        var streamExecutor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType<StreamPageQueryType>()
            .AddType<StringStreamPageConnectionType>()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var pageSchema = pageExecutor.Schema.ToString();
        var streamSchema = streamExecutor.Schema.ToString();

        // assert
        Assert.Equal(pageSchema, streamSchema);
        streamExecutor.Schema.MatchSnapshot();
    }

    private static async IAsyncEnumerable<string> CreateItems(params string[] items)
    {
        foreach (var item in items)
        {
            yield return item;
            await Task.Yield();
        }
    }

    [GraphQLName("Query")]
    public sealed class PageQuery
    {
        public PageConnection<string> GetItems()
            => new(Page<string>.Create(["a"], false, false, static item => item));
    }

    [GraphQLName("Query")]
    public sealed class StreamPageQuery
    {
        public StreamPageConnection<string> GetItems()
            => new(
                new StreamPage<string>(
                    CreateItems("a"),
                    new PagingArguments(first: 1),
                    static item => item));
    }

    public sealed class PageQueryType : ObjectType<PageQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<PageQuery> descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field(t => t.GetItems()).Type<NonNullType<StringPageConnectionType>>();
        }
    }

    public sealed class StreamPageQueryType : ObjectType<StreamPageQuery>
    {
        protected override void Configure(IObjectTypeDescriptor<StreamPageQuery> descriptor)
        {
            descriptor.Name("Query");
            descriptor.Field(t => t.GetItems()).Type<NonNullType<StringStreamPageConnectionType>>();
        }
    }

    public sealed class StringPageConnectionType : ObjectType<PageConnection<string>>;

    public sealed class StringStreamPageConnectionType : ObjectType<StreamPageConnection<string>>;
}

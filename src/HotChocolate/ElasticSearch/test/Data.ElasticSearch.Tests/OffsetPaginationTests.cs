using System.Text.Json.Nodes;
using HotChocolate.Execution;
using HotChocolate.Internal;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class OffsetPaginationTests : TestBase
{
    private readonly Foo[] _data;

    /// <inheritdoc />
    public OffsetPaginationTests(ElasticsearchResource resource) : base(resource)
    {
        _data = Enumerable
            .Range(0, 20)
            .Select(i => new Foo(i))
            .ToArray();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_Offset_Pagination_Default_Items()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test {
                items {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                }
                totalCount
            }
        }
        """;
        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_Offset_Pagination_Take_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(take: 2) {
                items {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                }
                totalCount
            }
        }
        """;

        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_Offset_Pagination_Skip_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(skip: 2) {
                items {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                }
                totalCount
            }
        }
        """;

        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_Offset_Pagination_Skip_Take_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(skip: 2 take: 2) {
                items {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                }
                totalCount
            }
        }
        """;

        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task Execute_Should_PageWithDefaultProvider_When_SourceIsNotElasticSearch()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x =>
            {
                x.Name("Query");
                x.Field("list").UseOffsetPaging<ObjectType<Foo>>().Resolve<List<Foo>>(_ => [.. _data]);
                x.Field("queryable").UseOffsetPaging<ObjectType<Foo>>().Resolve(_ => _data.AsQueryable());
            })
            .AddElasticSearchPagingProviders()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                list(skip: 1, take: 2) { items { index } }
                queryable(skip: 1, take: 2) { items { index } }
            }
            """,
            TestContext.Current.CancellationToken);

        // assert
        var json = JsonNode.Parse(result.ToJson())!;
        Assert.Null(json["errors"]);
        Assert.Equal("[{\"index\":1},{\"index\":2}]", json["data"]!["list"]!["items"]!.ToJsonString());
        Assert.Equal("[{\"index\":1},{\"index\":2}]", json["data"]!["queryable"]!["items"]!.ToJsonString());
    }

    [Fact]
    public async Task Execute_Should_LeaveSourceToOtherProvider_When_SourceIsNotAList()
    {
        // arrange
        var executor = await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x =>
            {
                x.Name("Query");
                x.Field("other").UseOffsetPaging<ObjectType<Foo>>().Resolve<OtherSource>(_ => new OtherSource());
            })
            .AddOffsetPagingProvider<OtherPagingProvider>()
            .AddElasticSearchPagingProviders()
            .BuildRequestExecutorAsync(cancellationToken: TestContext.Current.CancellationToken);

        // act
        var result = await executor.ExecuteAsync(
            "{ other { items { index } } }",
            TestContext.Current.CancellationToken);

        // assert
        var json = JsonNode.Parse(result.ToJson())!;
        Assert.Null(json["errors"]);
        Assert.Equal("[{\"index\":42}]", json["data"]!["other"]!["items"]!.ToJsonString());
    }

    // a source that only its own provider pages, as a MongoDB executable is.
    public sealed class OtherSource;

    public sealed class OtherPagingProvider : OffsetPagingProvider
    {
        public override bool CanHandle(IExtendedType source) => source.Source == typeof(OtherSource);

        protected override OffsetPagingHandler CreateHandler(IExtendedType source, PagingOptions options)
            => new OtherPagingHandler(options);
    }

    private sealed class OtherPagingHandler(PagingOptions options) : OffsetPagingHandler(options)
    {
        protected override ValueTask<CollectionSegment> SliceAsync(
            IResolverContext context,
            object source,
            OffsetPagingArguments arguments)
            => new(new CollectionSegment([new Foo(42)], new CollectionSegmentInfo(false, false), 1));
    }

    private ValueTask<IRequestExecutor> CreateExecutorAsync()
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseOffsetPaging<ObjectType<Foo>>(options: new PagingOptions { IncludeTotalCount = true })
                .UseTestReport()
                .ResolveTestData<Foo>(Client))
            .AddElasticSearchPagingProviders()
            .BuildTestExecutorAsync();
    }

    public class Foo
    {
        public int Index { get; set; }

        public Foo(int index)
        {
            Index = index;
        }
    }
}

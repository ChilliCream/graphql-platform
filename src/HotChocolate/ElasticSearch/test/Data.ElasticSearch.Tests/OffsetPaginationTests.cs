using System.Text.Json.Nodes;
using HotChocolate.Execution;
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

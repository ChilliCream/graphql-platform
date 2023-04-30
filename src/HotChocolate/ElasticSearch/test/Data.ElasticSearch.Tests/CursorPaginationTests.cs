using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class CursorPaginationTests : TestBase
{
    private readonly Foo[] _data;

    /// <inheritdoc />
    public CursorPaginationTests(ElasticsearchResource resource) : base(resource)
    {
        _data = Enumerable
            .Range(0, 20)
            .Select(i => new Foo(i))
            .ToArray();
    }

    [Fact]
    public async Task ElasticSearch_Cursor_Pagination_Default_Items()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test {
                edges {
                    node {
                        index
                    }
                    cursor
                }
                nodes {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                    startCursor
                    endCursor
                }
                totalCount
            }
        }
        """;
        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Cursor_Pagination_First_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(first: 2) {
                edges {
                    node {
                        index
                    }
                    cursor
                }
                nodes {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                    startCursor
                    endCursor
                }
                totalCount
            }
        }
        """;
        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Cursor_Pagination_First_2_After()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(first: 2 after: ""MQ=="") {
                edges {
                    node {
                        index
                    }
                    cursor
                }
                nodes {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                    startCursor
                    endCursor
                }
                totalCount
            }
        }
        """;
        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Cursor_Pagination_Last_2_Before()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(last: 1 after: ""MQ=="") {
                edges {
                    node {
                        index
                    }
                    cursor
                }
                nodes {
                    index
                }
                pageInfo {
                    hasNextPage
                    hasPreviousPage
                    startCursor
                    endCursor
                }
                totalCount
            }
        }
        """;
        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    private ValueTask<IRequestExecutor> CreateExecutorAsync()
    {
        return new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UsePaging<ObjectType<Foo>>(options: new PagingOptions { IncludeTotalCount = true })
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchPagingProvider()
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

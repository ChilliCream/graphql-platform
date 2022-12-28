using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

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
    public async Task ElasticSearch_Offset_Pagination_Default_Items()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = @"
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
}";
        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Offset_Pagination_Take_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = @"
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
}";

        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Offset_Pagination_Skip_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = @"
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
}";

        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Offset_Pagination_Skip_Take_2()
    {
        await IndexDocuments(_data);

        var executor = await CreateExecutorAsync();

        const string query = @"
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
}";

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
                .UseOffsetPaging<ObjectType<Foo>>(options: new PagingOptions { IncludeTotalCount = true })
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

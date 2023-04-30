using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class SortTests : TestBase
{
    private readonly IReadOnlyList<Foo> _data = new[]
    {
        new Foo { Name = "B" }, new Foo { Name = "A" }, new Foo { Name = "BA" },
        new Foo { Name = "C" }, new Foo { Name = "ac" }
    };

    /// <inheritdoc />
    public SortTests(ElasticsearchResource resource) : base(resource)
    {
    }

    [Fact]
    public async Task ElasticSearch_Sort_Ascending()
    {
        await IndexDocuments(_data);
        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(order: {name: ASC}) {
                name
            }
        }
        """;

        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    [Fact]
    public async Task ElasticSearch_Sort_Descending()
    {
        await IndexDocuments(_data);
        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test(order: {name: DESC}) {
                name
            }
        }
        """;

        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    private async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseSorting<SortInputType<Foo>>()
                .UseTestReport()
                .ResolveTestData(Client, _data))
            .AddElasticSearchSorting()
            .BuildTestExecutorAsync();
    }

    public class Foo
    {
        public string Name { get; set; } = string.Empty;
    }
}

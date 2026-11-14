using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class ElasticSearchFiledNameTest : TestBase
{
    /// <inheritdoc />
    public ElasticSearchFiledNameTest(ElasticsearchResource resource) : base(resource)
    {
    }

    [Fact]
    public async Task Execute_Should_MatchSnapshot_When_Custom_FieldName_IsUsed()
    {
        await IndexDocuments(new List<FieldNameTestTerm>
        {
            new(){ Value = "hello"},
            new(){ Value = "world"}
        });

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test {
                value
            }
        }
        """;

        var result = await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
        result.MatchQuerySnapshot();
    }

    public class FieldNameTestTerm
    {
        [System.Text.Json.Serialization.JsonPropertyName("special")]
        public string Value { get; set; } = string.Empty;
    }

    private async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseSorting<SortInputType<CursorPaginationTests.Foo>>()
                .UseTestReport()
                .ResolveTestData<FieldNameTestTerm>(Client))
            .AddElasticSearchSorting()
            .BuildTestExecutorAsync();
    }
}

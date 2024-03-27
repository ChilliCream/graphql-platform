using HotChocolate.Data.ElasticSearch.Attributes;
using HotChocolate.Data.Sorting;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using Squadron;
using Xunit;

namespace HotChocolate.Data.ElasticSearch;

[Collection("Elastic Tests")]
public class ElasticSearchFiledNameTest : TestBase
{
    /// <inheritdoc />
    public ElasticSearchFiledNameTest(ElasticsearchResource resource) : base(resource)
    {
    }

    [Fact]
    public async Task Custom_FieldName_IsUsed()
    {
        await IndexDocuments(new List<FieldNameTestTerm>
        {
            new(){ Value = "hello"},
            new(){ Value = "world"},
        });

        var executor = await CreateExecutorAsync();

        const string query = """
        query {
            test {
                value
            }
        }
        """;

        var result = await executor.ExecuteAsync(query);
        result.MatchQuerySnapshot();
    }

    public class FieldNameTestTerm
    {
        [ElasticSearchFieldName("special")]
        [PropertyName("special")]
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

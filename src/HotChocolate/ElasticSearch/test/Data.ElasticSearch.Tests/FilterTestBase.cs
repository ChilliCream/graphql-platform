using HotChocolate.Data.ElasticSearch.Filters;
using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data.ElasticSearch;

public abstract class FilterTestBase<TData, TFilterType> : TestBase
    where TData : class
    where TFilterType : FilterInputType<TData>
{
    /// <inheritdoc />
    protected FilterTestBase(ElasticsearchResource resource) : base(resource)
    {
    }

    protected abstract IReadOnlyList<TData> Data { get; }

    protected async Task<IExecutionResult> ExecuteFilterTest(string filter, string selection)
    {
        await IndexDocuments(Data);
        var executorAsync = await CreateExecutorAsync();

        string query = $$"""
        {
            test(where: { {{filter}} }) {
                {{selection}}
            }
        }
        """;

        return await executorAsync.ExecuteAsync(query);
    }

    private async Task<IRequestExecutor> CreateExecutorAsync()
    {
        return await new ServiceCollection()
            .AddGraphQL()
            .AddFiltering(x => x.BindRuntimeType<TData, TFilterType>().AddElasticSearchDefaults())
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<TFilterType>()
                .UseTestReport()
                .ResolveTestData<TData>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();
    }
}

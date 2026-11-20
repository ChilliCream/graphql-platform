using HotChocolate.Data.Filters;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.ElasticSearch;

public abstract class FilterTestBase<TData, TFilterType> : TestBase
    where TData : class
    where TFilterType : FilterInputType<TData>
{
    protected FilterTestBase(ElasticsearchResource resource) : base(resource)
    {
    }

    protected abstract IReadOnlyList<TData> Data { get; }

    protected async Task<IExecutionResult> ExecuteFilterTest(string filter, string selection)
    {
        await IndexDocuments(Data);
        var executor = await CreateExecutorAsync();

        var query = $$"""
        {
            test(where: { {{filter}} }) {
                {{selection}}
            }
        }
        """;

        return await executor.ExecuteAsync(query, TestContext.Current.CancellationToken);
    }

    private async Task<IRequestExecutor> CreateExecutorAsync()
        => await new ServiceCollection()
            .AddGraphQL()
            .AddQueryType(x => x
                .Name("Query")
                .Field("test")
                .UseFiltering<TFilterType>()
                .UseTestReport()
                .ResolveTestData<TData>(Client))
            .AddElasticSearchFiltering()
            .BuildTestExecutorAsync();
}

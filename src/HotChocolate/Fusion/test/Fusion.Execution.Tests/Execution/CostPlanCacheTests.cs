using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Verifies the capacity and warmup behavior of the dedicated <see cref="Cache{TValue}"/>
/// for <see cref="CostPlan"/> instances.
/// </summary>
public class CostPlanCacheTests : FusionTestBase
{
    [Fact]
    public async Task CostPlanCache_Should_HaveConfiguredCapacity_When_OptionIsSet()
    {
        // arrange
        const int cacheCapacity = 517;
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .ModifyCostOptions(o => o.CostPlanCacheSize = cacheCapacity)
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        var costPlanCache = executor.Schema.Services.GetRequiredService<Cache<CostPlan>>();

        // assert
        Assert.Equal(cacheCapacity, costPlanCache.Capacity);
    }

    [Fact]
    public async Task Warmup_Should_WriteCostAndOperationPlanCaches_Without_Executing()
    {
        // arrange
        IExecutionResult? warmupResult = null;
        var warmupRequest = OperationRequestBuilder.New()
            .SetDocument("query Warmup { field }")
            .MarkAsWarmupRequest()
            .Build();
        var services = new ServiceCollection();
        services
            .AddGraphQLGateway()
            .AddWarmupTask(async (executor, cancellationToken) =>
                warmupResult = await executor.ExecuteAsync(warmupRequest, cancellationToken))
            .AddInMemoryConfiguration(
                ComposeSchemaDocument(
                    """
                    type Query {
                      field: String!
                    }
                    """));

        // act
        var executor = await services.BuildServiceProvider().GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // assert
        Assert.IsType<WarmupExecutionResult>(warmupResult);
        var operationPlanCache = executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>();
        var costPlanCache = executor.Schema.Services.GetRequiredService<Cache<CostPlan>>();
        Assert.Equal(1, operationPlanCache.Count);
        Assert.Equal(1, costPlanCache.Count);
    }
}

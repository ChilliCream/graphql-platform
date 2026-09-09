using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion.Execution;

/// <summary>
/// Verifies the capacity, warmup, and rejection behavior of the dedicated <see cref="Cache{TValue}"/>
/// for <see cref="CostPlan"/> instances.
/// </summary>
public class CostPlanCacheTests : FusionTestBase
{
    private const string CostlySchema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR

        type Query {
          expensive: Expensive
        }

        type Expensive @cost(weight: "2000") {
          value: String
        }
        """;

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

    [Fact]
    public async Task SingleFlightFollowers_Should_ObserveCostExceededError_When_LeaderRejects()
    {
        // arrange
        const int requestCount = 4;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var executor = await new ServiceCollection()
            .AddGraphQLGateway()
            .AddInMemoryConfiguration(ComposeSchemaDocument(CostlySchema))
            .Services
            .BuildServiceProvider()
            .GetRequestExecutorAsync(cancellationToken: cts.Token);

        // act
        // A rejected single-flight leader signals "no plan, rejected" instead of faulting
        // the pipeline, so every follower observes the same HC0047 error, not an exception.
        var results = await Task.WhenAll(
            Enumerable.Range(0, requestCount)
                .Select(_ => executor.ExecuteAsync("{ expensive { value } }", cts.Token)));

        // assert
        Assert.All(
            results,
            result => Assert.Equal(
                ErrorCodes.Execution.CostExceeded,
                Assert.Single(result.ExpectOperationResult().Errors).Code));
    }
}

using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Verifies the pre-plan cost enforcement checkpoint (hc-3-mmh.8/.9): an over-cost request never
/// reaches the operation planner, its compiled <see cref="CostPlan"/> is still cached so repeat
/// rejection is evaluate-only, and enforcement can be turned off with default security.
/// </summary>
public class CostEnforcementTests : FusionTestBase
{
    private const string OverCostSchema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query {
          expensive: Expensive
        }

        type Expensive @cost(weight: "2000") {
          value: String
        }
        """;

    private const string OverCostQuery =
        """
        query OverCost {
          expensive {
            value
          }
        }
        """;

    [Fact(Skip = "enabled by fusion-cost-middleware")]
    public async Task OverCostRequest_Should_BeRejected_BeforePlanning_When_TypeCostExceedsLimit()
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(OverCostQuery);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);

        var (operationPlanCache, costPlanCache) = await GetCachesAsync(gateway);
        Assert.Equal(0, operationPlanCache.Count);
        Assert.Equal(1, costPlanCache.Count);
    }

    [Fact(Skip = "enabled by fusion-cost-middleware")]
    public async Task OverCostRequest_Should_EvaluateOnly_When_RepeatedAfterRejection()
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(OverCostQuery);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var uri = new Uri("http://localhost:5000/graphql");

        using var firstResponse = await client.PostAsync(request, uri, TestContext.Current.CancellationToken);
        await firstResponse.ReadAsResultAsync(TestContext.Current.CancellationToken);

        // act
        using var secondResponse = await client.PostAsync(request, uri, TestContext.Current.CancellationToken);
        await secondResponse.ReadAsResultAsync(TestContext.Current.CancellationToken);

        // assert - the second rejection reused the cached CostPlan instead of recompiling it
        var (operationPlanCache, costPlanCache) = await GetCachesAsync(gateway);
        Assert.Equal(0, operationPlanCache.Count);
        Assert.Equal(1, costPlanCache.Count);
    }

    [Fact(Skip = "enabled by fusion-cost-middleware")]
    public async Task OverCostRequest_Should_Execute_When_DefaultSecurityIsDisabled()
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            disableDefaultSecurity: true);
        var request = new OperationRequest(OverCostQuery);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }

    private static async Task<(Cache<OperationPlan> OperationPlans, Cache<CostPlan> CostPlans)> GetCachesAsync(
        Gateway gateway)
    {
        var manager = gateway.Services.GetRequiredService<FusionRequestExecutorManager>();
        var executor = await manager.GetExecutorAsync();
        return (
            executor.Schema.Services.GetRequiredService<Cache<OperationPlan>>(),
            executor.Schema.Services.GetRequiredService<Cache<CostPlan>>());
    }
}

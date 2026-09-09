using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.Caching.Memory;
using HotChocolate.CostAnalysis;
using HotChocolate.Execution;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Verifies that cost enforcement runs before planning and caches rejected operations'
/// <see cref="CostPlan"/> instances for evaluate-only retries.
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

    [Fact]
    public async Task OverCostRequest_Should_BeRejected_BeforePlanning_When_TypeCostExceedsLimit()
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new HotChocolate.Transport.OperationRequest(OverCostQuery);

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

    [Fact]
    public async Task OverCostRequest_Should_EvaluateOnly_When_RepeatedAfterRejection()
    {
        // arrange
        var analysisResults = new ConcurrentQueue<CostAnalysisResult>();
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: builder => builder.UseRequest(
                (_, next) => async context =>
                {
                    await next(context);

                    if (HotChocolate.Execution.FusionRequestContextExtensions.TryGetCostAnalysisResult(
                        context,
                        out var result))
                    {
                        analysisResults.Enqueue(result);
                    }
                },
                before: WellKnownRequestMiddleware.CostAnalyzerMiddleware,
                allowMultiple: true));
        var request = new HotChocolate.Transport.OperationRequest(OverCostQuery);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var uri = new Uri("http://localhost:5000/graphql");

        using var firstResponse = await client.PostAsync(request, uri, TestContext.Current.CancellationToken);
        await firstResponse.ReadAsResultAsync(TestContext.Current.CancellationToken);
        var hasFirstResult = analysisResults.TryDequeue(out var firstResult);

        // act
        using var secondResponse = await client.PostAsync(request, uri, TestContext.Current.CancellationToken);
        await secondResponse.ReadAsResultAsync(TestContext.Current.CancellationToken);
        var hasSecondResult = analysisResults.TryDequeue(out var secondResult);

        // assert
        var (operationPlanCache, costPlanCache) = await GetCachesAsync(gateway);
        Assert.True(hasFirstResult);
        Assert.True(hasSecondResult);
        Assert.Same(firstResult!.Plan, secondResult!.Plan);
        Assert.Equal(0, operationPlanCache.Count);
        Assert.Equal(1, costPlanCache.Count);
    }

    [Theory]
    [InlineData("application/graphql-response+json", HttpStatusCode.BadRequest)]
    [InlineData("application/json", HttpStatusCode.OK)]
    public async Task OverCostRequest_Should_ReturnExpectedStatus_When_AcceptIsSpecified(
        string accept,
        HttpStatusCode expectedStatus)
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            new Uri("http://localhost:5000/graphql"))
        {
            Content = new StringContent(
                """{ "query": "query OverCost { expensive { value } }" }""",
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        // act
        using var client = gateway.CreateClient();
        using var response = await client.SendAsync(
            request,
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(expectedStatus, response.StatusCode);
    }

    [Fact]
    public async Task OverCostRequest_Should_Execute_When_DefaultSecurityIsDisabled()
    {
        // arrange
        using var server = CreateSourceSchema("A", OverCostSchema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            disableDefaultSecurity: true);
        var request = new HotChocolate.Transport.OperationRequest(OverCostQuery);

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

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HotChocolate.AspNetCore;
using HotChocolate.Collections.Immutable;
using HotChocolate.CostAnalysis;
using HotChocolate.Fusion.Execution.CostAnalysis;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Verifies that GraphQL cost report and validate modes return per-result operation costs.
/// </summary>
public class CostReportingTests : FusionTestBase
{
    private const string CostHeader = "GraphQL-Cost";
    private const string ReportCost = "report";
    private const string ValidateCost = "validate";

    private const string Schema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query {
          items(n: Int!): [Item] @listSize(slicingArguments: ["n"])
          unannotatedItems: [Item]
          expensive: Expensive
        }

        type Item {
          value: Int @cost(weight: "5")
        }

        type Expensive @cost(weight: "2000") {
          value: String
        }
        """;

    private const string AcceptedBatchSchema =
        """
        directive @cost(weight: String!) on ARGUMENT_DEFINITION | ENUM | FIELD_DEFINITION | INPUT_FIELD_DEFINITION | OBJECT | SCALAR
        directive @listSize(assumedSize: Int, slicingArguments: [String!], sizedFields: [String!], requireOneSlicingArgument: Boolean = true) on FIELD_DEFINITION

        type Query {
          items(n: Int!): [Item] @listSize(slicingArguments: ["n"])
        }

        type Item {
          value: Int @cost(weight: "5")
        }
        """;

    private const string ItemsQuery =
        """
        query Items($n: Int!) {
          items(n: $n) {
            value
          }
        }
        """;

    private const string ExpensiveQuery =
        """
        query Expensive {
          expensive {
            value
          }
        }
        """;

    private static readonly Uri s_endpoint = new("http://localhost:5000/graphql");

    [Fact]
    public async Task Request_Should_AttachOperationCost_When_ReportHeaderIsSet()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(ItemsQuery, variables: new Dictionary<string, object?> { ["n"] = 3 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ReportCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results => AssertOperationCost(
                Assert.Single(results),
                """
                {
                  "fieldCost": 16,
                  "typeCost": 4
                }
                """));
    }

    [Fact]
    public async Task Request_Should_ReportStaticBound_When_ValidatedWithoutVariables()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(ItemsQuery);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ValidateCost),
            TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results => AssertOperationCost(
                Assert.Single(results),
                """
                {
                  "fieldCost": 6,
                  "typeCost": 2
                }
                """));
    }

    [Fact]
    public async Task Request_Should_ReportEvaluatedCost_When_ValidatedWithVariables()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(ItemsQuery, variables: new Dictionary<string, object?> { ["n"] = 3 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ValidateCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results => AssertOperationCost(
                Assert.Single(results),
                """
                {
                  "fieldCost": 16,
                  "typeCost": 4
                }
                """));
    }

    [Fact]
    public async Task RejectedRequest_Should_CarryOperationCost_When_ReportHeaderIsSet()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        var request = new OperationRequest(ExpensiveQuery);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ReportCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results => AssertOperationCost(
                Assert.Single(results),
                """
                {
                  "fieldCost": 1,
                  "typeCost": 2001
                }
                """));
    }

    [Fact]
    public async Task VariableBatch_Should_FailWhole_When_AnyResultExceedsLimit()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b =>
            {
                b.ModifyServerOptions(o => o.Batching = AllowedBatching.All);
                b.ModifyCostOptions(o => o.MaxFieldCost = double.PositiveInfinity);
            });
        var batch = new VariableBatchRequest(
            ItemsQuery,
            variables:
            [
                new Dictionary<string, object?> { ["n"] = 1 },
                new Dictionary<string, object?> { ["n"] = 1000 }
            ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            new GraphQLHttpRequest(batch, s_endpoint)
            {
                OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, ReportCost)
            },
            TestContext.Current.CancellationToken);

        // assert
        var results = new List<OperationResult>();
        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        results.MatchInlineSnapshots(
            [
                """
                {
                  "errors": [
                    {
                      "message": "The maximum allowed type cost was exceeded.",
                      "extensions": {
                        "code": "HC0047",
                        "typeCost": 2,
                        "maxTypeCost": 1000
                      }
                    }
                  ],
                  "extensions": {
                    "operationCost": {
                      "fieldCost": 6,
                      "typeCost": 2
                    }
                  }
                }
                """,
                """
                {
                  "errors": [
                    {
                      "message": "The maximum allowed type cost was exceeded.",
                      "extensions": {
                        "code": "HC0047",
                        "typeCost": 1001,
                        "maxTypeCost": 1000
                      }
                    }
                  ],
                  "extensions": {
                    "operationCost": {
                      "fieldCost": 5001,
                      "typeCost": 1001
                    }
                  }
                }
                """
            ]);

        foreach (var result in results)
        {
            result.Dispose();
        }

        Assert.Empty(gateway.Interactions);
    }

    [Fact]
    public async Task VariableBatch_Should_ReportEachAcceptedSet_When_ModeIsReport()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            b => b
                .AddDocumentFromString(AcceptedBatchSchema)
                .AddResolver(
                    "Query",
                    "items",
                    context => Enumerable
                        .Range(0, context.ArgumentValue<int>("n"))
                        .Select(_ => new AcceptedItem(123)))
                .AddResolver(
                    "Item",
                    "value",
                    context => context.Parent<AcceptedItem>().Value));
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.Batching = AllowedBatching.All));
        var batch = new VariableBatchRequest(
            ItemsQuery,
            variables:
            [
                new Dictionary<string, object?> { ["n"] = 1 },
                new Dictionary<string, object?> { ["n"] = 3 }
            ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            new GraphQLHttpRequest(batch, s_endpoint)
            {
                OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, ReportCost)
            },
            TestContext.Current.CancellationToken);
        var results = await ReadResultsAsync(response);

        // assert
        Assert.Equal(
            [1, 3],
            results.Select(r => r.Data.GetProperty("items").GetArrayLength()));
        results
            .Select(r => (object?)r.Extensions.GetProperty("operationCost"))
            .MatchInlineSnapshots(
            [
                """
                {
                  "fieldCost": 6,
                  "typeCost": 2
                }
                """,
                """
                {
                  "fieldCost": 16,
                  "typeCost": 4
                }
                """
            ]);
        DisposeResults(results);
    }

    [Fact]
    public async Task VariableBatch_Should_ReportWithoutExecuting_When_ModeIsValidate()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyServerOptions(o => o.Batching = AllowedBatching.All));
        var batch = new VariableBatchRequest(
            ItemsQuery,
            variables:
            [
                new Dictionary<string, object?> { ["n"] = 1 },
                new Dictionary<string, object?> { ["n"] = 1000 }
            ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            new GraphQLHttpRequest(batch, s_endpoint)
            {
                OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, ValidateCost)
            },
            TestContext.Current.CancellationToken);
        var results = await ReadResultsAsync(response);

        // assert
        Assert.Equal(HttpStatusCode.OK, response.HttpResponseMessage.StatusCode);
        Assert.Empty(gateway.Interactions);
        results.MatchInlineSnapshots(
            [
                """
                {
                  "extensions": {
                    "operationCost": {
                      "fieldCost": 6,
                      "typeCost": 2
                    }
                  }
                }
                """,
                """
                {
                  "extensions": {
                    "operationCost": {
                      "fieldCost": 5001,
                      "typeCost": 1001
                    }
                  }
                }
                """
            ]);
        DisposeResults(results);
    }

    [Fact]
    public async Task Request_Should_BeRejected_When_MaxResponseSizeIsExceeded()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyCostOptions(o =>
            {
                o.MaxFieldCost = double.PositiveInfinity;
                o.MaxTypeCost = double.PositiveInfinity;
                o.MaxResponseSize = 100;
            }));
        var request = new OperationRequest(ItemsQuery, variables: new Dictionary<string, object?> { ["n"] = 1000 });

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ReportCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results =>
            {
                var result = Assert.Single(results);
                var extensions = result.Errors[0].GetProperty("extensions");
                extensions.MatchInlineSnapshot(
                    """
                    {
                      "code": "HC0047",
                      "maxResponseSize": 1001,
                      "maxAllowedResponseSize": 100
                    }
                    """);
                AssertOperationCost(
                    result,
                    """
                    {
                      "fieldCost": 5001,
                      "typeCost": 1001,
                      "maxResponseSize": 1001
                    }
                    """);
            });
    }

    [Fact]
    public async Task UnannotatedList_Should_ReportInfiniteTypeCost_When_DefaultListSizeIsInfinite()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            // Overrides the FusionTestBase pin of 1 to exercise the product default.
            configureGatewayBuilder: b => b.ModifyCostOptions(o => o.DefaultListSize = double.PositiveInfinity));
        var request = new OperationRequest("{ unannotatedItems { value } }");

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ValidateCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results => AssertOperationCost(
                Assert.Single(results),
                """
                {
                  "fieldCost": "Infinity",
                  "typeCost": "Infinity"
                }
                """));
    }

    [Fact]
    public async Task RejectedInfiniteRequest_Should_ReportInfinityAsString()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            configureGatewayBuilder: b => b.ModifyCostOptions(
                o => o.DefaultListSize = double.PositiveInfinity));
        var request = new OperationRequest("{ unannotatedItems { value } }");

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var response = await client.SendAsync(
            WithCostHeader(request, ReportCost),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results =>
            {
                var result = Assert.Single(results);
                result.Errors[0].GetProperty("extensions").MatchInlineSnapshot(
                    """
                    {
                      "code": "HC0047",
                      "fieldCost": "Infinity",
                      "maxFieldCost": 1000
                    }
                    """);
                AssertOperationCost(
                    result,
                    """
                    {
                      "fieldCost": "Infinity",
                      "typeCost": "Infinity"
                    }
                    """);
            });
    }

    [Fact]
    public async Task ResponseStream_Should_AttachOperationCostToFirstResult_When_Reported()
    {
        // arrange
        var stream = new HotChocolate.Execution.ResponseStream(CreateStreamResults);
        var results = new List<HotChocolate.Execution.OperationResult>();

        // act
        var reported = Assert.IsType<HotChocolate.Execution.ResponseStream>(
            CostResultHelper.AddCost(stream, [new CostEstimate(2, 3, null)]));
        await foreach (var result in reported.ReadResultsAsync())
        {
            results.Add(result);
        }

        // assert
        results.MatchInlineSnapshots(
            [
                """
                {
                  "extensions": {
                    "item": 1,
                    "operationCost": {
                      "fieldCost": 2,
                      "typeCost": 3
                    }
                  }
                }
                """,
                """
                {
                  "extensions": {
                    "item": 2
                  }
                }
                """
            ]);

        foreach (var result in results)
        {
            await result.DisposeAsync();
        }
    }

    [Fact]
    public async Task RejectedRequest_Should_ReturnHttp400_When_AcceptIsGraphQLResponseJson()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        using var response = await SendRawAsync(gateway, "application/graphql-response+json");

        // assert
        response.MatchSnapshot();
    }

    [Fact]
    public async Task RejectedRequest_Should_ReturnHttp200_When_AcceptIsLegacyJson()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        using var response = await SendRawAsync(gateway, "application/json");

        // assert
        response.MatchSnapshot();
    }

    private sealed record AcceptedItem(int Value);

    private static GraphQLHttpRequest WithCostHeader(OperationRequest request, string mode)
        => new(request, s_endpoint)
        {
            OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, mode)
        };

    private static async Task<HttpResponseMessage> SendRawAsync(Gateway gateway, string accept)
    {
        const string requestBody =
            """
            {
                "query": "query Expensive { expensive { value } }"
            }
            """;

        using var client = gateway.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, s_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }

    private static async IAsyncEnumerable<HotChocolate.Execution.OperationResult> CreateStreamResults()
    {
        await Task.Yield();
        yield return new HotChocolate.Execution.OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("item", 1));
        yield return new HotChocolate.Execution.OperationResult(
            ImmutableOrderedDictionary<string, object?>.Empty.Add("item", 2));
    }

    private static void AssertOperationCost(OperationResult result, string snapshot)
        => result.Extensions.GetProperty("operationCost").MatchInlineSnapshot(snapshot);

    private static async Task<List<OperationResult>> ReadResultsAsync(GraphQLHttpResponse response)
    {
        var results = new List<OperationResult>();

        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            results.Add(result);
        }

        return results;
    }

    private static void DisposeResults(IEnumerable<OperationResult> results)
    {
        foreach (var result in results)
        {
            result.Dispose();
        }
    }
}

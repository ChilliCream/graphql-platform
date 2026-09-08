using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Verifies the <c>GraphQL-Cost</c> request header contract (hc-3-mmh.8/.10): report attaches
/// the evaluated cost to every response, validate probes the static bound or the evaluated cost
/// without executing, and the reported shape stays consistent whether a request is accepted,
/// rejected, or batched.
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

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

        // assert - extensions.operationCost { fieldCost, typeCost } is attached alongside data
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

        // assert - HTTP 200, no data, operationCost carries the worst-case static bound
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

        // assert - HTTP 200, no data, operationCost carries the evaluated cost for $n = 3
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

        // assert - the HC0047 error extensions and extensions.operationCost are both present
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
    public async Task VariableBatch_Should_FailWhole_When_AnyResultExceedsLimit()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
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

        // assert - every result carries HC0047 and its own operationCost; the n=1000 set fails the whole request
        var errorKinds = new List<JsonValueKind>();
        var hasCost = new List<bool>();
        var hasNoData = new List<bool>();
        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                errorKinds.Add(result.Errors.ValueKind);
                hasCost.Add(result.Extensions.TryGetProperty("operationCost", out _));
                hasNoData.Add(result.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null);
            }
        }

        Assert.Equal(2, errorKinds.Count);
        Assert.All(errorKinds, k => Assert.NotEqual(JsonValueKind.Undefined, k));
        Assert.All(hasNoData, Assert.True);
        Assert.All(hasCost, Assert.True);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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
        using var response = await client.PostAsync(request, s_endpoint, TestContext.Current.CancellationToken);

        // assert - HC0047 { maxResponseSize, maxAllowedResponseSize: 100 }
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            response,
            results =>
            {
                var extensions = Assert.Single(results).Errors[0].GetProperty("extensions");
                Assert.True(extensions.TryGetProperty("maxResponseSize", out _));
                Assert.Equal(100, extensions.GetProperty("maxAllowedResponseSize").GetDouble());
            });
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

        // assert - operationCost.typeCost is emitted as the JSON string "Infinity"
        await MatchSnapshotAsync(gateway, request, response);
    }

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
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

    [Fact(Skip = "enabled by fusion-report-modes-diagnostics")]
    public async Task RejectedRequest_Should_ReturnHttp200_When_AcceptIsLegacyJson()
    {
        // arrange
        using var server = CreateSourceSchema("A", Schema);
        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);

        // act
        using var response = await SendRawAsync(gateway, "application/json");

        // assert - same error body as the graphql-response+json variant, just a different status
        response.MatchSnapshot();
    }

    private static GraphQLHttpRequest WithCostHeader(OperationRequest request, string mode)
        => new(request, s_endpoint)
        {
            OnMessageCreated = (_, message, _) => message.Headers.Add(CostHeader, mode)
        };

    private static Task<HttpResponseMessage> SendRawAsync(Gateway gateway, string accept)
    {
        const string requestBody =
            """
            {
                "query": "query Expensive { expensive { value } }"
            }
            """;

        var request = new HttpRequestMessage(HttpMethod.Post, s_endpoint)
        {
            Content = new StringContent(requestBody, Encoding.UTF8, "application/json")
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(accept));

        return gateway.CreateClient().SendAsync(request);
    }
}

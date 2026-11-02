using System.Text;
using System.Text.Json;
using HotChocolate.Execution;
using HotChocolate.Features;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Transport.Http;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public class OperationPlanTelemetryTests : FusionTestBase
{
    [Fact]
    public async Task Trace_VariableSets_Should_Report_Own_Paths_When_Dependent_Lookup_Builds_Deeper_Paths()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                author: Author!
            }

            type Author @key(fields: "id") {
                id: ID!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
                authorById(id: ID!): Author @lookup
            }

            type Author @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1),
                ("B", server2),
                ("C", server3)
            ],
            configureGatewayBuilder: b =>
                b.ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                products {
                    id
                    reviews {
                        id
                        author {
                            id
                            name
                        }
                    }
                }
            }
            """);

        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var result = await response.ReadAsResultAsync(TestContext.Current.CancellationToken);

        // assert
        // The reviews lookup builds one variable set per product. The author lookup runs
        // afterwards and builds its own, deeper requirement paths from the reviews lookup's
        // merged results. The reviews lookup's trace must still report its own target paths
        // and must not alias the author lookup's paths.
        Assert.Equal(JsonValueKind.Undefined, result.Errors.ValueKind);

        var operationPlan = result.Extensions
            .GetProperty("fusion")
            .GetProperty("operationPlan");

        var reviewsNodePaths = GetVariableSetPaths(operationPlan, schemaName: "B");
        var authorNodePaths = GetVariableSetPaths(operationPlan, schemaName: "C");

        Assert.Equal(["products[0]", "products[1]", "products[2]"], reviewsNodePaths);
        Assert.All(
            authorNodePaths,
            path => Assert.Matches(@"^products\[\d+\]\.reviews\[\d+\]\.author$", path));
    }

    [Fact]
    public async Task Trace_VariableSets_Should_KeepValues_When_ResultIsReadAfterExecution()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", server1),
                ("B", server2)
            ],
            configureGatewayBuilder: b =>
                b.ModifyRequestOptions(o => o.CollectOperationPlanTelemetry = true));

        var executor = await gateway.Services.GetRequestExecutorAsync(
            cancellationToken: TestContext.Current.CancellationToken);

        // act
        await using var result = (await executor.ExecuteAsync(
            OperationRequestBuilder.New().SetDocument("{ products { id name } }").Build(),
            TestContext.Current.CancellationToken)).ExpectOperationResult();

        // assert
        result.Features.GetRequired<OperationPlanTrace>().Nodes.Values
            .SelectMany(node => node.VariableSets)
            .Select(variableSet => Encoding.UTF8.GetString(variableSet.Values.AsSequence()))
            .Order(StringComparer.Ordinal)
            .MatchInlineSnapshots(
            [
                """{"__fusion_1_id":"UHJvZHVjdDoxOTc0MTQ1MDk="}""",
                """{"__fusion_1_id":"UHJvZHVjdDoyMjE1MjQ2OTE2"}""",
                """{"__fusion_1_id":"UHJvZHVjdDozOTM1NzEyMDU5"}"""
            ]);
    }

    private static List<string> GetVariableSetPaths(JsonElement operationPlan, string schemaName)
    {
        foreach (var node in operationPlan.GetProperty("nodes").EnumerateArray())
        {
            if (node.TryGetProperty("schema", out var schema)
                && schema.GetString() == schemaName
                && node.TryGetProperty("variableSets", out var variableSets))
            {
                return variableSets
                    .EnumerateObject()
                    .Select(property => property.Name)
                    .Order(StringComparer.Ordinal)
                    .ToList();
            }
        }

        throw new InvalidOperationException(
            $"Expected the operation plan to contain a node for schema '{schemaName}' "
            + "with variable sets in its trace.");
    }
}

using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// Repro for customer report: HC0011 "Invalid number, expected digit but got: `c`" for a Relay
// connection query, the signature of an offset/memory corruption in the serialized subgraph
// operation, layout/field-order sensitive and non-deterministic.
//
// This variant targets the Relay `node(id:): Node @lookup @shareable` fan-out path
// (OperationPlanner.PlanNode -> NodeFieldPlanStep), whose `Branches` collection was changed from
// ImmutableDictionary to ImmutableOrderedDictionary in the "Planner Stabilizations" commit
// (non-deterministic -> deterministic iteration).
//
// The Organization entity is split across two subgraphs and has NO dedicated by-id lookup, so the
// only way to resolve a cross-subgraph field (`Number` from B) for an Organization is the shared
// `node`/`nodes` fan-out. Both a direct `node(id:)` query (which definitively creates a
// NodeFieldPlanStep with multiple branches) and the customer's connection query are exercised.
//
// Each iteration freshly plans/executes, passes `$cursor` (null and a value), and re-parses every
// captured subgraph request body with Utf8GraphQLParser. Any unparseable subgraph request, any
// corrupted `first: 100`, or any gateway error fails the test.
public class OrganizationsNodeFanoutReproTests : FusionTestBase
{
    private const int Iterations = 30;

    // Subgraph A owns the connection and Organization.displayName. It exposes ONLY the shared node
    // lookup (no organizationById), so cross-schema resolution must go through node/nodes.
    private const string SubgraphA =
        """
        interface Node {
          id: ID!
        }

        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
          organizations(first: Int, after: String): OrganizationConnection
        }

        type OrganizationConnection {
          pageInfo: PageInfo!
          edges: [OrganizationEdge!]
        }

        type OrganizationEdge {
          cursor: String!
          node: Organization!
        }

        type PageInfo @shareable {
          hasNextPage: Boolean!
          hasPreviousPage: Boolean!
          startCursor: String
          endCursor: String
        }

        type Organization implements Node {
          id: ID!
          displayName: String!
        }
        """;

    // Subgraph B owns Organization.Number. It exposes ONLY the shared node lookup (no
    // organizationById), so resolving Number for an Organization from A must fan out through node.
    private const string SubgraphB =
        """
        interface Node {
          id: ID!
        }

        type Query {
          node(id: ID!): Node @lookup @shareable
          nodes(ids: [ID!]!): [Node]! @shareable
        }

        type Organization implements Node {
          id: ID!
          Number: Int!
        }
        """;

    // Exact customer connection query.
    private const string ConnectionQuery =
        """
        query Organizations($cursor: String) {
          organizations(first: 100, after: $cursor) {
            pageInfo {
              hasNextPage
              endCursor
              __typename
            }
            edges {
              node {
                ...Organization
                __typename
              }
              __typename
            }
            __typename
          }
        }
        fragment Organization on Organization {
          id
          Number
          displayName
          __typename
        }
        """;

    // Direct node(id:) query selecting the split Organization. This is the canonical trigger for
    // OperationPlanner.PlanNode -> NodeFieldPlanStep with multiple branches. The id is an inline
    // literal (a String variable is not assignable to the ID! node argument).
    private const string NodeQuery =
        """
        query OrganizationNode {
          node(id: "T3JnYW5pemF0aW9uOjE=") {
            ... on Organization {
              id
              Number
              displayName
              __typename
            }
            __typename
          }
        }
        """;

    [Fact]
    public async Task Connection_NodeFanout_Cursor_Null()
        => await RunReproLoopAsync(ConnectionQuery, cursor: null, expectNodeFanout: true);

    [Fact]
    public async Task Connection_NodeFanout_Cursor_NonNull()
        => await RunReproLoopAsync(ConnectionQuery, cursor: "abc", expectNodeFanout: true);

    [Fact]
    public async Task Direct_Node_Fanout()
        => await RunReproLoopAsync(NodeQuery, cursor: null, expectNodeFanout: true);

    private async Task RunReproLoopAsync(string operationDocument, string? cursor, bool expectNodeFanout)
    {
        var failures = new List<string>();
        var sawNodeFanout = false;
        var sawNumberFromB = false;

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            // arrange
            // fresh subgraphs + gateway every iteration so the planner re-plans.
            using var serverA = CreateSourceSchema("A", SubgraphA);
            using var serverB = CreateSourceSchema("B", SubgraphB);

            using var gateway = await CreateCompositeSchemaAsync(
            [
                ("A", serverA),
                ("B", serverB)
            ]);

            using var client = GraphQLHttpClient.Create(gateway.CreateClient());

            var request = new OperationRequest(
                operationDocument,
                variables: new Dictionary<string, object?>
                {
                    ["cursor"] = cursor
                });

            // act
            using var response = await client.PostAsync(
                request,
                new Uri("http://localhost:5000/graphql"));

            using var result = await response.ReadAsResultAsync();

            // assert
            // 1. the gateway result must not contain errors (HC0011 would show up here).
            if (result.Errors.ValueKind is JsonValueKind.Array
                && result.Errors.GetArrayLength() > 0)
            {
                failures.Add(
                    $"iteration {iteration} (cursor: {Describe(cursor)}): gateway returned errors: "
                    + result.Errors.GetRawText());
            }

            // 2. every captured subgraph request body must re-parse cleanly.
            //    a corrupted `first: 100` (or any mangled literal) surfaces as a SyntaxException here.
            foreach (var (schemaName, schemaInteractions) in gateway.Interactions)
            {
                foreach (var interaction in schemaInteractions.Values)
                {
                    if (interaction.Request is not { } rawRequest)
                    {
                        continue;
                    }

                    rawRequest.Body.Position = 0;
                    using var json = JsonDocument.Parse(rawRequest.Body);

                    foreach (var query in EnumerateQueries(json.RootElement))
                    {
                        // node/nodes appearing in a subgraph request proves the node fan-out path.
                        if (ContainsNodeField(query))
                        {
                            sawNodeFanout = true;
                        }

                        if (schemaName == "B" && query.Contains("Number"))
                        {
                            sawNumberFromB = true;
                        }

                        try
                        {
                            _ = Utf8GraphQLParser.Parse(query);
                        }
                        catch (Exception ex)
                        {
                            failures.Add(
                                $"iteration {iteration} (cursor: {Describe(cursor)}): subgraph '{schemaName}' "
                                + $"request did not re-parse: {ex.Message}\n----\n{query}\n----");
                        }
                    }
                }
            }
        }

        if (expectNodeFanout)
        {
            // Prove the node(id:)/nodes fan-out was actually exercised, otherwise a clean run proves nothing.
            Assert.True(
                sawNodeFanout,
                "Expected at least one subgraph request to use the Relay `node`/`nodes` field (proving the "
                + "NodeFieldPlanStep fan-out path was exercised), but none were captured.");

            Assert.True(
                sawNumberFromB,
                "Expected at least one fan-out request to subgraph 'B' resolving `Number` (proving the split "
                + "entity was resolved cross-schema via node), but none were captured.");
        }

        if (failures.Count > 0)
        {
            Assert.Fail(
                $"Reproduced corrupted/failing subgraph requests in {failures.Count} of {Iterations} iterations:\n\n"
                + string.Join("\n\n", failures));
        }
    }

    private static bool ContainsNodeField(string query)
    {
        // crude but sufficient: the fan-out emits `node(id:` or `nodes(ids:` at the root.
        return query.Contains("node(id:")
            || query.Contains("nodes(ids:")
            || query.Contains("node(") && query.Contains("on Organization");
    }

    private static IEnumerable<string> EnumerateQueries(JsonElement root)
    {
        if (root.ValueKind is JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.TryGetProperty("query", out var batchQuery)
                    && batchQuery.ValueKind is JsonValueKind.String)
                {
                    yield return batchQuery.GetString()!;
                }
            }

            yield break;
        }

        if (root.ValueKind is JsonValueKind.Object
            && root.TryGetProperty("query", out var query)
            && query.ValueKind is JsonValueKind.String)
        {
            yield return query.GetString()!;
        }
    }

    private static string Describe(string? cursor)
        => cursor is null ? "null" : $"\"{cursor}\"";
}

using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// Repro for customer report: a Relay connection query (`organizations(first: 100, after: $cursor)`)
// against a Fusion v16 gateway intermittently produces a corrupted subgraph request, surfacing as
// HC0011 "Invalid number, expected digit but got: `c`" (the bad byte is borrowed from elsewhere in
// the document, e.g. "cursor"/"endCursor"/"__typename"). It is the signature of an offset/memory
// corruption in the serialized subgraph operation and was reported to be layout/field-order
// sensitive and non-deterministic.
//
// Unlike OrganizationsConnectionReproTests (single passthrough subgraph, exactly one operation),
// this test SPLITS the Organization entity across two subgraphs so that requesting
// `node { id Number displayName }` forces a batched entity lookup to subgraph B for every edge.
// Those structurally-identical lookups exercise the operation MERGING/BATCHING path
// (MergeStructurallyIdenticalOperations) that the single-subgraph repro never touches.
//
// Because the suspected corruption was iteration-order dependent, we build a fresh gateway/plan
// every iteration, run a loop, pass both `cursor: null` and `cursor: "abc"`, and re-parse EVERY
// captured subgraph request body with Utf8GraphQLParser. Any unparseable subgraph request (or any
// gateway error) fails the test.
public class OrganizationsConnectionMergeReproTests : FusionTestBase
{
    private const int Iterations = 25;

    private const string SubgraphA =
        """
        interface Node {
          id: ID!
        }

        type Query {
          node(id: ID!): Node @lookup @shareable
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

    private const string SubgraphB =
        """
        interface Node {
          id: ID!
        }

        type Query {
          node(id: ID!): Node @lookup @shareable
          organizationByIdFromB(id: ID!): Organization @lookup
        }

        type Organization implements Node {
          id: ID!
          Number: Int!
        }
        """;

    // Exact customer query.
    private const string CustomerQuery =
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

    // Customer-reported layout sensitivity: reordering `id` and `Number` "makes it work".
    // Exercise the reversed order too, in case the corruption only manifested for one layout.
    private const string CustomerQueryReorderedFragment =
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
          Number
          id
          displayName
          __typename
        }
        """;

    // Customer note: removing __typename from pageInfo flips the bad char to '_'.
    private const string CustomerQueryNoPageInfoTypename =
        """
        query Organizations($cursor: String) {
          organizations(first: 100, after: $cursor) {
            pageInfo {
              hasNextPage
              endCursor
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

    [Fact]
    public async Task Organizations_Connection_Split_Entity_Cursor_Null()
        => await RunReproLoopAsync(CustomerQuery, cursor: null);

    [Fact]
    public async Task Organizations_Connection_Split_Entity_Cursor_NonNull()
        => await RunReproLoopAsync(CustomerQuery, cursor: "abc");

    [Fact]
    public async Task Organizations_Connection_Split_Entity_Reordered_Fragment()
        => await RunReproLoopAsync(CustomerQueryReorderedFragment, cursor: "abc");

    [Fact]
    public async Task Organizations_Connection_Split_Entity_No_PageInfo_Typename()
        => await RunReproLoopAsync(CustomerQueryNoPageInfoTypename, cursor: null);

    private async Task RunReproLoopAsync(string operationDocument, string? cursor)
    {
        var failures = new List<string>();
        var sawMergedLookups = false;

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            // arrange
            // fresh subgraphs + gateway every iteration so the planner re-plans
            // (the suspected corruption was iteration-order dependent).
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
            var lookupRequestCount = 0;

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
                        if (schemaName == "B" && query.Contains("Number"))
                        {
                            lookupRequestCount++;
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

            // a single batched lookup operation to B that resolves several entities (one merged
            // operation, multiple variable sets) still counts as exercising the merge/batch path.
            if (lookupRequestCount > 0)
            {
                sawMergedLookups = true;
            }
        }

        // The merge/batch path must actually have been exercised, otherwise a clean run proves nothing.
        Assert.True(
            sawMergedLookups,
            "Expected at least one entity lookup to subgraph 'B' (proving the split-entity merge/batch "
            + "path was exercised), but none were captured. The composition or query is not forcing lookups.");

        if (failures.Count > 0)
        {
            Assert.Fail(
                $"Reproduced corrupted/failing subgraph requests in {failures.Count} of {Iterations} iterations:\n\n"
                + string.Join("\n\n", failures));
        }
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

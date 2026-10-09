using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// Mirrors the interface-object-with-requires federation-gateway-audit suite natively.
// Source schema A owns the NodeWithName interface, its User implementation (with the
// concrete-only "age" field), and the covering interface lookup. Source schema B is an
// @interfaceObject stand-in whose projected "username" field @requires the "name" field that
// A owns; "name" is declared @external on the stand-in. Values produced by B's root list are
// opaque: their concrete identity and A-owned fields are recovered through A's covering lookup.
public class InterfaceObjectRequiresTests : FusionTestBase
{
    private const string SchemaA =
        """
        type Query {
          users: [NodeWithName!]!
          nodeById(id: ID!): NodeWithName @lookup
        }

        interface NodeWithName {
          id: ID!
          name: String!
        }

        type User implements NodeWithName @key(fields: "id") {
          id: ID!
          name: String!
          age: Int!
        }
        """;

    private const string SchemaB =
        """
        type Query {
          anotherUsers: [NodeWithName!]!
          nodeByKey(id: ID!): NodeWithName @lookup @internal
        }

        type NodeWithName @interfaceObject @key(fields: "id") {
          id: ID!
          name: String! @external
          username(name: String @require(field: "name")): String!
        }
        """;

    private const string SchemaBWithProjectedScore =
        SchemaB
        + """

        extend type NodeWithName {
          score: Int!
        }
        """;

    [Fact]
    public async Task AnotherUsers_SelectsUserAge()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverB = CreateSourceSchema("B", SchemaB);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              anotherUsers {
                ... on User { age }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task AnotherUsers_SelectsRequiresUsername()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverB = CreateSourceSchema("B", SchemaB);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              anotherUsers {
                id
                name
                username
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // This deliberately uses an argument-free projected field. Preserving @requires metadata on a
    // projected concrete field is a separate concern from routing the concrete fragment back to the
    // stand-in without sending the unavailable User type condition.
    [Fact]
    public async Task AnotherUsers_SelectsConcreteFragmentStandInField()
    {
        // arrange
        using var serverA = CreateSourceSchema("A", SchemaA);
        using var serverB = CreateSourceSchema("B", SchemaBWithProjectedScore);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              anotherUsers {
                ... on User {
                  age
                  id
                  name
                  score
                }
                id
                name
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Collection(
                    response.Data.GetProperty("anotherUsers").EnumerateArray(),
                    user => AssertUserWithScore(user, "Tm9kZVdpdGhOYW1lOjE="),
                    user => AssertUserWithScore(user, "Tm9kZVdpdGhOYW1lOjI="),
                    user => AssertUserWithScore(user, "Tm9kZVdpdGhOYW1lOjM="));
            });
    }

    private static void AssertUserWithScore(JsonElement user, string id)
    {
        Assert.Equal(4, user.EnumerateObject().Count());
        Assert.Equal(123, user.GetProperty("age").GetInt32());
        Assert.Equal(id, user.GetProperty("id").GetString());
        Assert.Equal($"User: {id}", user.GetProperty("name").GetString());
        Assert.Equal(123, user.GetProperty("score").GetInt32());
    }
}

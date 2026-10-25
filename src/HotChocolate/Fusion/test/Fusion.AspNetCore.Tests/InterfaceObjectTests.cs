using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class InterfaceObjectTests : FusionTestBase
{
    // Source schema A defines the Media interface, its implementing types, and the covering
    // interface lookup. Source schema B is an @interfaceObject stand-in contributing "views" and
    // a list entry point. Values produced by B are opaque: their concrete identity is recovered
    // through A's covering lookup only when the operation observes it.
    private const string SchemaA =
        """
        type Query {
          mediaById(id: ID!): Media @lookup
        }

        interface Media {
          id: ID!
          title: String!
        }

        type Book implements Media @key(fields: "id") {
          id: ID!
          title: String!
          isbn: String!
        }

        type Movie implements Media @key(fields: "id") {
          id: ID!
          title: String!
          runtime: Int!
        }
        """;

    private const string SchemaB =
        """
        type Query {
          trendingMedia: [Media!]!
          mediaByKey(id: ID!): Media @lookup @internal
        }

        type Media @interfaceObject @key(fields: "id") {
          id: ID!
          views: Int!
        }
        """;

    [Fact]
    public async Task Default_Field_Inheritance_Resolves_From_StandIn_Only()
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
        // The client selects only interface-declared fields the stand-in owns, so identity is
        // never observed; the covering lookup is skipped and only schema B is fetched.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                id
                views
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
    public async Task Opaque_TypeName_Is_Recovered_Through_Covering_Lookup()
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
        // Selecting __typename observes identity, so the opaque element is upgraded to its concrete
        // type through A's covering lookup; __typename resolves to Book/Movie, never Media.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                __typename
                id
                views
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
    public async Task Id_Only_Selection_Is_A_Single_Fetch()
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
        // Only the id is selected, so identity is never observed; the covering lookup is skipped
        // and the value is served entirely from the stand-in.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                id
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
    public async Task Fragment_Narrowing_Resolves_Concrete_Fields_Through_Covering_Lookup()
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
        // Type-conditioned selections observe identity, so the covering lookup keyed by the
        // stand-in's id resolves the implementing-type fields and the concrete __typename.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                __typename
                id
                views
                ... on Book { isbn }
                ... on Movie { runtime }
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
    public async Task Fragment_Narrowing_Resolves_ConcreteOwned_Field_From_Opaque_Root()
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
        // The concrete fragment selects only "isbn", a field owned by the concrete-aware schema A,
        // over an opaque value produced by B. Identity and the field are both recovered through A's
        // covering lookup, with no interface-level sibling to anchor the fetch.
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                ... on Book { isbn }
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
    public async Task Fragment_Narrowing_Resolves_StandIn_Field_Through_Covering_Lookup()
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
        // The concrete fragment selects "views", a field the stand-in schema B owns. Identity is
        // recovered through A's covering lookup, then the projected field is fetched back from B
        // through its covering interface lookup keyed by the stand-in's id (a three-hop plan).
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              trendingMedia {
                ... on Book { views }
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
                    response.Data.GetProperty("trendingMedia").EnumerateArray(),
                    media => Assert.Equal(123, media.GetProperty("views").GetInt32()),
                    media => Assert.Equal(123, media.GetProperty("views").GetInt32()),
                    media => Assert.Equal(123, media.GetProperty("views").GetInt32()));
            });
    }
}

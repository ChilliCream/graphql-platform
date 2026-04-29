using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class ProvidesTests : FusionTestBase
{
    [Fact]
    public async Task Provides_Simple_Covers_Selection()
    {
        // arrange
        using var serverReviews = CreateSourceSchema(
            "reviews",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              reviews: [Review]
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
            }
            """);

        using var serverUsers = CreateSourceSchema(
            "users",
            """
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("reviews", serverReviews),
            ("users", serverUsers)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                author {
                  username
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        Assert.False(gateway.Interactions.ContainsKey("users"));
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Provides_Partial_Covers_Some()
    {
        // arrange
        using var serverReviews = CreateSourceSchema(
            "reviews",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              reviews: [Review]
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
            }
            """);

        using var serverUsers = CreateSourceSchema(
            "users",
            """
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              email: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("reviews", serverReviews),
            ("users", serverUsers)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                author {
                  username
                  email
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Provides_On_Interface()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              book: Book
            }

            type Book @key(fields: "id") {
              id: ID!
              featured: Animal @provides(fields: "... on Cat { age } ... on Dog { tricks }")
            }

            interface Animal {
              id: ID!
            }

            type Cat implements Animal @key(fields: "id") {
              id: ID!
              age: Int @external
            }

            type Dog implements Animal @key(fields: "id") {
              id: ID!
              tricks: [String] @external
            }
            """);

        using var serverB = CreateSourceSchema(
            "b",
            """
            schema {
              query: Query
            }

            type Query {
              catById(id: ID! @is(field: "id")): Cat @lookup @internal
              dogById(id: ID! @is(field: "id")): Dog @lookup @internal
            }

            interface Animal {
              id: ID!
            }

            type Cat implements Animal @key(fields: "id") {
              id: ID!
              age: Int
            }

            type Dog implements Animal @key(fields: "id") {
              id: ID!
              tricks: [String]
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", serverA),
            ("b", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              book {
                featured {
                  ... on Cat {
                    age
                  }
                  ... on Dog {
                    tricks
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    // note: The composite-schemas spec forbids @provides on fields that return a
    // union type (ProvidesOnNonCompositeFieldRule). The planner-level
    // Provides_On_Union scenario was dropped in Step 3 for the same reason; the
    // integration counterpart is unreachable and intentionally absent here.

    [Fact]
    public async Task Provides_External_Without_Cover()
    {
        // arrange
        using var serverReviews = CreateSourceSchema(
            "reviews",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              reviews: [Review]
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type Review @key(fields: "id") {
              id: ID!
              author: User @provides(fields: "username")
            }

            type User @key(fields: "id") {
              id: ID!
              username: String @external
            }
            """);

        using var serverUsers = CreateSourceSchema(
            "users",
            """
            schema {
              query: Query
            }

            type Query {
              userById(id: ID! @is(field: "id")): User @lookup @internal
            }

            type User @key(fields: "id") {
              id: ID!
              username: String
              email: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("reviews", serverReviews),
            ("users", serverUsers)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                author {
                  email
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Provides_Deeply_Nested_Chain()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "a",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              root: Root
            }

            type Root {
              level1: Level1 @provides(fields: "level2 { level3 { value } }")
            }

            type Level1 @key(fields: "id") {
              id: ID!
              level2: Level2 @external
            }

            type Level2 @key(fields: "id") {
              id: ID!
              level3: Level3 @external
            }

            type Level3 @key(fields: "id") {
              id: ID!
              value: String @external
            }
            """);

        using var serverB = CreateSourceSchema(
            "b",
            """
            schema {
              query: Query
            }

            type Query {
              level1ById(id: ID! @is(field: "id")): Level1 @lookup @internal
              level2ById(id: ID! @is(field: "id")): Level2 @lookup @internal
              level3ById(id: ID! @is(field: "id")): Level3 @lookup @internal
            }

            type Level1 @key(fields: "id") {
              id: ID!
              level2: Level2
            }

            type Level2 @key(fields: "id") {
              id: ID!
              level3: Level3
            }

            type Level3 @key(fields: "id") {
              id: ID!
              value: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", serverA),
            ("b", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              root {
                level1 {
                  level2 {
                    level3 {
                      value
                    }
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Planner_Should_Route_To_Owning_Source_When_Local_Field_Is_Orphan_External()
    {
        // arrange
        // The query enters the 'reviews' source (which owns Query.reviews). 'reviews'
        // also declares Product.name, but as @external with no @provides on the query
        // path referencing it. The off-path productByName root field exists only to
        // satisfy the composite-schemas-spec ExternalUnusedRule. The planner must not
        // trust 'reviews' for 'name' and must route it to 'products' via productById.
        using var serverReviews = CreateSourceSchema(
            "reviews",
            """
            directive @external on FIELD_DEFINITION

            schema {
              query: Query
            }

            type Query {
              reviews: [Review]
              productByName(name: String!): Product @provides(fields: "name")
            }

            type Review @key(fields: "id") {
              id: ID!
              body: String
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String @external
            }
            """);

        using var serverProducts = CreateSourceSchema(
            "products",
            """
            schema {
              query: Query
            }

            type Query {
              productById(id: ID! @is(field: "id")): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("reviews", serverReviews),
            ("products", serverProducts)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              reviews {
                product {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        // Every interaction with the 'reviews' source must fetch product { id } only,
        // never 'name'. A 'name' fetch from 'reviews' would mean the planner wrongly
        // trusted the orphan @external declaration. Every interaction with the
        // 'products' source must be a productById lookup that returns 'name'.
        var reviewsInteractions = gateway.Interactions.GetValueOrDefault("reviews");
        Assert.NotNull(reviewsInteractions);
        foreach (var interaction in reviewsInteractions!.Values)
        {
            Assert.NotNull(interaction.Request);
            interaction.Request!.Body.Position = 0;
            using var body = JsonDocument.Parse(interaction.Request.Body);
            var query = body.RootElement.GetProperty("query").GetString()!;
            Assert.DoesNotContain("name", query);
            Assert.Contains("product", query);
            interaction.Request.Body.Position = 0;
        }

        var productsInteractions = gateway.Interactions.GetValueOrDefault("products");
        Assert.NotNull(productsInteractions);
        foreach (var interaction in productsInteractions!.Values)
        {
            Assert.NotNull(interaction.Request);
            interaction.Request!.Body.Position = 0;
            using var body = JsonDocument.Parse(interaction.Request.Body);
            var query = body.RootElement.GetProperty("query").GetString()!;
            Assert.Contains("productById", query);
            Assert.Contains("name", query);
            interaction.Request.Body.Position = 0;
        }

        await MatchSnapshotAsync(gateway, request, result);
    }
}

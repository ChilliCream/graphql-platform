using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class ConditionalTests : FusionTestBase
{
    [Fact]
    public async Task Root_Skip_On_All_Selections()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              productBySlug(slug: "product") @skip(if: $skip) {
                name
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Root_All_Selections_Statically_Skipped()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery {
              productBySlug(slug: "product") @skip(if: true) {
                name
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
    public async Task Root_Selection_Twice_Only_Skipped_Once()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              productBySlug(slug: "product") {
                name
              }
              productBySlug(slug: "product") @skip(if: $skip) {
                price
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Root_Selection_Twice_Only_Skipped_Once_Identical_Selection_Sets()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($slug: String!, $skip: Boolean!) {
              productBySlug(slug: $slug) @skip(if: $skip) {
                name
              }
              productBySlug(slug: $slug) {
                name
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["slug"] = "product", ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #region node field

    [Fact]
    public async Task NodeField_Skip_On_NodeField()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") @skip(if: $skip) {
                __typename
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_Around_NodeField()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              ... @skip(if: $skip) {
                # Discussion:1
                node(id: "RGlzY3Vzc2lvbjox") {
                  __typename
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Shared_Selection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Author implements Node {
              id: ID!
              username: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                id @skip(if: $skip)
                ... on Discussion {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_Around_Shared_Selections()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Author implements Node {
              id: ID!
              username: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... @skip(if: $skip) {
                  id
                }
                ... on Discussion {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Selection_In_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                __typename
                ... on Discussion {
                  title @skip(if: $skip)
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                __typename
                ... on Discussion @skip(if: $skip) {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_Around_Multiple_Type_Refinements()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            type Author implements Node {
              id: ID!
              username: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... @skip(if: $skip) {
                  ... on Discussion {
                    title
                  }
                  ... on Author {
                    username
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Interface_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              title: String
              viewerCanVote: Boolean!
            }

            type Author implements Node & Votable {
              id: ID!
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Votable @skip(if: $skip) {
                  viewerCanVote
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_Around_Interface_Type_Refinement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Votable {
              viewerCanVote: Boolean!
            }

            type Discussion implements Node & Votable {
              id: ID!
              title: String
              viewerCanVote: Boolean!
            }

            type Author implements Node & Votable {
              id: ID!
              username: String
              viewerCanVote: Boolean!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... @skip(if: $skip) {
                  ... on Votable  {
                    viewerCanVote
                  }
                  __typename
                }
                ... on Discussion {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Interface_Selection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Node & Authorable {
              id: ID!
              title: String
              author: Author
            }

            type Author implements Node {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              username: String
              rating: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Authorable    {
                  author @skip(if: $skip) {
                    username
                  }
                }
                ... on Discussion {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task NodeField_Skip_On_Interface_Selection_Type_Refinement_With_Same_Unskipped_Selection()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            interface Node {
              id: ID!
            }

            interface Authorable {
              author: Author
            }

            type Discussion implements Node & Authorable {
              id: ID!
              author: Author
            }

            type Author implements Node {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              username: String
              rating: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Authorable {
                  author @skip(if: $skip) {
                    username
                  }
                }
                ... on Discussion {
                  author {
                    rating
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion
}

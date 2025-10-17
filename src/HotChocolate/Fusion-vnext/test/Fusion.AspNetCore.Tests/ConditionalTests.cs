using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class ConditionalTests : FusionTestBase
{
    #region Shared Path

    [Fact]
    public async Task SharedPath_Skip_On_Entry_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldA: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldB: String!
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
              viewer @skip(if: $skip) {
                fieldA
                fieldB
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

    [Fact(Skip = "Planning is just broken here")]
    public async Task SharedPath_Multiple_Skip_Levels_Around_Entry_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldA: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldB: String!
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              ... @skip(if: $skip1) {
                ... @skip(if: $skip2) {
                  viewer  {
                    fieldA
                    fieldB
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task SharedPath_Skip_On_Field_Below_Entry_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldA: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldB: String!
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
              viewer  {
                fieldA
                fieldB @skip(if: $skip)
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

    [Fact(Skip = "Conditions are not properly forwarded to B")]
    public async Task SharedPath_Multiple_Skip_Levels_Around_Fields_Below_Entry_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldA: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              fieldB: String!
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              viewer {
                ... @skip(if: $skip1) {
                  ... @skip(if: $skip2) {
                    fieldA
                    fieldB
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region Root

    [Fact]
    public async Task Root_Skip_On_Field()
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

    [Fact(Skip = "Does not yet work correctly")]
    public async Task Root_Field_Statically_Skipped()
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
    public async Task Root_Skip_Around_Fields_Of_Same_Source_Schema()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
              viewer: Viewer
            }

            type Product {
              id: ID!
              name: String!
            }

            type Viewer {
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
              ... @skip(if: $skip) {
                productBySlug(slug: "product") {
                  name
                }
                viewer {
                  name
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

    [Fact(Skip = "Conditionals not properly forwarded to B")]
    public async Task Root_Skip_Around_Fields_From_Different_Source_Schemas()
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

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
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
              ... @skip(if: $skip) {
                productBySlug(slug: "product") {
                  name
                }
                viewer {
                  name
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
    public async Task Root_Skip_Only_On_Some_Fields_Of_Same_Source_Schema()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
              viewer: Viewer
            }

            type Product {
              id: ID!
              name: String!
            }

            type Viewer {
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
              viewer {
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
    public async Task Root_Skip_Only_On_Some_Fields_From_Different_Source_Schemas()
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

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
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
              productBySlug(slug: "product") @skip(if: $skip) {
                name
              }
              viewer {
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

    [Fact(Skip = "Conditions not properly forwarded to B")]
    public async Task Root_Multiple_Skip_Levels_Around_Fields_From_Different_Source_Schemas()
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

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              ... @skip(if: $skip1) {
                ... @skip(if: $skip2) {
                  productBySlug(slug: "product") {
                    name
                  }
                  viewer {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    #endregion

    #region Lookup

    [Fact]
    public async Task Lookup_Skip_On_Parent_Field_Of_Field_Fetched_Through_Lookup()
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
              review: Review
            }

            type Review {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
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
              productBySlug(slug: "product") {
                name
                review @skip(if: $skip) {
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
    public async Task Lookup_Skip_Around_Fields_Fetched_Through_Lookup()
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
              review: Review
            }

            type Review {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
              rating: Int!
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
              productBySlug(slug: "product") {
                name
                review {
                  ... @skip(if: $skip) {
                    title
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

    [Fact]
    public async Task Lookup_Multiple_Skip_Levels_Around_Fields_Fetched_Through_Lookup()
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
              review: Review
            }

            type Review {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
              rating: Int!
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "product") {
                name
                review {
                  ... @skip(if: $skip1) {
                    ... @skip(if: $skip2) {
                      title
                      rating
                    }
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Lookup_Multiple_Skip_Levels_Around_Fields_Fetched_Through_Lookup_And_On_Same_Source_Schema()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productBySlug(slug: String!): Product
              reviewById(id: ID!): Review @lookup
            }

            type Product {
              id: ID!
              name: String!
              review: Review
            }

            type Review {
              id: ID!
              author: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
              rating: Int!
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              productBySlug(slug: "product") {
                name
                review {
                  ... @skip(if: $skip1) {
                    ... @skip(if: $skip2) {
                      title
                      author
                      rating
                    }
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Lookup_Skip_On_Field_Fetched_Through_Lookup()
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
              review: Review
            }

            type Review {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
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
              productBySlug(slug: "product") {
                name
                review {
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
    public async Task Lookup_Skip_Only_On_Some_Fields_Fetched_Through_Lookup()
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
              review: Review
            }

            type Review {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              reviewById(id: ID!): Review @lookup
            }

            type Review {
              id: ID!
              title: String!
              rating: Int!
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
              productBySlug(slug: "product") {
                name
                review {
                  title @skip(if: $skip)
                  rating
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

    #region Require

    [Fact(Skip = "Requirement is still executed")]
    public async Task Lookup_Skip_On_Field_With_Requirement()
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
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              size: Int!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              dimension(size: Int! @require(field: "size")): String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              productBySlug(slug: "product") {
                dimension @skip(if: $skip)
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
    public async Task Lookup_Skip_Around_Field_With_Requirement_With_Other_Field()
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
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              size: Int!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              dimension(size: Int! @require(field: "size")): String
              price: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              productBySlug(slug: "product") {
                ... @skip(if: $skip) {
                  dimension
                  price
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

    [Fact(Skip = "Requirement is still executed")]
    public async Task Lookup_Skip_Not_Only_On_Field_With_Requirement()
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
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              size: Int!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              dimension(size: Int! @require(field: "size")): String
              price: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($skip: Boolean!) {
              productBySlug(slug: "product") {
                dimension @skip(if: $skip)
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

    #endregion

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
                # We need another selection here or our rewriter
                # would (correctly) pull the skip down on the node field.
                __typename
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
    public async Task NodeField_Multiple_Skip_Levels_Around_NodeField()
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
            query testQuery($skip1: Boolean!, $skip2: Boolean!) {
              ... @skip(if: $skip1) {
                ... @skip(if: $skip2) {
                  # We need another selection here or our rewriter
                  # would (correctly) pull the skip down on the node field.
                  __typename
                  # Discussion:1
                  node(id: "RGlzY3Vzc2lvbjox") {
                    __typename
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["skip1"] = true, ["skip2"] = true });

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

    [Fact(Skip = "Step is still triggered since operation requirement is being fulfilled")]
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

    #region Introspection

    [Fact]
    public async Task Introspection_Skip_On_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
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
              __typename @skip(if: $skip)
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
    public async Task Introspection_Skip_Around_Field()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              field: String
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
                __typename
                __type(name: "Query") {
                  name
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

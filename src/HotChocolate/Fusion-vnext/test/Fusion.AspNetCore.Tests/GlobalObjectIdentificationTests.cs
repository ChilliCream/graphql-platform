using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// TODO:
// - Conditional selections (on, on top of and below node field, also with conditional fragment)
// - Selections on interface, all types of interfaces are on same subgraph and there's only node lookup
public class GlobalObjectIdentificationTests : FusionTestBase
{
    [Fact]
    public async Task Concrete_Type_Branch_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  title
                  commentCount
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
    public async Task Concrete_Type_Branch_Requested_Abstract_Lookup()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Product:1
              node(id: "UHJvZHVjdDox") {
                id
                ... on Product {
                  name
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
    public async Task Invalid_Id_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              node(id: "invalid") {
                ... on Discussion {
                  title
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
    public async Task Id_Of_Unknown_Type_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # User:1
              node(id: "VXNlcjox") {
                ... on Discussion {
                  title
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
    public async Task Id_Of_Type_Different_From_Concrete_Type_Selections_Requested()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }

            type Product implements Node {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Product:1
              node(id: "UHJvZHVjdDox") {
                id
                ... on Discussion {
                  title
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
    public async Task Only_Typename_Selected()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              authorById(id: ID!): Author @lookup
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Author implements Node {
              id: ID!
              username: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Only_Id_And_Typename_Selected()
    {
        // arrange
        using var serverA = CreateSourceSchema(
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
              title: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                id
                __typename
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
    public async Task Only_TypeName_Selected_On_Concrete_Type()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup @internal
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  __typename
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
    public async Task No_By_Id_Lookup_On_Best_Matching_Source_Schema()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              discussionByName(title: String! @is(field: "title")): Discussion @lookup
            }

            type Discussion @key(fields: "id") {
              id: ID!
              title: String!
              commentCount: Int!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node @key(fields: "title") {
              id: ID!
              title: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Discussion:1
              node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  commentCount
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
    public async Task Two_Node_Fields_With_Alias()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
            }
            """);
        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussion(id: ID!): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              # Discussion:1
              a: node(id: "RGlzY3Vzc2lvbjox") {
                ... on Discussion {
                  title
                }
              }
              # Discussion:2
              b: node(id: "RGlzY3Vzc2lvbjoy") {
                ... on Discussion {
                  title
                  commentCount
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
    public async Task Node_Field_Alongside_Regular_Root_Selections()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
              viewer: Viewer
            }

            type Viewer {
              username: String
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              title: String!
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
            query testQuery($id: ID!) {
              viewer {
                username
              }
              node(id: $id) {
                __typename
                ... on Discussion {
                  title
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Concrete_Type_Has_Dependency()
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
              name: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              discussionById(discussionId: ID! @is(field: "id")): Discussion @lookup
            }

            interface Node @key(fields: "id") {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              commentCount: Int
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  name
                  commentCount
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
              discussionById(id: ID!): Discussion @lookup
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              viewerRating: Float!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  id
                  viewerRating
                  product {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Two_Concrete_Types_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Review implements Node {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  product {
                    name
                  }
                }
                ... on Review {
                  product {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Two_Concrete_Types_Selections_Have_Different_Dependencies()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Discussion implements Node {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Review implements Node {
              id: ID!
              product: Product
            }

            type Product @key(fields: "id") {
              id: ID!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Discussion {
                  product {
                    id
                    name
                  }
                }
                ... on Review {
                  product {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Selections_On_Interface()
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
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Votable {
                  viewerCanVote
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Selections_On_Interface_And_Concrete_Type()
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
              viewerCanVote: Boolean!
              viewerRating: Float!
            }

            type Comment implements Node & Votable {
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
            query testQuery($id: ID!) {
              node(id: $id) {
                ... on Votable {
                  viewerCanVote
                }
                ... on Discussion {
                  viewerRating
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Discussion:1 */ "RGlzY3Vzc2lvbjox" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Selections_On_Interface_And_Concrete_Type_Both_Have_Different_Dependencies()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
              singularProduct: Product
            }

            type Product implements Node {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
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
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
                id
                ... on ProductList {
                  products {
                    id
                    name
                  }
                }
                ... on Item2 {
                  singularProduct {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Item1:1 */ "SXRlbTE6MQ==" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Node_Field_Selections_On_Interface_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            interface ProductList {
              products: [Product]
            }

            type Item1 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Item2 implements Node & ProductList {
              id: ID!
              products: [Product]
            }

            type Product implements Node {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              node(id: ID!): Node @lookup @shareable
            }

            interface Node {
              id: ID!
            }

            type Product implements Node {
              id: ID!
              name: String
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
            query testQuery($id: ID!) {
              node(id: $id) {
                __typename
                id
                ... on ProductList {
                  products {
                    name
                  }
                }
              }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = /* Item1:1 */ "SXRlbTE6MQ==" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }
}

using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

// TODO once execution is implemented:
// - Returning null for a union (list item)
// - Returning different combinations inside of list
// TODO
// - spreading interface selection on union field
public class UnionTests : FusionTestBase
{
    #region union { ... }

    [Fact]
    public async Task Union_Field_Just_Typename_Selected()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              imageUrl: String!
            }

            type Discussion {
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
            query testQuery {
              post {
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
    public async Task Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              post {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
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
    public async Task Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
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
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
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
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
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
    public async Task Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
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
    public async Task Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              post: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
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
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              post {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
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

    #endregion

    #region unions { ... }

    [Fact]
    public async Task Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              posts {
                ... on Photo {
                  subgraph2
                }
                ... on Discussion {
                  subgraph1
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
    public async Task Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
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
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
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
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph3
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
    public async Task Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  author {
                    subgraph2
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
    public async Task Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
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
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              posts {
                ... on Photo {
                  product {
                    subgraph2
                  }
                }
                ... on Discussion {
                  product {
                    subgraph2
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

    #endregion

    #region objects { union { ... } }

    [Fact]
    public async Task Object_List_Union_Field_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
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
    public async Task Object_List_Union_Field_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
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
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
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
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
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
    public async Task Object_List_Union_Field_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
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
    public async Task Object_List_Union_Field_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              postEdges: [PostEdge]
            }

            type PostEdge {
              node: Post
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
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
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              postEdges {
                node {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
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

    #endregion

    #region objects { unions { ... } }

    [Fact]
    public async Task Object_List_Union_List_Concrete_Type_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo @key(fields: "id") {
              id: ID!
            }

            type Discussion {
              id: ID!
              subgraph1: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              photoById(id: ID!): Photo @lookup
            }

            type Photo {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    subgraph2
                  }
                  ... on Discussion {
                    subgraph1
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
    public async Task Object_List_Union_List_Concrete_Type_Selection_Has_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
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
              subgraph2: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
              authorById(id: ID!): Author @lookup
            }

            type Author {
              id: ID!
              subgraph3: String!
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
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph3
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
    public async Task Object_List_Union_List_Concrete_Type_Selections_Have_Dependency_To_Same_Subgraph()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
              id: ID!
              author: Author
            }

            type Product @key(fields: "id") {
              id: ID!
            }

            type Author @key(fields: "id") {
              id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
              authorById(id: ID!): Author @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
            }

            type Author {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    author {
                      subgraph2
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
    public async Task Object_List_Union_List_Concrete_Type_Selections_Have_Same_Dependency()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              users: [User]
            }

            type User {
              posts: [Post]
            }

            union Post = Photo | Discussion

            type Photo {
              id: ID!
              product: Product
            }

            type Discussion {
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
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              subgraph2: String!
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
            query testQuery {
              users {
                posts {
                  ... on Photo {
                    product {
                      subgraph2
                    }
                  }
                  ... on Discussion {
                    product {
                      subgraph2
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

    #endregion
}

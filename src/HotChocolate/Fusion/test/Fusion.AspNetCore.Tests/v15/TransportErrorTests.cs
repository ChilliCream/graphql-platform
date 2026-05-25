using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class TransportErrorTests : FusionTestBase
{
    #region Resolve (node)

    [Fact]
    public async Task Resolve_Node_Service_Offline_EntryField_Nullable()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
              node(id: ID!): Node @lookup
            }

            type Brand implements Node {
              id: ID!
              name: String
            }

            interface Node {
              id: ID!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              node(id: "QnJhbmQ6MQ==") {
                id
                ... on Brand {
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

    #endregion

    #region Parallel, Shared Entry Field

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_Nullable_SharedEntryField_Nullable()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
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
            query {
              viewer {
                userId
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_NonNull_SharedEntryField_Nullable()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID!
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
            query {
              viewer {
                userId
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

    [Fact(Skip = "Error is not correctly shown")]
    public async Task Resolve_Parallel_One_Service_Offline_SubField_NonNull_SharedEntryField_NonNull()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              userId: ID!
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
            query {
              viewer {
                userId
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

    [Fact(Skip = "Only one error per field")]
    public async Task Resolve_Parallel_Both_Services_Offline_SharedEntryField_Nullable()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              userId: ID
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
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
    public async Task Resolve_Parallel_Both_Services_Offline_SharedEntryField_NonNull()
    {
        // arrange
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @shareable
            }

            type Viewer {
              name: String
            }
            """,
            isOffline: true);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer! @shareable
            }

            type Viewer {
              userId: ID
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                userId
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

    #endregion

    #region Parallel, No Shared Entry Field

    [Fact]
    public async Task Resolve_Parallel_Single_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
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
    public async Task Resolve_Parallel_Single_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
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
    public async Task Resolve_Parallel_One_Service_Offline_EntryFields_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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
    public async Task Resolve_Parallel_One_Service_Offline_EntryFields_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer!
            }

            type Viewer {
              name: String!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              other: Other!
            }

            type Other {
              userId: ID!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              viewer {
                name
              }
              other {
                userId
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

    #region Entity Resolver

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Single_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Single_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_Nullable_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_NonNull_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_First_Service_Offline_SubFields_NonNull_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_Nullable_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String
              price: Float
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_NonNull_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Second_Service_Offline_SubFields_NonNull_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Both_Services_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Ordering is not correct")]
    public async Task Entity_Resolver_Both_Services_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              name: String!
              price: Float!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product! @lookup
            }

            type Product {
              id: ID!
              score: Int!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              productById(id: "1") {
                id
                name
                price
                score
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

    #region Resolve Sequence

    [Fact]
    public async Task Resolve_Sequence_First_Service_Offline_EntryField_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
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
    public async Task Resolve_Sequence_First_Service_Offline_EntryField_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product!
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """,
            isOffline: true);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
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
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_Nullable_Parent_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
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
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_NonNull_Parent_Nullable()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
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
    public async Task Resolve_Sequence_Second_Service_Offline_SubField_NonNull_Parent_NonNull()
    {
        // arrange
        var subgraphA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
            }

            type Product {
              id: ID!
              brand: Brand!
            }

            type Brand @key(fields: "id") {
              id: ID!
            }
            """);

        var subgraphB = CreateSourceSchema(
            "B",
            """
            type Query {
              brandById(id: ID!): Brand @lookup
            }

            type Brand {
              id: ID!
              name: String!
            }
            """,
            isOffline: true);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", subgraphA),
            ("B", subgraphB)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
              product {
                id
                brand {
                  id
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

    #endregion

    #region ResolveByKey

    //     [Fact]
    //     public async Task ResolveByKey_Second_Service_Offline_SubField_Nullable()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]!
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int
    //             }
    //             """,
    //             isOffline: true);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_Second_Service_Offline_SubField_NonNull_ListItem_NonNull()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product!]!
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """,
    //             isOffline: true);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }
    //
    //     [Fact]
    //     public async Task ResolveByKey_Second_Service_Offline_SubField_NonNull_ListItem_Nullable()
    //     {
    //         // arrange
    //         var subgraphA = CreateSourceSchema(
    //             "A",
    //             """
    //             type Query {
    //               products: [Product]!
    //             }
    //
    //             type Product {
    //               id: ID!
    //               name: String!
    //             }
    //             """);
    //
    //         var subgraphB = CreateSourceSchema(
    //             "B",
    //             """
    //             type Query {
    //               productsById(ids: [ID!]!): [Product]
    //             }
    //
    //             type Product {
    //               id: ID!
    //               price: Int!
    //             }
    //             """,
    //             isOffline: true);
    //
    //         using var gateway = await CreateCompositeSchemaAsync(
    //         [
    //             ("A", subgraphA),
    //             ("B", subgraphB)
    //         ]);
    //
    //         // act
    //         using var client = GraphQLHttpClient.Create(gateway.CreateClient());
    //         using var result = await client.PostAsync(
    //             """
    //             query {
    //               products {
    //                 id
    //                 name
    //                 price
    //               }
    //             }
    //             """,
    //             new Uri("http://localhost:5000/graphql"));
    //
    //         // assert
    //         using var response = await result.ReadAsResultAsync();
    //         MatchSnapshot(gateway, request, response);
    //     }

    #endregion
}

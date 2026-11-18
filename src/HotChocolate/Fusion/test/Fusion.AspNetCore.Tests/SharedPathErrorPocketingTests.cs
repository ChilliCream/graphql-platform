using System.Text.Json;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using OperationRequest = HotChocolate.Transport.OperationRequest;

namespace HotChocolate.Fusion;

public class SharedPathErrorPocketingTests : FusionTestBase
{
    [Fact]
    public async Task Viewer_Null_With_Error_And_Nullable_Children_Pockets_Child_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @shareable @error
            }

            type Viewer @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable
            }

            type Viewer @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              viewer {
                a
                b
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_Null_Without_Error_And_Nullable_Children_Does_Not_Initialize_Parent()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @shareable @null
            }

            type Viewer @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable
            }

            type Viewer @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              viewer {
                a
                b
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_NonNull_With_Error_Fails_Fast()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer! @shareable @error
            }

            type Viewer @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer! @shareable
            }

            type Viewer @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              viewer {
                a
                b
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        Assert.True(response.Errors.ValueKind is JsonValueKind.Array);
        Assert.Equal(1, response.Errors.GetArrayLength());
        Assert.Equal("Unexpected Execution Error", response.Errors[0].GetProperty("message").GetString());
        Assert.Equal("viewer", response.Errors[0].GetProperty("path")[0].GetString());
        Assert.True(response.Data.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null);
    }

    [Fact]
    public async Task Viewer_Null_With_Error_And_NonNull_Child_Propagates_To_Parent_On_Post_Process()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @shareable @error
            }

            type Viewer @shareable {
              a: String!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable
            }

            type Viewer @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              viewer {
                a
                b
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Entity_Interface_With_Shared_Value_Type_Under_Inline_Fragment_Pockets_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node: INode @shareable
              productA(id: Int!): Product @lookup
            }

            interface INode @key(fields: "id") {
              id: Int!
            }

            type Product implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable @error
            }

            type SharedData @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node: INode @shareable
              productB(id: Int!): Product @lookup
            }

            interface INode @key(fields: "id") {
              id: Int!
            }

            type Product implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type SharedData @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              node {
                ... on Product {
                  shared {
                    a
                    b
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Viewer_Remains_Uninitialized_Promotes_One_Pocketed_Error_To_Parent()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              viewer: Viewer @shareable @error
            }

            type Viewer @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              viewer: Viewer @shareable @null
            }

            type Viewer @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              viewer {
                a
                b
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Inline_Fragments_With_Duplicate_Response_Name_Should_Not_Drop_Pocketed_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              node: INode @shareable @returns(types: ["Product"])
              productA(id: Int!): Product @lookup
              reviewA(id: Int!): Review @lookup
            }

            interface INode @key(fields: "id") {
              id: Int!
            }

            type Product implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable @error
            }

            type Review implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type SharedData @shareable {
              a: String
              b: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              node: INode @shareable @returns(types: ["Product"])
              productB(id: Int!): Product @lookup
              reviewB(id: Int!): Review @lookup
            }

            interface INode @key(fields: "id") {
              id: Int!
            }

            type Product implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type Review implements INode @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type SharedData @shareable {
              a: String
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              node {
                ... on Product {
                  shared {
                    a
                  }
                }
                ... on Review {
                  shared {
                    b
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Shared_Value_Type_List_Below_Unshared_Parent_Nulled_By_NonNull_Child_Keeps_Error(ErrorHandlingMode onError)
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              itemPage: ItemPage
            }

            type ItemPage {
              items: [Item!]
            }

            type Item @shareable {
              name: String! @null
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              item: Item
            }

            type Item @shareable {
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              itemPage {
                items {
                  name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Fact]
    public async Task Shared_Value_Type_NonNull_List_Below_Unshared_Parent_Nulled_By_NonNull_Child_Keeps_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              itemPage: ItemPage
            }

            type ItemPage {
              items: [Item!]!
            }

            type Item @shareable {
              name: String! @null
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              item: Item
            }

            type Item @shareable {
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              itemPage {
                items {
                  name
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData(ErrorHandlingMode.Propagate)]
    [InlineData(ErrorHandlingMode.Null)]
    public async Task Shared_Value_Type_Object_Below_Unshared_Parent_Nulled_By_NonNull_Child_Keeps_Error(ErrorHandlingMode onError)
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              itemPage: ItemPage
            }

            type ItemPage {
              item: Item
            }

            type Item @shareable {
              name: String! @null
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              item: Item
            }

            type Item @shareable {
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              itemPage {
                item {
                  name
                }
              }
            }
            """,
            onError: onError);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result, postFix: "OnError_" + onError);
    }

    [Fact]
    public async Task Shared_Value_Type_Below_Entity_Null_With_Error_And_Nullable_Child_Pockets_Child_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
              productA(id: Int!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable @error
            }

            type SharedData @shareable {
              a: String
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productB(id: Int!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type SharedData @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              product {
                shared {
                  a
                  b
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Shared_Value_Type_Below_Entity_Null_With_Error_And_NonNull_Child_Propagates_To_Shared_Parent()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              product: Product
              productA(id: Int!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable @error
            }

            type SharedData @shareable {
              a: String!
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              productB(id: Int!): Product @lookup
            }

            type Product @key(fields: "id") {
              id: Int!
              shared: SharedData @shareable
            }

            type SharedData @shareable {
              b: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              product {
                shared {
                  a
                  b
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Shared_Value_Type_List_Below_Unshared_Interface_Fragment_Nulled_By_NonNull_Child_Keeps_Error()
    {
        using var serverA = CreateSourceSchema(
            "A",
            """
            type Query {
              page: Page
            }

            interface Page {
              id: Int!
            }

            type ItemPage implements Page {
              id: Int!
              items: [Item!]
            }

            type Item @shareable {
              name: String! @null
            }
            """);

        using var serverB = CreateSourceSchema(
            "B",
            """
            type Query {
              item: Item
            }

            type Item @shareable {
              name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", serverA),
            ("B", serverB)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              page {
                ... on ItemPage {
                  items {
                    name
                  }
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task ValueCompletion_Should_ReportError_When_NestedSharedUnionIsNull()
    {
        // arrange
        using var sourceA = CreateSourceSchema("A",
            """
            type Query { page: Page }
            type Page { item: Item @error }
            union Item = Book
            type Book { name: String }
            """);
        using var sourceB = CreateSourceSchema("B",
            """
            type Query { other: Item }
            union Item = Photo
            type Photo { id: ID }
            """);
        using var gateway = await CreateCompositeSchemaAsync([("A", sourceA), ("B", sourceB)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              page {
                item {
                  __typename
                  ... on Book { name }
                }
              }
            }
            """);

        // act
        using var response = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, response);
    }
}

using System.Text.Json;
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
            new Uri("http://localhost:5000/graphql"));

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
            new Uri("http://localhost:5000/graphql"));

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
            new Uri("http://localhost:5000/graphql"));

        await MatchSnapshotAsync(gateway, request, result);
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
            new Uri("http://localhost:5000/graphql"));

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
            new Uri("http://localhost:5000/graphql"));

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
            new Uri("http://localhost:5000/graphql"));

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
            new Uri("http://localhost:5000/graphql"));

        using var response = await result.ReadAsResultAsync();

        await MatchSnapshotAsync(gateway, request, result);
    }
}

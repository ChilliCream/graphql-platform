using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;

namespace HotChocolate.Fusion;

public class DeferTests : FusionTestBase
{
    [Fact]
    public async Task Defer_Single_Fragment_Returns_Incremental_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                email: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                title: String!
                body: String!
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
            query GetUser {
                user(id: "1") {
                    name
                    ... @defer(label: "reviews") {
                        reviews {
                            title
                            body
                        }
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_IfFalse_Variable_Should_Return_NonStreamed_Result()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                email: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                title: String!
                body: String!
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
            query: """
            query GetUser($shouldDefer: Boolean!) {
                user(id: "1") {
                    name
                    ... @defer(if: $shouldDefer, label: "reviews") {
                        reviews {
                            title
                            body
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["shouldDefer"] = false });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_IfTrue_Variable_Should_Return_Streamed_Result()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                email: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                title: String!
                body: String!
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
            query: """
            query GetUser($shouldDefer: Boolean!) {
                user(id: "1") {
                    name
                    ... @defer(if: $shouldDefer, label: "reviews") {
                        reviews {
                            title
                            body
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["shouldDefer"] = true });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Nested_Should_Return_Incremental_Response_In_Order()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
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
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer(label: "inner") {
                            address
                        }
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Nested_Without_Label_On_Inner_Should_Return_Incremental_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
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
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
                        email
                        ... @defer {
                            address
                        }
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Two_Siblings_With_Overlapping_Fields_Should_Deduplicate()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
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
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
                    ... @defer(label: "location") {
                        email
                        address
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Two_Siblings_Sharing_Field_Emit_One_Incremental_That_Completes_Both_Groups()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
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
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                    }
                    ... @defer(label: "location") {
                        email
                        address
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        // The stable-stream snapshot lays out the per-frame timeline (pending /
        // incremental / completed) so the shared incremental plan emitting once under the
        // best delivery-group id while still completing both groups is visible
        // as a single block.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_With_Error_In_Deferred_Fragment_Should_Return_Error_In_Incremental_Payload()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String! @error
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
            query GetUser {
                user(id: "1") {
                    name
                    ... @defer(label: "email") {
                        email
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact(Skip = "Requires validation of @skip/@include interaction with @defer at the planning level")]
    public async Task Defer_With_Skip_Directive_Should_Skip_Deferred_Fragment()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                email: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                title: String!
                body: String!
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
            query GetUser {
                user(id: "1") {
                    name
                    ... @defer(label: "reviews") @include(if: false) {
                        reviews {
                            title
                            body
                        }
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert — with @include(if: false), the deferred fragment should be entirely
        // removed during planning, resulting in a single non-incremental response.
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync();
        var payloads = rawBody
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line))
            .ToList();

        Assert.Single(payloads);

        var initial = payloads[0].RootElement;
        Assert.True(initial.TryGetProperty("data", out var data));
        Assert.Equal("User: VXNlcjox", data.GetProperty("user").GetProperty("name").GetString());

        // Reviews should NOT be in the response since the fragment was skipped
        Assert.False(data.GetProperty("user").TryGetProperty("reviews", out _));

        // No incremental delivery
        Assert.False(initial.TryGetProperty("pending", out _));
        Assert.False(initial.TryGetProperty("incremental", out _));

        foreach (var doc in payloads)
        {
            doc.Dispose();
        }
    }

    [Fact(Skip = "Known limitation: @defer on mutations forces Query operation type in deferred plan")]
    public async Task Defer_On_Mutation_Result_Should_Return_Incremental_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Mutation {
                createProduct(name: String!): Product!
            }

            type Product @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                price: Float!
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
            mutation {
                createProduct(name: "Widget") {
                    name
                    ... @defer(label: "pricing") {
                        price
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert — initial payload should have the mutation result with name,
        // deferred payload should deliver the price from source B.
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync();
        var payloads = rawBody
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => JsonDocument.Parse(line))
            .ToList();

        Assert.Equal(2, payloads.Count);

        // --- Initial payload ---
        var initial = payloads[0].RootElement;
        Assert.True(initial.TryGetProperty("data", out var data));
        Assert.True(data.TryGetProperty("createProduct", out var product));
        Assert.Equal("Product: UHJvZHVjdDox", product.GetProperty("name").GetString());
        Assert.True(initial.GetProperty("hasNext").GetBoolean());

        // --- Deferred payload ---
        var deferred = payloads[1].RootElement;
        Assert.True(deferred.TryGetProperty("incremental", out var incremental));
        Assert.Equal(1, incremental.GetArrayLength());

        var incrementalData = incremental[0].GetProperty("data");
        Assert.True(
            incrementalData.GetProperty("createProduct").TryGetProperty("price", out _));

        Assert.False(deferred.GetProperty("hasNext").GetBoolean());

        foreach (var doc in payloads)
        {
            doc.Dispose();
        }
    }

    [Fact]
    public async Task Defer_With_Forwarded_Variable_Merges_Into_Subgraph_Request()
    {
        // arrange
        // Schema A owns the user lookup and exposes the entity key. Schema B owns the
        // posts(first:) field, so the deferred fetch hops to B with both the imported
        // parent key (__fusion_1_id) and the forwarded request variable ($limit).
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                posts(first: Int!): [Post!]!
            }

            type Post {
                id: ID!
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
            query: """
            query GetUserPosts($limit: Int!) {
                user(id: "1") {
                    id
                    ... @defer(label: "posts") {
                        posts(first: $limit) {
                            id
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["limit"] = 5 });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        // The snapshot's deferred subgraph interaction must show both the imported
        // __fusion_*_id key and the forwarded $limit variable in the variables payload.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Nested_With_Forwarded_Variable_At_Each_Level()
    {
        // arrange
        // Three subgraphs so each defer level requires its own hop. Each hop carries an
        // imported parent key plus a forwarded request variable scoped to that level.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                posts(first: Int!): [Post!]!
            }

            type Post @key(fields: "id") {
                id: ID!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
                postById(id: ID!): Post @lookup
            }

            type Post @key(fields: "id") {
                id: ID!
                comments(first: Int!): [Comment!]!
            }

            type Comment {
                id: ID!
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
            query: """
            query GetUserPostsAndComments($postLimit: Int!, $commentLimit: Int!) {
                user(id: "1") {
                    id
                    ... @defer(label: "posts") {
                        posts(first: $postLimit) {
                            id
                            ... @defer(label: "comments") {
                                comments(first: $commentLimit) {
                                    id
                                }
                            }
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?>
            {
                ["postLimit"] = 3,
                ["commentLimit"] = 2
            });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        // The snapshot must show both deferred subgraph calls carrying their respective
        // forwarded variables alongside the imported parent keys.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_With_Forwarded_Variable_Over_List_Anchor()
    {
        // arrange
        // Root list anchor so the deferred incremental plan expands across multiple imported
        // entries. Each expanded entry must carry the forwarded $limit variable.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                users: [User!]!
            }

            type User @key(fields: "id") {
                id: ID!
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                posts(first: Int!): [Post!]!
            }

            type Post {
                id: ID!
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
            query: """
            query GetUsersPosts($limit: Int!) {
                users {
                    id
                    ... @defer(label: "posts") {
                        posts(first: $limit) {
                            id
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["limit"] = 5 });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"));

        // assert
        // The deferred subgraph call expands across all imported user entries. Each
        // outbound variable set carries the forwarded $limit alongside the parent key.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }
}

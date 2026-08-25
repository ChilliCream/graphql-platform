using System.Net;
using System.Text;
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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_KeyOnlyField_Should_Return_NonStreamed_Result()
    {
        // arrange
        // The only deferred field is the entity's own key, and its only lookup is keyed by
        // that same field, so no incremental plan can ever key a lookup off it. The data is
        // already available (the key any subgraph exposing the entity resolves for free), so
        // the gateway serves it in the initial payload instead of deferring it.
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
            query GetUsers {
                users {
                    ... @defer {
                        id
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotEmitDescendantData_When_InaccessibleRuntimeTypeNullsPendingAncestor()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
                entity: Entity
            }

            type Entity {
                id: ID!
                wrapper: Wrapper
            }

            type Wrapper {
                visible: String
                value: Foo! @returns(types: ["Baz"])
            }

            interface Foo {
                name: String
            }

            type Baz implements Foo @inaccessible {
                name: String
            }

            type Qux implements Foo {
                name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            query {
                entity {
                    id
                    wrapper {
                        visible
                        ... @defer(label: "hidden") {
                            value {
                                __typename
                                name
                            }
                        }
                    }
                }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        rawBody.MatchSnapshot();
    }

    [Fact]
    public async Task Defer_Should_EmitSingleNullPatch_When_InaccessibleRuntimeTypeNullsPendingList()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
                entities: [Entity!]
            }

            type Entity {
                id: ID!
                value: Foo! @returns(types: ["Baz"])
            }

            interface Foo {
                name: String
            }

            type Baz implements Foo @inaccessible {
                name: String
            }

            type Qux implements Foo {
                name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            query {
                entities {
                    id
                    ... @defer(label: "hidden") {
                        value {
                            __typename
                            name
                        }
                    }
                }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        rawBody.MatchSnapshot();
    }

    [Fact]
    public async Task Defer_Should_SuppressDescendantPatch_When_NullMarkerBubblesAbovePendingPath()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
                deep: Deep
            }

            type Deep {
                parent: Parent
            }

            type Parent {
                visible: String
                child: Child!
            }

            type Child {
                visible: String
                value: Foo! @returns(types: ["Baz"])
            }

            interface Foo {
                name: String
            }

            type Baz implements Foo @inaccessible {
                name: String
            }

            type Qux implements Foo {
                name: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            query {
                deep {
                    parent {
                        visible
                        child {
                            visible
                            ... @defer(label: "hidden") {
                                value {
                                    __typename
                                    name
                                }
                            }
                        }
                    }
                }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync(
            TestContext.Current.CancellationToken);
        rawBody.MatchSnapshot();
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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert — with @include(if: false), the deferred fragment should be entirely
        // removed during planning, resulting in a single non-incremental response.
        var rawBody = await result.HttpResponseMessage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
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

    [Fact]
    public async Task Defer_On_Mutation_Result_Should_Return_Incremental_Response()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                product(id: ID!): Product @lookup
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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // The mutation must run exactly once on schema A (see the "interactions" in the
        // snapshot); the deferred "price" is delivered via a keyed lookup on schema B.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

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
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // The deferred subgraph call expands across all imported user entries. Each
        // outbound variable set carries the forwarded $limit alongside the parent key.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Anchored_Under_Second_Root_Node_Should_Report_Correct_ParentNode()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                products: [Product!]!
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
                users: [User!]!
            }

            type User @key(fields: "id") {
                id: ID!
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
            query t {
                products { id name }
                users {
                    __typename
                    ... @defer { id name }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // Both root fetch nodes target `$`, but only node 2 resolves `users`, the field
        // the deferred fragment is anchored under. The snapshot's incremental plan must
        // report parentNodeId: 2, not the first root node.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Anchored_Under_Third_Root_Node_Should_Report_Correct_ParentNode()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                products: [Product!]!
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
                users: [User!]!
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
                orders: [Order!]!
            }

            type Order @key(fields: "id") {
                id: ID!
                total: Float!
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
            query t {
                products { id name }
                users { id name }
                orders {
                    __typename
                    ... @defer { id total }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // The deferred fragment is anchored under `orders`, resolved by the third root
        // fetch node. The snapshot's incremental plan must report that node as its parent.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_Return_Patches_When_Path_Crosses_Intermediate_Lists()
    {
        // arrange
        using var server = CreateSourceSchema(
            "A",
            """
            type Query {
                users: UserConnection!
            }

            interface UserConnection {
                nodes: [User!]!
            }

            type DefaultUserConnection implements UserConnection {
                nodes: [User!]!
            }

            type User {
                reviews: [Review!]!
            }

            type Review {
                product: Product!
            }

            type Product {
                name: String!
                dimension: Dimension!
            }

            type Dimension {
                height: Float!
            }
            """,
            mockHttpResponse: _ => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "data": {
                            "users": {
                              "__typename": "DefaultUserConnection",
                              "nodes": [
                                {
                                  "reviews": [
                                    {
                                      "product": {
                                        "name": "Table",
                                        "dimension": {
                                          "height": 30.0
                                        }
                                      }
                                    },
                                    {
                                      "product": {
                                        "name": "Chair",
                                        "dimension": {
                                          "height": 40.0
                                        }
                                      }
                                    }
                                  ]
                                },
                                {
                                  "reviews": [
                                    {
                                      "product": {
                                        "name": "Couch",
                                        "dimension": {
                                          "height": 50.0
                                        }
                                      }
                                    }
                                  ]
                                }
                              ]
                            }
                          }
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                }));

        using var gateway = await CreateCompositeSchemaAsync([("A", server)]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            query {
                users {
                    nodes {
                        reviews {
                            product {
                                name
                                ... @defer(label: "dimension") {
                                    dimension {
                                        height
                                    }
                                }
                            }
                        }
                    }
                }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Composite_Field_Under_Type_Condition_On_Abstract_Parent_Is_Wrapped()
    {
        // arrange
        // Connector is a Node entity split across two schemas. The non-deferred selection and
        // one @defer resolve from schema A, while the second @defer requires an entity lookup
        // into schema B for the `devices` connection.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                node(id: ID!): Node @lookup @shareable
                connectorById(id: ID!): Connector @lookup @internal
            }

            interface Node {
                id: ID!
            }

            type Connector implements Node {
                id: ID!
                system: String!
                version: String!
                vendor: String!
                description: String
            }
            """);

        using var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                node(id: ID!): Node @lookup @shareable
                connectorById(id: ID!): Connector @lookup @internal
            }

            interface Node {
                id: ID!
            }

            type Connector implements Node {
                id: ID!
                devices(first: Int): ConnectorDeviceConnection!
            }

            type ConnectorDevice implements Node {
                id: ID!
                name: String!
            }

            type ConnectorDeviceConnection {
                edges: [ConnectorDeviceEdge!]
                nodes: [ConnectorDevice!]
                totalCount: Int!
            }

            type ConnectorDeviceEdge {
                node: ConnectorDevice!
                cursor: String!
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
            query ConnectorDetailsQuery($id: ID!) {
                node(id: $id) {
                    __typename
                    id
                    ... on Connector @defer(label: "Yrs") {
                        system
                        description
                    }
                    ...ConnectorDetailsHeaderFragment
                    ...ConnectorDevicesFragment @defer(label: "testing")
                }
            }

            fragment ConnectorDetailsHeaderFragment on Connector {
                system
                version
                vendor
            }

            fragment ConnectorDevicesFragment on Connector {
                id
                devices(first: 10) {
                    edges {
                        node {
                            ... on ConnectorDevice {
                                id
                                name
                                __typename
                            }
                        }
                        cursor
                    }
                    totalCount
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "Q29ubmVjdG9yOjE=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Nested_Composite_On_One_Of_Sibling_Type_Conditions()
    {
        // arrange
        // The SensorWidgetInfo branch (without `file`) is listed first; ImageWidgetInfo is declared first.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Dashboard implements Node {
                id: ID!
                name: String!
                widget: WidgetInfo!
            }

            interface WidgetInfo {
                id: ID!
            }

            type ImageWidgetInfo implements WidgetInfo {
                id: ID!
                entity: Entity!
            }

            type SensorWidgetInfo implements WidgetInfo {
                id: ID!
                entity: Entity!
            }

            type Entity {
                id: ID!
                feature: Feature!
            }

            union Feature = ImageFeature | Sensor

            type ImageFeature {
                file: File!
            }

            type Sensor {
                value: String!
            }

            type File {
                url: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            query: """
            query DashboardQuery($id: ID!) {
                node(id: $id) {
                    __typename
                    id
                    ...DashboardFragment @defer(label: "testing")
                }
            }

            fragment DashboardFragment on Dashboard {
                id
                widget {
                    __typename
                    ... on SensorWidgetInfo {
                        entity {
                            feature {
                                __typename
                                ... on Sensor { value }
                            }
                        }
                    }
                    ... on ImageWidgetInfo {
                        entity {
                            feature {
                                __typename
                                ... on ImageFeature { file { url } }
                            }
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "RGFzaGJvYXJkOjE=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Nested_Composite_On_Both_Sibling_Type_Conditions()
    {
        // arrange
        // Both branches select a nested composite `file`, but with a different deeper child
        // (`meta` vs `clip`) absent in the sibling; VideoWidgetInfo/VideoFeature are declared first.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                node(id: ID!): Node @lookup
            }

            interface Node {
                id: ID!
            }

            type Dashboard implements Node {
                id: ID!
                name: String!
                widget: WidgetInfo!
            }

            interface WidgetInfo {
                id: ID!
            }

            type VideoWidgetInfo implements WidgetInfo {
                id: ID!
                entity: Entity!
            }

            type ImageWidgetInfo implements WidgetInfo {
                id: ID!
                entity: Entity!
            }

            type Entity {
                id: ID!
                feature: Feature!
            }

            union Feature = VideoFeature | ImageFeature

            type VideoFeature {
                file: VideoFile!
            }

            type ImageFeature {
                file: ImageFile!
            }

            type VideoFile {
                clip: VideoClip!
            }

            type ImageFile {
                meta: ImageMeta!
            }

            type VideoClip {
                duration: String!
            }

            type ImageMeta {
                url: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            query: """
            query DashboardQuery($id: ID!) {
                node(id: $id) {
                    __typename
                    id
                    ...DashboardFragment @defer(label: "testing")
                }
            }

            fragment DashboardFragment on Dashboard {
                id
                widget {
                    __typename
                    ... on ImageWidgetInfo {
                        entity {
                            feature {
                                __typename
                                ... on ImageFeature { file { meta { url } } }
                            }
                        }
                    }
                    ... on VideoWidgetInfo {
                        entity {
                            feature {
                                __typename
                                ... on VideoFeature { file { clip { duration } } }
                            }
                        }
                    }
                }
            }
            """,
            variables: new Dictionary<string, object?> { ["id"] = "RGFzaGJvYXJkOjE=" });

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_ListAnchor_Should_PlanKeyedLookup_When_UsersFieldHasSiblingLookup()
    {
        // arrange
        // users and userById share a subgraph; deferred fields fetch per item by id.
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                users: [User!]!
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync([("A", server1)]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                users {
                    ... @defer {
                        id
                        name
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // One users request carrying id, one userById request per item, no second users request.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_MixedSubgraphFields_Should_KeepBothFields_When_SameAndForeignSubgraphSplit()
    {
        // arrange
        // birthdate is same-subgraph on accounts; reviewCount requires reviews via the entity key.
        using var server1 = CreateSourceSchema(
            "accounts",
            """
            type Query {
                users: [User!]!
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
                birthdate: String!
            }
            """);

        using var server2 = CreateSourceSchema(
            "reviews",
            """
            type Query {
                userById(id: ID!): User @lookup @internal
            }

            type User @key(fields: "id") {
                id: ID!
                reviewCount: Int!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("accounts", server1),
            ("reviews", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                users {
                    name
                    ... @defer {
                        birthdate
                        reviewCount
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // The incremental payload carries both birthdate and reviewCount, with birthdate non-null.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }
}

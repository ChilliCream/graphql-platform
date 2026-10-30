using System.Collections.Immutable;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using HotChocolate.Execution;
using HotChocolate.Fusion.Configuration;
using HotChocolate.Fusion.Diagnostics;
using HotChocolate.Fusion.Execution;
using HotChocolate.Fusion.Execution.Nodes;
using HotChocolate.Language;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using OperationRequest = HotChocolate.Transport.OperationRequest;

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
    public async Task Defer_Should_DeliverLookupFields_When_RootFieldsPrecedeTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(configureGatewayBuilder: null);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                a: __typename
                b: __typename
                products {
                    id
                    ... @defer {
                        reviews {
                            id
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
    public async Task Defer_Should_DeliverLookupFields_When_NoRootFieldsPrecedeTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(configureGatewayBuilder: null);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                products {
                    id
                    ... @defer {
                        reviews {
                            id
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
    public async Task Defer_Should_DeliverLookupFields_When_PlainRootFieldPrecedesTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(configureGatewayBuilder: null);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                version
                products {
                    id
                    ... @defer {
                        reviews {
                            id
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
    public async Task Defer_Should_DeliverLookupFields_When_FragmentRootFieldsPrecedeTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(configureGatewayBuilder: null);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                ... on Query {
                    version
                    a: __typename
                }
                ...RootFields
                products {
                    id
                    ... @defer {
                        reviews {
                            id
                        }
                    }
                }
            }

            fragment RootFields on Query {
                b: __typename
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
    public async Task Defer_Should_DeliverNestedDeferFields_When_RootFieldsPrecedeTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(configureGatewayBuilder: null);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                a: __typename
                b: __typename
                products {
                    id
                    ... @defer {
                        reviews {
                            id
                            ... @defer {
                                text
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
    public async Task Defer_Should_DeliverSiblingDeferFields_When_RootFieldsPrecedeTheDeferredList()
    {
        // arrange
        using var gateway = await CreateProductRatingGatewayAsync();
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                a: __typename
                b: __typename
                products {
                    id
                    ... @defer {
                        rating
                    }
                    ... @defer {
                        reviews {
                            id
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
    public async Task Defer_Should_CompleteDeliveryGroupWithError_When_ImportedPathHasNoSelection()
    {
        // arrange
        using var gateway = await CreateProductGatewayAsync(
            configureGatewayBuilder: builder => builder.UseRequest(
                (_, next) => async context =>
                {
                    var plan = context.GetOperationPlan()!;
                    var compiler = context.Schema.Services.GetRequiredService<OperationCompiler>();
                    var operation = compiler.Compile(
                        "unrelated",
                        "unrelated",
                        "unrelated",
                        (OperationDefinitionNode)Utf8GraphQLParser.Parse("{ version }").Definitions[0]);
                    var incrementalPlan = plan.IncrementalPlans[0];
                    // the delivery paths must resolve against the swapped incremental operation
                    var planDeliveryGroups = plan.DeliveryGroups
                        .Select(group => group with { Path = SelectionPath.Root })
                        .ToImmutableArray();
                    var deliveryGroups = incrementalPlan.DeliveryGroups
                        .Select(group => planDeliveryGroups.Single(t => t.Id == group.Id))
                        .ToImmutableArray();

                    context.SetOperationPlan(
                        OperationPlan.Create(
                            plan.Id,
                            plan.Operation,
                            plan.RootNodes,
                            plan.AllNodes,
                            planDeliveryGroups,
                            [
                                new IncrementalPlan(
                                    operation,
                                    incrementalPlan.RootNodes,
                                    incrementalPlan.AllNodes,
                                    deliveryGroups,
                                    incrementalPlan.Requirements)
                            ],
                            plan.SearchSpace,
                            plan.ExpandedNodes));

                    await next(context);
                },
                key: "SwapIncrementalPlan",
                before: WellKnownRequestMiddleware.OperationExecutionMiddleware));
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
                a: __typename
                b: __typename
                products {
                    id
                    ... @defer {
                        reviews {
                            id
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

    private async Task<Gateway> CreateProductGatewayAsync(
        Action<IFusionGatewayBuilder>? configureGatewayBuilder)
    {
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                version: String!
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                text: String!
            }
            """);

        return await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ],
        configureGatewayBuilder: configureGatewayBuilder);
    }

    private async Task<Gateway> CreateProductRatingGatewayAsync()
    {
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                version: String!
                products: [Product!]!
            }

            type Product @key(fields: "id") {
                id: ID!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                productById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                reviews: [Review!]!
            }

            type Review {
                id: ID!
                text: String!
            }
            """);

        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
                productRatingById(id: ID!): Product @lookup
            }

            type Product @key(fields: "id") {
                id: ID!
                rating: Int!
            }
            """);

        return await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);
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
    public async Task Defer_KeyOnlyField_Should_KeepTypename_When_ClientSelectsTypenameSibling()
    {
        // arrange
        // The client selects __typename alongside the deferred key field. Absorbing the
        // redundant defer into the parent step must keep that client-requested selection
        // in the initial payload, not drop it.
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
                    __typename
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
    public async Task Defer_KeyOnlyField_Should_KeepTypename_When_TypenameInsideDefer()
    {
        // arrange
        // The client selects __typename together with the entity's own key field inside the
        // defer itself. Absorbing the redundant defer into the parent step must surface both
        // in the initial, non-streamed payload rather than silently dropping __typename.
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
                        __typename
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
    public async Task Defer_IfOmitted_Should_Return_Streamed_Result()
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
    public async Task Defer_IfTrue_Literal_Should_Return_Streamed_Result()
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
                    ... @defer(if: true, label: "reviews") {
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
    public async Task Defer_IfFalse_Literal_Should_Return_NonStreamed_Result()
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
                    ... @defer(if: false, label: "reviews") {
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
    public async Task Defer_IfNull_Literal_Should_Return_ValidationError()
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
                    ... @defer(if: null, label: "reviews") {
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
    public async Task Defer_NullableIfVariable_Should_Return_Error_When_VariableIsNull()
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
            query GetUser($shouldDefer: Boolean) {
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
            variables: new Dictionary<string, object?> { ["shouldDefer"] = null });

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
        // as a single block. The shared email is fetched once, address is fetched by its own
        // request, so B receives two requests.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 2");
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

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            includeOperationPlan: false);
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

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            includeOperationPlan: false);
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

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", server)],
            includeOperationPlan: false);
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

    [Fact]
    public async Task Defer_Should_SendOneSubgraphRequestPerGroup_When_SiblingsSelectDisjointFields()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

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
        // Baseline without sharing: each delivery group owns one incremental plan and B receives one request per plan.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 2");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_SendOneSubgraphRequestPerGroupSet_When_SiblingsOverlapPartially()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithAddressAfterEmailAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "first") {
                        email
                    }
                    ... @defer(label: "second") {
                        email
                        address
                    }
                    ... @defer(label: "third") {
                        address
                        phone
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        // The delivery-group sets {first, second}, {second, third} and {third} give three B requests.
        // email and address are each fetched once.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 3");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotRefetchField_When_FieldIsInInitialSelectionAndDeferredFragment()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    email
                    ... @defer(label: "more") {
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
        // email is fetched by the initial B request only. The deferred plan fetches address by a second B request.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 2");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotRefetchField_When_FieldIsInInitialSelectionAndSiblingFragments()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    email
                    ... @defer(label: "contact") {
                        email
                        phone
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
        // email is fetched by the initial B request only. Each deferred plan fetches its own field by
        // one more B request.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 3");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotRefetchField_When_NestedFragmentsShareField()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

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
                            email
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
        // email belongs to the outer group only and is fetched once. The outer plan also sends a second
        // A request for the user id that the inner plan depends on, although the initial A request
        // already returned it.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 2, B: 2");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotRefetchField_When_SiblingAndNestedFragmentsShareField()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

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
                        address
                        ... @defer(label: "details") {
                            email
                            phone
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
        // email is shared by the contact group and the nested details group and is fetched once. address
        // and phone are fetched by their own B requests.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 2, B: 3");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact(Skip = "Planner drops a root subgraph field that is shared by sibling defer groups.")]
    public async Task Defer_Should_DeliverRootSubgraphField_When_SiblingsShareIt()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
                    ... @defer(label: "contact") {
                        name
                        email
                    }
                    ... @defer(label: "location") {
                        name
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
        // The plan for the group set {contact, location} has no nodes, so name is never fetched and the
        // non-null field is missing from the final data without an error. Once fixed, snapshot the request
        // counts per subgraph.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_FetchSharedField_Once_When_SiblingsReachItThroughLookupOnSecondSubgraph()
    {
        // arrange
        // Each subgraph request is held until the client has received the previous payload: email, score, address.
        var sequence = new SubgraphRequestSequence("email", "score", "address");
        using var gateway = await CreateUserAccountGatewayAsync(
            new SequencedSubgraph(sequence),
            new SequencedSubgraph(sequence));

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        score
                    }
                    ... @defer(label: "location") {
                        score
                        address
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        await sequence.ReleaseAsPayloadsArriveAsync(result, TestContext.Current.CancellationToken);

        // assert
        // score lives on C only and is fetched by one C request for the group set {contact, location}.
        // email and address are fetched by one B request each.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 2, C: 1");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotRefetchNestedField_When_InitialAndDeferredSelectionUseLookupChain()
    {
        // arrange
        using var gateway = await CreateUserAccountGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    account {
                        balance
                    }
                    ... @defer(label: "contact") {
                        email
                        account {
                            balance
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
        // balance is fetched from C by the initial plan only. The deferred plan fetches email from B and
        // does not call C.
        var requestCounts = await GetRequestCountsAsync(gateway, result);
        requestCounts.MatchInlineSnapshot("A: 1, B: 2, C: 1");
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact(Skip = "Planner rejects a nested field with a lookup requirement that is shared by sibling defer groups.")]
    public async Task Defer_Should_FetchSharedNestedField_Once_When_SiblingsReachItThroughLookupChain()
    {
        // arrange
        using var gateway = await CreateUserAccountGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "contact") {
                        email
                        account {
                            balance
                        }
                    }
                    ... @defer(label: "location") {
                        address
                        account {
                            balance
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
        // Planning fails with "The deferred incremental plan's requirement '__fusion_2_id' at path
        // '$.user.account' could not be resolved from the parent plan". Once fixed, snapshot the request
        // counts per subgraph.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact(Skip = "Executor throws when a defer group has no fields of its own and only wraps a nested defer.")]
    public async Task Defer_Should_DeliverNestedGroup_When_ParentGroupHasNoOwnFields()
    {
        // arrange
        using var gateway = await CreateUserGatewayAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "outer") {
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
        // Execution fails with "The given key '0' was not present in the dictionary" in
        // OperationPlanExecutor.ExecuteWithDeferAsync. Once fixed, snapshot the request counts per
        // subgraph.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_AnchorSharedData_At_CompletingGroup_When_SecondSiblingCompletesFirst()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithAddressAfterEmailAsync();

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    name
                    ... @defer(label: "first") {
                        email
                        address
                    }
                    ... @defer(label: "second") {
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
        // Both groups share the path, so the shared email is anchored at the group this payload completes.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotAnnounceNestedGroup_When_ParentGroupFails()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithEmailAsync(emailFails: true, ResponseGate.None);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
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
        // The failed outer group completes with the error and the inner group is never announced.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotAnnounceNestedGroups_When_SharedPlanOfSiblingGroupsFails()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithEmailAsync(emailFails: true, ResponseGate.None);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
                    name
                    ... @defer(label: "contact") {
                        email
                        ... @defer(label: "contactAddress") {
                            address
                        }
                    }
                    ... @defer(label: "location") {
                        email
                        ... @defer(label: "locationPhone") {
                            phone
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
        // Both sibling groups share the failed plan and complete with the error. Neither nested group is announced.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_CompleteSharedGroupWithErrors_When_FailingPlanFinishesFirst()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithEmailAsync(
            emailFails: true,
            ResponseGate.AddressUntilErrorDelivered);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
                    name
                    ... @defer(label: "first") {
                        email
                    }
                    ... @defer(label: "second") {
                        email
                        address
                        ... @defer(label: "nested") {
                            phone
                        }
                    }
                    ... @defer(label: "third") {
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
        // The failed plan finishes before the address plan. The shared group completes with the error, the address
        // is delivered under the third group, and the nested group is never announced.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_NotStartNestedGroup_When_SharedParentGroupFailsInSecondPlan()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithEmailAsync(
            emailFails: true,
            ResponseGate.EmailUntilAddressDelivered);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
                    name
                    ... @defer(label: "first") {
                        email
                    }
                    ... @defer(label: "second") {
                        email
                        address
                        ... @defer(label: "nested") {
                            phone
                        }
                    }
                    ... @defer(label: "third") {
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
        // The second group completes with the email plan errors and the nested group is never announced.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
        Assert.Equal(0, CountSourceSchemaRequests(gateway, "B", "phone"));

        var payloads = await ReadPayloadsAsync(result);
        payloads.MatchInlineSnapshots(
        [
            """
            {"data":{"user":{"id":"VXNlcjoyMDkyMTcyNjc2","name":"User: VXNlcjoyMDkyMTcyNjc2"}},"pending":[{"id":"0","path":["user"],"label":"first"},{"id":"1","path":["user"],"label":"second"},{"id":"3","path":["user"],"label":"third"}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"3","data":{"address":"User: VXNlcjoyMDkyMTcyNjc2"}}],"completed":[{"id":"3"}],"hasNext":true}
            """,
            """
            {"completed":[{"id":"0","errors":[{"message":"Unexpected Execution Error","path":["user"]},{"message":"Unexpected Execution Error","path":["user","email"]}]},{"id":"1","errors":[{"message":"Unexpected Execution Error","path":["user"]},{"message":"Unexpected Execution Error","path":["user","email"]}]}],"hasNext":false}
            """
        ]);
    }

    [Fact]
    public async Task Defer_Should_AnnounceNestedGroup_When_SharedParentGroupCompletesInSecondPlan()
    {
        // arrange
        using var gateway = await CreateUserGatewayWithEmailAsync(
            emailFails: false,
            ResponseGate.EmailUntilAddressDelivered);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            query {
                user(id: "1") {
                    id
                    name
                    ... @defer(label: "first") {
                        email
                    }
                    ... @defer(label: "second") {
                        email
                        address
                        ... @defer(label: "nested") {
                            phone
                        }
                    }
                    ... @defer(label: "third") {
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
        // The nested group is announced in the payload that completes the second group.
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);

        var payloads = await ReadPayloadsAsync(result);
        payloads.MatchInlineSnapshots(
        [
            """
            {"data":{"user":{"id":"VXNlcjoyMDkyMTcyNjc2","name":"User: VXNlcjoyMDkyMTcyNjc2"}},"pending":[{"id":"0","path":["user"],"label":"first"},{"id":"1","path":["user"],"label":"second"},{"id":"3","path":["user"],"label":"third"}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"3","data":{"address":"User: VXNlcjoyMDkyMTcyNjc2"}}],"completed":[{"id":"3"}],"hasNext":true}
            """,
            """
            {"pending":[{"id":"2","path":["user"],"label":"nested"}],"incremental":[{"id":"0","data":{"email":"User: VXNlcjoyMDkyMTcyNjc2"}}],"completed":[{"id":"0"},{"id":"1"}],"hasNext":true}
            """,
            """
            {"incremental":[{"id":"2","data":{"phone":"User: VXNlcjoyMDkyMTcyNjc2"}}],"completed":[{"id":"2"}],"hasNext":false}
            """
        ]);
    }

    private async Task<Gateway> CreateUserGatewayAsync()
    {
        var server1 = CreateSourceSchema(
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

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
                phone: String!
            }
            """);

        return await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);
    }

    private async Task<Gateway> CreateUserGatewayWithEmailAsync(bool emailFails, ResponseGate gate)
    {
        // The key lookup response of the deferred plan is held back until the email node has handed its
        // result to the result store, so the email result is always applied first.
        var emailNodeCompleted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var addressDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpMessageInvoker? invokerA = null;
        HttpMessageInvoker? invokerB = null;

        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                user(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                name: String!
            }
            """,
            mockHttpResponse: async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();

                if (body.Contains("Op_defer", StringComparison.Ordinal))
                {
                    await emailNodeCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }

                return await invokerA!.SendAsync(request, CancellationToken.None);
            });

        var server2 = CreateSourceSchema(
            "B",
            $$"""
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!{{(emailFails ? " @error" : "")}}
                address: String!
                phone: String!
            }
            """,
            mockHttpResponse: async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();

                if (body.Contains("address", StringComparison.Ordinal))
                {
                    if (gate is ResponseGate.AddressUntilErrorDelivered)
                    {
                        await errorDelivered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                    }
                }
                else if (body.Contains("email", StringComparison.Ordinal))
                {
                    if (gate is ResponseGate.EmailUntilAddressDelivered)
                    {
                        await addressDelivered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                    }
                }

                return await invokerB!.SendAsync(request, CancellationToken.None);
            });

        invokerA = new HttpMessageInvoker(server1.CreateHandler());
        invokerB = new HttpMessageInvoker(server2.CreateHandler());

        return await CreateCompositeSchemaAsync(
            [
                ("A", server1),
                ("B", server2)
            ],
            configureApplication: app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Body = new SignalOnWriteStream(
                        new SignalOnWriteStream(
                            context.Response.Body,
                            "\"errors\""u8.ToArray(),
                            errorDelivered),
                        "\"address\""u8.ToArray(),
                        addressDelivered);
                    await next(context);
                });
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            },
            configureGatewayBuilder: b => b.AddDiagnosticEventListener(
                _ => new NodeCompletedListener("B", "email", emailNodeCompleted)));
    }

    private static int CountSourceSchemaRequests(Gateway gateway, string schemaName, string text)
    {
        if (!gateway.Interactions.TryGetValue(schemaName, out var interactions))
        {
            return 0;
        }

        return interactions.Values.Count(i =>
            i.Request is not null
            && Encoding.UTF8.GetString(i.Request.Body.ToArray()).Contains(text, StringComparison.Ordinal));
    }

    private async Task<Gateway> CreateUserGatewayWithAddressAfterEmailAsync()
    {
        // The address response is held back until the gateway has written the payload that carries the email
        // data to the client. This fixes the order in which sibling groups complete.
        var emailDelivered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpMessageInvoker? invoker = null;

        var server1 = CreateSourceSchema(
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

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
                phone: String!
            }
            """,
            mockHttpResponse: async request =>
            {
                var body = await request.Content!.ReadAsStringAsync();

                if (body.Contains("address", StringComparison.Ordinal))
                {
                    await emailDelivered.Task.WaitAsync(TimeSpan.FromSeconds(30));
                }

                return await invoker!.SendAsync(request, CancellationToken.None);
            });

        invoker = new HttpMessageInvoker(server2.CreateHandler());

        return await CreateCompositeSchemaAsync(
            [
                ("A", server1),
                ("B", server2)
            ],
            configureApplication: app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Response.Body = new SignalOnWriteStream(
                        context.Response.Body,
                        "\"email\""u8.ToArray(),
                        emailDelivered);
                    await next(context);
                });
                app.UseRouting();
                app.UseEndpoints(endpoint => endpoint.MapGraphQL());
            });
    }

    private Task<Gateway> CreateUserAccountGatewayAsync()
        => CreateUserAccountGatewayAsync(null, null);

    private async Task<Gateway> CreateUserAccountGatewayAsync(
        SequencedSubgraph? subgraphB,
        SequencedSubgraph? subgraphC)
    {
        var server1 = CreateSourceSchema(
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

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
                userById(id: ID!): User @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                email: String!
                address: String!
                account: Account!
            }

            type Account @key(fields: "id") {
                id: ID!
            }
            """,
            mockHttpResponse: subgraphB is null ? null : subgraphB.SendAsync);

        subgraphB?.Connect(server2);

        var server3 = CreateSourceSchema(
            "C",
            """
            type Query {
                scoredUserById(id: ID!): User @lookup
                accountById(id: ID!): Account @lookup
            }

            type User @key(fields: "id") {
                id: ID!
                score: Int!
            }

            type Account @key(fields: "id") {
                id: ID!
                balance: Int!
            }
            """,
            mockHttpResponse: subgraphC is null ? null : subgraphC.SendAsync);

        subgraphC?.Connect(server3);

        return await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2),
            ("C", server3)
        ]);
    }

    private static async Task<List<string>> ReadPayloadsAsync(GraphQLHttpResponse response)
    {
        var body = await response.HttpResponseMessage.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        var payloads = new List<string>();

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var payload = JsonNode.Parse(line)!.AsObject();
            payload.Remove("extensions");
            payloads.Add(payload.ToJsonString());
        }

        return payloads;
    }

    private static async Task<string> GetRequestCountsAsync(Gateway gateway, GraphQLHttpResponse response)
    {
        await response.HttpResponseMessage.Content.LoadIntoBufferAsync();

        return string.Join(
            ", ",
            gateway.Interactions
                .OrderBy(t => t.Key, StringComparer.Ordinal)
                .Select(t => $"{t.Key}: {t.Value.Count}"));
    }

    private sealed class SignalOnWriteStream(
        Stream inner,
        byte[] marker,
        TaskCompletionSource signal) : Stream
    {
        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
            => Write(buffer.AsSpan(offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            inner.Write(buffer);
            Notify(buffer);
        }

        public override async ValueTask WriteAsync(
            ReadOnlyMemory<byte> buffer,
            CancellationToken cancellationToken = default)
        {
            await inner.WriteAsync(buffer, cancellationToken);
            Notify(buffer.Span);
        }

        private void Notify(ReadOnlySpan<byte> buffer)
        {
            if (buffer.IndexOf(marker) >= 0)
            {
                signal.TrySetResult();
            }
        }
    }

    private sealed class NodeCompletedListener(
        string expectedSchemaName,
        string operationText,
        TaskCompletionSource signal) : FusionExecutionDiagnosticEventListener
    {
        public override IDisposable ExecuteOperationNode(
            OperationPlanContext context,
            OperationExecutionNode node,
            string schemaName)
        {
            if (schemaName == expectedSchemaName
                && Encoding.UTF8.GetString(node.Operation.Value.Span).Contains(operationText, StringComparison.Ordinal))
            {
                return new SignalOnDispose(signal);
            }

            return base.ExecuteOperationNode(context, node, schemaName);
        }

        private sealed class SignalOnDispose(TaskCompletionSource signal) : IDisposable
        {
            public void Dispose() => signal.TrySetResult();
        }
    }

    private enum ResponseGate
    {
        None,
        AddressUntilErrorDelivered,
        EmailUntilAddressDelivered
    }

    private sealed class SubgraphRequestSequence
    {
        private static readonly TimeSpan s_timeout = TimeSpan.FromSeconds(30);

        private readonly string[] _fields;
        private readonly TaskCompletionSource[] _delivered;

        public SubgraphRequestSequence(params string[] fields)
        {
            _fields = fields;
            _delivered = new TaskCompletionSource[fields.Length];

            for (var i = 0; i < _delivered.Length; i++)
            {
                _delivered[i] = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            }
        }

        public int IndexOf(string requestBody)
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                if (requestBody.Contains(_fields[i], StringComparison.Ordinal))
                {
                    return i;
                }
            }

            throw new InvalidOperationException(
                $"The subgraph request matches none of the sequence fields ({string.Join(", ", _fields)}): "
                + requestBody);
        }

        public async Task WaitForTurnAsync(int index, CancellationToken cancellationToken)
        {
            if (index == 0)
            {
                return;
            }

            try
            {
                await _delivered[index - 1].Task.WaitAsync(s_timeout, cancellationToken);
            }
            catch (TimeoutException)
            {
                throw new TimeoutException(
                    $"The client did not receive the payload of '{_fields[index - 1]}' within {s_timeout}, "
                    + $"so the subgraph request for '{_fields[index]}' was never released.");
            }
        }

        public async Task ReleaseAsPayloadsArriveAsync(
            GraphQLHttpResponse response,
            CancellationToken cancellationToken)
        {
            var content = response.HttpResponseMessage.Content;
            var body = new StringBuilder();

            await using (var stream = await content.ReadAsStreamAsync(cancellationToken))
            {
                using var reader = new StreamReader(stream);

                while (await reader.ReadLineAsync(cancellationToken) is { } payload)
                {
                    body.Append(payload).Append('\n');
                    MarkDelivered(payload);
                }
            }

            var buffered = new ByteArrayContent(Encoding.UTF8.GetBytes(body.ToString()));

            foreach (var header in content.Headers)
            {
                buffered.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            response.HttpResponseMessage.Content = buffered;
            content.Dispose();
        }

        private void MarkDelivered(string payload)
        {
            for (var i = 0; i < _fields.Length; i++)
            {
                if (payload.Contains($"\"{_fields[i]}\":", StringComparison.Ordinal))
                {
                    _delivered[i].TrySetResult();
                }
            }
        }
    }

    private sealed class SequencedSubgraph(SubgraphRequestSequence sequence)
    {
        private HttpMessageInvoker? _invoker;

        public void Connect(TestServer server)
            => _invoker = new HttpMessageInvoker(server.CreateHandler());

        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
        {
            var cancellationToken = TestContext.Current.CancellationToken;
            var body = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            await sequence.WaitForTurnAsync(sequence.IndexOf(body), cancellationToken);

            return await _invoker!.SendAsync(request, cancellationToken);
        }
    }

    [Fact]
    public async Task Defer_RootFragment_Should_DeliverDeferredFetch_When_RootSelectsNothingElse()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                users: [User!]!
            }

            type User {
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
                ... @defer {
                    users {
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
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_RootFragment_Should_DeliverDeferredFetch_When_RootSelectsOnlyTypename()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
                users: [User!]!
            }

            type User {
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
                __typename
                ... @defer {
                    users {
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
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    [Fact]
    public async Task Defer_Should_ResolveAllPrerequisites_When_RequirementMixesNativeAndComputedFields()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<MixedRequirementSourceA.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<MixedRequirementSourceB.Query>());

        using var server3 = CreateSourceSchema(
            "C",
            b => b.AddQueryType<MixedRequirementSourceC.Query>());

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
            {
                product {
                    id
                    ... @defer {
                        total
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
    public async Task Defer_Should_ResolveAllPrerequisites_When_RequirementSelectsNestedFieldWithOwnRequirement()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<NestedRequirementSourceA.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NestedRequirementSourceB.Query>());

        using var server3 = CreateSourceSchema(
            "C",
            b => b.AddQueryType<NestedRequirementSourceC.Query>());

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
            {
                product {
                    id
                    ... @defer {
                        total
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
    public async Task Defer_Should_ResolveNestedLookupInDeferredPayload_When_DeferredFieldTraversesSubgraphs()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "A",
            b => b.AddQueryType<NestedLookupSourceA.Query>());

        using var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NestedLookupSourceB.Query>());

        using var server3 = CreateSourceSchema(
            "C",
            b => b.AddQueryType<NestedLookupSourceC.Query>());

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
            {
                products {
                    name
                    ... @defer(label: "deeper") {
                        related {
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

        // assert
        await MatchSnapshotAsync(gateway, request, result, stableStream: true);
    }

    private static class NestedLookupSourceA
    {
        public class Query
        {
            public Product[] GetProducts() => [new(1), new(2)];

            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id);
    }

    private static class NestedLookupSourceB
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public Product Related => new(Id + 10);
        }
    }

    private static class NestedLookupSourceC
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public string Name => $"Product {Id}";
        }
    }

    private static class MixedRequirementSourceA
    {
        public class Query
        {
            public Product GetProduct() => new(1);

            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public int Raw => 7;

            public int GetComputed([Require("value")] int value) => value * 10;
        }
    }

    private static class MixedRequirementSourceB
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public int Value => 3;
        }
    }

    private static class MixedRequirementSourceC
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public int GetTotal([Require("{ raw computed }")] TotalInput input)
                => input.Raw + input.Computed;
        }

        public record TotalInput(int Raw, int Computed);
    }

    private static class NestedRequirementSourceA
    {
        public class Query
        {
            public Product GetProduct() => new(1);

            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);

            [Lookup]
            [Internal]
            public User? GetUserById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public User Owner => new(2);
        }

        public record User([property: ID] int Id)
        {
            public int Raw => 5;

            public int GetComputed([Require("value")] int value) => value * 10;
        }
    }

    private static class NestedRequirementSourceB
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public User? GetUserById([ID] int id) => new(id);
        }

        public record User([property: ID] int Id)
        {
            public int Value => 4;
        }
    }

    private static class NestedRequirementSourceC
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById([ID] int id) => new(id);
        }

        public record Product([property: ID] int Id)
        {
            public int GetTotal([Require("owner.{ raw computed }")] OwnerInput input)
                => input.Raw * 1000 + input.Computed;
        }

        public record OwnerInput(int Raw, int Computed);
    }
}

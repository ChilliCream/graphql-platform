using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloExternalTransitionTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ResolveFieldThroughKey_When_FieldAboveIsExternalInRequiringSubgraph()
    {
        // arrange
        using var a = CreateSourceSchema(
            "a",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedNestedTypeA.Query>(d => d.Name("Query"))
                .AddType<KeyedNestedTypeA.T>());
        using var b = CreateSourceSchema(
            "b",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedNestedTypeB.Query>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [("a", a), ("b", b)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              e {
                r
                next {
                  x
                  y
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
        await MatchSnapshotAsync(gateway, request, result);
    }

    // a computes r from the x of next, which it only receives through the representation, and
    // resolves y of T by its key.
    public static class KeyedNestedTypeA
    {
        public sealed class Query
        {
            [Shareable]
            public E GetE() => new() { Id = "e-a" };
        }

        [Key("id")]
        [Shareable]
        public sealed class E
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public T? Next { get; set; }

            [Requires("next { x }")]
            public string GetR() => $"r:{Next?.X}";

            [ReferenceResolver]
            public static E ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        [Shareable]
        public sealed class T
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public string? X { get; set; }

            public string GetY() => $"y-{Id}";

            [ReferenceResolver]
            public static T ResolveReference(string id) => new() { Id = id };
        }
    }

    // b serves e with its next T, without keys.
    public static class KeyedNestedTypeB
    {
        public sealed class Query
        {
            [Shareable]
            public E GetE() => new() { Id = "e1", Next = new T { Id = "t1", X = "x1" } };
        }

        [Shareable]
        public sealed class E
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public T? Next { get; init; }
        }

        [Shareable]
        public sealed class T
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public string? X { get; init; }
        }
    }
}

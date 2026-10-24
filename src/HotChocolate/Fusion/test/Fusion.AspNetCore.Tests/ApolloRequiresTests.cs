using System.Globalization;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class ApolloRequiresTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_Resolve_Root_Name_When_Requires_Selects_External_Self_Reference()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                name
              }
            }
            """);

        // act
        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "entity": {
                  "name": "1:20"
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Nested_Name_When_Requires_Selects_External_Self_Reference()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                entity {
                  name
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

    [Fact]
    public async Task Execute_Should_Resolve_Deeply_Nested_Name_When_Requires_Selects_External_Self_Reference()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                entity {
                  entity {
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
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "entity": {
                  "entity": {
                    "entity": {
                      "name": "3:40"
                    }
                  }
                }
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_Resolve_Name_At_Every_Depth_When_Requires_Selects_External_Self_Reference()
    {
        // arrange
        using var gateway = await CreateSelfReferenceGatewayAsync(includeOperationPlan: true);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              entity {
                name
                entity {
                  name
                  entity {
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
        await MatchSnapshotAsync(gateway, request, result);
    }

    private Task<Gateway> CreateSelfReferenceGatewayAsync(bool includeOperationPlan)
    {
        var schemaA = CreateSourceSchema(
            "a",
            builder => builder.AddApolloFederation().AddQueryType<SelfReferenceRequiresA.Query>());
        var schemaB = CreateSourceSchema(
            "b",
            builder => builder.AddApolloFederation().AddQueryType<SelfReferenceRequiresB.Query>());

        return CreateCompositeSchemaAsync(
            [("a", schemaA), ("b", schemaB)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: includeOperationPlan);
    }

    // a resolves name from its own id and the age of the entity it references, which it only
    // receives through the representation.
    public static class SelfReferenceRequiresA
    {
        public sealed class Query
        {
            [Shareable]
            public RequiringEntity GetEntity() => new() { Id = "1" };
        }

        [GraphQLName("Entity")]
        [Key("id")]
        [Shareable]
        public sealed class RequiringEntity
        {
            [GraphQLType(typeof(IdType))]
            public string? Id { get; set; }

            [External]
            public int Age { get; set; }

            [External]
            public RequiringEntity Entity { get; set; } = default!;

            [Requires("entity { age }")]
            public string GetName() => $"{Id}:{Entity.Age}";

            [ReferenceResolver]
            public static RequiringEntity ResolveReference(string id) => new() { Id = id };
        }
    }

    // b serves the chain 1 -> 2 -> 3 -> 4, where each entity has the age id * 10.
    public static class SelfReferenceRequiresB
    {
        public sealed class Query
        {
            [Shareable]
            public Entity? GetEntity() => new(1);
        }

        [Shareable]
        public sealed class Entity(int id)
        {
            [GraphQLType(typeof(IdType))]
            public string? Id => id.ToString(CultureInfo.InvariantCulture);

            public int Age => id * 10;

            public Entity GetEntity() => new(id + 1);
        }
    }
}

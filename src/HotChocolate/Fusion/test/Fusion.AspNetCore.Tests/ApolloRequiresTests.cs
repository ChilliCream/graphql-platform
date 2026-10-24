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

    [Fact]
    public async Task Execute_Should_ResolveEstimateFromSecondProvider_When_SecondProviderAlsoRequiresData()
    {
        // arrange
        var req = CreateSourceSchema(
            "req",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderReq.Query>(d => d.Name("Query")));
        var sup = CreateSourceSchema(
            "sup",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderSup.Query>(d => d.Name("Query"))
                .AddType<SecondProviderSup.SupProduct>());
        var alt = CreateSourceSchema(
            "alt",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderRequiringAlt.Query>(d => d.Name("Query"))
                .AddType<SecondProviderRequiringAlt.AltProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("req", req), ("sup", sup), ("alt", alt)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                  products {
                    id
                    estimate
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "estimate": 31
                  },
                  {
                    "id": "B",
                    "estimate": 61
                  },
                  {
                    "id": "C",
                    "estimate": 91
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveEstimateAndQuoteFromSecondProvider_When_SecondProviderServesTwoRequiringFields()
    {
        // arrange
        var req = CreateSourceSchema(
            "req",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderResolvableReq.Query>(d => d.Name("Query"))
                .AddType<SecondProviderResolvableReq.ReqProduct>());
        var sup = CreateSourceSchema(
            "sup",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderSup.Query>(d => d.Name("Query"))
                .AddType<SecondProviderSup.SupProduct>());
        var alt = CreateSourceSchema(
            "alt",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderQuotingAlt.Query>(d => d.Name("Query"))
                .AddType<SecondProviderQuotingAlt.AltProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("req", req), ("sup", sup), ("alt", alt)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                  products {
                    id
                    estimate
                    quote
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "estimate": 20,
                    "quote": 1010
                  },
                  {
                    "id": "B",
                    "estimate": 40,
                    "quote": 1020
                  },
                  {
                    "id": "C",
                    "estimate": 60,
                    "quote": 1030
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveFieldsFromOtherProviders_When_RequiringSchemaKeyIsNotResolvable()
    {
        // arrange
        var req = CreateSourceSchema(
            "req",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<OtherProvidersReq.Query>(d => d.Name("Query")));
        var sup = CreateSourceSchema(
            "sup",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderSup.Query>(d => d.Name("Query"))
                .AddType<SecondProviderSup.SupProduct>());
        var alt = CreateSourceSchema(
            "alt",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<OtherProvidersAlt.Query>(d => d.Name("Query"))
                .AddType<OtherProvidersAlt.AltProduct>());
        var far = CreateSourceSchema(
            "far",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<OtherProvidersFar.Query>(d => d.Name("Query"))
                .AddType<OtherProvidersFar.FarProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("req", req), ("sup", sup), ("alt", alt), ("far", far)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                  products {
                    id
                    estimate
                    quote
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "estimate": 110,
                    "quote": 5010
                  },
                  {
                    "id": "B",
                    "estimate": 120,
                    "quote": 5020
                  },
                  {
                    "id": "C",
                    "estimate": 130,
                    "quote": 5030
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveFieldFromSecondProvider_When_RequiringSchemaAlsoHoldsResolvableEntity()
    {
        // arrange
        var req = CreateSourceSchema(
            "req",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ResolvableEntityReq.Query>(d => d.Name("Query"))
                .AddType<ResolvableEntityReq.ReqStore>());
        var sup = CreateSourceSchema(
            "sup",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ResolvableEntitySup.Query>(d => d.Name("Query"))
                .AddType<ResolvableEntitySup.SupProduct>());
        var alt = CreateSourceSchema(
            "alt",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<OtherProvidersAlt.Query>(d => d.Name("Query"))
                .AddType<OtherProvidersAlt.AltProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("req", req), ("sup", sup), ("alt", alt)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
            new OperationRequest(
                """
                {
                  products {
                    id
                    estimate
                    store {
                      name
                    }
                  }
                }
                """),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "A",
                    "estimate": 110,
                    "store": {
                      "name": "North"
                    }
                  },
                  {
                    "id": "B",
                    "estimate": 120,
                    "store": {
                      "name": "South"
                    }
                  },
                  {
                    "id": "C",
                    "estimate": 130,
                    "store": {
                      "name": "North"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveEstimateFromSecondProvider_When_RequiringSchemaKeyIsNotResolvable()
    {
        // arrange
        var req = CreateSourceSchema(
            "req",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderReq.Query>(d => d.Name("Query")));
        var sup = CreateSourceSchema(
            "sup",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderSup.Query>(d => d.Name("Query"))
                .AddType<SecondProviderSup.SupProduct>());
        var alt = CreateSourceSchema(
            "alt",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<SecondProviderAlt.Query>(d => d.Name("Query"))
                .AddType<SecondProviderAlt.AltProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("req", req), ("sup", sup), ("alt", alt)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              products {
                id
                estimate
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

    // The products A, B and C with their weights, shared by the second provider sources.
    private static readonly (string Id, string Name, int Weight)[] s_secondProviderRows =
    [
        ("A", "Amber", 10),
        ("B", "Blue", 20),
        ("C", "Coral", 30)
    ];

    // req lists the products and computes estimate as twice the weight it receives, under a key
    // that is not resolvable.
    public static class SecondProviderReq
    {
        public sealed class Query
        {
            public IEnumerable<ReqProduct?> GetProducts()
                => s_secondProviderRows.Select(row => new ReqProduct { Id = row.Id });
        }

        [GraphQLName("Product")]
        [Key("id", resolvable: false)]
        public sealed class ReqProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public int? Weight { get; set; }

            [Shareable]
            [Requires("weight")]
            public int? GetEstimate()
                => Weight * 2 ?? throw new GraphQLException($"product {Id} reached estimate without a weight");
        }
    }

    // sup holds the name and the weight of each product.
    public static class SecondProviderSup
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class SupProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public string? Name { get; init; }

            public int? Weight { get; init; }

            [ReferenceResolver]
            public static SupProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new SupProduct { Id = row.Id, Name = row.Name, Weight = row.Weight })
                    .FirstOrDefault();
        }
    }

    // alt computes estimate as three times the weight and one more, from the weight it receives.
    public static class SecondProviderRequiringAlt
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class AltProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public int? Weight { get; set; }

            [Shareable]
            [Requires("weight")]
            public int? GetEstimate()
                => Weight * 3 + 1 ?? throw new GraphQLException($"product {Id} reached estimate without a weight");

            [ReferenceResolver]
            public static AltProduct ResolveReference(string id) => new() { Id = id };
        }
    }

    // req lists the products under a resolvable key and computes estimate and quote from the
    // weight it receives.
    public static class SecondProviderResolvableReq
    {
        public sealed class Query
        {
            public IEnumerable<ReqProduct?> GetProducts()
                => s_secondProviderRows.Select(row => new ReqProduct { Id = row.Id });
        }

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class ReqProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public int? Weight { get; set; }

            [Shareable]
            [Requires("weight")]
            public int? GetEstimate()
                => Weight * 5 ?? throw new GraphQLException($"product {Id} reached estimate without a weight");

            [Shareable]
            [Requires("weight")]
            public int? GetQuote()
                => Weight * 7 ?? throw new GraphQLException($"product {Id} reached quote without a weight");

            [ReferenceResolver]
            public static ReqProduct ResolveReference(string id) => new() { Id = id };
        }
    }

    // alt serves estimate and quote from its own data.
    public static class SecondProviderQuotingAlt
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class AltProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [Shareable]
            public int? Estimate { get; init; }

            [Shareable]
            public int? Quote { get; init; }

            [ReferenceResolver]
            public static AltProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new AltProduct { Id = row.Id, Estimate = row.Weight * 2, Quote = 1000 + row.Weight })
                    .FirstOrDefault();
        }
    }

    // req lists the products under a key that is not resolvable and computes estimate as twice and
    // quote as three times the weight it receives.
    public static class OtherProvidersReq
    {
        public sealed class Query
        {
            public IEnumerable<ReqProduct?> GetProducts()
                => s_secondProviderRows.Select(row => new ReqProduct { Id = row.Id });
        }

        [GraphQLName("Product")]
        [Key("id", resolvable: false)]
        public sealed class ReqProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public int? Weight { get; set; }

            [Shareable]
            [Requires("weight")]
            public int? GetEstimate()
                => Weight * 2 ?? throw new GraphQLException($"product {Id} reached estimate without a weight");

            [Shareable]
            [Requires("weight")]
            public int? GetQuote()
                => Weight * 3 ?? throw new GraphQLException($"product {Id} reached quote without a weight");
        }
    }

    // alt serves estimate as the weight plus 100 from its own data.
    public static class OtherProvidersAlt
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class AltProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [Shareable]
            public int? Estimate { get; init; }

            [ReferenceResolver]
            public static AltProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new AltProduct { Id = row.Id, Estimate = 100 + row.Weight })
                    .FirstOrDefault();
        }
    }

    // far serves quote as the weight plus 5000 from its own data.
    public static class OtherProvidersFar
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class FarProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [Shareable]
            public int? Quote { get; init; }

            [ReferenceResolver]
            public static FarProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new FarProduct { Id = row.Id, Quote = 5000 + row.Weight })
                    .FirstOrDefault();
        }
    }

    // The stores S1 and S2 with their names, and the store of each product A, B and C.
    private static readonly (string Id, string Name)[] s_storeRows =
    [
        ("S1", "North"),
        ("S2", "South")
    ];

    private static readonly (string ProductId, string StoreId)[] s_productStoreRows =
    [
        ("A", "S1"),
        ("B", "S2"),
        ("C", "S1")
    ];

    // req lists the products under a key that is not resolvable, computes estimate as twice the
    // weight it receives and resolves the name of each store by its id.
    public static class ResolvableEntityReq
    {
        public sealed class Query
        {
            public IEnumerable<ReqProduct?> GetProducts()
                => s_secondProviderRows.Select(row => new ReqProduct { Id = row.Id });
        }

        [GraphQLName("Product")]
        [Key("id", resolvable: false)]
        public sealed class ReqProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [External]
            public int? Weight { get; set; }

            [Shareable]
            [Requires("weight")]
            public int? GetEstimate()
                => Weight * 2 ?? throw new GraphQLException($"product {Id} reached estimate without a weight");
        }

        [GraphQLName("Store")]
        [Key("id")]
        public sealed class ReqStore
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public string? Name { get; init; }

            [ReferenceResolver]
            public static ReqStore? ResolveReference(string id)
                => s_storeRows
                    .Where(row => row.Id == id)
                    .Select(row => new ReqStore { Id = row.Id, Name = row.Name })
                    .FirstOrDefault();
        }
    }

    // sup holds the name, the weight and the store of each product, under a store key that is not
    // resolvable.
    public static class ResolvableEntitySup
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class SupProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public string? Name { get; init; }

            public int? Weight { get; init; }

            public SupStore? Store { get; init; }

            [ReferenceResolver]
            public static SupProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new SupProduct
                    {
                        Id = row.Id,
                        Name = row.Name,
                        Weight = row.Weight,
                        Store = new SupStore { Id = s_productStoreRows.Single(s => s.ProductId == row.Id).StoreId }
                    })
                    .FirstOrDefault();
        }

        [GraphQLName("Store")]
        [Key("id", resolvable: false)]
        public sealed class SupStore
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }
        }
    }

    // alt serves estimate from its own data.
    public static class SecondProviderAlt
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class AltProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [Shareable]
            public int? Estimate { get; init; }

            [ReferenceResolver]
            public static AltProduct? ResolveReference(string id)
                => s_secondProviderRows
                    .Where(row => row.Id == id)
                    .Select(row => new AltProduct { Id = row.Id, Estimate = row.Weight * 2 })
                    .FirstOrDefault();
        }
    }
}

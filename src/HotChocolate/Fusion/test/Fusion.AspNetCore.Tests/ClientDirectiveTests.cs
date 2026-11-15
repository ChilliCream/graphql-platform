using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class ClientDirectiveTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ForwardClientDirective_When_EverySourceDefinesIt()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            """
            directive @marker(note: String!) on FIELD

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);

        using var pricing = CreateSourceSchema(
            "pricing",
            """
            directive @marker(note: String!) on FIELD

            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("catalog", catalog),
            ("pricing", pricing)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              products @marker(note: "root") {
                id
                name
                price @marker(note: "entity")
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
    public async Task Execute_Should_RejectClientDirective_When_ASourceDoesNotDefineIt()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            """
            directive @marker(note: String!) on FIELD

            type Query {
              products: [Product]
            }

            type Product @key(fields: "id") {
              id: ID!
              name: String
            }
            """);

        using var pricing = CreateSourceSchema(
            "pricing",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product @key(fields: "id") {
              id: ID!
              price: Int
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("catalog", catalog),
            ("pricing", pricing)
        ]);

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              products @marker(note: "root") {
                id
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
              "errors": [
                {
                  "message": "The specified directive `marker` is not supported by the current schema.",
                  "locations": [
                    {
                      "line": 2,
                      "column": 12
                    }
                  ],
                  "path": [
                    "products"
                  ],
                  "extensions": {
                    "specifiedBy": "https://spec.graphql.org/September2025/#sec-Directives-Are-Defined"
                  }
                }
              ]
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ForwardClientDirective_When_EveryApolloSubgraphDefinesIt()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            builder => builder
                .AddApolloFederation()
                .AddDirectiveType<MarkerDirectiveType>()
                .AddQueryType<ApolloCatalog.Query>(d => d.Name("Query")));
        using var pricing = CreateSourceSchema(
            "pricing",
            builder => builder
                .AddApolloFederation()
                .AddDirectiveType<MarkerDirectiveType>()
                .AddQueryType<ApolloPricing.Query>(d => d.Name("Query"))
                .AddType<ApolloPricing.PricedProduct>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("pricing", pricing)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              products @marker(note: "root") {
                id
                name
                price @marker(note: "entity")
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

    // directive @marker(note: String!) on FIELD, which no resolver reads.
    private sealed class MarkerDirectiveType : DirectiveType
    {
        protected override void Configure(IDirectiveTypeDescriptor descriptor)
        {
            descriptor.Name("marker");
            descriptor.Location(DirectiveLocation.Field);
            descriptor.Argument("note").Type<NonNullType<StringType>>();
        }
    }

    // catalog lists the products with their names.
    public static class ApolloCatalog
    {
        public sealed class Query
        {
            public IEnumerable<CatalogProduct> GetProducts()
                => [new CatalogProduct { Id = "A", Name = "Amber" }, new CatalogProduct { Id = "B", Name = "Blue" }];
        }

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class CatalogProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public string? Name { get; init; }

            [ReferenceResolver]
            public static CatalogProduct ResolveReference(string id) => new() { Id = id, Name = $"name-{id}" };
        }
    }

    // pricing holds the price of each product.
    public static class ApolloPricing
    {
        public sealed class Query;

        [GraphQLName("Product")]
        [Key("id")]
        public sealed class PricedProduct
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int? Price { get; init; }

            [ReferenceResolver]
            public static PricedProduct ResolveReference(string id)
                => new() { Id = id, Price = id == "A" ? 15 : 25 };
        }
    }
}

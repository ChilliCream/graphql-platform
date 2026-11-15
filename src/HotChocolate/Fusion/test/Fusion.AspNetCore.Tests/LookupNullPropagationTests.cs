using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class LookupNullPropagationTests : FusionTestBase
{
    [Fact]
    public async Task Require_Should_SkipNulledEntity_When_EarlierLookupPropagatedNull()
    {
        // arrange
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<ProductsQuery>(d => d.Name("Query")));
        using var catalogApi = CreateSourceSchema(
            "catalog-api",
            builder => builder.AddQueryType<CatalogQuery>(d => d.Name("Query")));
        using var pricingApi = CreateSourceSchema(
            "pricing-api",
            builder => builder.AddQueryType<PricingQuery>(d => d.Name("Query")));
        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("products-api", productsApi),
            ("catalog-api", catalogApi),
            ("pricing-api", pricingApi)
        ]);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              products { id name price }
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

    public sealed class ProductsQuery
    {
        public List<ProductsProduct?> GetProducts()
            => [new() { Id = "1" }, new() { Id = "2" }, new() { Id = "3" }];

        [Lookup, Internal]
        public ProductsProduct? GetProductById([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("Product")]
    public sealed class ProductsProduct
    {
        [ID]
        public required string Id { get; init; }
    }

    public sealed class CatalogQuery
    {
        [Lookup]
        public CatalogProduct? GetProductById([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("Product")]
    public sealed class CatalogProduct
    {
        [ID]
        public required string Id { get; init; }

        public string GetName()
            => Id == "2"
                ? throw new GraphQLException("NAME_UNAVAILABLE")
                : $"Product {Id}";
    }

    public sealed class PricingQuery
    {
        [Lookup, Internal]
        public PricingProduct? GetProductById([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("Product")]
    public sealed class PricingProduct
    {
        [ID]
        public required string Id { get; init; }

        public int GetPrice([Require] string name)
            => name.Length * 100;
    }
}

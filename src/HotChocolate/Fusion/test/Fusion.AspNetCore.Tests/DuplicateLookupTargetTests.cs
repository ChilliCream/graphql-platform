using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class DuplicateLookupTargetTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Lookup_Should_CompleteEveryTarget_When_TargetsShareAnEntity()
    {
        // arrange
        using var listingsApi = CreateListingsApi();
        using var catalogApi = CreateCatalogApi();
        using var gateway = await CreateGatewayAsync(listingsApi, catalogApi);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              listings { id product { upc name price } }
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
    public async Task Execute_Lookup_Should_CompleteEveryTarget_When_TargetsAreAtDifferentPaths()
    {
        // arrange
        using var listingsApi = CreateListingsApi();
        using var catalogApi = CreateCatalogApi();
        using var gateway = await CreateGatewayAsync(listingsApi, catalogApi);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              listings { id product { name price } }
              featured { id product { name price } }
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
    public async Task Execute_Lookup_Should_MaskEnumValueOnEveryTarget_When_ValueIsInaccessible()
    {
        // arrange
        using var listingsApi = CreateListingsApi();
        using var catalogApi = CreateCatalogApi();
        using var gateway = await CreateGatewayAsync(listingsApi, catalogApi);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              listings { id product { name status } }
              featured { id product { name status } }
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
    public async Task Execute_Lookup_Should_CompleteEveryTarget_When_EntityHasNestedObject()
    {
        // arrange
        using var listingsApi = CreateListingsApi();
        using var catalogApi = CreateCatalogApi();
        using var gateway = await CreateGatewayAsync(listingsApi, catalogApi);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              listings { id product { upc dimensions { width height } } }
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
    public async Task Execute_Lookup_Should_ReportErrorOnEveryTarget_When_SharedEntityHasFieldError()
    {
        // arrange
        using var listingsApi = CreateListingsApi();
        using var catalogApi = CreateCatalogApi();
        using var gateway = await CreateGatewayAsync(listingsApi, catalogApi);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        var request = new OperationRequest(
            """
            {
              listings { id product { upc warranty } }
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

    private TestServer CreateListingsApi()
        => CreateSourceSchema(
            "listings-api",
            builder => builder.AddQueryType<ListingsQuery>(d => d.Name("Query")));

    private TestServer CreateCatalogApi()
        => CreateSourceSchema(
            "catalog-api",
            builder => builder.AddQueryType<CatalogQuery>(d => d.Name("Query")));

    private Task<Gateway> CreateGatewayAsync(TestServer listingsApi, TestServer catalogApi)
        => CreateCompositeSchemaAsync(
            [
                ("listings-api", listingsApi),
                ("catalog-api", catalogApi)
            ]);

    public sealed class ListingsQuery
    {
        private static readonly List<Listing> s_listings =
        [
            new() { Id = "1", ProductUpc = "1" },
            new() { Id = "2", ProductUpc = "1" },
            new() { Id = "3", ProductUpc = "2" },
            new() { Id = "4", ProductUpc = "3" },
            new() { Id = "5", ProductUpc = "1" },
            new() { Id = "6", ProductUpc = "3" }
        ];

        private static readonly List<Listing> s_featured =
        [
            new() { Id = "7", ProductUpc = "2" },
            new() { Id = "8", ProductUpc = "1" }
        ];

        public List<Listing> GetListings() => s_listings;

        public List<Listing> GetFeatured() => s_featured;

        [Lookup, Internal]
        public ListingProduct? GetProductByUpc([ID] string upc)
            => new() { Upc = upc };
    }

    public sealed class Listing
    {
        public required string Id { get; init; }

        public required string ProductUpc { get; init; }

        public ListingProduct GetProduct()
            => new() { Upc = ProductUpc };
    }

    [GraphQLName("Product")]
    public sealed class ListingProduct
    {
        [ID]
        public required string Upc { get; init; }
    }

    public sealed class CatalogQuery
    {
        private static readonly Dictionary<string, CatalogProduct> s_products = new()
        {
            ["1"] = new()
            {
                Upc = "1",
                Name = "Table",
                Price = 899,
                Status = ProductStatus.Available,
                Dimensions = new() { Width = 120, Height = 75 }
            },
            ["2"] = new()
            {
                Upc = "2",
                Name = "Couch",
                Price = 1299,
                Status = ProductStatus.Hidden,
                Dimensions = new() { Width = 210, Height = 90 }
            },
            ["3"] = new()
            {
                Upc = "3",
                Name = "Glass",
                Price = 15,
                Status = ProductStatus.Discontinued,
                Dimensions = new() { Width = 8, Height = 12 }
            }
        };

        [Lookup]
        public CatalogProduct? GetProductByUpc([ID] string upc)
            => s_products.GetValueOrDefault(upc);
    }

    [GraphQLName("Product")]
    public sealed class CatalogProduct
    {
        [ID]
        public required string Upc { get; init; }

        public required string Name { get; init; }

        public int Price { get; init; }

        public ProductStatus? Status { get; init; }

        public required Dimensions Dimensions { get; init; }

        public string? GetWarranty()
            => Upc == "3"
                ? throw new GraphQLException("WARRANTY_UNAVAILABLE")
                : "2 years";
    }

    public sealed class Dimensions
    {
        public int Width { get; init; }

        public int Height { get; init; }
    }

    public enum ProductStatus
    {
        Available,
        Discontinued,
        [Inaccessible] Hidden
    }
}

using System.Text.Json;
using HotChocolate.Transport.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class EntityReuseTests : FusionTestBase
{
    [Fact]
    public async Task Execute_K6Query_Should_ReturnSameData_When_EntityReuseIsEnabled()
    {
        // arrange
        using var accountsApi = CreateAccountsApi();
        using var inventoryApi = CreateInventoryApi();
        using var productsApi = CreateProductsApi();
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateGatewayAsync(accountsApi, inventoryApi, productsApi, reviewsApi);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            accountsApi,
            inventoryApi,
            productsApi,
            reviewsApi,
            enableEntityReuse: false);

        // act
        var data = await ExecuteAsync(gateway, K6ExecutionTests.K6Query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, K6ExecutionTests.K6Query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse)
        }.MatchInlineSnapshot(
            """
            {
              "WithReuse": "accounts-api: 1, inventory-api: 1, products-api: 1, reviews-api: 1",
              "WithoutReuse": "accounts-api: 2, inventory-api: 1, products-api: 2, reviews-api: 1"
            }
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_ReuseEntity_When_RootEntityHoldsSelectedFields()
    {
        // arrange
        using var accountsApi = CreateAccountsApi();
        using var inventoryApi = CreateInventoryApi();
        using var productsApi = CreateProductsApi();
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateGatewayAsync(accountsApi, inventoryApi, productsApi, reviewsApi);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            accountsApi,
            inventoryApi,
            productsApi,
            reviewsApi,
            enableEntityReuse: false);

        const string query =
            """
            {
              users { id name username reviews { author { name username } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "accounts-api: 1, reviews-api: 1",
            CountRequests(gateway));
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_RootEntityIsNotADependency()
    {
        // arrange
        using var accountsApi = CreateAccountsApi();
        using var inventoryApi = CreateInventoryApi();
        using var productsApi = CreateProductsApi();
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateGatewayAsync(accountsApi, inventoryApi, productsApi, reviewsApi);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            accountsApi,
            inventoryApi,
            productsApi,
            reviewsApi,
            enableEntityReuse: false);

        // The author lookup does not depend on the users fetch, so users may not be complete yet.
        const string query =
            """
            {
              users { id name username }
              topProducts(first: 2) { reviews { author { name username } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "accounts-api: 2, products-api: 1, reviews-api: 1",
            CountRequests(gateway));
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_RootEntityLacksSelectedField()
    {
        // arrange
        using var accountsApi = CreateAccountsApi();
        using var inventoryApi = CreateInventoryApi();
        using var productsApi = CreateProductsApi();
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateGatewayAsync(accountsApi, inventoryApi, productsApi, reviewsApi);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            accountsApi,
            inventoryApi,
            productsApi,
            reviewsApi,
            enableEntityReuse: false);

        // users does not select username, which the author lookup selects.
        const string query =
            """
            {
              users { id name reviews { author { name username } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "accounts-api: 2, reviews-api: 1",
            CountRequests(gateway));
    }

    private Task<Gateway> CreateGatewayAsync(
        TestServer accountsApi,
        TestServer inventoryApi,
        TestServer productsApi,
        TestServer reviewsApi,
        bool enableEntityReuse = true)
        => CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyOptions(options => options.EnableEntityReuse = enableEntityReuse));

    private TestServer CreateAccountsApi()
        => CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<K6ExecutionTests.AccountsQuery>(d => d.Name("Query")));

    private TestServer CreateInventoryApi()
        => CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<K6ExecutionTests.InventoryQuery>(d => d.Name("Query")));

    private TestServer CreateProductsApi()
        => CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<K6ExecutionTests.ProductsQuery>(d => d.Name("Query")));

    private TestServer CreateReviewsApi()
        => CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<K6ExecutionTests.ReviewsQuery>(d => d.Name("Query")));

    private static async Task<string> ExecuteAsync(Gateway gateway, string query)
    {
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            query,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        var errors = response.Errors.ValueKind is JsonValueKind.Undefined
            ? string.Empty
            : response.Errors.GetRawText();

        return response.Data.GetRawText() + errors;
    }

    private static string CountRequests(Gateway gateway)
        => string.Join(
            ", ",
            gateway.Interactions
                .OrderBy(t => t.Key, StringComparer.Ordinal)
                .Select(t => $"{t.Key}: {t.Value.Count}"));
}

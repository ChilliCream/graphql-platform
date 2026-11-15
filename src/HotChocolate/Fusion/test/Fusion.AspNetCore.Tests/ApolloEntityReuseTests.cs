using System.Text.Json;
using System.Text.RegularExpressions;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Fusion.Execution.Clients;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed partial class ApolloEntityReuseTests : FusionTestBase
{
    [Fact]
    public async Task Execute_K6Query_Should_ReturnSameData_When_ApolloEntityReuseIsEnabled()
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
        Assert.Equal(
            "accounts-api: 1, inventory-api: 1, products-api: 1, reviews-api: 1",
            CountRequests(gateway));
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_ReuseEntity_When_RootEntityHoldsSelectedFields()
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
    public async Task Execute_ApolloLookup_Should_FetchEntity_When_RootEntityIsNotADependency()
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
    public async Task Execute_ApolloLookup_Should_SendOnlyMissingRepresentations_When_SomeEntitiesAreReusable()
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

        // The author reviewed products 1 and 2, but topProducts only holds product 1.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc name price weight
                reviews { author { id reviews { product { name price weight } } } }
              }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            """[{"__typename":"Product","upc":"2"}]""",
            Assert.Single(GetRepresentations(gateway, "products-api")));
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_RunDependentLookup_When_EveryEntityIsReused()
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

        // The inventory lookup requires price and weight from the reused products lookup.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc name price weight
                reviews { id product { name price weight inStock shippingEstimate } }
              }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "inventory-api: 1, products-api: 1, reviews-api: 1",
            CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"topProducts":[{"upc":"1","name":"Table","price":899,"weight":100,"reviews":[{"id":"1","product":{"name":"Table","price":899,"weight":100,"inStock":true,"shippingEstimate":50}},{"id":"2","product":{"name":"Table","price":899,"weight":100,"inStock":true,"shippingEstimate":50}},{"id":"3","product":{"name":"Table","price":899,"weight":100,"inStock":true,"shippingEstimate":50}},{"id":"4","product":{"name":"Table","price":899,"weight":100,"inStock":true,"shippingEstimate":50}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_ApolloBatchLookup_Should_RunDependentLookup_When_OneOperationIsReused()
    {
        // arrange
        using var accountsApi = CreateAccountsApi(
            new AccountsData([new AccountsUser { Id = "1", Name = "Uri Goldshtein", Username = "urigo" }]));
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

        // The products batch reuses the price and weight lookup that a lookup in the inventory batch requires.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc price weight
                reviews { product { price weight shippingEstimate } }
              }
              users { reviews { product { name inStock shippingEstimate } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "accounts-api: 1, inventory-api: 1, products-api: 2, reviews-api: 1",
            CountRequests(gateway));
        Assert.Equal(
            """[{"__typename":"Product","upc":"1"},{"__typename":"Product","upc":"2"}]""",
            Assert.Single(GetRepresentations(gateway, "products-api")));
        data.MatchInlineSnapshot(
            """
            {"topProducts":[{"upc":"1","price":899,"weight":100,"reviews":[{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}}]}],"users":[{"reviews":[{"product":{"name":"Table","inStock":true,"shippingEstimate":50}},{"product":{"name":"Couch","inStock":false,"shippingEstimate":0}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_ApolloBatchLookup_Should_SendNoRequest_When_EveryOperationIsReused()
    {
        // arrange
        using var accountsApi = CreateAccountsApi(
            new AccountsData([new AccountsUser { Id = "1", Name = "Uri Goldshtein", Username = "urigo" }]));
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

        // Both lookups of the products batch are reused from topProducts.
        const string query =
            """
            {
              topProducts(first: 2) {
                upc name price weight
                reviews { product { price weight shippingEstimate } }
              }
              users { reviews { product { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal(
            "accounts-api: 1, inventory-api: 1, products-api: 1, reviews-api: 1",
            CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"topProducts":[{"upc":"1","name":"Table","price":899,"weight":100,"reviews":[{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}}]},{"upc":"2","name":"Couch","price":1299,"weight":1000,"reviews":[{"product":{"price":1299,"weight":1000,"shippingEstimate":0}},{"product":{"price":1299,"weight":1000,"shippingEstimate":0}}]}],"users":[{"reviews":[{"product":{"name":"Table"}},{"product":{"name":"Couch"}}]}]}
            """);
    }

    [Theory]
    [InlineData(SourceSchemaClientCapabilities.None)]
    [InlineData(SourceSchemaClientCapabilities.Default)]
    public async Task Execute_ApolloBatchLookup_Should_RunDependentLookup_When_OtherOperationRequestFails(
        SourceSchemaClientCapabilities capabilities)
    {
        // arrange
        using var accountsApi = CreateAccountsApi();
        using var inventoryApi = CreateInventoryApi();
        using var productsApi = CreateProductsApiFailingNameLookups(capabilities);
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateGatewayAsync(accountsApi, inventoryApi, productsApi, reviewsApi);

        // The products batch reuses the price and weight lookup that the inventory lookup requires
        // and sends only the name lookup, which fails.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc price weight
                reviews { product { price weight shippingEstimate } }
              }
              users { reviews { product { name } } }
            }
            """;

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(
            query,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        // assert
        var shippingEstimates = response.Data
            .GetProperty("topProducts")[0]
            .GetProperty("reviews")
            .EnumerateArray()
            .Select(review => review.GetProperty("product").GetProperty("shippingEstimate").GetRawText());
        Assert.Equal(["50", "50", "50", "50"], shippingEstimates);
        Assert.Single(gateway.Interactions["inventory-api"]);
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_FetchEntity_When_RequiredFieldValuesDiffer()
    {
        // arrange
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<PricedProductsQuery>(d => d.Name("Query"))
                .AddType<PricedProduct>());
        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<InventoryQuery>(d => d.Name("Query"))
                .AddType<ReviewedInventoryProduct>());
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ReviewsQuery>(d => d.Name("Query"))
                .AddType<WeightReview>()
                .AddType<WeightReviewProduct>());
        (string, TestServer)[] sourceSchemas =
        [
            ("inventory-api", inventoryApi),
            ("products-api", productsApi),
            ("reviews-api", reviewsApi)
        ];
        using var gateway = await CreateCompositeSchemaAsync(
            sourceSchemas,
            enableGlobalObjectIdentification: false);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            sourceSchemas,
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false),
            enableGlobalObjectIdentification: false);

        // The reviewed product is product 1 again, but reviews resolves a different weight for it.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc price weight inStock shippingEstimate
                latestReview { id product { upc price weight inStock shippingEstimate } }
              }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        // The nested price is reused from topProducts, the nested inventory entity is fetched.
        Assert.Equal(dataWithoutReuse, data);
        data.MatchInlineSnapshot(
            """
            {"topProducts":[{"upc":"1","price":899,"weight":100,"inStock":true,"shippingEstimate":50,"latestReview":{"id":"1","product":{"upc":"1","price":899,"weight":999,"inStock":true,"shippingEstimate":499}}}]}
            """);
        Assert.Equal(
            "inventory-api: 3, products-api: 1, reviews-api: 2",
            CountRequests(gateway));
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_FetchEntity_When_ProducerFieldIsNull()
    {
        // arrange
        using var accountsApi = CreateAccountsApi(
            new AccountsData([new AccountsUser { Id = "1", Name = null, Username = "urigo" }]));
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
        Assert.Equal("accounts-api: 2, reviews-api: 1", CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"users":[{"id":"1","name":null,"username":"urigo","reviews":[{"author":{"name":null,"username":"urigo"}},{"author":{"name":null,"username":"urigo"}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_KeepFieldErrors_When_ProducerFieldHasError()
    {
        // arrange
        using var accountsApi = CreateAccountsApi(
            new AccountsData(
                [new AccountsUser { Id = "1", Name = "Uri Goldshtein", Username = "urigo" }],
                ReportNameError: true));
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

        // name returns a value and reports an error.
        const string query =
            """
            {
              users { id name reviews { author { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal("accounts-api: 2, reviews-api: 1", CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"users":[{"id":"1","name":"Uri Goldshtein","reviews":[{"author":{"name":"Uri Goldshtein"}},{"author":{"name":"Uri Goldshtein"}}]}]}[{"message":"NAME_WARNING","path":["users",0,"name"]},{"message":"NAME_WARNING","path":["users",0,"reviews",0,"author","name"]},{"message":"NAME_WARNING","path":["users",0,"reviews",1,"author","name"]}]
            """);
    }

    [Fact]
    public async Task Execute_ApolloLookup_Should_FetchEntity_When_KeyIsNested()
    {
        // arrange
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<NestedKeyProductsQuery>(d => d.Name("Query"))
                .AddType<NestedKeyProduct>());
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ReviewsQuery>(d => d.Name("Query"))
                .AddType<NestedKeyReviewProduct>());
        using var gateway = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            enableGlobalObjectIdentification: false);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false),
            enableGlobalObjectIdentification: false);

        // The product key is { sku region { code } }.
        const string query =
            """
            {
              products { sku region { code } name reviews { product { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal("products-api: 2, reviews-api: 1", CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"products":[{"sku":"A","region":{"code":"EU"},"name":"Table","reviews":[{"product":{"name":"Table"}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_ApolloMutation_Should_FetchChangedEntity_When_EarlierMutationFieldHoldsIt()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<AccountsQuery>(d => d.Name("Query"))
                .AddMutationType<AccountsMutation>(d => d.Name("Mutation"))
                .AddType<AccountsUser>(),
            configureServices: services => services.AddSingleton(
                new AccountsData([new AccountsUser { Id = "1", Name = "Uri Goldshtein", Username = "urigo" }])));
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableGlobalObjectIdentification: false);

        // Both fields rename user 1, so the author must be fetched after the second rename.
        const string mutation =
            """
            mutation {
              first: renameUser(id: "1", name: "First") { id name }
              second: renameUser(id: "1", name: "Second") { id reviews { author { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, mutation);

        // assert
        Assert.Equal("accounts-api: 3, reviews-api: 1", CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"first":{"id":"1","name":"First"},"second":{"id":"1","reviews":[{"author":{"name":"Second"}},{"author":{"name":"Second"}}]}}
            """);
    }

    [Fact]
    public async Task Subscribe_ApolloLookup_Should_FetchEntityForEachEvent_When_EventsHoldSameEntity()
    {
        // arrange
        using var eventsApi = CreateSourceSchema(
            "events-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<EventsQuery>(d => d.Name("Query"))
                .AddSubscriptionType<EventsSubscription>(d => d.Name("Subscription"))
                .AddType<EventsProduct>());
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<CountingProductsQuery>(d => d.Name("Query"))
                .AddType<CountingProduct>(),
            configureServices: services => services.AddSingleton(new NameCounter()));
        using var gateway = await CreateCompositeSchemaAsync(
            [("events-api", eventsApi), ("products-api", productsApi)],
            enableGlobalObjectIdentification: false);
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // Every event holds product 1, and products returns a new name for every lookup.
        const string subscription =
            """
            subscription {
              onProductChanged { upc name }
            }
            """;

        // act
        using var response = await client.PostAsync(
            subscription,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        var events = new List<string>();

        await foreach (var result in response.ReadAsResultStreamAsync()
            .WithCancellation(TestContext.Current.CancellationToken))
        {
            using (result)
            {
                events.Add(result.Data.GetRawText());
            }
        }

        // assert
        events.MatchInlineSnapshots(
            [
                """
                {"onProductChanged":{"upc":"1","name":"Name 1"}}
                """,
                """
                {"onProductChanged":{"upc":"1","name":"Name 2"}}
                """,
                """
                {"onProductChanged":{"upc":"1","name":"Name 3"}}
                """
            ]);
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
                builder.ModifyOptions(options => options.EnableEntityReuse = enableEntityReuse),
            enableGlobalObjectIdentification: false);

    private TestServer CreateAccountsApi(AccountsData? data = null)
        => CreateSourceSchema(
            "accounts-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<AccountsQuery>(d => d.Name("Query"))
                .AddType<AccountsUser>(),
            configureServices: services => services.AddSingleton(data ?? AccountsData.Default));

    private TestServer CreateInventoryApi()
        => CreateSourceSchema(
            "inventory-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<InventoryQuery>(d => d.Name("Query"))
                .AddType<InventoryProduct>());

    private TestServer CreateProductsApi()
        => CreateSourceSchema(
            "products-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ProductsQuery>(d => d.Name("Query"))
                .AddType<ProductsProduct>());

    // A products source schema that answers every _entities request selecting the product name with HTTP 500.
    private TestServer CreateProductsApiFailingNameLookups(SourceSchemaClientCapabilities capabilities)
        => CreateSourceSchema(
            "products-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<NullableNameProductsQuery>(d => d.Name("Query"))
                .AddType<NullableNameProduct>(),
            configureApplication: app =>
            {
                app.Use(async (context, next) =>
                {
                    context.Request.EnableBuffering();
                    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                    var body = await reader.ReadToEndAsync(context.RequestAborted);
                    context.Request.Body.Position = 0;

                    if (body.Contains("_entities", StringComparison.Ordinal) && NameField().IsMatch(body))
                    {
                        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                        return;
                    }

                    await next(context);
                });
                app.UseRouting();
                app.UseEndpoints(endpoints => endpoints.MapGraphQL(schemaName: "products-api"));
            },
            capabilities: capabilities);

    [GeneratedRegex(@"\bname\b")]
    private static partial Regex NameField();

    private TestServer CreateReviewsApi()
        => CreateSourceSchema(
            "reviews-api",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ReviewsQuery>(d => d.Name("Query"))
                .AddType<ReviewsProduct>()
                .AddType<ReviewsUser>());

    private static async Task<string> ExecuteAsync(Gateway gateway, string query)
    {
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        using var result = await client.PostAsync(
            query,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        using var response = await result.ReadAsResultAsync(TestContext.Current.CancellationToken);

        var data = response.Data.ValueKind is JsonValueKind.Undefined
            ? string.Empty
            : response.Data.GetRawText();
        var errors = response.Errors.ValueKind is JsonValueKind.Undefined
            ? string.Empty
            : response.Errors.GetRawText();

        return data + errors;
    }

    private static string CountRequests(Gateway gateway)
        => string.Join(
            ", ",
            gateway.Interactions
                .OrderBy(t => t.Key, StringComparer.Ordinal)
                .Select(t => $"{t.Key}: {t.Value.Count}"));

    // The representations variable of every _entities request that was sent to the source schema.
    private static List<string> GetRepresentations(Gateway gateway, string schemaName)
    {
        var representations = new List<string>();

        foreach (var interaction in gateway.Interactions[schemaName].Values.OrderBy(t => t.NodeId))
        {
            using var body = JsonDocument.Parse(interaction.Request!.Body.ToArray());

            if (body.RootElement.TryGetProperty("variables", out var variables)
                && variables.TryGetProperty("representations", out var value))
            {
                representations.Add(value.GetRawText());
            }
        }

        return representations;
    }

    public sealed record AccountsData(List<AccountsUser> Users, bool ReportNameError = false)
    {
        public static AccountsData Default { get; } = new(
        [
            new AccountsUser { Id = "1", Name = "Uri Goldshtein", Username = "urigo" },
            new AccountsUser { Id = "2", Name = "Dotan Simha", Username = "dotansimha" },
            new AccountsUser { Id = "3", Name = "Kamil Kisiela", Username = "kamilkisiela" },
            new AccountsUser { Id = "4", Name = "Arda Tanrikulu", Username = "ardatan" },
            new AccountsUser { Id = "5", Name = "Gil Gardosh", Username = "gilgardosh" },
            new AccountsUser { Id = "6", Name = "Laurin Quast", Username = "laurin" }
        ]);
    }

    public sealed class AccountsQuery
    {
        public List<AccountsUser> GetUsers([Service] AccountsData data) => data.Users;
    }

    [GraphQLName("User")]
    [Key("id")]
    public sealed class AccountsUser
    {
        [GraphQLType(typeof(IdType))]
        public required string Id { get; init; }

        [GraphQLIgnore]
        public string? Name { get; init; }

        public string? Username { get; init; }

        [GraphQLName("name")]
        public string? GetName(IResolverContext context, [Service] AccountsData data)
        {
            if (data.ReportNameError)
            {
                context.ReportError("NAME_WARNING");
            }

            return Name;
        }

        [ReferenceResolver]
        public static AccountsUser? ResolveReference(string id, [Service] AccountsData data)
            => data.Users.FirstOrDefault(u => u.Id == id);
    }

    public sealed class AccountsMutation
    {
        public AccountsUser RenameUser(
            [GraphQLType(typeof(NonNullType<IdType>))] string id,
            string name,
            [Service] AccountsData data)
        {
            var index = data.Users.FindIndex(u => u.Id == id);
            var user = new AccountsUser { Id = id, Name = name, Username = data.Users[index].Username };
            data.Users[index] = user;
            return user;
        }
    }

    public sealed class EventsQuery
    {
        public string Foo() => "Foo";
    }

    public sealed class EventsSubscription
    {
        public async IAsyncEnumerable<EventsProduct> OnProductChangedStream()
        {
            for (var i = 0; i < 3; i++)
            {
                await Task.Yield();
                yield return new EventsProduct { Upc = "1" };
            }
        }

        [Subscribe(With = nameof(OnProductChangedStream))]
        public EventsProduct OnProductChanged([EventMessage] EventsProduct product) => product;
    }

    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("upc")]
    public sealed class EventsProduct
    {
        [External]
        public string Upc { get; set; } = default!;

        [ReferenceResolver]
        public static EventsProduct ResolveReference(string upc) => new() { Upc = upc };
    }

    // The number of times a product name was resolved.
    public sealed class NameCounter
    {
        private int _count;

        public int Next() => Interlocked.Increment(ref _count);
    }

    public sealed class CountingProductsQuery;

    [GraphQLName("Product")]
    [Key("upc")]
    public sealed class CountingProduct
    {
        public required string Upc { get; init; }

        public string GetName([Service] NameCounter counter) => $"Name {counter.Next()}";

        [ReferenceResolver]
        public static CountingProduct ResolveReference(string upc) => new() { Upc = upc };
    }

    public sealed class ProductsQuery
    {
        internal static readonly List<ProductsProduct> s_products =
        [
            new() { Upc = "1", Name = "Table", Price = 899, Weight = 100 },
            new() { Upc = "2", Name = "Couch", Price = 1299, Weight = 1000 },
            new() { Upc = "3", Name = "Glass", Price = 15, Weight = 20 },
            new() { Upc = "4", Name = "Chair", Price = 499, Weight = 100 },
            new() { Upc = "5", Name = "TV", Price = 1299, Weight = 1000 }
        ];

        public IEnumerable<ProductsProduct> GetTopProducts(int first = 5)
            => s_products.Take(first);
    }

    [GraphQLName("Product")]
    [Key("upc")]
    public sealed class ProductsProduct
    {
        public required string Upc { get; init; }

        public required string Name { get; init; }

        public int Price { get; init; }

        public int Weight { get; init; }

        [ReferenceResolver]
        public static ProductsProduct? ResolveReference(string upc)
            => ProductsQuery.s_products.FirstOrDefault(p => p.Upc == upc);
    }

    public sealed class NullableNameProductsQuery
    {
        public IEnumerable<NullableNameProduct> GetTopProducts(int first = 5)
            => ProductsQuery.s_products.Take(first).Select(NullableNameProduct.From);
    }

    [GraphQLName("Product")]
    [Key("upc")]
    public sealed class NullableNameProduct
    {
        public required string Upc { get; init; }

        public string? Name { get; init; }

        public int Price { get; init; }

        public int Weight { get; init; }

        [ReferenceResolver]
        public static NullableNameProduct? ResolveReference(string upc)
            => ProductsQuery.s_products.FirstOrDefault(p => p.Upc == upc) is { } product
                ? From(product)
                : null;

        internal static NullableNameProduct From(ProductsProduct product)
            => new() { Upc = product.Upc, Name = product.Name, Price = product.Price, Weight = product.Weight };
    }

    public sealed class InventoryQuery;

    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("upc")]
    public sealed class InventoryProduct
    {
        [External]
        public string Upc { get; set; } = default!;

        [External]
        public int Weight { get; set; }

        [External]
        public int Price { get; set; }

        public bool InStock { get; set; }

        [Requires("price weight")]
        public int? GetShippingEstimate() => Price > 1000 ? 0 : Weight / 2;

        [ReferenceResolver]
        public static InventoryProduct ResolveReference(string upc)
            => new() { Upc = upc, InStock = upc is "1" or "5" };
    }

    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("upc")]
    public sealed class ReviewedInventoryProduct
    {
        [External]
        public string Upc { get; set; } = default!;

        [External]
        public int Weight { get; set; }

        [External]
        public int Price { get; set; }

        public bool InStock { get; set; }

        [Requires("price weight")]
        public int? GetShippingEstimate() => Price > 1000 ? 0 : Weight / 2;

        public InventoryReview GetLatestReview() => new() { Id = "1" };

        [ReferenceResolver]
        public static ReviewedInventoryProduct ResolveReference(string upc)
            => new() { Upc = upc, InStock = upc is "1" or "5" };
    }

    [GraphQLName("Review")]
    [Key("id", resolvable: false)]
    public sealed class InventoryReview
    {
        [GraphQLType(typeof(IdType))]
        public required string Id { get; init; }
    }

    public sealed class PricedProductsQuery
    {
        public IEnumerable<PricedProduct> GetTopProducts(int first = 5)
            => ProductsQuery.s_products.Take(first).Select(p => new PricedProduct { Upc = p.Upc, Price = p.Price });
    }

    [GraphQLName("Product")]
    [Key("upc")]
    public sealed class PricedProduct
    {
        public required string Upc { get; init; }

        public int Price { get; init; }

        [ReferenceResolver]
        public static PricedProduct? ResolveReference(string upc)
            => ProductsQuery.s_products.FirstOrDefault(p => p.Upc == upc) is { } product
                ? new PricedProduct { Upc = product.Upc, Price = product.Price }
                : null;
    }

    [GraphQLName("Review")]
    [Key("id")]
    public sealed class WeightReview
    {
        [GraphQLType(typeof(IdType))]
        public required string Id { get; init; }

        public WeightReviewProduct GetProduct() => new() { Upc = "1", Weight = 999 };

        [ReferenceResolver]
        public static WeightReview ResolveReference(string id) => new() { Id = id };
    }

    // Owns weight and reports a different weight for a looked up product than for a reviewed one.
    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("upc")]
    public sealed class WeightReviewProduct
    {
        [External]
        public string Upc { get; set; } = default!;

        public int Weight { get; set; }

        [ReferenceResolver]
        public static WeightReviewProduct ResolveReference(string upc) => new() { Upc = upc, Weight = 100 };
    }

    public sealed class ReviewsQuery;

    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("upc")]
    public sealed class ReviewsProduct
    {
        [External]
        public string Upc { get; set; } = default!;

        public IEnumerable<ReviewsReview> GetReviews()
            => ReviewsReview.s_all.Where(r => r.ProductUpc == Upc);

        [ReferenceResolver]
        public static ReviewsProduct ResolveReference(string upc)
            => new() { Upc = upc };
    }

    [GraphQLName("User")]
    [ExtendServiceType]
    [Key("id")]
    public sealed class ReviewsUser
    {
        [External]
        [GraphQLType(typeof(IdType))]
        public string Id { get; set; } = default!;

        // Every user reviewed products 1 and 2.
        public IEnumerable<ReviewsReview> GetReviews()
            => [ReviewsReview.s_all[0], ReviewsReview.s_all[4]];

        [ReferenceResolver]
        public static ReviewsUser ResolveReference(string id)
            => new() { Id = id };
    }

    [GraphQLName("Review")]
    public sealed class ReviewsReview
    {
        internal static readonly List<ReviewsReview> s_all =
        [
            new() { Id = "1", AuthorId = "1", ProductUpc = "1", Body = "Review 1" },
            new() { Id = "2", AuthorId = "1", ProductUpc = "1", Body = "Review 2" },
            new() { Id = "3", AuthorId = "1", ProductUpc = "1", Body = "Review 3" },
            new() { Id = "4", AuthorId = "1", ProductUpc = "1", Body = "Review 4" },
            new() { Id = "5", AuthorId = "1", ProductUpc = "2", Body = "Review 5" },
            new() { Id = "6", AuthorId = "1", ProductUpc = "2", Body = "Review 6" },
            new() { Id = "7", AuthorId = "1", ProductUpc = "3", Body = "Review 7" },
            new() { Id = "8", AuthorId = "1", ProductUpc = "4", Body = "Review 8" }
        ];

        [GraphQLType(typeof(IdType))]
        public required string Id { get; init; }

        public required string Body { get; init; }

        [GraphQLIgnore]
        public required string AuthorId { get; init; }

        [GraphQLIgnore]
        public required string ProductUpc { get; init; }

        public ReviewsUser GetAuthor() => new() { Id = AuthorId };

        public ReviewsProduct GetProduct() => new() { Upc = ProductUpc };
    }

    public sealed class NestedKeyProductsQuery
    {
        internal static readonly List<NestedKeyProduct> s_products =
        [
            new() { Sku = "A", Region = new NestedKeyRegion { Code = "EU" }, Name = "Table" }
        ];

        public List<NestedKeyProduct> GetProducts() => s_products;
    }

    [GraphQLName("Product")]
    [Key("sku region { code }")]
    public sealed class NestedKeyProduct
    {
        public required string Sku { get; init; }

        public required NestedKeyRegion Region { get; init; }

        public required string Name { get; init; }

        [ReferenceResolver]
        public static NestedKeyProduct? ResolveReference(string sku, [Map("region.code")] string code)
            => NestedKeyProductsQuery.s_products.FirstOrDefault(p => p.Sku == sku && p.Region.Code == code);
    }

    [GraphQLName("Region")]
    public sealed class NestedKeyRegion
    {
        public required string Code { get; init; }
    }

    [GraphQLName("Product")]
    [ExtendServiceType]
    [Key("sku region { code }")]
    public sealed class NestedKeyReviewProduct
    {
        [External]
        public string Sku { get; set; } = default!;

        [External]
        public NestedKeyReviewRegion Region { get; set; } = default!;

        public IEnumerable<NestedKeyReview> GetReviews()
            => [new NestedKeyReview { Id = "1", Product = this }];

        [ReferenceResolver]
        public static NestedKeyReviewProduct ResolveReference(string sku, [Map("region.code")] string code)
            => new() { Sku = sku, Region = new NestedKeyReviewRegion { Code = code } };
    }

    [GraphQLName("Region")]
    [ExtendServiceType]
    public sealed class NestedKeyReviewRegion
    {
        [External]
        public string Code { get; set; } = default!;
    }

    [GraphQLName("Review")]
    public sealed class NestedKeyReview
    {
        [GraphQLType(typeof(IdType))]
        public required string Id { get; init; }

        public required NestedKeyReviewProduct Product { get; init; }
    }
}

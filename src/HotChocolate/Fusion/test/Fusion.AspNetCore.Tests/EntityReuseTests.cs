using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
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

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_ProducerAliasesKeyField()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<AliasAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<AliasReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

        // Alice's username equals Bob's id, and users returns the username under the response name id.
        const string query =
            """
            {
              users { id: username email name reviews { author { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        data.MatchInlineSnapshot(
            """
            {"users":[{"id":"2","email":"alice@example.com","name":"Alice","reviews":[{"author":{"name":"Bob"}}]},{"id":"bob","email":"bob@example.com","name":"Bob","reviews":[]}]}
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_KeepFieldErrors_When_ProducerFieldHasError()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<ErrorAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateReviewsApi();
        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

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
        data.MatchInlineSnapshot(
            """
            {"users":[{"id":"1","name":"Uri Goldshtein","reviews":[{"author":{"name":"Uri Goldshtein"}},{"author":{"name":"Uri Goldshtein"}}]}]}[{"message":"NAME_WARNING","path":["users",0,"name"]},{"message":"NAME_WARNING","path":["users",0,"reviews",0,"author","name"]},{"message":"NAME_WARNING","path":["users",0,"reviews",1,"author","name"]}]
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_CandidateKeyIsNull()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<NullKeyAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<NullKeyReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

        // Alice and the review author Bob both have no email, the only key of the author lookup.
        const string query =
            """
            {
              users { id email name reviews { author { name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        data.MatchInlineSnapshot(
            """
            {"users":[{"id":"1","email":null,"name":"Alice","reviews":[{"author":{"name":null}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_ReuseEntity_When_CompositeKeyIsPartlyNull()
    {
        // arrange
        using var productsApi = CreateCompositeKeyProductsApi(
            new CompositeKeyProduct { Sku = "A", Region = null, Name = "Table" });
        using var reviewsApi = CreateCompositeKeyReviewsApi();
        using var gateway = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

        // act
        var data = await ExecuteAsync(gateway, CompositeKeyQuery);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, CompositeKeyQuery);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        Assert.Equal("products-api: 1, reviews-api: 1", CountRequests(gateway));
        data.MatchInlineSnapshot(
            """
            {"products":[{"sku":"A","region":null,"name":"Table","reviews":[{"product":{"name":"Table"}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_CompositeKeyIsAllNull()
    {
        // arrange
        using var productsApi = CreateCompositeKeyProductsApi(
            new CompositeKeyProduct { Sku = null, Region = null, Name = "Unknown" });
        using var reviewsApi = CreateCompositeKeyReviewsApi();
        using var gateway = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

        // act
        var data = await ExecuteAsync(gateway, CompositeKeyQuery);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, CompositeKeyQuery);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        data.MatchInlineSnapshot(
            """
            {"products":[{"sku":null,"region":null,"name":"Unknown","reviews":[{"product":{"name":null}}]}]}
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_KeyIsNested()
    {
        // arrange
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<NestedKeyProductsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<NestedKeyReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)]);
        using var gatewayWithoutReuse = await CreateCompositeSchemaAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            configureGatewayBuilder: builder => builder.ModifyOptions(options => options.EnableEntityReuse = false));

        // The product lookup key is { sku region { code } }.
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
    public async Task Execute_BatchLookup_Should_RunDependentLookup_When_OneOperationIsReused()
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

        // The products batch reuses the price and weight lookup that a lookup in the inventory batch requires.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc price weight
                reviews { product { price weight shippingEstimate } }
              }
              me { reviews { product { name inStock shippingEstimate } } }
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
        data.MatchInlineSnapshot(
            """
            {"topProducts":[{"upc":"1","price":899,"weight":100,"reviews":[{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}}]}],"me":{"reviews":[{"product":{"name":"Table","inStock":true,"shippingEstimate":50}},{"product":{"name":"Table","inStock":true,"shippingEstimate":50}}]}}
            """);
    }

    [Fact]
    public async Task Execute_BatchLookup_Should_SendNoRequest_When_EveryOperationIsReused()
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

        // Both lookups of the products batch are reused from topProducts.
        const string query =
            """
            {
              topProducts(first: 1) {
                upc name price weight
                reviews { product { price weight shippingEstimate } }
              }
              me { reviews { product { name } } }
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
            {"topProducts":[{"upc":"1","name":"Table","price":899,"weight":100,"reviews":[{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}},{"product":{"price":899,"weight":100,"shippingEstimate":50}}]}],"me":{"reviews":[{"product":{"name":"Table"}},{"product":{"name":"Table"}}]}}
            """);
    }

    [Fact]
    public async Task Execute_Mutation_Should_FetchChangedEntity_When_EarlierMutationFieldHoldsIt()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder
                .AddQueryType<MutationAccountsQuery>(d => d.Name("Query"))
                .AddMutationType<MutationAccountsMutation>(d => d.Name("Mutation")),
            configureServices: services => services.AddSingleton(new MutationAccountsData()));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<MutationReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateCompositeSchemaAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)]);

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
            {"first":{"id":"1","name":"First"},"second":{"id":"1","reviews":[{"author":{"name":"Second"}}]}}
            """);
    }

    [Fact]
    public async Task Subscribe_Lookup_Should_FetchEntityForEachEvent_When_EventsHoldSameEntity()
    {
        // arrange
        using var eventsApi = CreateSourceSchema(
            "events-api",
            builder => builder
                .AddQueryType<EventsQuery>(d => d.Name("Query"))
                .AddSubscriptionType<EventsSubscription>(d => d.Name("Subscription")));
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<CountingProductsQuery>(d => d.Name("Query")),
            configureServices: services => services.AddSingleton(new NameCounter()));
        using var gateway = await CreateCompositeSchemaAsync(
            [("events-api", eventsApi), ("products-api", productsApi)]);
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

    private const string CompositeKeyQuery =
        """
        {
          products { sku region name reviews { product { name } } }
        }
        """;

    private TestServer CreateCompositeKeyProductsApi(params CompositeKeyProduct[] products)
        => CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<CompositeKeyProductsQuery>(d => d.Name("Query")),
            configureServices: services => services.AddSingleton(new CompositeKeyProductData(products)));

    private TestServer CreateCompositeKeyReviewsApi()
        => CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<CompositeKeyReviewsQuery>(d => d.Name("Query")));

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

    public sealed class AliasAccountsQuery
    {
        private static readonly List<AliasAccountUser> s_users =
        [
            new() { Id = "1", Email = "alice@example.com", Username = "2", Name = "Alice" },
            new() { Id = "2", Email = "bob@example.com", Username = "bob", Name = "Bob" }
        ];

        public List<AliasAccountUser> GetUsers() => s_users;

        [Lookup]
        public AliasAccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(t => t.Id == id);
    }

    [GraphQLName("User")]
    public sealed class AliasAccountUser
    {
        [ID]
        public required string Id { get; init; }

        [Shareable]
        public required string Email { get; init; }

        public required string Username { get; init; }

        public required string Name { get; init; }
    }

    public sealed class AliasReviewsQuery
    {
        [Lookup, Internal]
        public AliasReviewUser? GetUserByEmail(string email)
            => AliasReviewUser.ByEmail(email);
    }

    [GraphQLName("User")]
    public sealed class AliasReviewUser
    {
        private static readonly Dictionary<string, string> s_emailById = new()
        {
            ["1"] = "alice@example.com",
            ["2"] = "bob@example.com"
        };

        [ID]
        [Shareable]
        public required string Id { get; init; }

        [Shareable]
        public required string Email { get; init; }

        public IEnumerable<AliasReview> GetReviews()
            => Id == "1" ? [new AliasReview { Id = "1", AuthorId = "2" }] : [];

        public static AliasReviewUser? ByEmail(string email)
            => s_emailById.FirstOrDefault(t => t.Value == email) is { Key: { } id }
                ? new AliasReviewUser { Id = id, Email = email }
                : null;

        public static AliasReviewUser ById(string id)
            => new() { Id = id, Email = s_emailById[id] };
    }

    [GraphQLName("Review")]
    public sealed class AliasReview
    {
        [ID]
        public required string Id { get; init; }

        public required string AuthorId { get; init; }

        public AliasReviewUser GetAuthor() => AliasReviewUser.ById(AuthorId);
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
                yield return new EventsProduct("1");
            }
        }

        [Subscribe(With = nameof(OnProductChangedStream))]
        public EventsProduct OnProductChanged([EventMessage] EventsProduct product) => product;
    }

    [GraphQLName("Product")]
    [EntityKey("upc")]
    public sealed record EventsProduct(string Upc);

    // The number of times a product name was resolved.
    public sealed class NameCounter
    {
        private int _count;

        public int Next() => Interlocked.Increment(ref _count);
    }

    public sealed class CountingProductsQuery
    {
        [Lookup, Internal]
        public CountingProduct? GetProductByUpc(string upc) => new() { Upc = upc };
    }

    [GraphQLName("Product")]
    public sealed class CountingProduct
    {
        public required string Upc { get; init; }

        public string GetName([Service] NameCounter counter) => $"Name {counter.Next()}";
    }

    public sealed class MutationAccountsData
    {
        public Dictionary<string, string> NameById { get; } = new() { ["1"] = "Alice" };
    }

    public sealed class MutationAccountsQuery
    {
        [Lookup]
        public MutationAccountUser? GetUser([ID] string id, [Service] MutationAccountsData data)
            => data.NameById.TryGetValue(id, out var name) ? new MutationAccountUser { Id = id, Name = name } : null;
    }

    public sealed class MutationAccountsMutation
    {
        public MutationAccountUser RenameUser([ID] string id, string name, [Service] MutationAccountsData data)
        {
            data.NameById[id] = name;
            return new MutationAccountUser { Id = id, Name = name };
        }
    }

    [GraphQLName("User")]
    public sealed class MutationAccountUser
    {
        [ID]
        public required string Id { get; init; }

        public required string Name { get; init; }
    }

    public sealed class MutationReviewsQuery
    {
        [Lookup, Internal]
        public MutationReviewUser? GetUserById([ID] string id) => new() { Id = id };
    }

    [GraphQLName("User")]
    public sealed class MutationReviewUser
    {
        [ID]
        [Shareable]
        public required string Id { get; init; }

        public IEnumerable<MutationReview> GetReviews() => [new MutationReview { Id = "1", AuthorId = Id }];
    }

    [GraphQLName("Review")]
    public sealed class MutationReview
    {
        [ID]
        public required string Id { get; init; }

        [GraphQLIgnore]
        public required string AuthorId { get; init; }

        public MutationReviewUser GetAuthor() => new() { Id = AuthorId };
    }

    public sealed class ErrorAccountsQuery
    {
        private static readonly List<ErrorAccountUser> s_users =
        [
            new() { Id = "1", Name = "Uri Goldshtein" }
        ];

        public List<ErrorAccountUser> GetUsers() => s_users;

        [Lookup]
        public ErrorAccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(t => t.Id == id);
    }

    [GraphQLName("User")]
    public sealed class ErrorAccountUser
    {
        [ID]
        public required string Id { get; init; }

        [GraphQLIgnore]
        public required string Name { get; init; }

        [GraphQLName("name")]
        public string GetName(IResolverContext context)
        {
            context.ReportError("NAME_WARNING");
            return Name;
        }
    }

    public sealed class NullKeyAccountsQuery
    {
        private static readonly List<NullKeyAccountUser> s_users =
        [
            new() { Id = "1", Email = null, Name = "Alice" }
        ];

        public List<NullKeyAccountUser> GetUsers() => s_users;

        [Lookup]
        public NullKeyAccountUser? GetUserByEmail(string? email)
            => email is null ? null : s_users.FirstOrDefault(t => t.Email == email);
    }

    [GraphQLName("User")]
    public sealed class NullKeyAccountUser
    {
        [ID]
        [Shareable]
        public required string Id { get; init; }

        [Shareable]
        public string? Email { get; init; }

        public string? Name { get; init; }
    }

    public sealed class NullKeyReviewsQuery
    {
        [Lookup, Internal]
        public NullKeyReviewUser? GetUser([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("User")]
    public sealed class NullKeyReviewUser
    {
        [ID]
        public required string Id { get; init; }

        [Shareable]
        public string? Email => null;

        public IEnumerable<NullKeyReview> GetReviews()
            => Id == "1" ? [new NullKeyReview { Id = "1", AuthorId = "2" }] : [];
    }

    [GraphQLName("Review")]
    public sealed class NullKeyReview
    {
        [ID]
        public required string Id { get; init; }

        public required string AuthorId { get; init; }

        public NullKeyReviewUser GetAuthor() => new() { Id = AuthorId };
    }

    public sealed record CompositeKeyProductData(CompositeKeyProduct[] Products);

    public sealed class CompositeKeyProductsQuery
    {
        public IEnumerable<CompositeKeyProduct> GetProducts([Service] CompositeKeyProductData data)
            => data.Products;

        [Lookup]
        public CompositeKeyProduct? GetProductByKey(
            string? sku,
            string? region,
            [Service] CompositeKeyProductData data)
            => sku is null && region is null
                ? null
                : data.Products.FirstOrDefault(t => t.Sku == sku && t.Region == region);
    }

    [GraphQLName("Product")]
    public sealed class CompositeKeyProduct
    {
        [Shareable]
        public string? Sku { get; init; }

        [Shareable]
        public string? Region { get; init; }

        public string? Name { get; init; }
    }

    public sealed class CompositeKeyReviewsQuery
    {
        [Lookup, Internal]
        public CompositeKeyReviewProduct? GetProductByKey(string? sku, string? region)
            => new() { Sku = sku, Region = region };
    }

    [GraphQLName("Product")]
    public sealed class CompositeKeyReviewProduct
    {
        [Shareable]
        public string? Sku { get; init; }

        [Shareable]
        public string? Region { get; init; }

        public IEnumerable<CompositeKeyReview> GetReviews()
            => [new CompositeKeyReview { Id = "1", Sku = Sku, Region = Region }];
    }

    [GraphQLName("Review")]
    public sealed class CompositeKeyReview
    {
        [ID]
        public required string Id { get; init; }

        public string? Sku { get; init; }

        public string? Region { get; init; }

        public CompositeKeyReviewProduct GetProduct() => new() { Sku = Sku, Region = Region };
    }

    public sealed class NestedKeyProductsQuery
    {
        private static readonly List<NestedKeyProduct> s_products =
        [
            new() { Sku = "A", Region = new NestedKeyRegion { Code = "EU" }, Name = "Table" }
        ];

        public List<NestedKeyProduct> GetProducts() => s_products;

        [Lookup]
        public NestedKeyProduct? GetProductByKey(string sku, [Is("region.code")] string regionCode)
            => s_products.FirstOrDefault(t => t.Sku == sku && t.Region.Code == regionCode);
    }

    [GraphQLName("Product")]
    public sealed class NestedKeyProduct
    {
        [Shareable]
        public required string Sku { get; init; }

        [Shareable]
        public required NestedKeyRegion Region { get; init; }

        public required string Name { get; init; }
    }

    [GraphQLName("Region")]
    public sealed class NestedKeyRegion
    {
        [Shareable]
        public required string Code { get; init; }
    }

    public sealed class NestedKeyReviewsQuery
    {
        [Lookup, Internal]
        public NestedKeyReviewProduct? GetProductByKey(string sku, [Is("region.code")] string regionCode)
            => new() { Sku = sku, Region = new NestedKeyRegion { Code = regionCode } };
    }

    [GraphQLName("Product")]
    public sealed class NestedKeyReviewProduct
    {
        [Shareable]
        public required string Sku { get; init; }

        [Shareable]
        public required NestedKeyRegion Region { get; init; }

        public IEnumerable<NestedKeyReview> GetReviews()
            => [new NestedKeyReview { Id = "1", Product = this }];
    }

    [GraphQLName("Review")]
    public sealed class NestedKeyReview
    {
        [ID]
        public required string Id { get; init; }

        public required NestedKeyReviewProduct Product { get; init; }
    }
}

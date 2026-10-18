using System.Text.Json;
using HotChocolate.Resolvers;
using HotChocolate.Transport.Http;
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
}

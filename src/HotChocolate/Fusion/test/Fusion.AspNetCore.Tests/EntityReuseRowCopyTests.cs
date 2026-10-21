using System.Text.Json;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

/// <summary>
/// Covers the entity reuse that matches targets to producers on raw key values and fills them
/// by row offset, and the leaf copy into duplicate lookup targets by row offset. Every test
/// compares the gateway response with and without entity reuse.
/// </summary>
public sealed class EntityReuseRowCopyTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Lookup_Should_ReuseEntities_When_EveryTargetHasAProducer()
    {
        // arrange
        using var accountsApi = CreateK6Api<K6ExecutionTests.AccountsQuery>("accounts-api");
        using var reviewsApi = CreateK6Api<K6ExecutionTests.ReviewsQuery>("reviews-api");
        using var gateway = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        // The authors at two target paths are all held by users.
        const string query =
            """
            {
              users {
                id
                name
                username
                reviews {
                  author { name username }
                  product { reviews { author { name username } } }
                }
              }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchRemainingEntities_When_SomeTargetsHaveNoProducer()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<PartialAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<PartialReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        // Alice and Bob review each other and Carol, who is not in users.
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
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchInlineSnapshot(
            """
            {
              "WithReuse": "accounts-api: 2, reviews-api: 1",
              "WithoutReuse": "accounts-api: 2, reviews-api: 1",
              "Data": "{\"users\":[{\"id\":\"1\",\"name\":\"Alice\",\"reviews\":[{\"author\":{\"name\":\"Bob\"}},{\"author\":{\"name\":\"Carol\"}}]},{\"id\":\"2\",\"name\":\"Bob\",\"reviews\":[{\"author\":{\"name\":\"Alice\"}}]}]}"
            }
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_ReuseEnumValues_When_ProducerHoldsEnumField()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<EnumAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<PartialReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        const string query =
            """
            {
              users { id role reviews { author { id role } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchInlineSnapshot(
            """
            {
              "WithReuse": "accounts-api: 1, reviews-api: 1",
              "WithoutReuse": "accounts-api: 2, reviews-api: 1",
              "Data": "{\"users\":[{\"id\":\"1\",\"role\":\"ADMIN\",\"reviews\":[{\"author\":{\"id\":\"2\",\"role\":\"MEMBER\"}},{\"author\":{\"id\":\"3\",\"role\":\"MEMBER\"}}]},{\"id\":\"2\",\"role\":\"MEMBER\",\"reviews\":[{\"author\":{\"id\":\"1\",\"role\":\"ADMIN\"}}]},{\"id\":\"3\",\"role\":\"MEMBER\",\"reviews\":[]}]}"
            }
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_FetchEntity_When_ProducerFieldIsNull()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<NullFieldAccountsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<PartialReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        // Bob has no name, so the author lookup for Bob is fetched.
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
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchInlineSnapshot(
            """
            {
              "WithReuse": "accounts-api: 2, reviews-api: 1",
              "WithoutReuse": "accounts-api: 2, reviews-api: 1",
              "Data": "{\"users\":[{\"id\":\"1\",\"name\":\"Alice\",\"reviews\":[{\"author\":{\"name\":null}},{\"author\":{\"name\":\"Carol\"}}]},{\"id\":\"2\",\"name\":null,\"reviews\":[{\"author\":{\"name\":\"Alice\"}}]},{\"id\":\"3\",\"name\":\"Carol\",\"reviews\":[]}]}"
            }
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_ReuseEntity_When_KeyIsNumber()
    {
        // arrange
        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<NumberKeyProductsQuery>(d => d.Name("Query")));
        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<NumberKeyReviewsQuery>(d => d.Name("Query")));
        using var gateway = await CreateGatewayAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("products-api", productsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        const string query =
            """
            {
              products { id name price reviews { product { name price } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchInlineSnapshot(
            """
            {
              "WithReuse": "products-api: 1, reviews-api: 1",
              "WithoutReuse": "products-api: 2, reviews-api: 1",
              "Data": "{\"products\":[{\"id\":1,\"name\":\"Table\",\"price\":899,\"reviews\":[{\"product\":{\"name\":\"Table\",\"price\":899}},{\"product\":{\"name\":\"Table\",\"price\":899}}]},{\"id\":2,\"name\":\"Couch\",\"price\":1299,\"reviews\":[{\"product\":{\"name\":\"Couch\",\"price\":1299}},{\"product\":{\"name\":\"Couch\",\"price\":1299}}]}]}"
            }
            """);
    }

    [Fact]
    public async Task Execute_Lookup_Should_CopyLeafValues_When_DuplicateTargetsSelectDifferentFields()
    {
        // arrange
        using var accountsApi = CreateK6Api<K6ExecutionTests.AccountsQuery>("accounts-api");
        using var productsApi = CreateK6Api<K6ExecutionTests.ProductsQuery>("products-api");
        using var reviewsApi = CreateK6Api<K6ExecutionTests.ReviewsQuery>("reviews-api");
        using var gateway = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("products-api", productsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: true);
        using var gatewayWithoutReuse = await CreateGatewayAsync(
            [("accounts-api", accountsApi), ("products-api", productsApi), ("reviews-api", reviewsApi)],
            enableEntityReuse: false);

        // The same products are looked up at two paths that select different fields.
        const string query =
            """
            {
              me { reviews { product { upc name price weight } } }
              users { id reviews { product { upc name } } }
            }
            """;

        // act
        var data = await ExecuteAsync(gateway, query);
        var dataWithoutReuse = await ExecuteAsync(gatewayWithoutReuse, query);

        // assert
        Assert.Equal(dataWithoutReuse, data);
        new
        {
            WithReuse = CountRequests(gateway),
            WithoutReuse = CountRequests(gatewayWithoutReuse),
            Data = data
        }.MatchMarkdownSnapshot();
    }

    private TestServer CreateK6Api<TQuery>(string name)
        where TQuery : class
        => CreateSourceSchema(name, builder => builder.AddQueryType<TQuery>(d => d.Name("Query")));

    private Task<Gateway> CreateGatewayAsync(
        (string SchemaName, TestServer Server)[] sourceSchemaServers,
        bool enableEntityReuse)
        => CreateCompositeSchemaAsync(
            sourceSchemaServers,
            configureGatewayBuilder: builder =>
                builder.ModifyOptions(options => options.EnableEntityReuse = enableEntityReuse));

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

    public sealed class PartialAccountsQuery
    {
        private static readonly List<PartialAccountUser> s_users =
        [
            new() { Id = "1", Name = "Alice" },
            new() { Id = "2", Name = "Bob" },
            new() { Id = "3", Name = "Carol" }
        ];

        public IEnumerable<PartialAccountUser> GetUsers() => s_users.Take(2);

        [Lookup]
        public PartialAccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(t => t.Id == id);
    }

    [GraphQLName("User")]
    public sealed class PartialAccountUser
    {
        [ID]
        public required string Id { get; init; }

        public string? Name { get; init; }
    }

    public sealed class PartialReviewsQuery
    {
        [Lookup, Internal]
        public PartialReviewUser? GetUser([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("User")]
    public sealed class PartialReviewUser
    {
        [ID]
        public required string Id { get; init; }

        public IEnumerable<PartialReview> GetReviews()
            => Id switch
            {
                "1" => [new PartialReview { Id = "1", AuthorId = "2" }, new PartialReview { Id = "2", AuthorId = "3" }],
                "2" => [new PartialReview { Id = "3", AuthorId = "1" }],
                _ => []
            };
    }

    [GraphQLName("Review")]
    public sealed class PartialReview
    {
        [ID]
        public required string Id { get; init; }

        public required string AuthorId { get; init; }

        public PartialReviewUser GetAuthor() => new() { Id = AuthorId };
    }

    public enum UserRole
    {
        Admin,
        Member
    }

    public sealed class EnumAccountsQuery
    {
        private static readonly List<EnumAccountUser> s_users =
        [
            new() { Id = "1", Role = UserRole.Admin },
            new() { Id = "2", Role = UserRole.Member },
            new() { Id = "3", Role = UserRole.Member }
        ];

        public IEnumerable<EnumAccountUser> GetUsers() => s_users;

        [Lookup]
        public EnumAccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(t => t.Id == id);
    }

    [GraphQLName("User")]
    public sealed class EnumAccountUser
    {
        [ID]
        public required string Id { get; init; }

        public UserRole Role { get; init; }
    }

    public sealed class NullFieldAccountsQuery
    {
        private static readonly List<PartialAccountUser> s_users =
        [
            new() { Id = "1", Name = "Alice" },
            new() { Id = "2", Name = null },
            new() { Id = "3", Name = "Carol" }
        ];

        public IEnumerable<PartialAccountUser> GetUsers() => s_users;

        [Lookup]
        public PartialAccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(t => t.Id == id);
    }

    public sealed class NumberKeyProductsQuery
    {
        private static readonly List<NumberKeyProduct> s_products =
        [
            new() { Id = 1, Name = "Table", Price = 899 },
            new() { Id = 2, Name = "Couch", Price = 1299 }
        ];

        public IEnumerable<NumberKeyProduct> GetProducts() => s_products;

        [Lookup]
        public NumberKeyProduct? GetProduct(int id)
            => s_products.FirstOrDefault(t => t.Id == id);
    }

    [GraphQLName("Product")]
    public sealed class NumberKeyProduct
    {
        public int Id { get; init; }

        public required string Name { get; init; }

        public int Price { get; init; }
    }

    public sealed class NumberKeyReviewsQuery
    {
        [Lookup, Internal]
        public NumberKeyReviewProduct? GetProduct(int id)
            => new() { Id = id };
    }

    [GraphQLName("Product")]
    public sealed class NumberKeyReviewProduct
    {
        public int Id { get; init; }

        public IEnumerable<NumberKeyReview> GetReviews()
            => [new NumberKeyReview { Id = "1", ProductId = Id }, new NumberKeyReview { Id = "2", ProductId = Id }];
    }

    [GraphQLName("Review")]
    public sealed class NumberKeyReview
    {
        [ID]
        public required string Id { get; init; }

        public int ProductId { get; init; }

        public NumberKeyReviewProduct GetProduct() => new() { Id = ProductId };
    }
}

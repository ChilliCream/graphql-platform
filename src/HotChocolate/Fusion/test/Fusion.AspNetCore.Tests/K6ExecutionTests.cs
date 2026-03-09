using System.Text.Json;
using HotChocolate;
using HotChocolate.Transport.Http;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class K6ExecutionTests : FusionTestBase
{
    [Fact]
    public async Task Execute_K6Query_With_RequestGrouping_Enabled_Completes_Without_Errors()
    {
        // arrange
        using var accountsApi = CreateSourceSchema(
            "accounts-api",
            builder => builder.AddQueryType<AccountsQuery>(d => d.Name("Query")));

        using var inventoryApi = CreateSourceSchema(
            "inventory-api",
            builder => builder.AddQueryType<InventoryQuery>(d => d.Name("Query")));

        using var productsApi = CreateSourceSchema(
            "products-api",
            builder => builder.AddQueryType<ProductsQuery>(d => d.Name("Query")));

        using var reviewsApi = CreateSourceSchema(
            "reviews-api",
            builder => builder.AddQueryType<ReviewsQuery>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [
                ("accounts-api", accountsApi),
                ("inventory-api", inventoryApi),
                ("products-api", productsApi),
                ("reviews-api", reviewsApi)
            ],
            configureGatewayBuilder: builder =>
                builder.ModifyPlannerOptions(options => options.EnableRequestGrouping = true));

        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        // act
        using var result = await client.PostAsync(
                K6Query,
                new Uri("http://localhost:5000/graphql"))
            .WaitAsync(TimeSpan.FromSeconds(10));

        using var response = await result.ReadAsResultAsync();

        // assert
        Assert.NotEqual(JsonValueKind.Array, response.Errors.ValueKind);
        Assert.Equal(JsonValueKind.Object, response.Data.ValueKind);

        var users = response.Data.GetProperty("users");
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.True(users.GetArrayLength() > 0);

        var topProducts = response.Data.GetProperty("topProducts");
        Assert.Equal(JsonValueKind.Array, topProducts.ValueKind);
        Assert.True(topProducts.GetArrayLength() > 0);

        var topProductReviews = topProducts[0].GetProperty("reviews");
        Assert.Equal(JsonValueKind.Array, topProductReviews.ValueKind);
        Assert.True(topProductReviews.GetArrayLength() > 0);
    }

    public sealed class AccountsQuery
    {
        private static readonly List<AccountUser> s_users =
        [
            new() { Id = "1", Name = "Uri Goldshtein", Username = "urigo", Birthday = 1234567890 },
            new() { Id = "2", Name = "Dotan Simha", Username = "dotansimha", Birthday = 1234567890 },
            new() { Id = "3", Name = "Kamil Kisiela", Username = "kamilkisiela", Birthday = 1234567890 },
            new() { Id = "4", Name = "Arda Tanrikulu", Username = "ardatan", Birthday = 1234567890 },
            new() { Id = "5", Name = "Gil Gardosh", Username = "gilgardosh", Birthday = 1234567890 },
            new() { Id = "6", Name = "Laurin Quast", Username = "laurin", Birthday = 1234567890 }
        ];

        public AccountUser? GetMe() => s_users[0];

        [Lookup]
        public AccountUser? GetUser([ID] string id)
            => s_users.FirstOrDefault(u => u.Id == id);

        public List<AccountUser> GetUsers() => s_users;
    }

    [GraphQLName("User")]
    public sealed class AccountUser
    {
        [ID]
        public required string Id { get; init; }

        public string? Name { get; init; }

        public string? Username { get; init; }

        public int? Birthday { get; init; }
    }

    public sealed class ProductsQuery
    {
        private static readonly List<ProductModel> s_products =
        [
            new() { Upc = "1", Name = "Table", Price = 899, Weight = 100 },
            new() { Upc = "2", Name = "Couch", Price = 1299, Weight = 1000 },
            new() { Upc = "3", Name = "Glass", Price = 15, Weight = 20 },
            new() { Upc = "4", Name = "Chair", Price = 499, Weight = 100 },
            new() { Upc = "5", Name = "TV", Price = 1299, Weight = 1000 },
            new() { Upc = "6", Name = "Lamp", Price = 6999, Weight = 300 },
            new() { Upc = "7", Name = "Grill", Price = 3999, Weight = 2000 },
            new() { Upc = "8", Name = "Fridge", Price = 100000, Weight = 6000 },
            new() { Upc = "9", Name = "Sofa", Price = 9999, Weight = 800 }
        ];

        public IEnumerable<ProductModel> GetTopProducts(int first = 5)
            => s_products.Take(first);

        [Lookup]
        public ProductModel? GetProduct([ID] string upc)
            => s_products.FirstOrDefault(p => p.Upc == upc);
    }

    [GraphQLName("Product")]
    public sealed class ProductModel
    {
        [ID]
        public required string Upc { get; init; }

        public required string Name { get; init; }

        public long Price { get; init; }

        public long Weight { get; init; }
    }

    public sealed class InventoryQuery
    {
        private static readonly Dictionary<string, InventoryProduct> s_inventory = new()
        {
            { "1", new InventoryProduct { Upc = "1", InStock = true } },
            { "2", new InventoryProduct { Upc = "2", InStock = false } },
            { "3", new InventoryProduct { Upc = "3", InStock = false } },
            { "4", new InventoryProduct { Upc = "4", InStock = false } },
            { "5", new InventoryProduct { Upc = "5", InStock = true } },
            { "6", new InventoryProduct { Upc = "6", InStock = true } },
            { "7", new InventoryProduct { Upc = "7", InStock = true } },
            { "8", new InventoryProduct { Upc = "8", InStock = false } },
            { "9", new InventoryProduct { Upc = "9", InStock = true } }
        };

        [Lookup, Internal]
        public InventoryProduct? GetProductByUpc([ID] string upc)
        {
            s_inventory.TryGetValue(upc, out var product);
            return product;
        }
    }

    [GraphQLName("Product")]
    public sealed class InventoryProduct
    {
        [ID]
        public required string Upc { get; init; }

        public bool InStock { get; init; }

        public long? GetShippingEstimate([Require] long weight, [Require] long price)
            => price > 1000 ? 0 : weight / 2;
    }

    public sealed class ReviewsQuery
    {
        [Lookup]
        public Review? GetReview([ID] string id)
            => ReviewRepository.GetById(id);

        [Lookup, Internal]
        public ReviewProduct GetProduct([ID] string upc)
            => new() { Upc = upc };

        [Lookup, Internal]
        public ReviewUser GetUser([ID] string id)
            => new() { Id = id };
    }

    [GraphQLName("User")]
    public sealed class ReviewUser
    {
        [ID]
        public required string Id { get; init; }

        public IEnumerable<Review> GetReviews()
            => ReviewRepository.GetByUserId(Id);
    }

    [GraphQLName("Product")]
    public sealed class ReviewProduct
    {
        [ID]
        public required string Upc { get; init; }

        public IEnumerable<Review> GetReviews()
            => ReviewRepository.GetByProductUpc(Upc);
    }

    public sealed class Review
    {
        [ID]
        public required string Id { get; init; }

        public required string Body { get; init; }

        public required string AuthorId { get; init; }

        public required string ProductUpc { get; init; }

        public ReviewUser GetAuthor()
            => new() { Id = AuthorId };

        public ReviewProduct GetProduct()
            => new() { Upc = ProductUpc };
    }

    public static class ReviewRepository
    {
        private static readonly List<Review> s_reviews =
        [
            new() { Id = "1", AuthorId = "1", ProductUpc = "1", Body = "Review 1" },
            new() { Id = "2", AuthorId = "1", ProductUpc = "1", Body = "Review 2" },
            new() { Id = "3", AuthorId = "1", ProductUpc = "1", Body = "Review 3" },
            new() { Id = "4", AuthorId = "1", ProductUpc = "1", Body = "Review 4" },
            new() { Id = "5", AuthorId = "1", ProductUpc = "2", Body = "Review 5" },
            new() { Id = "6", AuthorId = "1", ProductUpc = "2", Body = "Review 6" },
            new() { Id = "7", AuthorId = "1", ProductUpc = "2", Body = "Review 7" },
            new() { Id = "8", AuthorId = "1", ProductUpc = "2", Body = "Review 8" },
            new() { Id = "9", AuthorId = "1", ProductUpc = "3", Body = "Review 9" },
            new() { Id = "10", AuthorId = "1", ProductUpc = "4", Body = "Review 10" },
            new() { Id = "11", AuthorId = "1", ProductUpc = "4", Body = "Review 11" }
        ];

        public static IEnumerable<Review> GetByUserId(string authorId)
            => s_reviews.Take(2);

        public static IEnumerable<Review> GetByProductUpc(string upc)
            => s_reviews.Where(r => r.ProductUpc == upc);

        public static Review? GetById(string id)
            => s_reviews.FirstOrDefault(r => r.Id == id);
    }

    private const string K6Query =
        """
        query TestQuery {
          users {
            id
            username
            name
            reviews {
              id
              body
              product {
                inStock
                name
                price
                shippingEstimate
                upc
                weight
                reviews {
                  id
                  body
                  author {
                    id
                    username
                    name
                    reviews {
                      id
                      body
                      product {
                        inStock
                        name
                        price
                        shippingEstimate
                        upc
                        weight
                      }
                    }
                  }
                }
              }
            }
          }
          topProducts(first: 5) {
            inStock
            name
            price
            shippingEstimate
            upc
            weight
            reviews {
              id
              body
              author {
                id
                username
                name
                reviews {
                  id
                  body
                  product {
                    inStock
                    name
                    price
                    shippingEstimate
                    upc
                    weight
                  }
                }
              }
            }
          }
        }
        """;
}

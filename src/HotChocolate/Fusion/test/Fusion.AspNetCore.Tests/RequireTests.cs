using System.Text.Json;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public class RequireTests : FusionTestBase
{
    [Fact]
    public async Task Requirement_On_Leaf_Field()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableField: String
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableLeafFieldRequirement.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldWithNullableRequirement
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Requirement_On_Nullable_Leaf_Field_Returning_Null()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableField: String @null
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableLeafFieldRequirement.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldWithNullableRequirement
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Requirement_On_Property_Within_Nullable_Object()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableObject: Wrapper
            }

            type Wrapper {
              field: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableObjectFieldRequirement.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldWithNullableRequirement
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Requirement_On_Property_Within_Nullable_Object_Returning_Null()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              nullableObject: Wrapper @null
            }

            type Wrapper {
              field: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            b => b.AddQueryType<NullableObjectFieldRequirement.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldWithNullableRequirement
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Lookup_With_NonNull_Argument_Selecting_Nullable_Path_Returning_Null()
    {
        // arrange
        // The lookup argument is non-null, but its key selection resolves null
        // at runtime. The entity fetch must be skipped instead of sending an
        // invalid null variable to the source schema.
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              brand: Brand @null
            }

            type Brand {
              name: String!
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productByBrandName(brandName: String! @is(field: "brand.name")): Product @lookup @internal
            }

            type Product {
              fieldB: String
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldB
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Lookup_With_NonNull_Input_Field_Selecting_Nullable_Field_Returning_Null()
    {
        // arrange
        // The non-null input field of the lookup key cannot be satisfied by
        // the null value, so the entity fetch must be skipped instead of
        // sending an invalid input object to the source schema.
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              sku: String @null
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productByKey(key: ProductKeyInput! @is(field: "{ sku }")): Product @lookup @internal
            }

            type Product {
              fieldB: String
            }

            input ProductKeyInput {
              sku: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldB
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_With_NonNull_Input_Field_Selecting_Nullable_Field_Returning_Null()
    {
        // arrange
        // The non-null input field of the requirement cannot be satisfied by
        // the null value, so the entity fetch must be skipped instead of
        // sending an invalid input object to the source schema.
        var server1 = CreateSourceSchema(
            "A",
            """
            type Query {
              productById(id: ID!): Product @lookup
            }

            type Product {
              id: ID!
              sku: String @null
            }
            """);

        var server2 = CreateSourceSchema(
            "B",
            """
            type Query {
              productById(id: ID!): Product @lookup @internal
            }

            type Product {
              id: ID!
              fieldB(key: ProductKeyInput! @require(field: "{ sku }")): String
            }

            input ProductKeyInput {
              sku: String!
            }
            """);

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              productById(id: "1") {
                fieldB
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Theory]
    [InlineData("1", "WithPromoCode")]
    [InlineData("2", "WithoutPromoCode")]
    public async Task Require_Should_InvokeResolverWithProjectedValue_When_ProjectionTargetIsNullable(
        string cartId,
        string postFix)
    {
        // arrange
        // The promotions schema extends Cart with a nullable promoCode that is null for cart 2.
        using var server1 = CreateSourceSchema(
            "cart",
            b => b.AddQueryType<CartWithPromoCodeRequirement.Query>());

        using var server2 = CreateSourceSchema(
            "promotions",
            b => b.AddQueryType<Promotions.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("cart", server1),
            ("promotions", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            $$"""
            {
              cartById(id: "{{cartId}}") {
                discountPercent
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result, postFix);
    }

    [Fact]
    public async Task Require_Object_In_A_List()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<BookCatalog.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<BookInventory.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<BookShipping.Query>());

        using var server4 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<BookGenre.Query>()
                .AddType<BookGenre.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3),
            ("d", server4)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                books {
                  nodes {
                    title
                    estimatedDelivery
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_DoesNotLeak_RequirementOnlyChild_When_ClientSelectsParent()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<BookCatalog.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<BookInventory.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<BookShipping.Query>());

        using var server4 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<BookGenre.Query>()
                .AddType<BookGenre.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3),
            ("d", server4)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                books {
                  nodes {
                    title
                    dimension {
                      width
                    }
                    estimatedDelivery
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_Enumerable_In_List()
    {
        // arrange
        using var server1 = CreateSourceSchema(
            "a",
            b => b.AddQueryType<BookCatalog.Query>());

        using var server2 = CreateSourceSchema(
            "b",
            b => b.AddQueryType<BookInventory.Query>());

        using var server3 = CreateSourceSchema(
            "c",
            b => b.AddQueryType<BookShipping.Query>());
        using var server4 = CreateSourceSchema(
            "d",
            b => b.AddQueryType<BookGenre.Query>()
                .AddType<BookGenre.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("a", server1),
            ("b", server2),
            ("c", server3),
            ("d", server4)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
                books {
                  nodes {
                    title
                    genres {
                      name
                    }
                  }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact(Skip = "Not yet supported")]
    public async Task Require_On_MutationPayload()
    {
        // arrange
        var server1 = CreateSourceSchema(
            "A",
            """
            type User {
                id: ID!
                someField: String!
            }

            type Query {
                userById(id: ID!): User @lookup
            }
            """
        );

        var server2 = CreateSourceSchema(
            "B",
            """
            type User {
                id: ID!
                nestedField(someField: String! @require(field: "someField")): NestedType!
            }

            type NestedType {
                otherField: Int!
            }

            type Mutation {
                createUser: CreateUserPayload
            }

            type CreateUserPayload {
                user: User!
            }

            type Query {
                userById(id: ID!): User @lookup @internal
            }
            """
        );

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("A", server1),
            ("B", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            mutation {
                createUser {
                    user {
                        nestedField {
                            otherField
                        }
                    }
                }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await MatchSnapshotAsync(gateway, request, result);
    }

    [Fact]
    public async Task Require_Should_Fulfill_List_Requirement_When_Path_Traverses_Connection_With_Nested_Require()
    {
        // arrange
        // cart owns items and subtotal; subtotal requires items(first: 50).nodes[product.discountedPrice].
        // discountedPrice is owned by promotions and itself requires price, which is owned by products.
        using var server1 = CreateSourceSchema(
            "cart",
            b => b.AddQueryType<CartService.Query>());

        using var server2 = CreateSourceSchema(
            "products",
            b => b.AddQueryType<ProductPricing.Query>());

        using var server3 = CreateSourceSchema(
            "promotions",
            b => b.AddQueryType<ProductPromotions.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("cart", server1),
            ("products", server2),
            ("promotions", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              cart {
                subtotal
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"cart":{"subtotal":15}}
                    """,
                    response.Data.GetRawText());
            });
    }

    [Fact]
    public async Task Require_Should_Fulfill_Input_Object_List_Requirement_When_Path_Traverses_Connection_With_Nested_Require()
    {
        // arrange
        // same topology as above, the requirement uses the input object list form
        // items(first: 50).nodes[{ unitPrice: product.discountedPrice }].
        using var server1 = CreateSourceSchema(
            "cart",
            b => b.AddQueryType<CartService.Query>());

        using var server2 = CreateSourceSchema(
            "products",
            b => b.AddQueryType<ProductPricing.Query>());

        using var server3 = CreateSourceSchema(
            "promotions",
            b => b.AddQueryType<ProductPromotions.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("cart", server1),
            ("products", server2),
            ("promotions", server3)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              cart {
                lineTotal
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"cart":{"lineTotal":15}}
                    """,
                    response.Data.GetRawText());
            });
    }

    [Fact]
    public async Task Require_Should_Fulfill_Nested_Requirement_When_Two_Fields_Require_It_Next_To_Required_Parent()
    {
        // arrange
        // unitPrice and lineTotal both require product.discountedPrice, which is owned by
        // promotions, while the client also selects product next to them.
        using var server1 = CreateSourceSchema(
            "cart",
            b => b.AddQueryType<CartItemPricing.Query>());

        using var server2 = CreateSourceSchema(
            "promotions",
            b => b.AddQueryType<CartItemPromotions.Query>());

        using var gateway = await CreateCompositeSchemaAsync(
        [
            ("cart", server1),
            ("promotions", server2)
        ]);

        // act
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());

        var request = new OperationRequest(
            """
            {
              item {
                unitPrice
                lineTotal
                product {
                  id
                }
              }
            }
            """);

        using var result = await client.PostAsync(
            request,
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);

        // assert
        await AssertAndMatchSnapshotAsync(
            gateway,
            request,
            result,
            results =>
            {
                var response = Assert.Single(results);
                Assert.Equal(JsonValueKind.Undefined, response.Errors.ValueKind);
                Assert.Equal(
                    """
                    {"item":{"unitPrice":5,"lineTotal":15,"product":{"id":1}}}
                    """,
                    response.Data.GetRawText());
            });
    }

    private static class NullableLeafFieldRequirement
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public string GetFieldWithNullableRequirement([Require("nullableField")] string? nullableArgument)
            {
                return nullableArgument is null ? "Required field is null" : "Required field is not null";
            }
        }
    }

    private static class NullableObjectFieldRequirement
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById([ID] int id) => new Product(id);
        }

        public record Product([property: ID] int Id)
        {
            public string GetFieldWithNullableRequirement([Require("nullableObject.field")] string? nullableArgument)
            {
                return nullableArgument is null ? "Required field is null" : "Required field is not null";
            }
        }
    }

    private static class CartWithPromoCodeRequirement
    {
        public class Query
        {
            [Lookup]
            public Cart? GetCartById([ID] int id) => new Cart(id);
        }

        public record Cart([property: ID] int Id)
        {
            public int GetDiscountPercent(
                [Require("promoCode.{ discountPercent isExpired }")] PromoInput? promo)
                => promo is null || promo.IsExpired ? 0 : promo.DiscountPercent;
        }

        public class PromoInput
        {
            public required int DiscountPercent { get; set; }

            public required bool IsExpired { get; set; }
        }
    }

    private static class Promotions
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Cart? GetCartById([ID] int id) => new Cart(id);
        }

        public record Cart([property: ID] int Id)
        {
            public PromoCode? PromoCode
                => Id == 1 ? new PromoCode("SAVE10", 10, false) : null;
        }

        public record PromoCode(string Code, int DiscountPercent, bool IsExpired);
    }

    public static class BookCatalog
    {
        private static readonly Dictionary<int, Book> s_books = new()
        {
            {
                1, new Book { Id = 1, Title = "The Great Gatsby", Author = new Author { Id = 1 }, GenreIds = [1, 3] }
            },
            { 2, new Book { Id = 2, Title = "1984", Author = new Author { Id = 2 }, GenreIds = [2, 3] } },
            { 3, new Book { Id = 3, Title = "The Catcher in the Rye", Author = new Author { Id = 3 }, GenreIds = [1] } }
        };

        public class Query
        {
            [UsePaging]
            public Book[] GetBooks()
                => s_books.Values.ToArray();

            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => s_books.TryGetValue(id, out var book) ? book : null;
        }

        public class Book
        {
            public int Id { get; set; }

            public required string Title { get; set; }

            public required Author Author { get; set; }

            public required IEnumerable<int> GenreIds { get; set; }
        }

        public class Author
        {
            public int Id { get; set; }
        }
    }

    public static class BookInventory
    {
        private static readonly Dictionary<int, Book> s_books = new()
        {
            { 1, new Book { Id = 1, Dimension = new BookDimension { Width = 100, Height = 200 } } },
            { 2, new Book { Id = 2, Dimension = new BookDimension { Width = 150, Height = 300 } } },
            { 3, new Book { Id = 3, Dimension = new BookDimension { Width = 200, Height = 400 } } }
        };

        public class Query
        {
            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => s_books.TryGetValue(id, out var book) ? book : null;
        }

        public class Book
        {
            public int Id { get; set; }

            public required BookDimension Dimension { get; set; }
        }

        public class BookDimension
        {
            public int Width { get; set; }

            public int Height { get; set; }
        }
    }

    public static class BookGenre
    {
        private static readonly Dictionary<int, Genre> s_books = new()
        {
            { 1, new Genre { Id = 1, Name = "Fiction" } },
            { 2, new Genre { Id = 2, Name = "Science Fiction" } },
            { 3, new Genre { Id = 3, Name = "Classic" } }
        };

        public class Query
        {
            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => new() { Id = id };
        }

        public class Genre
        {
            public required int Id { get; set; }
            public required string Name { get; set; }
        }

        public class Book
        {
            public int Id { get; set; }

            public IEnumerable<Genre> Genres(
                [Require("genreIds")] IEnumerable<int> genreIds)
            {
                return genreIds.Select(id => s_books[id]);
            }
        }
    }

    public static class BookShipping
    {
        public class Query
        {
            [Lookup]
            [Shareable]
            public Book? GetBook(int id)
                => new() { Id = id };
        }

        public class Book
        {
            public required int Id { get; set; }

            public int EstimatedDelivery(
                [Require(
                    """
                    {
                      title,
                      width: dimension.width,
                      height: dimension.height
                    }
                    """)]
                BookDimensionInput dimension)
            {
                return dimension.Width + dimension.Height;
            }
        }
    }

    public class BookDimensionInput
    {
        public required string Title { get; set; }

        public required int Width { get; set; }

        public required int Height { get; set; }
    }

    public static class CartService
    {
        public class Query
        {
            public Cart GetCart() => new() { Id = 1 };

            [Lookup]
            [Internal]
            public Cart? GetCartById(int id) => new() { Id = id };

            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new() { Id = id };
        }

        public class Cart
        {
            public int Id { get; set; }

            [UsePaging]
            public CartItem[] GetItems() =>
            [
                new CartItem { Product = new Product { Id = 1 } },
                new CartItem { Product = new Product { Id = 2 } }
            ];

            public double GetSubtotal(
                [Require("items(first: 50).nodes[product.discountedPrice]")] List<double>? lines)
                => lines?.Sum() ?? -1;

            public double GetLineTotal(
                [Require("items(first: 50).nodes[{ unitPrice: product.discountedPrice }]")] List<CartLineInput>? lines)
                => lines?.Sum(line => line.UnitPrice) ?? -1;
        }

        public class CartItem
        {
            public required Product Product { get; set; }
        }

        public class Product
        {
            public int Id { get; set; }
        }

        public class CartLineInput
        {
            public double UnitPrice { get; set; }
        }
    }

    public static class ProductPricing
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new() { Id = id };
        }

        public class Product
        {
            public int Id { get; set; }

            public double GetPrice() => Id * 10;
        }
    }

    public static class ProductPromotions
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new() { Id = id };
        }

        public class Product
        {
            public int Id { get; set; }

            public double GetDiscountedPrice([Require("price")] double price) => price / 2;
        }
    }

    public static class CartItemPricing
    {
        public class Query
        {
            public CartItem GetItem() => new() { Id = 1, Quantity = 3, Product = new Product { Id = 1 } };

            [Lookup]
            [Internal]
            public CartItem? GetCartItemById(int id)
                => new() { Id = id, Quantity = 3, Product = new Product { Id = id } };
        }

        public class CartItem
        {
            public int Id { get; set; }

            public int Quantity { get; set; }

            public required Product Product { get; set; }

            public double GetUnitPrice([Require("product.discountedPrice")] double price) => price;

            public double GetLineTotal([Require("product.discountedPrice")] double price) => price * Quantity;
        }

        public class Product
        {
            [Shareable]
            public int Id { get; set; }
        }
    }

    public static class CartItemPromotions
    {
        public class Query
        {
            [Lookup]
            [Internal]
            public Product? GetProductById(int id) => new() { Id = id };
        }

        public class Product
        {
            public int Id { get; set; }

            public double GetDiscountedPrice() => Id * 5;
        }
    }
}

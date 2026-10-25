using HotChocolate.ApolloFederation;
using HotChocolate.ApolloFederation.Resolvers;
using HotChocolate.ApolloFederation.Types;
using HotChocolate.Transport;
using HotChocolate.Transport.Http;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using CompositionSourceSchemaOptions = HotChocolate.Fusion.Options.SourceSchemaOptions;

namespace HotChocolate.Fusion;

public sealed class UnreachableSourceFieldTests : FusionTestBase
{
    [Fact]
    public async Task Execute_Should_ResolveEntityFieldThroughLookup_When_EntityKeyIsResolvable()
    {
        // arrange
        using var a = CreateSourceSchema(
            "A",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<ResolvableReviews.Query>(d => d.Name("Query"))
                .AddType<ResolvableReviews.Product>());
        using var b = CreateSourceSchema(
            "B",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<Products.Query>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", a), ("B", b)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              products {
                id
                name
                reviewCount
              }
              topReviewer
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "p1",
                    "name": "Pen",
                    "reviewCount": 3
                  },
                  {
                    "id": "p2",
                    "name": "Pad",
                    "reviewCount": 7
                  }
                ],
                "topReviewer": "Rita"
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveFieldsFromOtherSchema_When_UnreachableNonResolvableEntityDeclaresOnlyKeyFields()
    {
        // arrange
        using var a = CreateSourceSchema(
            "A",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyOnlyReviews.Query>(d => d.Name("Query"))
                .AddType<KeyOnlyReviews.Product>());
        using var b = CreateSourceSchema(
            "B",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<Products.Query>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", a), ("B", b)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              products {
                id
                name
              }
              topReviewer
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "p1",
                    "name": "Pen"
                  },
                  {
                    "id": "p2",
                    "name": "Pad"
                  }
                ],
                "topReviewer": "Rita"
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveRootFields_When_NonResolvableEntityIsReachableOnlyThroughInaccessibleField()
    {
        // arrange
        using var a = CreateSourceSchema(
            "A",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<NonResolvableReviews.Query>(d => d.Name("Query"))
                .AddType<NonResolvableReviews.Product>());
        using var b = CreateSourceSchema(
            "B",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<HiddenProducts.Query>(d => d.Name("Query")));

        using var gateway = await CreateCompositeSchemaAsync(
            [("A", a), ("B", b)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              productCount
              topReviewer
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "productCount": 2,
                "topReviewer": "Rita"
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveFieldThroughLookup_When_SchemaKeysEntityThatOnlyAnotherSchemaReturns()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedAuthorCatalog.Query>(d => d.Name("Query"))
                .AddType<KeyedAuthorCatalog.Author>());
        using var authors = CreateSourceSchema(
            "authors",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedAuthorAuthors.Query>(d => d.Name("Query")));
        using var pricing = CreateSourceSchema(
            "pricing",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedAuthorPricing.Query>(d => d.Name("Query"))
                .AddType<KeyedAuthorPricing.Book>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("authors", authors), ("pricing", pricing)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              books {
                id
                price
                author {
                  id
                  name
                }
              }
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "books": [
                  {
                    "id": "b1",
                    "price": 12,
                    "author": {
                      "id": "u1",
                      "name": "Ursula"
                    }
                  },
                  {
                    "id": "b2",
                    "price": 18,
                    "author": {
                      "id": "u2",
                      "name": "Umberto"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveFieldThroughLookup_When_SchemaKeysTypeThatOnlyAnotherSchemaReturns()
    {
        // arrange
        using var catalog = CreateSourceSchema(
            "catalog",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedWarehouseCatalog.Query>(d => d.Name("Query"))
                .AddType<KeyedWarehouseCatalog.Warehouse>());
        using var inventory = CreateSourceSchema(
            "inventory",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedWarehouseInventory.Query>(d => d.Name("Query")));
        using var pricing = CreateSourceSchema(
            "pricing",
            builder => builder
                .AddApolloFederation()
                .AddQueryType<KeyedWarehousePricing.Query>(d => d.Name("Query"))
                .AddType<KeyedWarehousePricing.Product>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("catalog", catalog), ("inventory", inventory), ("pricing", pricing)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false);

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              products {
                id
                price
                warehouse {
                  id
                  address
                }
              }
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "id": "p1",
                    "price": 40,
                    "warehouse": {
                      "id": "w1",
                      "address": "1 Dock Road"
                    }
                  },
                  {
                    "id": "p2",
                    "price": 55,
                    "warehouse": {
                      "id": "w2",
                      "address": "2 Dock Road"
                    }
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task Execute_Should_ResolveExtensionFieldThroughLookup_When_FederationV1ExtensionDeclaresKey()
    {
        // arrange
        using var authors = CreateSourceSchema(
            "authors",
            builder => builder
                .AddApolloFederation(FederationVersion.Federation10)
                .AddQueryType<FederationV1Authors.Query>(d => d.Name("Query")));
        using var awards = CreateSourceSchema(
            "awards",
            builder => builder
                .AddApolloFederation(FederationVersion.Federation10)
                .AddQueryType<FederationV1Awards.Query>(d => d.Name("Query"))
                .AddType<FederationV1Awards.Author>());

        using var gateway = await CreateCompositeSchemaAsync(
            [("authors", authors), ("awards", awards)],
            enableGlobalObjectIdentification: false,
            includeOperationPlan: false,
            configureComposerOptions: o =>
            {
                o.SourceSchemas["authors"] = new CompositionSourceSchemaOptions { IsApolloFederationV1 = true };
                o.SourceSchemas["awards"] = new CompositionSourceSchemaOptions { IsApolloFederationV1 = true };
            });

        // act
        using var response = await ExecuteAsync(
            gateway,
            """
            {
              authors {
                id
                name
                awards {
                  id
                  title
                }
              }
            }
            """);

        // assert
        response.MatchInlineSnapshot(
            """
            {
              "data": {
                "authors": [
                  {
                    "id": "u1",
                    "name": "Ursula",
                    "awards": [
                      {
                        "id": "w1",
                        "title": "award-u1"
                      }
                    ]
                  },
                  {
                    "id": "u2",
                    "name": "Umberto",
                    "awards": [
                      {
                        "id": "w2",
                        "title": "award-u2"
                      }
                    ]
                  }
                ]
              }
            }
            """);
    }

    private static async Task<OperationResult> ExecuteAsync(Gateway gateway, string operation)
    {
        using var client = GraphQLHttpClient.Create(gateway.CreateClient());
        using var result = await client.PostAsync(
            new OperationRequest(operation),
            new Uri("http://localhost:5000/graphql"),
            TestContext.Current.CancellationToken);
        return await result.ReadAsResultAsync(TestContext.Current.CancellationToken);
    }

    // B lists the products p1 and p2 with their names.
    public static class Products
    {
        public sealed class Query
        {
            public IEnumerable<Product> GetProducts()
                => [new Product { Id = "p1", Name = "Pen" }, new Product { Id = "p2", Name = "Pad" }];
        }

        [Key("id")]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public required string Name { get; init; }

            [ReferenceResolver]
            public static Product ResolveReference(string id) => new() { Id = id, Name = $"name-{id}" };
        }
    }

    // B hides its product list and serves the product count.
    public static class HiddenProducts
    {
        public sealed class Query
        {
            [Inaccessible]
            public IEnumerable<Products.Product> GetProducts()
                => [new Products.Product { Id = "p1", Name = "Pen" }, new Products.Product { Id = "p2", Name = "Pad" }];

            public int GetProductCount() => 2;
        }
    }

    // A serves the top reviewer and the review count of each product by its key.
    public static class ResolvableReviews
    {
        public sealed class Query
        {
            public string GetTopReviewer() => "Rita";
        }

        [Key("id")]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int ReviewCount { get; init; }

            [ReferenceResolver]
            public static Product ResolveReference(string id)
                => new() { Id = id, ReviewCount = id == "p1" ? 3 : 7 };
        }
    }

    // A serves the top reviewer and declares Product with only its non-resolvable key.
    public static class KeyOnlyReviews
    {
        public sealed class Query
        {
            public string GetTopReviewer() => "Rita";
        }

        [Key("id", resolvable: false)]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }
        }
    }

    // A serves the top reviewer and declares Product with a review count under a
    // non-resolvable key.
    public static class NonResolvableReviews
    {
        public sealed class Query
        {
            public string GetTopReviewer() => "Rita";
        }

        [Key("id", resolvable: false)]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int ReviewCount { get; init; }
        }
    }

    // catalog lists the books b1 and b2 and resolves the name of each author by its key.
    public static class KeyedAuthorCatalog
    {
        public sealed class Query
        {
            [Shareable]
            public IEnumerable<Book> GetBooks() => [new Book { Id = "b1" }, new Book { Id = "b2" }];
        }

        [Key("id")]
        [Shareable]
        public sealed class Book
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Shelf> GetShelves() => [new Shelf { Id = "s1" }];

            [ReferenceResolver]
            public static Book ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        [Shareable]
        public sealed class Shelf
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Book> GetBooks() => [new Book { Id = "b1" }, new Book { Id = "b2" }];

            [ReferenceResolver]
            public static Shelf ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Author
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public required string Name { get; init; }

            [ReferenceResolver]
            public static Author ResolveReference(string id)
                => new() { Id = id, Name = id == "u1" ? "Ursula" : "Umberto" };
        }
    }

    // authors lists the books b1 and b2 and holds the author of each book.
    public static class KeyedAuthorAuthors
    {
        public sealed class Query
        {
            [Shareable]
            public IEnumerable<Book> GetBooks() => [new Book { Id = "b1" }, new Book { Id = "b2" }];
        }

        [Key("id")]
        [Shareable]
        public sealed class Book
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public Author GetAuthor() => new() { Id = Id == "b1" ? "u1" : "u2" };

            [ReferenceResolver]
            public static Book ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        [Shareable]
        public sealed class Shelf
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Book> GetBooks() => [new Book { Id = "b1" }, new Book { Id = "b2" }];

            [ReferenceResolver]
            public static Shelf ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Author
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [ReferenceResolver]
            public static Author ResolveReference(string id) => new() { Id = id };
        }
    }

    // pricing holds the price of each book.
    public static class KeyedAuthorPricing
    {
        public sealed class Query;

        [Key("id")]
        [Shareable]
        public sealed class Book
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int Price { get; init; }

            [ReferenceResolver]
            public static Book ResolveReference(string id) => new() { Id = id, Price = id == "b1" ? 12 : 18 };
        }
    }

    // catalog lists the products p1 and p2 and resolves the address of each warehouse by its key.
    public static class KeyedWarehouseCatalog
    {
        public sealed class Query
        {
            [Shareable]
            public IEnumerable<Product> GetProducts() => [new Product { Id = "p1" }, new Product { Id = "p2" }];
        }

        [Key("id")]
        [Shareable]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Seller> GetSellers() => [new Seller { Id = "s1" }];

            [ReferenceResolver]
            public static Product ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        [Shareable]
        public sealed class Seller
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Product> GetProducts() => [new Product { Id = "p1" }, new Product { Id = "p2" }];

            [ReferenceResolver]
            public static Seller ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Warehouse
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public required string Address { get; init; }

            [ReferenceResolver]
            public static Warehouse ResolveReference(string id)
                => new() { Id = id, Address = id == "w1" ? "1 Dock Road" : "2 Dock Road" };
        }
    }

    // inventory lists the products p1 and p2 and holds the warehouse of each product.
    public static class KeyedWarehouseInventory
    {
        public sealed class Query
        {
            [Shareable]
            public IEnumerable<Product> GetProducts() => [new Product { Id = "p1" }, new Product { Id = "p2" }];
        }

        [Key("id")]
        [Shareable]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public Warehouse GetWarehouse() => new() { Id = Id == "p1" ? "w1" : "w2" };

            [ReferenceResolver]
            public static Product ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        [Shareable]
        public sealed class Seller
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public IEnumerable<Product> GetProducts() => [new Product { Id = "p1" }, new Product { Id = "p2" }];

            [ReferenceResolver]
            public static Seller ResolveReference(string id) => new() { Id = id };
        }

        [Key("id")]
        public sealed class Warehouse
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            [ReferenceResolver]
            public static Warehouse ResolveReference(string id) => new() { Id = id };
        }
    }

    // pricing holds the price of each product.
    public static class KeyedWarehousePricing
    {
        public sealed class Query;

        [Key("id")]
        [Shareable]
        public sealed class Product
        {
            [GraphQLType<NonNullType<IdType>>]
            public required string Id { get; init; }

            public int Price { get; init; }

            [ReferenceResolver]
            public static Product ResolveReference(string id) => new() { Id = id, Price = id == "p1" ? 40 : 55 };
        }
    }

    // authors lists the authors u1 and u2 with their names.
    public static class FederationV1Authors
    {
        public sealed class Query
        {
            public IEnumerable<Author> GetAuthors()
                => [new Author { Id = "u1", Name = "Ursula" }, new Author { Id = "u2", Name = "Umberto" }];
        }

        [Key("id")]
        public sealed class Author
        {
            public required string Id { get; init; }

            public required string Name { get; init; }

            [ReferenceResolver]
            public static Author ResolveReference(string id)
                => new() { Id = id, Name = id == "u1" ? "Ursula" : "Umberto" };
        }
    }

    // awards extends Author by its key with the awards of each author.
    public static class FederationV1Awards
    {
        public sealed class Query;

        [ExtendServiceType]
        [Key("id")]
        public sealed class Author
        {
            [External]
            public required string Id { get; init; }

            public IEnumerable<Award> GetAwards() => [new Award(Id == "u1" ? "w1" : "w2", $"award-{Id}")];

            [ReferenceResolver]
            public static Author ResolveReference(string id) => new() { Id = id };
        }

        public sealed record Award(string Id, string Title);
    }
}

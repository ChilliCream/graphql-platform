using HotChocolate.Execution;
using HotChocolate.Types.Composite;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Fusion;

public sealed class InMemoryConnectorTests
{
    [Fact]
    public async Task Execute_Should_ReturnData_When_SingleInMemorySchema()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddGraphQL("products")
            .AddQueryType<SingleSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQLGateway()
            .AddInMemorySchema("products");

        var executor = await services.BuildGatewayAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Execute_Should_ReturnComposedData_When_TwoInMemorySchemas()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddGraphQL("products")
            .AddQueryType<TwoSchemas.ProductsSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQL("reviews")
            .AddQueryType<TwoSchemas.ReviewsSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQLGateway()
            .AddInMemorySchema("products")
            .AddInMemorySchema("reviews");

        var executor = await services.BuildGatewayAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              productById(id: 1) {
                id
                name
              }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Execute_Should_ReturnProductAndReviews_When_CrossSchemaLookup()
    {
        // arrange
        var services = new ServiceCollection();

        services.AddGraphQL("products")
            .AddQueryType<CrossSchema.ProductsSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQL("reviews")
            .AddQueryType<CrossSchema.ReviewsSchema.Query>()
            .AddSourceSchemaDefaults();

        services.AddGraphQLGateway()
            .AddInMemorySchema("products")
            .AddInMemorySchema("reviews");

        var executor = await services.BuildGatewayAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              products {
                id
                name
                reviews {
                  body
                  stars
                }
              }
            }
            """);

        // assert
        result.MatchMarkdownSnapshot();
    }

    private static class SingleSchema
    {
        public class Query
        {
            [Lookup]
            public Product? GetProductById(int id)
                => id is >= 1 and <= 3
                    ? new Product(id, $"Product {id}")
                    : null;
        }

        [EntityKey("id")]
        public record Product(int Id, string Name);
    }

    private static class TwoSchemas
    {
        public static class ProductsSchema
        {
            public class Query
            {
                [Lookup]
                public Product? GetProductById(int id)
                    => id is >= 1 and <= 3
                        ? new Product(id, $"Product {id}")
                        : null;
            }

            [EntityKey("id")]
            public record Product(int Id, string Name);
        }

        public static class ReviewsSchema
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Product? GetProductById(int id)
                    => id is >= 1 and <= 3
                        ? new Product(id)
                        : null;
            }

            [EntityKey("id")]
            [GraphQLName("Product")]
            public record Product(int Id);
        }
    }

    private static class CrossSchema
    {
        public static class ProductsSchema
        {
            public class Query
            {
                public IEnumerable<Product> GetProducts()
                    =>
                    [
                        new Product(1, "Product A"),
                        new Product(2, "Product B")
                    ];

                [Lookup]
                [Internal]
                public Product? GetProductById(int id)
                    => id switch
                    {
                        1 => new Product(1, "Product A"),
                        2 => new Product(2, "Product B"),
                        _ => null
                    };
            }

            [EntityKey("id")]
            public record Product(int Id, string Name);
        }

        public static class ReviewsSchema
        {
            public class Query
            {
                [Lookup]
                [Internal]
                public Product? GetProductById(int id)
                    => id is >= 1 and <= 2
                        ? new Product(id)
                        : null;
            }

            [EntityKey("id")]
            [GraphQLName("Product")]
            public record Product(int Id)
            {
                public IEnumerable<Review> GetReviews()
                    =>
                    [
                        new Review(1, "Great product!", 5),
                        new Review(2, "Not bad", 3)
                    ];
            }

            public record Review(int Id, string Body, int Stars);
        }
    }
}

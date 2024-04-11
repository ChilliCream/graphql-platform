using HotChocolate.Data.TestContext;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

public class IntegrationPagingHelperTests(PostgreSqlResource resource) : IClassFixture<PostgreSqlResource>
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task GetDefaultPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        
        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                """
                {
                    brands {
                        nodes {
                            id
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }
                """);

        // Assert
        result.MatchMarkdownSnapshot();
    }
    
    [Fact]
    public async Task GetSecondPage_With_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        
        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                """
                {
                    brands(first: 2, after: "QnJhbmQxNzoxOA==") {
                        nodes {
                            id
                            name
                        }
                        pageInfo {
                            hasNextPage
                            hasPreviousPage
                            startCursor
                            endCursor
                        }
                    }
                }
                """);

        // Assert
        result.MatchMarkdownSnapshot();
    }
    
    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt", };
        context.ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand { Name = "Brand" + i, };
            context.Brands.Add(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product
                {
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand,
                };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }

    public class Query
    {
        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsAsync(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
            => await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
    }
}
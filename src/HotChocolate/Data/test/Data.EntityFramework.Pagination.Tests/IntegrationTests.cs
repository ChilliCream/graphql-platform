using CookieCrumble;
using HotChocolate.Data.Sorting;
using HotChocolate.Data.TestContext;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationTests(PostgreSqlResource resource)
{
    private string CreateConnectionString()
        => resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_With_Default_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 10) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Next_2_With_Default_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddSorting()
            .AddDbContextCursorPagingProvider()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 2, after: "MTA=") {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_With_User_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 10, order: { name: ASC }) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Next_2_With_User_Sorting()
    {
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var executor = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQLServer()
            .AddQueryType<Query>()
            .AddDbContextCursorPagingProvider()
            .AddSorting()
            .BuildRequestExecutorAsync();

        var result = await executor.ExecuteAsync(q => q
            .SetDocument(
                """
                {
                    brands(first: 2, after: "QnJhbmQxNzoxOA==", order: { name: ASC }) {
                        nodes {
                            name
                        }
                        pageInfo {
                            endCursor
                        }
                    }
                }
                """)
            .SetGlobalState("printSQL", true));

        result.MatchMarkdownSnapshot();
    }

    public class Query
    {
        [UsePaging]
        [UseSorting]
        public IQueryable<Brand> GetBrands(CatalogContext context, ISortingContext sorting)
        {
            sorting.Handled(false);
            sorting.OnAfterSortingApplied<IQueryable<Brand>>(
                static (sortingApplied, query) =>
                {
                    if (sortingApplied)
                    {
                        return ((IOrderedQueryable<Brand>)query).ThenBy(b => b.Id);
                    }

                    return query.OrderBy(b => b.Id);
                });

            return context.Brands;
        }
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt", };
        context.ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand" + i,
                DisplayName = i % 2 == 0 ? "BrandDisplay" + i : null,
                BrandDetails = new() { Country = new() { Name = "Country" + i } }
            };
            context.Brands.Add(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product
                {
                    Name = $"Product {i}-{j}", Type = type, Brand = brand,
                };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }
}

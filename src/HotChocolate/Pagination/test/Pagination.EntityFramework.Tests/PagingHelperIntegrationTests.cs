using HotChocolate.Data.TestContext;
using CookieCrumble;
using HotChocolate.Execution;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

public class IntegrationPagingHelperTests(PostgreSqlResource resource)
    : IClassFixture<PostgreSqlResource>
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
    public async Task GetDefaultPage2()
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
                    brands2 {
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

    [Fact]
    public async Task GetDefaultPage_With_Nullable()
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
                    brandsNullable {
                        edges {
                            cursor
                        }
                        nodes {
                            id
                            name
                            displayName
                            brandDetails {
                                country {
                                    name
                                }
                            }
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
    public async Task GetDefaultPage_With_Nullable_SecondPage()
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
                    brandsNullable(first: 2, after: "QnJhbmQxMDpcbnVsbDoxMQ==") {
                        edges {
                            cursor
                        }
                        nodes {
                            id
                            name
                            displayName
                            brandDetails {
                                country {
                                    name
                                }
                            }
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
    public async Task GetDefaultPage_With_Nullable_Fallback()
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
                    brandsNullableFallback {
                        edges {
                            cursor
                        }
                        nodes {
                            id
                            name
                            displayName
                            brandDetails {
                                country {
                                    name
                                }
                            }
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
    public async Task GetDefaultPage_With_Nullable_Fallback_SecondPage()
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
                    brandsNullableFallback(first: 2, after: "QnJhbmQxMToxMg==") {
                        edges {
                            cursor
                        }
                        nodes {
                            id
                            name
                            displayName
                            brandDetails {
                                country {
                                    name
                                }
                            }
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
    public async Task GetDefaultPage_With_Deep()
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
                    brandsDeep {
                        edges {
                            cursor
                        }
                        nodes {
                            id
                            name
                            displayName
                            brandDetails {
                                country {
                                    name
                                }
                            }
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
    public async Task GetDefaultPage_With_Deep_SecondPage()
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
                @"
                    {
                        brandsDeep(first: 2, after: ""Q291bnRyeTE6Mg=="") {
                            edges {
                                cursor
                            }
                            nodes {
                                id
                                name
                                displayName
                                brandDetails {
                                    country {
                                        name
                                    }
                                }
                            }
                            pageInfo {
                                hasNextPage
                                hasPreviousPage
                                startCursor
                                endCursor
                            }
                        }
                    }
                    ");

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

        [UsePaging]
        public async Task<Connection<Brand>> GetBrands2Async(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
        {
            var page = await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct);

            return page.ToConnection();
        }

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsNullable(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
            => await context.Brands
                .OrderBy(t => t.Name)
                .ThenBy(x => x.AlwaysNull)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsNullableFallback(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
            => await context.Brands
                .OrderBy(t => t.DisplayName ?? t.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();

        [UsePaging]
        public async Task<Connection<Brand>> GetBrandsDeep(
            CatalogContext context,
            PagingArguments arguments,
            CancellationToken ct)
            => await context.Brands
                .OrderBy(x => x.BrandDetails.Country.Name)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments, cancellationToken: ct)
                .ToConnectionAsync();
    }
}

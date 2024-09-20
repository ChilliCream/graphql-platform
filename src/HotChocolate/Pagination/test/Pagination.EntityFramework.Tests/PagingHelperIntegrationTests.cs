using System.Collections.Immutable;
using HotChocolate.Data.TestContext;
using CookieCrumble;
using GreenDonut;
using GreenDonut.Projections;
using HotChocolate.Execution;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Pagination;
using HotChocolate.Resolvers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationPagingHelperTests(PostgreSqlResource resource)
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

    [Fact]
    public async Task Nested_Paging_First_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(BrandExtensions))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brands(first: 2) {
                        edges {
                            cursor
                        }
                        nodes {
                            products(first: 2) {
                                nodes {
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
                    }
                }
                """);

        // Assert
        var operationResult = result.ExpectOperationResult();
        var sql = operationResult.Extensions!["sql"]!.ToString();

#if NET8_0_OR_GREATER
        await Snapshot.Create()
#elif NET7_0_OR_GREATER
        await Snapshot.Create("NET7")
#else
        await Snapshot.Create("NET6")
#endif
            .Add(sql, "SQL", "sql")
            .Add(operationResult.WithExtensions(ImmutableDictionary<string, object?>.Empty))
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Nested_Paging_First_2_With_Projections()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddTypeExtension(typeof(BrandExtensionsWithSelect))
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brands(first: 2) {
                        edges {
                            cursor
                        }
                        nodes {
                            products(first: 2) {
                                nodes {
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
                    }
                }
                """);

        // Assert
        var operationResult = result.ExpectOperationResult();
        var sql = operationResult.Extensions!["sql"]!.ToString();

#if NET8_0_OR_GREATER
        await Snapshot.Create()
#elif NET7_0_OR_GREATER
        await Snapshot.Create("NET7")
#else
        await Snapshot.Create("NET6")
#endif
            .Add(sql, "SQL", "sql")
            .Add(operationResult.WithExtensions(ImmutableDictionary<string, object?>.Empty))
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_Empty_PagingArgs()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments();
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .Add(new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .Add(new
            {
                result.HasNextPage,
                result.HasPreviousPage,
                First = result.First?.Id,
                FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                Last = result.Last?.Id,
                LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
            })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_After_Id_13()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 5, After = "QnJhbmQxMjoxMw==" };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .Add(new
            {
                result.HasNextPage,
                result.HasPreviousPage,
                First = result.First?.Id,
                FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                Last = result.Last?.Id,
                LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
            })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_Last_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .Add(new
            {
                result.HasNextPage,
                result.HasPreviousPage,
                First = result.First?.Id,
                FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                Last = result.Last?.Id,
                LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
            })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_Before_Id_96()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 5, Before = "QnJhbmQ5NTo5Ng==" };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot.Create()
            .Add(new
            {
                result.HasNextPage,
                result.HasPreviousPage,
                First = result.First?.Id,
                FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                Last = result.Last?.Id,
                LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
            })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task BatchPaging_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
        var snapshot = Snapshot.Create();
        foreach (var page in results)
        {
            snapshot.Add(
                new
                {
                    First = page.Value.CreateCursor(page.Value.First!),
                    Last = page.Value.CreateCursor(page.Value.Last!),
                    page.Value.Items
                },
                name: page.Key.ToString());
        }
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchPaging_Last_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
        var snapshot = Snapshot.Create();
        foreach (var page in results)
        {
            snapshot.Add(
                new
                {
                    First = page.Value.CreateCursor(page.Value.First!),
                    Last = page.Value.CreateCursor(page.Value.Last!),
                    page.Value.Items
                },
                name: page.Key.ToString());
        }
        snapshot.MatchMarkdownSnapshot();
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

    [ExtendObjectType<Brand>]
    public static class BrandExtensions
    {
        [UsePaging]
        public static async Task<Connection<Product>> GetProducts(
            [Parent] Brand brand,
            ProductsByBrandDataLoader dataLoader,
            PagingArguments arguments,
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            var sql = new SqlQuery();

            var stateFullDataLoader = dataLoader
                .WithPagingArguments(arguments)
                .SetState(sql);

            var result = await stateFullDataLoader
                .LoadAsync(brand.Id, cancellationToken)
                .ToConnectionAsync();

            if (!string.IsNullOrEmpty(sql.Text))
            {
                ((IMiddlewareContext)context).OperationResult.SetExtension("sql", sql.Text);
            }

            return result;
        }
    }

    [ExtendObjectType<Brand>]
    public static class BrandExtensionsWithSelect
    {
        [UsePaging]
        public static async Task<Connection<Product>> GetProducts(
            [Parent] Brand brand,
            ProductsByBrandDataLoader dataLoader,
            ISelection selection,
            PagingArguments arguments,
            IResolverContext context,
            CancellationToken cancellationToken)
        {
            var sql = new SqlQuery();
            var stateFullDataLoader =
                dataLoader
                    .WithPagingArguments(arguments)
                    .Select(selection)
                    .SetState(sql);

            var result = await stateFullDataLoader
                .LoadAsync(brand.Id, cancellationToken)
                .ToConnectionAsync();

            if (!string.IsNullOrEmpty(sql.Text))
            {
                ((IMiddlewareContext)context).OperationResult.SetExtension("sql", sql.Text);
            }

            return result;
        }
    }

    public class ProductsByBrandDataLoader : StatefulBatchDataLoader<int, Page<Product>>
    {
        private readonly IServiceProvider _services;

        public ProductsByBrandDataLoader(
            IServiceProvider services,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options)
            : base(batchScheduler, options)
        {
            _services = services;
        }

        protected override async Task<IReadOnlyDictionary<int, Page<Product>>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Page<Product>> context,
            CancellationToken cancellationToken)
        {
            var pagingArgs = context.GetPagingArguments();
            var sql = context.GetState<SqlQuery>();

            await using var scope = _services.CreateAsyncScope();
            await using var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogContext>();

            sql!.Text = catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .Select(context.GetSelector(), b => b.Id)
                .OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToQueryString();

            return await catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .Select(context.GetSelector(), b => b.Id)
                .OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
        }
    }

    private class SqlQuery
    {
        public string? Text { get; set; }
    }
}

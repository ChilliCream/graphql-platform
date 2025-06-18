using System.Linq.Expressions;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingHelperIntegrationTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_Empty_PagingArgs()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments();
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
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
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
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
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            First = 5,
            After = "QnJhbmQxMjoxMw=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
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
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstName = result.First?.Name,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastName = result.Last?.Name,
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
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            Last = 5,
            Before = "QnJhbmQ5NTo5Ng=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
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
    public async Task Paging_WithChildCollectionProjectionExpression_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            First = 5
        };

        var result = await context.Brands
            .Select(BrandWithProductsDto.Projection)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(pagingArgs);

        // Assert
        await Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries)
            .Add(
                new
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
        var snapshot =
            Snapshot.Create(
                postFix:
                    TestEnvironment.TargetFramework == "NET8_0"
                        ? TestEnvironment.TargetFramework
                        : null);

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
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

        snapshot.AddQueries(capture.Queries);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchPaging_Last_5()
    {
        // Arrange
        var snapshot =
            Snapshot.Create(
                postFix:
                    TestEnvironment.TargetFramework == "NET8_0"
                        ? TestEnvironment.TargetFramework
                        : null);

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 2 };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
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

        snapshot.AddQueries(capture.Queries);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task BatchPaging_With_Relative_Cursor()
    {
        // Arrange
        var snapshot =
            Snapshot.Create(
                postFix:
                    TestEnvironment.TargetFramework == "NET8_0"
                        ? TestEnvironment.TargetFramework
                        : null);

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        await using var context = new CatalogContext(connectionString);

        // Act
        var pagingArgs = new PagingArguments { First = 2, EnableRelativeCursors = true };

        var results = await context.Products
            .Where(t => t.BrandId == 1 || t.BrandId == 2 || t.BrandId == 3)
            .OrderBy(p => p.Id)
            .ToBatchPageAsync(k => k.BrandId, pagingArgs);

        // Assert
        foreach (var page in results)
        {
            snapshot.Add(
                new
                {
                    First = page.Value.CreateCursor(page.Value.First!, 0),
                    Last = page.Value.CreateCursor(page.Value.Last!, 0),
                    page.Value.Items
                },
                name: page.Key.ToString());
        }

        snapshot.AddQueries(capture.Queries);
        snapshot.MatchMarkdownSnapshot();
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType
        {
            Name = "T-Shirt"
        };
        context.ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand:" + i,
                DisplayName = i % 2 == 0 ? "BrandDisplay" + i : null,
                BrandDetails = new() { Country = new() { Name = "Country" + i } }
            };
            context.Brands.Add(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product
                {
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand
                };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }

    public class BrandDto
    {
        public BrandDto(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    public class BrandWithProductsDto
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyCollection<ProductDto> Products { get; init; }

        public static Expression<Func<Brand, BrandWithProductsDto>> Projection
            => brand => new BrandWithProductsDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Products = brand.Products.AsQueryable().Select(ProductDto.Projection).ToList()
            };
    }

    public class ProductDto
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public static Expression<Func<Product, ProductDto>> Projection
            => product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name
            };
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
            var selector = context.GetSelector();

            await using var scope = _services.CreateAsyncScope();
            await using var catalogContext = scope.ServiceProvider.GetRequiredService<CatalogContext>();

            return await catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .Select(b => b.BrandId, selector)
                .OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
        }
    }
}

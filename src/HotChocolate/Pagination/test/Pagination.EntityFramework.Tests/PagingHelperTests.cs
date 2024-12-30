using HotChocolate.Data.TestContext;
using HotChocolate.Pagination;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingHelperTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Fetch_First_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // .. get first page
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Third_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // .. get first page
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // .. get first page
        var arguments = new PagingArguments(4);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.First!), before: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(last: 2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Before_Last_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // .. get last page
        var arguments = new PagingArguments(last: 2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = arguments with { Before = page.CreateCursor(page.First!), };
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // .. get last page
        var arguments = new PagingArguments(last: 4);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(after: page.CreateCursor(page.First!), last: 2, before: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Batch_Fetch_First_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        int[] brandIds = [1, 2, 3];
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var pages = await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .ToBatchPageAsync(t => t.BrandId, arguments);

        // Assert
        var snapshot = Snapshot.Create();
        foreach (var page in pages)
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
    public async Task Fetch_First_2_Items_Second_Page_Descending_AllTypes()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedTestAsync(connectionString);

        await using var context = new CatalogContext(connectionString);

        Dictionary<string, IOrderedQueryable<Test>> queries = new()
        {
            { "Bool", context.Tests.OrderByDescending(t => t.Bool) },
            { "DateOnly", context.Tests.OrderByDescending(t => t.DateOnly) },
            { "DateTime", context.Tests.OrderByDescending(t => t.DateTime) },
            { "DateTimeOffset", context.Tests.OrderByDescending(t => t.DateTimeOffset) },
            { "Decimal", context.Tests.OrderByDescending(t => t.Decimal) },
            { "Double", context.Tests.OrderByDescending(t => t.Double) },
            { "Float", context.Tests.OrderByDescending(t => t.Float) },
            { "Guid", context.Tests.OrderByDescending(t => t.Guid) },
            { "Int", context.Tests.OrderByDescending(t => t.Int) },
            { "Long", context.Tests.OrderByDescending(t => t.Long) },
            { "Short", context.Tests.OrderByDescending(t => t.Short) },
            { "String", context.Tests.OrderByDescending(t => t.String) },
            { "TimeOnly", context.Tests.OrderByDescending(t => t.TimeOnly) },
            { "UInt", context.Tests.OrderByDescending(t => t.UInt) },
            { "ULong", context.Tests.OrderByDescending(t => t.ULong) },
            { "UShort", context.Tests.OrderByDescending(t => t.UShort) }
        };

        // Act
        Dictionary<string, Page<Test>> pages = [];

        foreach (var (label, query) in queries)
        {
            // Get 1st page.
            var arguments = new PagingArguments(2);
            var page = await query.ThenByDescending(t => t.Id).ToPageAsync(arguments);

            // Get 2nd page.
            arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
            pages.Add(label, await query.ThenByDescending(t => t.Id).ToPageAsync(arguments));
        }

        // Assert
        pages.MatchMarkdownSnapshot();
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
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand,
                };
                context.Products.Add(product);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTestAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= 10; i++)
        {
            var test = new Test
            {
                Id = i,
                Bool = i % 2 == 0,
                DateOnly = DateOnly.FromDateTime(DateTime.UnixEpoch.AddDays(i - 1)),
                DateTime = DateTime.UnixEpoch.AddDays(i - 1),
                DateTimeOffset = DateTimeOffset.UnixEpoch.AddDays(i - 1),
                Decimal = i,
                Double = i,
                Float = i,
                Guid = Guid.ParseExact($"0000000000000000000000000000000{i - 1}", "N"),
                Int = i,
                Long = i,
                Short = (short)i,
                String = i.ToString(),
                TimeOnly = TimeOnly.MinValue.AddHours(i),
                TimeSpan = TimeSpan.FromHours(i),
                UInt = (uint)i,
                ULong = (ulong)i,
                UShort = (ushort)i
            };

            context.Tests.Add(test);
        }

        await context.SaveChangesAsync();
    }
}

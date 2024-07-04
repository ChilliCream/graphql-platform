using HotChocolate.Data.TestContext;
using HotChocolate.Pagination;
using CookieCrumble;
using Microsoft.EntityFrameworkCore;
using Squadron;

namespace HotChocolate.Data;

public class PagingHelperTests(PostgreSqlResource resource) : IClassFixture<PostgreSqlResource>
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
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
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
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        arguments = arguments with { Before = page.CreateCursor(page.First!), };
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
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Brands
            .Include(t => t.Products.OrderBy(p => p.Name).ThenBy(p => p.Id))
            .ToBatchPageAsync(t => t.Id, arguments);

        // Assert
        page.MatchMarkdownSnapshot();
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
}

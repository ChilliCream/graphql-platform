#if NET9_0_OR_GREATER
using CookieCrumble.Resources;
using GreenDonut.Data.TestContext;

namespace GreenDonut.Data;

// Mirrors PagingHelperIntegrationTests.cs against ToStreamPageAsync for the single-page cases;
// the batch cases are covered by StreamBatchPagingTests.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingHelperIntegrationTests(PostgreSqlResource resource)
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
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments();
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            pagingArgs,
            cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Paging_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { First = 5 };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            pagingArgs,
            cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Paging_First_5_After_Id_13()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            First = 5,
            After = "QnJhbmQxMjoxMw=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            pagingArgs,
            cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Paging_Last_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments { Last = 5 };
        var result = await context.Brands
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(pagingArgs, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Paging_First_5_Before_Id_96()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            Last = 5,
            Before = "QnJhbmQ5NTo5Ng=="
        };
        var result = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            pagingArgs,
            cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
    }

    [Fact]
    public async Task Paging_WithChildCollectionProjectionExpression_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        await using var context = new CatalogContext(connectionString);

        var pagingArgs = new PagingArguments
        {
            First = 5
        };

        var result = await context.Brands
            .Select(PagingHelperIntegrationTests.BrandWithProductsDto.Projection)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(pagingArgs, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(capture.Queries);
        await snapshot.AddStreamPageAsync(result, cancellationToken);
        await snapshot.MatchMarkdownAsync(cancellationToken);
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
}
#endif

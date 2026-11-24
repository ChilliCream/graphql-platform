#if NET9_0_OR_GREATER
using CookieCrumble.Resources;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

// Pins the concurrency behaviour documented as a risk in the design: a live stream page owns
// its DbContext's connection until it completes or is disposed, so a second operation on the
// SAME context while a page is paused mid-iteration fails with a provider-specific exception; a
// second, separate context is unaffected.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingConcurrencyTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Fetch_Concurrent_Operation_On_Same_Context_Throws_While_Page_Is_Paused()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new CatalogContext(connectionString);

        // Act
        // first: 5 asks for a 6-row query. only the primed first row has been read, so the
        // context's reader is genuinely open and paused, not merely idle, when the second
        // operation below runs.
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(5),
            cancellationToken: cancellationToken);

        var exception = await Assert.ThrowsAnyAsync<Exception>(
            () => context.Brands.CountAsync(cancellationToken));

        // Assert
        Assert.True(
            exception is InvalidOperationException || exception.GetType().Name.Contains("OperationInProgress"),
            $"Expected a concurrency exception (InvalidOperationException or *OperationInProgress*), but got {exception.GetType()}.");

        await page.DisposeAsync();
    }

    [Fact]
    public async Task Fetch_Concurrent_Operation_On_Second_Context_Succeeds_While_Page_Is_Paused()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedBrandsAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new CatalogContext(connectionString);
        await using var otherContext = new CatalogContext(connectionString);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(5),
            cancellationToken: cancellationToken);

        var otherCount = await otherContext.Brands.CountAsync(cancellationToken);

        // Assert
        Assert.Equal(10, otherCount);

        await page.DisposeAsync();
    }

    private static async Task SeedBrandsAsync(string connectionString, int count)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Brands.Add(new Brand
            {
                Name = $"Item{i:D4}",
                BrandDetails = new() { Country = new() { Name = $"Country{i:D4}" } }
            });
        }

        await context.SaveChangesAsync();
    }
}
#endif

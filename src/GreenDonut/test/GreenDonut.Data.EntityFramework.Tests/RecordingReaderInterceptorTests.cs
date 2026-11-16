using CookieCrumble.Resources;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class RecordingReaderInterceptorTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ReaderEvents_Should_RecordOneEventPerRowPlusEndOfResults_When_QueryReturnsRows()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var interceptor = new RecordingReaderInterceptor();
        await using var context = new CatalogContext(connectionString, [interceptor]);

        // act
        var brands = await context.Brands
            .OrderBy(b => b.Name)
            .ToListAsync(Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(["A", "B"], brands.Select(b => b.Name));
        Assert.Single(interceptor.CommandTexts);
        Assert.Equal(
            [new ReaderEvent(0, 1, true), new ReaderEvent(0, 2, true), new ReaderEvent(0, 3, false)],
            interceptor.Events);
    }

    [Fact]
    public async Task ReaderEvents_Should_RecordASeparateCommandIndex_PerExecutedQuery()
    {
        // arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var interceptor = new RecordingReaderInterceptor();
        await using var context = new CatalogContext(connectionString, [interceptor]);

        // act
        await context.Brands.OrderBy(b => b.Name).ToListAsync(Xunit.TestContext.Current.CancellationToken);
        await context.Brands.OrderBy(b => b.Name).ToListAsync(Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Equal(2, interceptor.CommandTexts.Count);
        Assert.Equal([0, 1], interceptor.Events.Select(e => e.CommandIndex).Distinct());
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        context.Brands.Add(new Brand { Name = "A", BrandDetails = new() { Country = new() { Name = "Country A" } } });
        context.Brands.Add(new Brand { Name = "B", BrandDetails = new() { Country = new() { Name = "Country B" } } });

        await context.SaveChangesAsync();
    }
}

using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

// Locks down the cross-API cursor round trip (hc-fork-1-m89.7, hc-fork-1-m89.10): a cursor
// produced by one API, fed into the other, returns the same rows and continues navigation with
// byte-identical cursors, for every cursor kind. Each test compares the cross-API result against a
// same-API reference fetched with the very same cursor value, so a mismatch names which side of the
// pair broke.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class CursorInteropTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnSameRowsAndCursor_When_GivenPlainCursorFromToPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2),
            cancellationToken);
        var plainCursor = firstPage.CreateCursor(firstPage.Last!.Value);

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2, after: plainCursor),
            cancellationToken);

        // Act
        var streamPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2, after: plainCursor),
            cancellationToken: cancellationToken);
        var streamEntries = await DrainEntriesAndDisposeAsync(streamPage, cancellationToken);

        // Assert
        Assert.Equal(referencePage.Select(t => t.Name).ToArray(), streamEntries.Select(e => e.Item.Name).ToArray());
        Assert.Equal(referencePage.CreateCursor(referencePage.Last!.Value), streamPage.CreateCursor(streamEntries[^1]));
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnSameRowsAndCursor_When_GivenPlainCursorFromToStreamPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2),
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(firstPage, cancellationToken);
        var plainCursor = firstPage.CreateCursor(firstEntries[^1]);

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2, after: plainCursor),
            cancellationToken: cancellationToken);
        var referenceEntries = await DrainEntriesAndDisposeAsync(referencePage, cancellationToken);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2, after: plainCursor),
            cancellationToken);

        // Assert
        Assert.Equal(referenceEntries.Select(e => e.Item.Name).ToArray(), page.Select(t => t.Name).ToArray());
        Assert.Equal(referencePage.CreateCursor(referenceEntries[^1]), page.CreateCursor(page.Last!.Value));
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnSameRowsAndCursor_When_GivenRelativeCursorFromToPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2) { EnableRelativeCursors = true },
            cancellationToken);
        var relativeCursor = firstPage.CreateCursor(firstPage.Last!.Value, 0);

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2, after: relativeCursor) { EnableRelativeCursors = true },
            cancellationToken);

        // Act
        var streamPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2, after: relativeCursor) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var streamEntries = await DrainEntriesAndDisposeAsync(streamPage, cancellationToken);

        // Assert
        Assert.Equal(referencePage.Index, streamPage.Index);
        Assert.Equal(referencePage.Select(t => t.Name).ToArray(), streamEntries.Select(e => e.Item.Name).ToArray());
        Assert.Equal(
            referencePage.CreateCursor(referencePage.Last!.Value, 0),
            streamPage.CreateCursor(streamEntries[^1], 0));
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnSameRowsAndCursor_When_GivenRelativeCursorFromToStreamPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(firstPage, cancellationToken);
        var relativeCursor = firstPage.CreateCursor(firstEntries[^1], 0);

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(2, after: relativeCursor) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var referenceEntries = await DrainEntriesAndDisposeAsync(referencePage, cancellationToken);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(2, after: relativeCursor) { EnableRelativeCursors = true },
            cancellationToken);

        // Assert
        Assert.Equal(referencePage.Index, page.Index);
        Assert.Equal(referenceEntries.Select(e => e.Item.Name).ToArray(), page.Select(t => t.Name).ToArray());
        Assert.Equal(
            referencePage.CreateCursor(referenceEntries[^1], 0),
            page.CreateCursor(page.Last!.Value, 0));
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnSamePage_When_GivenEndCursorFromToPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var entryPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            cancellationToken);
        var endCursor = entryPage.CreateLastPageCursor().Cursor;

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(last: 10, before: endCursor),
            cancellationToken);

        // Act
        var streamPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(last: 10, before: endCursor),
            cancellationToken: cancellationToken);
        var streamEntries = await DrainEntriesAndDisposeAsync(streamPage, cancellationToken);

        // Assert
        Assert.Equal(referencePage.Index, streamPage.Index);
        Assert.Equal(referencePage.Select(t => t.Name).ToArray(), streamEntries.Select(e => e.Item.Name).ToArray());
        Assert.Equal(
            referencePage.CreateCursor(referencePage.Last!.Value, 0),
            streamPage.CreateCursor(streamEntries[^1], 0));
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnSamePage_When_GivenEndCursorFromToStreamPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var entryPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        await DrainEntriesAndDisposeAsync(entryPage, cancellationToken);
        var endCursor = entryPage.CreateLastPageCursor().Cursor;

        var referencePage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(last: 10, before: endCursor),
            cancellationToken: cancellationToken);
        var referenceEntries = await DrainEntriesAndDisposeAsync(referencePage, cancellationToken);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(last: 10, before: endCursor),
            cancellationToken);

        // Assert
        Assert.Equal(referencePage.Index, page.Index);
        Assert.Equal(referenceEntries.Select(e => e.Item.Name).ToArray(), page.Select(t => t.Name).ToArray());
        Assert.Equal(
            referencePage.CreateCursor(referenceEntries[^1], 0),
            page.CreateCursor(page.Last!.Value, 0));
    }

    private static async ValueTask<List<PageEntry<Brand>>> DrainEntriesAndDisposeAsync(
        StreamPage<Brand> page,
        CancellationToken cancellationToken)
    {
        List<PageEntry<Brand>> entries = [];

        await foreach (var entry in page.EnumerateEntriesAsync(cancellationToken))
        {
            entries.Add(entry);
        }

        await page.DisposeAsync();

        return entries;
    }

    private static async Task SeedSequentialAsync(string connectionString, int count)
    {
        await using var context = new TestContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Brands.Add(new Brand { Name = $"Item{i:D4}" });
        }

        await context.SaveChangesAsync();
    }

    public class TestContext(string connectionString) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);
        }

        public DbSet<Brand> Brands => Set<Brand>();
    }

    public class Brand
    {
        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}

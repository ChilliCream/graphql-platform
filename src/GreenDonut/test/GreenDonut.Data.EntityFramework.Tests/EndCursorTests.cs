#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

// Gated to NET9_0_OR_GREATER, like RelativeCursorTests.cs and StreamPagingHelperTests.cs: the
// physical-command-count assertions below (RecordingReaderInterceptor) rely on the single-command
// inlined-count query shape validated for EF Core 9+ (hc-fork-1-m89.4); EF Core 8 splits it into an
// extra up-front command.
//
// Locks down the end-cursor test matrix (hc-fork-1-m89.10, hc-fork-1-m89.11) for ToPageAsync. Its
// stream twin, StreamEndCursorTests below, carries the same cases plus the streaming-specific
// guarantees (trimmed front rows are read but never yielded, no pre-query). Batch end-cursor
// cases beyond the existing per-key trim test in RelativeCursorTests.cs are out of scope here.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class EndCursorTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToPageAsync_Should_ReturnLastPage_When_RemainderIsNonZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        using var capture = new CapturePagingQueryInterceptor();
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((3, 25, false, true), (page.Index, page.TotalCount, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], page.Select(t => t.Name).ToArray());
        Assert.Single(capture.Queries);
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnLastPage_When_RemainderIsZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 30);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 30) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((3, 30, false, true), (page.Index, page.TotalCount, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(
            ["Item0021", "Item0022", "Item0023", "Item0024", "Item0025", "Item0026", "Item0027", "Item0028", "Item0029", "Item0030"],
            page.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnWholeSet_When_TotalIsLessThanOrEqualToSize()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 7);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 7) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((1, 7, false, false), (page.Index, page.TotalCount, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(7, page.Count);
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnEmptyPage_When_TotalIsZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 0);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 0) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((1, 0, false, false), (page.Index, page.TotalCount, page.HasNextPage, page.HasPreviousPage));
        Assert.Empty(page);
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnAlignedPage_When_OffsetIsNegativeOne()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((2, true, true), (page.Index, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(
            ["Item0011", "Item0012", "Item0013", "Item0014", "Item0015", "Item0016", "Item0017", "Item0018", "Item0019", "Item0020"],
            page.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task ToPageAsync_Should_ReturnAlignedPage_When_OffsetIsNegativeTwo()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-2, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((1, true, false), (page.Index, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(
            ["Item0001", "Item0002", "Item0003", "Item0004", "Item0005", "Item0006", "Item0007", "Item0008", "Item0009", "Item0010"],
            page.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task ToPageAsync_Should_InlineFreshCount_When_EndCursorTotalIsStale()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        using var capture = new CapturePagingQueryInterceptor();
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 24) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal((3, 25, false, true), (page.Index, page.TotalCount, page.HasNextPage, page.HasPreviousPage));
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], page.Select(t => t.Name).ToArray());
        Assert.Single(capture.Queries);
    }

    [Fact]
    public async Task ToPageAsync_Should_ProducePageNumbers_MatchingFormula_When_UsingCreateRelativeLastPageCursors()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var entryPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);

        // Act
        // ceil(25 / 10) == 3, so the pages ahead of page 1 are page 2 (offset -1) and page 3
        // (offset 0), which is exactly what CreateRelativeLastPageCursors' formula computes.
        var lastPageCursors = entryPage.CreateRelativeLastPageCursors(5);
        var lastCursor = lastPageCursors[^1];
        var lastPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(last: 10, before: lastCursor.Cursor),
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([2, 3], lastPageCursors.Select(c => c.Page).ToArray());
        Assert.Equal(lastCursor.Page, lastPage.Index);
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], lastPage.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task ToPageAsync_Should_ThrowArgumentException_When_FirstIsUsedWithEndCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(first: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task ToPageAsync_Should_ThrowArgumentException_When_AfterIsUsedWithEndCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(last: 10) { After = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task ToPageAsync_Should_ThrowInvalidOperationException_When_EndCursorBodyIsMalformed()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var malformedCursor = Convert.ToBase64String("{end|x}"u8);
        var arguments = new PagingArguments(last: 10) { Before = malformedCursor };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public async Task ToPageAsync_Should_AcceptLegacyThreeNumberCursor_When_UsedAsAfter()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);

        // the cursor below carries the pre-existing three-number `{offset|page|total}` page info,
        // the format the new `{end|offset|total}` branch was added next to.
        var legacyCursor = firstPage.CreateCursor(firstPage.Last!.Value, 0);

        // Act
        var secondPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10, after: legacyCursor) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(2, secondPage.Index);
        Assert.Equal(
            ["Item0011", "Item0012", "Item0013", "Item0014", "Item0015", "Item0016", "Item0017", "Item0018", "Item0019", "Item0020"],
            secondPage.Select(t => t.Name).ToArray());
    }

    [Fact]
    public async Task ToPageAsync_Should_ReachSamePageAndCursors_When_NavigatedViaEndCursorOrForwardRelativeCursors()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var page1 = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);
        var page2 = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10, after: page1.CreateCursor(page1.Last!.Value, 0)) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);

        // Act
        // ceil(25 / 10) == 3, so page3 reached by forward navigation must line up with the page
        // reached directly through the end cursor.
        var forwardPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(10, after: page2.CreateCursor(page2.Last!.Value, 0)) { EnableRelativeCursors = true },
            Xunit.TestContext.Current.CancellationToken);
        var endCursorPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) },
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(3, forwardPage.Index);
        Assert.Equal(forwardPage.Index, endCursorPage.Index);
        Assert.Equal(forwardPage.Select(t => t.Name).ToArray(), endCursorPage.Select(t => t.Name).ToArray());
        Assert.Equal(
            forwardPage.CreateCursor(forwardPage.Last!.Value, 0),
            endCursorPage.CreateCursor(endCursorPage.Last!.Value, 0));
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

    public class TestContext(string connectionString, IEnumerable<IInterceptor>? interceptors = null) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);

            if (interceptors is not null)
            {
                optionsBuilder.AddInterceptors(interceptors);
            }
        }

        public DbSet<Brand> Brands => Set<Brand>();
    }

    public class Brand
    {
        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}

// The stream twin of EndCursorTests above: same matrix, same names (prefixed ToStreamPageAsync
// instead of ToPageAsync), plus the streaming-specific guarantees from the testing strategy
// (hc-fork-1-m89.10): the front rows trimmed from an offset-zero end cursor page are read from the
// database but never handed to the consumer, and no query runs ahead of the row query that carries
// the first served row.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamEndCursorTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnLastPage_When_RemainderIsNonZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        using var capture = new CapturePagingQueryInterceptor();
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (3, 25, false, true),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], items);
        Assert.Single(capture.Queries);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnLastPage_When_RemainderIsZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 30);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 30) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (3, 30, false, true),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(
            ["Item0021", "Item0022", "Item0023", "Item0024", "Item0025", "Item0026", "Item0027", "Item0028", "Item0029", "Item0030"],
            items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnWholeSet_When_TotalIsLessThanOrEqualToSize()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 7);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 7) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (1, 7, false, false),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(7, items.Length);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnEmptyPage_When_TotalIsZero()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 0);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 0) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (1, 0, false, false),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Empty(items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnAlignedPage_When_OffsetIsNegativeOne()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (2, true, true),
            (page.Index, await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(
            ["Item0011", "Item0012", "Item0013", "Item0014", "Item0015", "Item0016", "Item0017", "Item0018", "Item0019", "Item0020"],
            items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnAlignedPage_When_OffsetIsNegativeTwo()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-2, 25) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (1, true, false),
            (page.Index, await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(
            ["Item0001", "Item0002", "Item0003", "Item0004", "Item0005", "Item0006", "Item0007", "Item0008", "Item0009", "Item0010"],
            items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReportFreshTotal_When_EndCursorTotalIsStale()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 26);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: ct);
        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal(
            (2, 26, true, true),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(
            ["Item0012", "Item0013", "Item0014", "Item0015", "Item0016", "Item0017", "Item0018", "Item0019", "Item0020", "Item0021"],
            items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReturnEmptyPage_When_OffsetPointsBeforeFirstPage()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 40);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-3, 25) };
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: ct);
        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal(
            (1, 40, false, false),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Empty(items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_InlineFreshCount_When_EndCursorTotalIsStale()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        using var capture = new CapturePagingQueryInterceptor();
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 24) };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);
        var ct = Xunit.TestContext.Current.CancellationToken;

        // Assert
        Assert.Equal(
            (3, 25, false, true),
            (page.Index, await page.TotalCountAsync(ct), await page.HasNextPageAsync(ct), await page.HasPreviousPageAsync(ct)));
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], items);
        Assert.Single(capture.Queries);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ProducePageNumbers_MatchingFormula_When_UsingCreateRelativeLastPageCursors()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var entryPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        await DrainEntriesAndDisposeAsync(entryPage, Xunit.TestContext.Current.CancellationToken);

        // Act
        var lastPageCursors = entryPage.CreateRelativeLastPageCursors(5);
        var lastCursor = lastPageCursors[^1];
        var lastPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(last: 10, before: lastCursor.Cursor),
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(lastPage);

        // Assert
        Assert.Equal([2, 3], lastPageCursors.Select(c => c.Page).ToArray());
        Assert.Equal(lastCursor.Page, lastPage.Index);
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ThrowArgumentException_When_FirstIsUsedWithEndCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(first: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(arguments);
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ThrowArgumentException_When_AfterIsUsedWithEndCursor()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(last: 10) { After = CursorFormatter.FormatEndCursor(0, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(arguments);
        }

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ThrowInvalidOperationException_When_EndCursorBodyIsMalformed()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var malformedCursor = Convert.ToBase64String("{end|x}"u8);
        var arguments = new PagingArguments(last: 10) { Before = malformedCursor };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(arguments);
        }

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_AcceptLegacyThreeNumberCursor_When_UsedAsAfter()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(firstPage, cancellationToken);

        // the cursor below carries the pre-existing three-number `{offset|page|total}` page info,
        // the format the new `{end|offset|total}` branch was added next to.
        var legacyCursor = firstPage.CreateCursor(firstEntries[^1], 0);

        // Act
        var secondPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10, after: legacyCursor) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(secondPage);

        // Assert
        Assert.Equal(2, secondPage.Index);
        Assert.Equal(
            ["Item0011", "Item0012", "Item0013", "Item0014", "Item0015", "Item0016", "Item0017", "Item0018", "Item0019", "Item0020"],
            items);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReachSamePageAndCursors_When_NavigatedViaEndCursorOrForwardRelativeCursors()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var page1 = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var page1Entries = await DrainEntriesAndDisposeAsync(page1, cancellationToken);
        var page2 = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10, after: page1.CreateCursor(page1Entries[^1], 0)) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var page2Entries = await DrainEntriesAndDisposeAsync(page2, cancellationToken);

        // Act
        // ceil(25 / 10) == 3, so page3 reached by forward navigation must line up with the page
        // reached directly through the end cursor.
        var forwardPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(10, after: page2.CreateCursor(page2Entries[^1], 0)) { EnableRelativeCursors = true },
            cancellationToken: cancellationToken);
        var forwardEntries = await DrainEntriesAndDisposeAsync(forwardPage, cancellationToken);
        var endCursorPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) },
            cancellationToken: cancellationToken);
        var endCursorEntries = await DrainEntriesAndDisposeAsync(endCursorPage, cancellationToken);

        // Assert
        Assert.Equal(3, forwardPage.Index);
        Assert.Equal(forwardPage.Index, endCursorPage.Index);
        Assert.Equal(forwardEntries.Select(e => e.Item.Name).ToArray(), endCursorEntries.Select(e => e.Item.Name).ToArray());
        Assert.Equal(
            forwardPage.CreateCursor(forwardEntries[^1], 0),
            endCursorPage.CreateCursor(endCursorEntries[^1], 0));
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ReadButNotYieldTrimmedFrontRows_When_EndCursorPageIsPartial()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new TestContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act

        // priming the page (inside ToStreamPageAsync) reads and discards the 5 trimmed front rows,
        // then reads and buffers the first served row: 6 reads before the consumer ever pulls.
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var commandsBeforePull = interceptor.CommandTexts.Count;
        var readsBeforePull = interceptor.Events.Count(e => e.Result);

        // pulling the first item off the enumerator serves it straight from the row already
        // buffered by priming, with no additional read from the database.
        await using var enumerator = page.GetAsyncEnumerator(cancellationToken);
        await enumerator.MoveNextAsync();
        var first = enumerator.Current.Name;
        var readsAfterFirstItem = interceptor.Events.Count(e => e.Result);

        var items = new List<string> { first };

        while (await enumerator.MoveNextAsync())
        {
            items.Add(enumerator.Current.Name);
        }

        // Assert
        Assert.Equal(1, commandsBeforePull);
        Assert.Equal((6, 6), (readsBeforePull, readsAfterFirstItem));
        Assert.Equal("Item0021", first);
        Assert.Equal(10, interceptor.Events.Count(e => e.Result));
        Assert.Equal(["Item0021", "Item0022", "Item0023", "Item0024", "Item0025"], items);
    }

    private static async ValueTask<string[]> ToArrayAsync(StreamPage<Brand> page)
    {
        var items = new List<string>();

        await foreach (var brand in page)
        {
            items.Add(brand.Name);
        }

        return [.. items];
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

    public class TestContext(string connectionString, IEnumerable<IInterceptor>? interceptors = null) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);

            if (interceptors is not null)
            {
                optionsBuilder.AddInterceptors(interceptors);
            }
        }

        public DbSet<Brand> Brands => Set<Brand>();
    }

    public class Brand
    {
        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}
#endif

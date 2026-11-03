#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

// Locks down the end-cursor test matrix for ToPageAsync.
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
        // The pages ahead of page 1 are computed by CreateRelativeLastPageCursors.
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
    public async Task ToPageAsync_Should_ThrowArgumentException_When_AbsoluteAfterIsCombinedWithEndCursorBefore()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        string afterCursor;
        await using (var context = new TestContext(connectionString))
        {
            var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
                new PagingArguments(5),
                Xunit.TestContext.Current.CancellationToken);
            afterCursor = firstPage.CreateCursor(firstPage.Last!.Value);
        }

        var arguments = new PagingArguments(last: 10)
        {
            After = afterCursor,
            Before = CursorFormatter.FormatEndCursor(0, 25)
        };

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
    public async Task ToPageAsync_Should_ThrowInvalidOperationException_When_EndCursorOffsetIsIntMinValue()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(int.MinValue, 25) };

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
    public async Task ToPageAsync_Should_ThrowArgumentException_When_EndCursorSkipOverflowsInt()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(last: 2) { Before = CursorFormatter.FormatEndCursor(-int.MaxValue, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The end cursor offset points too far before the last page for the requested page size. (Parameter 'arguments')",
            exception.Message);
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

        // The cursor below carries the pre-existing three-number `{offset|page|total}` format.
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
        // Page 3 reached by forward navigation must line up with the page reached directly through the end cursor.
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
#endif

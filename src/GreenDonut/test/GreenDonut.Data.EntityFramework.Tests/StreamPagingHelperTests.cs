#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingHelperTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Fetch_Forward_First_After()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        arguments = arguments with { After = await page.CreateEndCursorAsync(Xunit.TestContext.Current.CancellationToken) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var secondItems = await ToArrayAsync(second);

        // Assert
        var ct = Xunit.TestContext.Current.CancellationToken;
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                First = new
                {
                    HasNext = await page.HasNextPageAsync(ct),
                    HasPrevious = await page.HasPreviousPageAsync(ct),
                    Items = items
                },
                Second = new
                {
                    HasNext = await second.HasNextPageAsync(ct),
                    HasPrevious = await second.HasPreviousPageAsync(ct),
                    Items = secondItems
                }
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Backward_Last_Before()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);

        // establish a cursor to page backward from.
        var forward = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            new PagingArguments(8),
            Xunit.TestContext.Current.CancellationToken);
        var beforeCursor = forward.CreateEndCursor();

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var arguments = new PagingArguments(last: 2, before: beforeCursor);
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                HasNext = await page.HasNextPageAsync(Xunit.TestContext.Current.CancellationToken),
                HasPrevious = await page.HasPreviousPageAsync(Xunit.TestContext.Current.CancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Count_Only()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { IncludeItems = false };

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                TotalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Count_Only_Without_Total_Count_Throws()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { IncludeItems = false };

        // Act
        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                includeTotalCount: false,
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task Fetch_Rows_Only()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: false,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                TotalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_TotalCount_Awaited_Before_Iteration()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(["Item0001", "Item0002"], items);
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_TotalCount_Awaited_After_Iteration()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var items = await ToArrayAsync(page);
        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(10, totalCount);
        Assert.Equal(["Item0001", "Item0002"], items);
    }

    [Fact]
    public async Task Fetch_Relative_Entry_Point_Reports_Index_One()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal(1, page.Index);
        Assert.Equal(10, await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken));
        Assert.Equal(["Item0001", "Item0002"], items);
    }

    [Fact]
    public async Task Fetch_Empty_Result_With_Count()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 0);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                TotalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken),
                HasNext = await page.HasNextPageAsync(Xunit.TestContext.Current.CancellationToken),
                HasPrevious = await page.HasPreviousPageAsync(Xunit.TestContext.Current.CancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_25_10()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(page);

        // Assert
        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Index = page.Index,
                TotalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken),
                HasNext = await page.HasNextPageAsync(Xunit.TestContext.Current.CancellationToken),
                HasPrevious = await page.HasPreviousPageAsync(Xunit.TestContext.Current.CancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Cursor_From_ToPageAsync_Works_With_ToStreamPageAsync()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2);

        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act
        arguments = arguments with { After = page.CreateCursor(page.Last!.Value) };
        var streamPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var items = await ToArrayAsync(streamPage);

        // Assert
        Assert.Equal(["Item0003", "Item0004"], items);
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
#endif

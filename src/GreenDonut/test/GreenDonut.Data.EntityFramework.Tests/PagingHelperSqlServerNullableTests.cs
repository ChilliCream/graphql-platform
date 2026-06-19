using GreenDonut.Data.TestContext;
using Squadron;
using Record = GreenDonut.Data.TestContext.Record;

namespace GreenDonut.Data;

[Collection(SqlServerCacheCollectionFixture.DefinitionName)]
public class PagingHelperSqlServerNullableTests(SqlServerResource resource)
{
    private string CreateConnectionString()
        => resource.CreateConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_Nullable_Ascending_Cursor_Value_NonNull()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Nullable_Descending_Cursor_Value_NonNull()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Nullable_Ascending_Cursor_Value_Null()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_Nullable_Descending_Cursor_Value_Null()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(4) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_NullableReference_Ascending_Cursor_Value_NonNull()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_NullableReference_Descending_Cursor_Value_NonNull()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_NullableReference_Ascending_Cursor_Value_Null()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(postFix: TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_NullableReference_Descending_Cursor_Value_Null()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(4) { NullOrdering = NullOrdering.NativeNullsFirst };
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        var snapshot = new Snapshot(TestEnvironment.TargetFramework);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Paging_NullableNavigation_ValueTypeLeaf_Pages_Across_Null_Boundary()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedItemsAsync(connectionString);
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);

        // Act
        // Sort by a non-nullable value-type leaf (Detail.Number) behind a nullable
        // navigation; with nulls first the second page is requested after a
        // null-navigation boundary.
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsFirst };
        var page1 = await context.Items
            .OrderBy(x => x.Detail!.Number)
            .ThenBy(x => x.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Items
            .OrderBy(x => x.Detail!.Number)
            .ThenBy(x => x.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([2, 3], page1.Select(x => x.Id));
        Assert.Equal([4, 1], page2.Select(x => x.Id));
    }

    [Fact]
    public async Task Paging_NullableNavigation_ReferenceLeaf_Pages_Across_Null_Boundary()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedItemsAsync(connectionString);
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);

        // Act
        // Sort by a non-nullable reference leaf (Detail.Name) behind a nullable
        // navigation; with nulls first the second page is requested after a
        // null-navigation boundary.
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsFirst };
        var page1 = await context.Items
            .OrderBy(x => x.Detail!.Name)
            .ThenBy(x => x.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        arguments = arguments with { After = page1.CreateEndCursor() };
        var page2 = await context.Items
            .OrderBy(x => x.Detail!.Name)
            .ThenBy(x => x.Id)
            .ToPageAsync(arguments, Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal([2, 3], page1.Select(x => x.Id));
        Assert.Equal([4, 1], page2.Select(x => x.Id));
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        await context.Database.EnsureCreatedAsync();

        // https://stackoverflow.com/questions/68971695/cursor-pagination-prev-next-with-null-values
        // ... with the addition of a String property to test nullable reference types.
        context.Records.AddRange(
            new Record
            {
                Id = Guid.Parse("68a5c7c2-1234-4def-bc01-9f1a23456789"),
                Date = new DateOnly(2017, 10, 28),
                Time = new TimeOnly(22, 00, 00),
                String = "22:00:00"
            },
            new Record
            {
                Id = Guid.Parse("d3b7e9f1-4567-4abc-a102-8c2b34567890"),
                Date = new DateOnly(2017, 11, 03),
                Time = null,
                String = null
            },
            new Record
            {
                Id = Guid.Parse("dd8f3a21-89ab-4cde-a203-7d3c45678901"),
                Date = new DateOnly(2017, 11, 03),
                Time = new TimeOnly(21, 45, 00),
                String = "21:45:00"
            },
            new Record
            {
                Id = Guid.Parse("62ce9d54-2345-4f01-b304-6e4d56789012"),
                Date = new DateOnly(2017, 11, 04),
                Time = new TimeOnly(14, 00, 00),
                String = "14:00:00"
            },
            new Record
            {
                Id = Guid.Parse("a1d5b763-6789-4f23-c405-5f5e67890123"),
                Date = new DateOnly(2017, 11, 04),
                Time = new TimeOnly(19, 40, 00),
                String = "19:40:00"
            });

        await context.SaveChangesAsync();
    }

    private static async Task SeedItemsAsync(string connectionString)
    {
        await using var context = new NullableTestsContext(Provider.SqlServer, connectionString);
        await context.Database.EnsureCreatedAsync();

        // Only the first item has a navigation; the rest are null and sort first,
        // so paging crosses a null-navigation boundary between page 1 and page 2.
        context.Items.AddRange(
            new Item { Id = 1, Detail = new Detail { Number = 10, Name = "a" } },
            new Item { Id = 2 },
            new Item { Id = 3 },
            new Item { Id = 4 });

        await context.SaveChangesAsync();
    }
}

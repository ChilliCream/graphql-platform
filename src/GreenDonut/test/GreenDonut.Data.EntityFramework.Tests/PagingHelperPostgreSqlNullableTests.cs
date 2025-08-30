using GreenDonut.Data.TestContext;
using Squadron;
using Record = GreenDonut.Data.TestContext.Record;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingHelperPostgreSqlNullableTests(PostgreSqlResource resource)
{
    private string CreateConnectionString()
        => resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Paging_Nullable_Ascending_Cursor_Value_NonNull()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

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
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        var snapshot = new Snapshot(
            postFix: TestEnvironment.TargetFramework == "NET10_0"
                ? TestEnvironment.TargetFramework
                : null);
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
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.Time)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

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
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.Time)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        var snapshot = new Snapshot(
            postFix: TestEnvironment.TargetFramework == "NET10_0"
                ? TestEnvironment.TargetFramework
                : null);
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
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

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
        var arguments = new PagingArguments(2) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        var snapshot = new Snapshot(
            postFix: TestEnvironment.TargetFramework == "NET10_0"
                ? TestEnvironment.TargetFramework
                : null);
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
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderBy(t => t.Date)
            .ThenBy(t => t.String)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

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
        var arguments = new PagingArguments(3) { NullOrdering = NullOrdering.NativeNullsLast };
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
        var page1 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
        var page2 = await context.Records
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.String)
            .ThenByDescending(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        var snapshot = new Snapshot(
            postFix: TestEnvironment.TargetFramework == "NET10_0"
                ? TestEnvironment.TargetFramework
                : null);
        snapshot.Add(page1);
        snapshot.Add(page2);
        snapshot.AddSql(interceptor);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]// move to separate test class?
    public async Task Paging_Nullable_Throws_When_NullOrdering_Not_Specified()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        async Task Act()
        {
            var arguments = new PagingArguments(2);
            await using var context
                = new NullableTestsContext(Provider.PostgreSql, connectionString);
            var page1 = await context.Records
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Time)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments);

            arguments = arguments with { After = page1.CreateCursor(page1.Last!) };
            await context.Records
                .OrderBy(t => t.Date)
                .ThenBy(t => t.Time)
                .ThenBy(t => t.Id)
                .ToPageAsync(arguments);
        }

        // Assert
        Assert.Equal(
            "The NullOrdering option must be specified in the paging options or arguments when "
            + "using nullable keys.",
            (await Assert.ThrowsAsync<Exception>(Act)).Message);
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new NullableTestsContext(Provider.PostgreSql, connectionString);
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
}

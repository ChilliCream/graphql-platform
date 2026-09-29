#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

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

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);

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

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialContext(connectionString, [interceptor]);
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

        // count-only executes exactly one command against the database: the hoisted count.
        interceptor.CommandTexts.MatchInlineSnapshot(
            """
            [
              "SELECT count(*)::int\nFROM \"Brands\" AS b"
            ]
            """);
    }

    [Fact]
    public async Task Fetch_Count_Only_Without_Total_Count_Throws()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new SequentialContext(connectionString);
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

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialContext(connectionString, [interceptor]);
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

        // rows-only executes exactly one command against the database: the row query.
        interceptor.CommandTexts
            .Select(t => t.Replace("@__p_0", "@p"))
            .MatchInlineSnapshot(
                """
                [
                  "SELECT b.\"Id\", b.\"Name\"\nFROM \"Brands\" AS b\nORDER BY b.\"Name\", b.\"Id\"\nLIMIT @p"
                ]
                """);
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_TotalCount_Awaited_Before_Iteration()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);
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

        await using var context = new SequentialContext(connectionString);
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

    // The following cases lock down the streaming-specific guarantees: reads happen in lockstep
    // with what the caller actually asks for, and the lifetime is released exactly once.

    [Fact]
    public async Task Fetch_Forward_First_Item_Yielded_After_Exactly_One_Read()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: false,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var readsBeforeFirstItem = interceptor.Events.Count;
        var enumerator = page.GetAsyncEnumerator(Xunit.TestContext.Current.CancellationToken);
        var hasFirstItem = await enumerator.MoveNextAsync();
        var firstItem = enumerator.Current.Name;

        // Assert
        Assert.True(hasFirstItem);
        Assert.Equal("Item0001", firstItem);
        Assert.Equal([new ReaderEvent(0, 1, true)], interceptor.Events);
        Assert.Equal(1, readsBeforeFirstItem);
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_CountFirst_Reads_Exactly_One_Row()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);
        var eventsAfterCreate = interceptor.Events.ToArray();

        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);
        var eventsAfterCount = interceptor.Events.ToArray();

        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal([new ReaderEvent(0, 1, true)], eventsAfterCreate);
        Assert.Equal(10, totalCount);
        Assert.Equal(eventsAfterCreate, eventsAfterCount);
        Assert.Equal(["Item0001", "Item0002"], items);
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_IterationFirst_Needs_No_Extra_Command()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        var interceptor = new RecordingReaderInterceptor();
        await using var context = new SequentialContext(connectionString, [interceptor]);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var items = await ToArrayAsync(page);
        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Item0001", "Item0002"], items);
        Assert.Equal(10, totalCount);
        Assert.Single(interceptor.CommandTexts);
    }

    [Fact]
    public async Task Fetch_Rows_And_Count_Disposes_Lifetime_After_Drain_Not_Held_For_Pending_Count()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        var context = new SequentialContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            lifetime: lifetime,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var disposedBeforeCount = lifetime.DisposeCount;
        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);
        var disposedAfterCount = lifetime.DisposeCount;
        var items = await ToArrayAsync(page);

        // Assert
        Assert.Equal(0, disposedBeforeCount);
        Assert.Equal(0, disposedAfterCount);
        Assert.Equal(10, totalCount);
        Assert.Equal(["Item0001", "Item0002"], items);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_DisposeLifetimeAfterDrain_When_TotalCountIsStillPending()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 10);

        var context = new SequentialContext(connectionString);
        var lifetime = new RecordingLifetime(context);
        var arguments = new PagingArguments(2);

        // Act
        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            includeTotalCount: true,
            lifetime: lifetime,
            cancellationToken: Xunit.TestContext.Current.CancellationToken);

        var items = await ToArrayAsync(page);
        var disposedAfterDrain = lifetime.DisposeCount;
        var totalCount = await page.TotalCountAsync(Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(["Item0001", "Item0002"], items);
        Assert.Equal(1, disposedAfterDrain);
        Assert.Equal(10, totalCount);
        Assert.Equal(1, lifetime.DisposeCount);
    }

    // The following cases mirror PagingHelperTests.cs one for one against ToStreamPageAsync.

    [Fact]
    public async Task Fetch_First_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get first page
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Act
        arguments = new PagingArguments(2, after: await page.CreateEndCursorAsync(cancellationToken));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(cancellationToken);
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_With_Offset_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get first page
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var entries = await DrainEntriesAndDisposeAsync(page, cancellationToken);

        // Act
        var cursor = page.CreateCursor(entries[^1], 2);
        arguments = new PagingArguments(2, after: cursor);
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(cancellationToken);
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_With_Offset_Negative_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new CatalogContext(connectionString);

        // -> get first page
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // -> get second page
        var cursor = first.CreateCursor(firstEntries[^1], 0);
        arguments = new PagingArguments(2, after: cursor) { EnableRelativeCursors = true };
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var pageEntries = await DrainEntriesAndDisposeAsync(page, cancellationToken);

        // -> get third page
        cursor = page.CreateCursor(pageEntries[^1], 0);
        arguments = new PagingArguments(2, after: cursor) { EnableRelativeCursors = true };
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        pageEntries = await DrainEntriesAndDisposeAsync(page, cancellationToken);

        // Act
        /*
         1  Product 0-0
         2  Product 0-1
        11  Product 0-10
        12  Product 0-11
        13  Product 0-12   <- Cursor is set here - 1
        14  Product 0-13
        15  Product 0-14
        16  Product 0-15
        17  Product 0-16
        18  Product 0-17
        */
        cursor = page.CreateCursor(pageEntries[^1], -1);
        arguments = new PagingArguments(last: 2, before: cursor);
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await DrainEntriesAndDisposeAsync(page, cancellationToken);

        // Assert
        /*
         1  Product 0-0    <- first
         2  Product 0-1    <- last
        11  Product 0-10
        12  Product 0-11
        13  Product 0-12   <- Cursor is set here - 1
        14  Product 0-13
        15  Product 0-14
        16  Product 0-15
        17  Product 0-16
        18  Product 0-17
        */
        new
        {
            First = items[0].Item.Name,
            Last = items[^1].Item.Name,
            ItemsCount = items.Count
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Third_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get first page
        var arguments = new PagingArguments(2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        arguments = new PagingArguments(2, after: await page.CreateEndCursorAsync(cancellationToken));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Act
        arguments = new PagingArguments(2, after: await page.CreateEndCursorAsync(cancellationToken));
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(cancellationToken);
    }

    [Fact]
    public async Task Fetch_First_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get first page
        var arguments = new PagingArguments(4);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var startCursor = await page.CreateStartCursorAsync(cancellationToken);
        var endCursor = await page.CreateEndCursorAsync(cancellationToken);

        // Act
        arguments = new PagingArguments(2, after: startCursor, before: endCursor);
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(cancellationToken);
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
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: Xunit.TestContext.Current.CancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(Xunit.TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task QueryContext_Simple_Selector()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Products
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Brand()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        query = query.Include(t => t.Brand);

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Products
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Brand_Name()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        query = query.Select(t => new Product { Brand = new Brand { Name = t.Brand!.Name } });

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Products
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Product_List()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Brand>().AddDescending(t => t.Id));

        query = query.Select(t => new Brand { Products = t.Products.Select(p => new Product { Id = p.Id, Name = p.Name }).ToList() });

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_CreateCursor_When_SelectorContainsNestedOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        // The projection's nested OrderByDescending key must not be hoisted into the Brand selector.
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Brand>().AddAscending(t => t.Id));

        query = query.Select(t => new Brand
        {
            DisplayName = t.Products
                .OrderByDescending(p => p.Price)
                .ThenBy(p => p.AvailableStock)
                .FirstOrDefault()!.Name
        });

        var arguments = new PagingArguments(first: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // A cursor must be creatable for each edge, against the projected Brand.
        await page.CreateStartCursorAsync(cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_NotHoistInnerOrderProperties_When_BackwardPagingSelectorContainsNestedOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        // The projection's nested OrderByDescending key must not be hoisted into the Brand selector.
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Brand>().AddAscending(t => t.Id));

        query = query.Select(t => new Brand
        {
            DisplayName = t.Products
                .OrderByDescending(p => p.Price)
                .ThenBy(p => p.AvailableStock)
                .FirstOrDefault()!.Name
        });

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // A cursor must be creatable for each edge, against the projected Brand.
        await page.CreateStartCursorAsync(cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_CreateCursor_When_PredicateContainsNestedOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        // The Where lambda's nested OrderByDescending key must not be hoisted or collected as a cursor key.
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name },
            Predicate: t => t.Products.OrderByDescending(p => p.Price).FirstOrDefault()!.Price >= 0m,
            Sorting: new SortDefinition<Brand>().AddAscending(t => t.Id));

        var arguments = new PagingArguments(first: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // A cursor must be creatable for each edge, against the projected Brand.
        await page.CreateStartCursorAsync(cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_PreserveNestedOrdering_When_BackwardPagingPredicateContainsOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        // Backward paging must not reverse the order operations inside the Where lambda.
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name },
            Predicate: t => t.Products.OrderByDescending(p => p.Price).FirstOrDefault()!.Price >= 0m,
            Sorting: new SortDefinition<Brand>().AddDescending(t => t.Id));

        var arguments = new PagingArguments(last: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // A cursor must be creatable for each edge, against the projected Brand.
        await page.CreateStartCursorAsync(cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_CreateCursor_When_OrderKeyContainsNestedOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // Act
        // The sort key's root member must be hoisted into the selector; the inner key must not be.
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id },
            Sorting: new SortDefinition<Brand>()
                .AddAscending(t => t.Products
                    .Where(p => t.Name.Length > 0)
                    .OrderBy(p => p.Price)
                    .First()
                    .Price)
                .AddAscending(t => t.Id));

        var arguments = new PagingArguments(first: 2);

        await using var context = new CatalogContext(connectionString);

        var page = await context.Brands
            .With(query)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);

        // A cursor must be creatable for each edge, against the computed order key.
        await page.CreateStartCursorAsync(cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        await snapshot.AddStreamPageAsync(page, cancellationToken);
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task ToPageAsync_Should_FetchSecondPage_When_OrderKeyContainsNestedOrderBy()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id },
            Sorting: new SortDefinition<Brand>()
                .AddAscending(t => t.Products
                    .Where(p => t.Name.Length > 0)
                    .OrderBy(p => p.Price)
                    .First()
                    .Price)
                .AddAscending(t => t.Id));

        await using var context = new CatalogContext(connectionString);

        // -> get first page
        var arguments = new PagingArguments(first: 2);
        var page = await context.Brands.With(query).ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var endCursor = await page.CreateEndCursorAsync(cancellationToken);

        // Act
        // The keyset predicate from the computed order key must translate to SQL.
        arguments = new PagingArguments(first: 2, after: endCursor);
        page = await context.Brands.With(query).ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var entries = await DrainEntriesAndDisposeAsync(page, cancellationToken);

        // Assert
        var snapshot = Snapshot
            .Create(postFix: TestEnvironment.TargetFramework)
            .AddQueries(interceptor.Queries);
        snapshot.Add(entries.ConvertAll(e => e.Item.Id).ToArray(), "Page");
        snapshot.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Before_Last_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get last page
        var arguments = new PagingArguments(last: 2);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var startCursor = await page.CreateStartCursorAsync(cancellationToken);
        await page.DisposeAsync();

        // Act
        arguments = arguments with { Before = startCursor };
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        await page.MatchMarkdownSnapshotAsync(cancellationToken);
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // -> get last page
        var arguments = new PagingArguments(last: 4);
        await using var context = new CatalogContext(connectionString);
        var page = await context.Products
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToStreamPageAsync(arguments, cancellationToken: cancellationToken);
        var startCursor = await page.CreateStartCursorAsync(cancellationToken);
        var endCursor = await page.CreateEndCursorAsync(cancellationToken);

        // Act
        arguments = new PagingArguments(after: startCursor, last: 2, before: endCursor);
        page = await context.Products.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);

        // Assert
        // HasPreviousPage is intentionally left out of this snapshot: ToStreamPageAsync and
        // ToPageAsync disagree on its value for after+last.
        var entries = await DrainEntriesAndDisposeAsync(page, cancellationToken);
        new
        {
            HasNextPage = await page.HasNextPageAsync(cancellationToken),
            Items = entries.ConvertAll(e => e.Item),
            Cursors = entries.ConvertAll(page.CreateCursor)
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_Descending_AllTypes()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedTestAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new CatalogContext(connectionString);

        Dictionary<string, IOrderedQueryable<Test>> queries = new()
        {
            { "Bool", context.Tests.OrderByDescending(t => t.Bool) },
            { "DateOnly", context.Tests.OrderByDescending(t => t.DateOnly) },
            { "DateTime", context.Tests.OrderByDescending(t => t.DateTime) },
            { "DateTimeOffset", context.Tests.OrderByDescending(t => t.DateTimeOffset) },
            { "Decimal", context.Tests.OrderByDescending(t => t.Decimal) },
            { "Double", context.Tests.OrderByDescending(t => t.Double) },
            { "Float", context.Tests.OrderByDescending(t => t.Float) },
            { "Guid", context.Tests.OrderByDescending(t => t.Guid) },
            { "Int", context.Tests.OrderByDescending(t => t.Int) },
            { "Long", context.Tests.OrderByDescending(t => t.Long) },
            { "Short", context.Tests.OrderByDescending(t => t.Short) },
            { "String", context.Tests.OrderByDescending(t => t.String) },
            { "TimeOnly", context.Tests.OrderByDescending(t => t.TimeOnly) },
            { "UInt", context.Tests.OrderByDescending(t => t.UInt) },
            { "ULong", context.Tests.OrderByDescending(t => t.ULong) },
            { "UShort", context.Tests.OrderByDescending(t => t.UShort) },
            { "ByteEnum", context.Tests.OrderByDescending(t => t.ByteEnum) },
            { "SbyteEnum", context.Tests.OrderByDescending(t => t.SbyteEnum) },
            { "ShortEnum", context.Tests.OrderByDescending(t => t.ShortEnum) },
            { "UshortEnum", context.Tests.OrderByDescending(t => t.UshortEnum) },
            { "IntEnum", context.Tests.OrderByDescending(t => t.IntEnum) },
            { "UintEnum", context.Tests.OrderByDescending(t => t.UintEnum) },
            { "LongEnum", context.Tests.OrderByDescending(t => t.LongEnum) },
            { "UlongEnum", context.Tests.OrderByDescending(t => t.UlongEnum) }
        };

        // Act
        Dictionary<string, List<PageEntry<Test>>> pages = [];

        foreach (var (label, query) in queries)
        {
            // Get 1st page.
            var arguments = new PagingArguments(2);
            var page = await query.ThenByDescending(t => t.Id).ToStreamPageAsync(
                arguments,
                cancellationToken: cancellationToken);
            var endCursor = await page.CreateEndCursorAsync(cancellationToken);

            // Get 2nd page.
            arguments = new PagingArguments(2, after: endCursor);
            var secondPage = await query.ThenByDescending(t => t.Id).ToStreamPageAsync(
                arguments,
                cancellationToken: cancellationToken);
            pages.Add(label, await DrainEntriesAndDisposeAsync(secondPage, cancellationToken));
        }

        // Assert
        pages.ToDictionary(
            p => p.Key,
            p =>
                p.Value.Select(
                    e =>
                        new
                        {
                            e.Item.Id,
                            Value = e.Item.GetType().GetProperty(p.Key)?.GetValue(e.Item)
                        })).MatchMarkdownSnapshot();
    }

    private static async ValueTask<string[]> ToArrayAsync(StreamPage<SequentialBrand> page)
    {
        var items = new List<string>();

        await foreach (var brand in page)
        {
            items.Add(brand.Name);
        }

        return [.. items];
    }

    private static async ValueTask<List<PageEntry<T>>> DrainEntriesAndDisposeAsync<T>(
        StreamPage<T> page,
        CancellationToken cancellationToken)
    {
        List<PageEntry<T>> entries = [];

        await foreach (var entry in page.GetEntriesAsync(cancellationToken))
        {
            entries.Add(entry);
        }

        await page.DisposeAsync();

        return entries;
    }

    private static async Task SeedSequentialAsync(string connectionString, int count)
    {
        await using var context = new SequentialContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Brands.Add(new SequentialBrand { Name = $"Item{i:D4}" });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt" };
        context.ProductTypes.Add(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand" + i,
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

    private static async Task SeedTestAsync(string connectionString)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= 8; i++)
        {
            var test = new Test
            {
                Id = i,
                Bool = i > 4,
                DateOnly = DateOnly.FromDateTime(DateTime.UnixEpoch.AddDays(i - 1)),
                DateTime = DateTime.UnixEpoch.AddDays(i - 1),
                DateTimeOffset = DateTimeOffset.UnixEpoch.AddDays(i - 1),
                Decimal = i,
                Double = i,
                Float = i,
                Guid = Guid.ParseExact($"0000000000000000000000000000000{i}", "N"),
                Int = i,
                Long = i,
                Short = (short)i,
                String = i.ToString(),
                TimeOnly = TimeOnly.MinValue.AddHours(i),
                TimeSpan = TimeSpan.FromHours(i),
                UInt = (uint)i,
                ULong = (ulong)i,
                UShort = (ushort)i,
                ByteEnum = i > 4 ? TestByteEnum.Two : TestByteEnum.One,
                SbyteEnum = i > 4 ? TestSbyteEnum.Two : TestSbyteEnum.One,
                ShortEnum = i > 4 ? TestShortEnum.Two : TestShortEnum.One,
                UshortEnum = i > 4 ? TestUshortEnum.Two : TestUshortEnum.One,
                IntEnum = i > 4 ? TestIntEnum.Two : TestIntEnum.One,
                UintEnum = i > 4 ? TestUintEnum.Two : TestUintEnum.One,
                LongEnum = i > 4 ? TestLongEnum.Two : TestLongEnum.One,
                UlongEnum = i > 4 ? TestUlongEnum.Two : TestUlongEnum.One
            };

            context.Tests.Add(test);
        }

        await context.SaveChangesAsync();
    }

    public class SequentialContext(string connectionString, IEnumerable<IInterceptor>? interceptors = null) : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseNpgsql(connectionString);

            if (interceptors is not null)
            {
                optionsBuilder.AddInterceptors(interceptors);
            }
        }

        public DbSet<SequentialBrand> Brands => Set<SequentialBrand>();
    }

    public class SequentialBrand
    {
        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }

    private sealed class RecordingLifetime(IAsyncDisposable inner) : IAsyncDisposable
    {
        public int DisposeCount { get; private set; }

        public async ValueTask DisposeAsync()
        {
            DisposeCount++;
            await inner.DisposeAsync();
        }
    }
}
#endif

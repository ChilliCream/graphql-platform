#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

// Mirrors the Fetch_* navigation cases of RelativeCursorTests.cs against ToStreamPageAsync, plus
// the BatchStreamFetch_* per-key end cursor cases against ToBatchStreamPageAsync.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamRelativeCursorTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Fetch_Second_Page()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(second);

        // Assert

        /*
        1. Aetherix
        2. Brightex     <- Cursor
        3. Celestara    <- Page 2 - Item 1
        4. Dynamova     <- Page 2 - Item 2
        5. Evolvance
        6. Futurova
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, TotalCount = await second.TotalCountAsync(cancellationToken), Items = items })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], 1) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(second);

        // Assert

        /*
        1. Aetherix
        2. Brightex     <- Cursor
        3. Celestara
        4. Dynamova
        5. Evolvance    <- Page 3 - Item 1
        6. Futurova     <- Page 3 - Item 2
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, TotalCount = await second.TotalCountAsync(cancellationToken), Items = items })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var secondEntries = await DrainEntriesAndDisposeAsync(second, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(secondEntries[^1], 1) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(fourth);

        // Assert

        /*
        1. Aetherix
        2. Brightex
        3. Celestara
        4. Dynamova     <- Cursor
        5. Evolvance
        6. Futurova
        7. Glacient     <- Page 4 - Item 1
        8. Hyperionix   <- Page 4 - Item 2
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, TotalCount = await fourth.TotalCountAsync(cancellationToken), Items = items })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], 2) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(fourth);

        // Assert

        /*
        1. Aetherix
        2. Brightex     <- Cursor
        3. Celestara
        4. Dynamova
        5. Evolvance
        6. Futurova
        7. Glacient     <- Page 4 - Item 1
        8. Hyperionix   <- Page 4 - Item 2
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, TotalCount = await fourth.TotalCountAsync(cancellationToken), Items = items })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var lastEntries = await DrainEntriesAndDisposeAsync(last, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(lastEntries[0], 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(secondToLast);

        // Assert

        /*
        14. Nebularis
        15. Omniflex
        16. Pulsarix
        17. Quantumis   <- Selected - Item 1
        18. Radiantum   <- Selected - Item 2
        19. Synerflux   <- Cursor
        20. Vertexis
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = secondToLast.Index,
                TotalCount = await secondToLast.TotalCountAsync(cancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Third_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var lastEntries = await DrainEntriesAndDisposeAsync(last, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(lastEntries[0], -1) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(thirdToLast);

        // Assert

        /*
        14. Nebularis
        15. Omniflex    <- Selected - Item 1
        16. Pulsarix    <- Selected - Item 2
        17. Quantumis
        18. Radiantum
        19. Synerflux   <- Cursor
        20. Vertexis
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = thirdToLast.Index,
                TotalCount = await thirdToLast.TotalCountAsync(cancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_Offset_Negative_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var lastEntries = await DrainEntriesAndDisposeAsync(last, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(lastEntries[0], -2) };
        var fourthToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(fourthToLast);

        // Assert

        /*
        11. Kinetiq
        12. Luminara
        13. Momentumix  <- Selected - Item 1
        14. Nebularis   <- Selected - Item 2
        15. Omniflex
        16. Pulsarix
        17. Quantumis
        18. Radiantum
        19. Synerflux   <- Cursor
        20. Vertexis
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = fourthToLast.Index,
                TotalCount = await fourthToLast.TotalCountAsync(cancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_From_Second_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var lastEntries = await DrainEntriesAndDisposeAsync(last, cancellationToken);
        arguments = arguments with { Before = last.CreateCursor(lastEntries[0], 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var secondToLastEntries = await DrainEntriesAndDisposeAsync(secondToLast, cancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLastEntries[0], -1) };
        var fourthToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var items = await ToArrayAsync(fourthToLast);

        // Assert

        /*
        11. Kinetiq
        12. Luminara
        13. Momentumix  <- Selected - Item 1
        14. Nebularis   <- Selected - Item 2
        15. Omniflex
        16. Pulsarix
        17. Quantumis   <- Cursor
        18. Radiantum
        19. Synerflux
        20. Vertexis
        */

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = fourthToLast.Index,
                TotalCount = await fourthToLast.TotalCountAsync(cancellationToken),
                Items = items
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Backward_With_Positive_Offset()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var lastEntries = await DrainEntriesAndDisposeAsync(last, cancellationToken);
        arguments = arguments with { Before = last.CreateCursor(lastEntries[0], 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var secondToLastEntries = await DrainEntriesAndDisposeAsync(secondToLast, cancellationToken);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLastEntries[0], 0) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var thirdToLastEntries = await DrainEntriesAndDisposeAsync(thirdToLast, cancellationToken);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLastEntries[0], 1) };

        async Task Error()
        {
            await using var ctx = new TestContext(connectionString);
            await ctx.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(arguments);
        }

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task RequestedSize_Not_Evenly_Divisible_By_TotalCount()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(12) { EnableRelativeCursors = true };

        // Act
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var totalCount = await first.TotalCountAsync(cancellationToken);
        var forwardCursors = await first.CreateRelativeForwardCursorsAsync(cancellationToken: cancellationToken);

        // Assert
        Assert.Equal(20, totalCount);
        Assert.Single(forwardCursors);
    }

    [Fact]
    public async Task BatchStreamFetch_End_Cursor_Offset_Negative_1_Aligns_Per_Key()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };

        // Act
        var pages = await context.Brands
            .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);

        // Assert: every key aligns to its own total, not the cursor's cached total.
        Snapshot.Create()
            .Add(new
            {
                Key1 = await SnapshotAsync(pages[1], cancellationToken),
                Key2 = await SnapshotAsync(pages[2], cancellationToken),
                Key3 = await SnapshotAsync(pages[3], cancellationToken)
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchStreamFetch_End_Cursor_Offset_Negative_2_Aligns_Per_Key()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-2, 25) };

        // Act
        var pages = await context.Brands
            .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);

        // Assert
        Snapshot.Create()
            .Add(new
            {
                Key1 = await SnapshotAsync(pages[1], cancellationToken),
                Key2 = await SnapshotAsync(pages[2], cancellationToken),
                Key3 = await SnapshotAsync(pages[3], cancellationToken)
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchStreamFetch_End_Cursor_Offset_Negative_3_Aligns_Per_Key()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-3, 25) };

        // Act
        var pages = await context.Brands
            .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);

        // Assert: keys already before their own first page at this offset get an empty page.
        Snapshot.Create()
            .Add(new
            {
                Key1 = await SnapshotAsync(pages[1], cancellationToken),
                Key2 = await SnapshotAsync(pages[2], cancellationToken),
                Key3 = await SnapshotAsync(pages[3], cancellationToken)
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ThrowArgumentException_When_EndCursorSkipOverflowsInt()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = new PagingArguments(last: 2) { Before = CursorFormatter.FormatEndCursor(-int.MaxValue, 25) };

        // Act
        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands
                .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);
        }

        // Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The end cursor offset points too far before the last page for the requested page size. (Parameter 'arguments')",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToStreamPageAsync_Should_ThrowArgumentException_When_FirstIsNotGreaterThanZero(int first)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(first);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                cancellationToken: cancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`first` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToStreamPageAsync_Should_ThrowArgumentException_When_LastIsNotGreaterThanZero(int last)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: last);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                cancellationToken: cancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`last` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Fact]
    public async Task ToStreamPageAsync_Should_ThrowArgumentException_When_RelativeOffsetOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(10) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // This offset times the page size overflows an int and must be rejected.
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], int.MaxValue / 2) };

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
                arguments,
                cancellationToken: cancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The relative cursor offset is too large for the requested page size. (Parameter 'arguments')",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToBatchStreamPageAsync_Should_ThrowArgumentException_When_FirstIsNotGreaterThanZero(int first)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = new PagingArguments(first);

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands
                .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);
        }

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`first` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToBatchStreamPageAsync_Should_ThrowArgumentException_When_LastIsNotGreaterThanZero(int last)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;
        var arguments = new PagingArguments(last: last);

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands
                .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);
        }

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`last` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ThrowArgumentException_When_RelativeOffsetOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(10) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToStreamPageAsync(
            arguments,
            cancellationToken: cancellationToken);
        var firstEntries = await DrainEntriesAndDisposeAsync(first, cancellationToken);

        // This offset times the page size overflows an int and must be rejected.
        arguments = arguments with { After = first.CreateCursor(firstEntries[^1], int.MaxValue / 2) };

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchStreamPageAsync(
                t => t.GroupId,
                arguments,
                cancellationToken: cancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The relative cursor offset is too large for the requested page size. (Parameter 'arguments')",
            exception.Message);
    }

    [Fact]
    public async Task ToBatchStreamPageAsync_Should_ThrowArgumentException_When_BatchWindowOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        // a skip of zero pages before the last one fits an int on its own, so only the guard on
        // twice the requested count may report the failure, not the end-cursor-skip guard.
        const int requestedCount = int.MaxValue / 2 + 1;
        var arguments = new PagingArguments(last: requestedCount)
        {
            Before = CursorFormatter.FormatEndCursor(-1, 25)
        };

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands
                .Where(t => new[] { 1, 2, 3 }.Contains(t.GroupId))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(t => t.GroupId, arguments, cancellationToken: cancellationToken);
        }

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "Twice the requested page size does not fit into an int, so the batch end cursor "
            + "window cannot be computed. (Parameter 'arguments')",
            exception.Message);
    }

    private static async ValueTask<object> SnapshotAsync(StreamPage<Brand> page, CancellationToken cancellationToken)
        => new
        {
            page.Index,
            TotalCount = await page.TotalCountAsync(cancellationToken),
            HasNextPage = await page.HasNextPageAsync(cancellationToken),
            HasPreviousPage = await page.HasPreviousPageAsync(cancellationToken),
            Items = await ToArrayAsync(page)
        };

    private static async Task SeedThreeGroupsAsync(
        string connectionString,
        int countGroup1,
        int countGroup2,
        int countGroup3)
    {
        await using var context = new TestContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= countGroup1; i++)
        {
            context.Brands.Add(new Brand { Name = $"A{i:D4}", GroupId = 1 });
        }

        for (var i = 1; i <= countGroup2; i++)
        {
            context.Brands.Add(new Brand { Name = $"B{i:D4}", GroupId = 2 });
        }

        for (var i = 1; i <= countGroup3; i++)
        {
            context.Brands.Add(new Brand { Name = $"C{i:D4}", GroupId = 3 });
        }

        await context.SaveChangesAsync();
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

        await foreach (var entry in page.GetEntriesAsync(cancellationToken))
        {
            entries.Add(entry);
        }

        await page.DisposeAsync();

        return entries;
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var context = new TestContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        /*
        1. Aetherix
        2. Brightex
        3. Celestara
        4. Dynamova
        5. Evolvance
        6. Futurova
        7. Glacient
        8. Hyperionix
        9. Innovexa
        10. Joventra
        11. Kinetiq
        12. Luminara
        13. Momentumix
        14. Nebularis
        15. Omniflex
        16. Pulsarix
        17. Quantumis
        18. Radiantum
        19. Synerflux
        20. Vertexis
        */

        context.Brands.Add(new Brand { Name = "Aetherix", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Brightex", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Celestara", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Dynamova", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Evolvance", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Futurova", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Glacient", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Hyperionix", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Innovexa", GroupId = 1 });
        context.Brands.Add(new Brand { Name = "Joventra", GroupId = 1 });

        context.Brands.Add(new Brand { Name = "Kinetiq", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Luminara", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Momentumix", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Nebularis", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Omniflex", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Pulsarix", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Quantumis", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Radiantum", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Synerflux", GroupId = 2 });
        context.Brands.Add(new Brand { Name = "Vertexis", GroupId = 2 });

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
        public int GroupId { get; set; }

        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}
#endif

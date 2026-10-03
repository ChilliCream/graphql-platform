#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using CookieCrumble.Resources;
using GreenDonut.Data.Cursors;
using Microsoft.EntityFrameworkCore;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class RelativeCursorTests(PostgreSqlResource resource)
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

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Second_Page()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var second = map[1];

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
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 1) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var second = map[1];

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
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!.Value, 1) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!.Value, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var fourth = map[1];

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
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 2) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, 2) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var fourth = map[1];

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
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
                secondToLast.TotalCount,
                Items = secondToLast.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var secondToLast = map[2];

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
                secondToLast.TotalCount,
                Items = secondToLast.Select(t => t.Name).ToArray()
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

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, -1) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
                thirdToLast.TotalCount,
                Items = thirdToLast.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Third_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, -1) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var thirdToLast = map[2];

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
                thirdToLast.TotalCount,
                Items = thirdToLast.Select(t => t.Name).ToArray()
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

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, -2) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_To_Last_Page_Offset_Negative_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, -2) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var thirdToLast = map[2];

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
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Select(t => t.Name).ToArray()
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

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!.Value, -1) };
        var fourthToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

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
                fourthToLast.TotalCount,
                Items = fourthToLast.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_To_Last_Page_From_Second_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!.Value, -1) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments, Xunit.TestContext.Current.CancellationToken);
        var fourthToLast = map[2];

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
                fourthToLast.TotalCount,
                Items = fourthToLast.Select(t => t.Name).ToArray()
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

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!.Value, 0) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLast.First!.Value, 1) };

        async Task Error()
        {
            await using var ctx = new TestContext(connectionString);
            await ctx.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task BatchFetch_Backward_With_Positive_Offset()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = last.CreateCursor(last.First!.Value, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!.Value, 0) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLast.First!.Value, 1) };

        async Task Error()
        {
            await using var ctx = new TestContext(connectionString);
            await ctx.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToBatchPageAsync(t => t.GroupId, arguments);
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
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(12) { EnableRelativeCursors = true };

        // Act
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(20, first.TotalCount);
        Assert.Single(first.CreateRelativeForwardCursors());
    }

    [Fact]
    public async Task Fetch_End_Cursor_Last_Page_Not_Evenly_Divisible()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act

        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_Last_Page_Evenly_Divisible()
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

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_Last_Page_Smaller_Than_RequestedSize()
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

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_Last_Page_Empty_Dataset()
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

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_One_Page_Before_Last()
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

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_Two_Pages_Before_Last()
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

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_One_Page_Before_Last_Forces_Fresh_Count()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 26);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };

        // Act

        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            includeTotalCount: true,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_Offset_Before_First_Page_Returns_Empty_Page()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 40);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-3, 25) };

        // Act

        var page = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                page.Index,
                page.TotalCount,
                page.HasNextPage,
                page.HasPreviousPage,
                Items = page.Select(t => t.Name).ToArray()
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_End_Cursor_With_First_Throws()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(first: 10)
        {
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
    public async Task Fetch_End_Cursor_In_After_Throws()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedSequentialAsync(connectionString, 25);

        var arguments = new PagingArguments(last: 10)
        {
            After = CursorFormatter.FormatEndCursor(0, 25)
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
    public async Task BatchFetch_End_Cursor_Trims_Per_Key_Total()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(0, 25) };

        // Act

        var map = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
            t => t.GroupId,
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                Key1 = new
                {
                    map[1].Index,
                    map[1].TotalCount,
                    map[1].HasNextPage,
                    map[1].HasPreviousPage,
                    Items = map[1].Select(t => t.Name).ToArray()
                },
                Key2 = new
                {
                    map[2].Index,
                    map[2].TotalCount,
                    map[2].HasNextPage,
                    map[2].HasPreviousPage,
                    Items = map[2].Select(t => t.Name).ToArray()
                }
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_End_Cursor_Offset_Before_First_Page_Returns_Empty_Pages()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-3, 25) };

        // Act

        var map = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
            t => t.GroupId,
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                Key1 = new
                {
                    map[1].Index,
                    map[1].TotalCount,
                    map[1].HasNextPage,
                    map[1].HasPreviousPage,
                    Items = map[1].Select(t => t.Name).ToArray()
                },
                Key2 = new
                {
                    map[2].Index,
                    map[2].TotalCount,
                    map[2].HasNextPage,
                    map[2].HasPreviousPage,
                    Items = map[2].Select(t => t.Name).ToArray()
                }
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_End_Cursor_Offset_Negative_1_Aligns_Per_Key()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-1, 25) };

        // Act

        var map = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
            t => t.GroupId,
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        // Each key aligns to its own total, not the cursor's cached total.
        Snapshot.Create()
            .Add(new
            {
                Key1 = new
                {
                    map[1].Index,
                    map[1].TotalCount,
                    map[1].HasNextPage,
                    map[1].HasPreviousPage,
                    Items = map[1].Select(t => t.Name).ToArray()
                },
                Key2 = new
                {
                    map[2].Index,
                    map[2].TotalCount,
                    map[2].HasNextPage,
                    map[2].HasPreviousPage,
                    Items = map[2].Select(t => t.Name).ToArray()
                },
                Key3 = new
                {
                    map[3].Index,
                    map[3].TotalCount,
                    map[3].HasNextPage,
                    map[3].HasPreviousPage,
                    Items = map[3].Select(t => t.Name).ToArray()
                }
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_End_Cursor_Offset_Negative_2_Aligns_Per_Key()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-2, 25) };

        // Act

        var map = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
            t => t.GroupId,
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert

        Snapshot.Create()
            .Add(new
            {
                Key1 = new
                {
                    map[1].Index,
                    map[1].TotalCount,
                    map[1].HasNextPage,
                    map[1].HasPreviousPage,
                    Items = map[1].Select(t => t.Name).ToArray()
                },
                Key2 = new
                {
                    map[2].Index,
                    map[2].TotalCount,
                    map[2].HasNextPage,
                    map[2].HasPreviousPage,
                    Items = map[2].Select(t => t.Name).ToArray()
                },
                Key3 = new
                {
                    map[3].Index,
                    map[3].TotalCount,
                    map[3].HasNextPage,
                    map[3].HasPreviousPage,
                    Items = map[3].Select(t => t.Name).ToArray()
                }
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_End_Cursor_Offset_Negative_3_Aligns_Per_Key()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedThreeGroupsAsync(connectionString, 25, 30, 40);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(-3, 25) };

        // Act

        var map = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
            t => t.GroupId,
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // Assert
        // The 25- and 30-row groups get empty pages; the 40-row group is still in range.
        Snapshot.Create()
            .Add(new
            {
                Key1 = new
                {
                    map[1].Index,
                    map[1].TotalCount,
                    map[1].HasNextPage,
                    map[1].HasPreviousPage,
                    Items = map[1].Select(t => t.Name).ToArray()
                },
                Key2 = new
                {
                    map[2].Index,
                    map[2].TotalCount,
                    map[2].HasNextPage,
                    map[2].HasPreviousPage,
                    Items = map[2].Select(t => t.Name).ToArray()
                },
                Key3 = new
                {
                    map[3].Index,
                    map[3].TotalCount,
                    map[3].HasNextPage,
                    map[3].HasPreviousPage,
                    Items = map[3].Select(t => t.Name).ToArray()
                }
            })
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Absolute_After_With_End_Cursor_Before_Throws()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

        string afterCursor;
        await using (var context = new TestContext(connectionString))
        {
            var firstPage = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
                new PagingArguments(5),
                Xunit.TestContext.Current.CancellationToken);
            afterCursor = firstPage.CreateCursor(firstPage.Last!.Value);
        }

        // An `after` cursor combined with an end cursor `before` must be rejected, not resolved
        // to an empty page.
        var arguments = new PagingArguments(last: 10)
        {
            After = afterCursor,
            Before = CursorFormatter.FormatEndCursor(-3, 25)
        };

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);
        }

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    [Fact]
    public async Task ToBatchPageAsync_Should_ThrowInvalidOperationException_When_EndCursorOffsetIsIntMinValue()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

        var arguments = new PagingArguments(last: 10) { Before = CursorFormatter.FormatEndCursor(int.MinValue, 25) };

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);
        }

        // Assert

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(Error);
        Assert.Equal("The cursor page info could not be parsed.", exception.Message);
    }

    [Fact]
    public async Task ToBatchPageAsync_Should_ThrowArgumentException_When_EndCursorSkipOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

        var arguments = new PagingArguments(last: 2) { Before = CursorFormatter.FormatEndCursor(-int.MaxValue, 25) };

        // Act

        async Task Error()
        {
            await using var context = new TestContext(connectionString);
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);
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
    public async Task ToPageAsync_Should_ThrowArgumentException_When_FirstIsNotGreaterThanZero(int first)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(first);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`first` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToPageAsync_Should_ThrowArgumentException_When_LastIsNotGreaterThanZero(int last)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: last);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`last` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Fact]
    public async Task ToPageAsync_Should_ThrowArgumentException_When_RelativeOffsetOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(10) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // This offset times the page size overflows an int and must be rejected.
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, int.MaxValue / 2) };

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The relative cursor offset is too large for the requested page size. (Parameter 'arguments')",
            exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToBatchPageAsync_Should_ThrowArgumentException_When_FirstIsNotGreaterThanZero(int first)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(first);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`first` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ToBatchPageAsync_Should_ThrowArgumentException_When_LastIsNotGreaterThanZero(int last)
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: last);

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal("`last` must be greater than zero. (Parameter 'arguments')", exception.Message);
    }

    [Fact]
    public async Task ToBatchPageAsync_Should_ThrowArgumentException_When_RelativeOffsetOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(10) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(
            arguments,
            Xunit.TestContext.Current.CancellationToken);

        // This offset times the page size overflows an int and must be rejected.
        arguments = arguments with { After = first.CreateCursor(first.Last!.Value, int.MaxValue / 2) };

        // Act

        async Task Error()
            => await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "The relative cursor offset is too large for the requested page size. (Parameter 'arguments')",
            exception.Message);
    }

    [Fact]
    public async Task ToBatchPageAsync_Should_ThrowArgumentException_When_BatchWindowOverflowsInt()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedTwoGroupsAsync(connectionString, 25, 30);

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
            await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToBatchPageAsync(
                t => t.GroupId,
                arguments,
                Xunit.TestContext.Current.CancellationToken);
        }

        // Assert

        var exception = await Assert.ThrowsAsync<ArgumentException>(Error);
        Assert.Equal(
            "Twice the requested page size does not fit into an int, so the batch end cursor "
            + "window cannot be computed. (Parameter 'arguments')",
            exception.Message);
    }

    private static async Task SeedSequentialAsync(string connectionString, int count)
    {
        await using var context = new TestContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        for (var i = 1; i <= count; i++)
        {
            context.Brands.Add(new Brand { Name = $"Item{i:D4}", GroupId = 1 });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedTwoGroupsAsync(string connectionString, int countGroup1, int countGroup2)
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

        await context.SaveChangesAsync();
    }

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

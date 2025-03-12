#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Squadron;

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
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
        1. Aetherix
        2. Brightex     <- Cursor
        3. Celestara    <- Page 2 - Item 1
        4. Dynamova     <- Page 2 - Item 2
        5. Evolvance
        6. Futurova
        */

        Snapshot.Create()
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 2,
                  "TotalCount": 20,
                  "Items": [
                    "Celestara",
                    "Dynamova"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                -- @__p_2='3'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Second_Page()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 2,
                  "TotalCount": 20,
                  "Items": [
                    "Celestara",
                    "Dynamova"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND (b0."Name" > @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" > @__value_1))
                    ) AS b2
                    WHERE b2.row <= 3
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name", b3."Id"
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
        1. Aetherix
        2. Brightex     <- Cursor
        3. Celestara
        4. Dynamova
        5. Evolvance    <- Page 3 - Item 1
        6. Futurova     <- Page 3 - Item 2
        */

        Snapshot.Create()
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 3,
                  "TotalCount": 20,
                  "Items": [
                    "Evolvance",
                    "Futurova"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 3,
                  "TotalCount": 20,
                  "Items": [
                    "Evolvance",
                    "Futurova"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND (b0."Name" > @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" > @__value_1))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name", b3."Id"
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Dynamova'
                -- @__value_1='4'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Dynamova'
                -- @__value_1='4'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND (b0."Name" > @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" > @__value_1))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name", b3."Id"
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 2) };
        var fourth = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                -- @__p_3='3'
                -- @__p_2='4'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 2) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Brightex'
                -- @__value_1='2'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND (b0."Name" > @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" > @__value_1))
                    ) AS b2
                    WHERE 4 < b2.row AND b2.row <= 7
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name", b3."Id"
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new
            {
                Page = secondToLast.Index,
                secondToLast.TotalCount,
                Items = secondToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 9,
                  "TotalCount": 20,
                  "Items": [
                    "Quantumis",
                    "Radiantum"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                -- @__p_2='3'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new
            {
                Page = secondToLast.Index,
                secondToLast.TotalCount,
                Items = secondToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 9,
                  "TotalCount": 20,
                  "Items": [
                    "Quantumis",
                    "Radiantum"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND (b0."Name" < @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" < @__value_1))
                    ) AS b2
                    WHERE b2.row <= 3
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name" DESC, b3."Id" DESC
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Third_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -1) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 8,
                  "TotalCount": 20,
                  "Items": [
                    "Omniflex",
                    "Pulsarix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Third_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -1) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 8,
                  "TotalCount": 20,
                  "Items": [
                    "Omniflex",
                    "Pulsarix"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND (b0."Name" < @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" < @__value_1))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name" DESC, b3."Id" DESC
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_Offset_Negative_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -2) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 7,
                  "TotalCount": 20,
                  "Items": [
                    "Momentumix",
                    "Nebularis"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                -- @__p_3='3'
                -- @__p_2='4'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Fourth_To_Last_Page_Offset_Negative_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -2) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 7,
                  "TotalCount": 20,
                  "Items": [
                    "Momentumix",
                    "Nebularis"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Synerflux'
                -- @__value_1='19'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND (b0."Name" < @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" < @__value_1))
                    ) AS b2
                    WHERE 4 < b2.row AND b2.row <= 7
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name" DESC, b3."Id" DESC
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_From_Second_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, -1) };
        var fourthToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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

        Snapshot.Create()
            .Add(new
            {
                Page = fourthToLast.Index,
                fourthToLast.TotalCount,
                Items = fourthToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 7,
                  "TotalCount": 20,
                  "Items": [
                    "Momentumix",
                    "Nebularis"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Quantumis'
                -- @__value_1='17'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."GroupId", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task BatchFetch_Fourth_To_Last_Page_From_Second_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, -1) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
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

        Snapshot.Create()
            .Add(new
            {
                Page = fourthToLast.Index,
                fourthToLast.TotalCount,
                Items = fourthToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchInline(
                """
                ---------------
                {
                  "Page": 7,
                  "TotalCount": 20,
                  "Items": [
                    "Momentumix",
                    "Nebularis"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='Quantumis'
                -- @__value_1='17'
                SELECT b1."GroupId", b3."Id", b3."GroupId", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."GroupId", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."GroupId", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."Name" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND (b0."Name" < @__value_0 OR (b0."Name" = @__value_0 AND b0."Id" < @__value_1))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."Name" DESC, b3."Id" DESC
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Backward_With_Positive_Offset()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, 0) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLast.First!, 1) };

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
        var last = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, 0) };
        var thirdToLast = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLast.First!, 1) };

        async Task Error()
        {
            await using var ctx = new TestContext(connectionString);
            await ctx.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.Name).ThenBy(t => t.Id)
                .ToBatchPageAsync(t => t.GroupId, arguments);
        }

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(Error);
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

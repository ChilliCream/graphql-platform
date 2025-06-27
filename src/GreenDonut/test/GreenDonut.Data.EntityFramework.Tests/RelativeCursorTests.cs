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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Second_Page_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                -- @__p_3='3'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" >= @__value_0 AND (b."CreatedOn" > @__value_0 OR ((b."ModifiedOn" >= @__value_1 OR b."ModifiedOn" IS NULL) AND (b."ModifiedOn" > @__value_1 OR b."ModifiedOn" IS NULL OR b."Id" > @__value_2)))
                ORDER BY b."CreatedOn", b."ModifiedOn", b."Id"
                LIMIT @__p_3
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1_Ordering_By_Nullable_Date_Column()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var fourth = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
        1. Aetherix
        2. Brightex
        3. Celestara
        4. Evolvance    <- Cursor
        5. Futurova
        6. Glacient
        7. Innovexa     <- Page 4 - Item 1
        8. Joventra     <- Page 4 - Item 2
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
                    "Innovexa",
                    "Joventra"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='01/02/2025' (DbType = Date)
                -- @__value_1='5'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE (b."ModifiedOn" >= @__value_0 OR b."ModifiedOn" IS NULL) AND (b."ModifiedOn" > @__value_0 OR b."ModifiedOn" IS NULL OR b."Id" > @__value_1)
                ORDER BY b."ModifiedOn", b."Id"
                LIMIT @__p_3 OFFSET @__p_2
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Second_Page_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn", b0."ModifiedOn", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND b0."CreatedOn" >= @__value_0 AND (b0."CreatedOn" > @__value_0 OR ((b0."ModifiedOn" >= @__value_1 OR b0."ModifiedOn" IS NULL) AND (b0."ModifiedOn" > @__value_1 OR b0."ModifiedOn" IS NULL OR b0."Id" > @__value_2)))
                    ) AS b2
                    WHERE b2.row <= 3
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn", b3."ModifiedOn", b3."Id"
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Third_Page_With_Offset_1_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };
        var second = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                -- @__p_4='3'
                -- @__p_3='2'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" >= @__value_0 AND (b."CreatedOn" > @__value_0 OR ((b."ModifiedOn" >= @__value_1 OR b."ModifiedOn" IS NULL) AND (b."ModifiedOn" > @__value_1 OR b."ModifiedOn" IS NULL OR b."Id" > @__value_2)))
                ORDER BY b."CreatedOn", b."ModifiedOn", b."Id"
                LIMIT @__p_4 OFFSET @__p_3
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = second.Index, second.TotalCount, Items = second.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Third_Page_With_Offset_1_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn", b0."ModifiedOn", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND b0."CreatedOn" >= @__value_0 AND (b0."CreatedOn" > @__value_0 OR ((b0."ModifiedOn" >= @__value_1 OR b0."ModifiedOn" IS NULL) AND (b0."ModifiedOn" > @__value_1 OR b0."ModifiedOn" IS NULL OR b0."Id" > @__value_2)))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn", b3."ModifiedOn", b3."Id"
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1_Ordering_By_Nullable_Columns_NULL_Cursor()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var fourth = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
         1. Aetherix
         2. Brightex
         3. Celestara
         4. Dynamova     <- NULL Cursor
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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='4'
                -- @__p_3='3'
                -- @__p_2='2'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" >= @__value_0 AND (b."CreatedOn" > @__value_0 OR (b."ModifiedOn" IS NULL AND b."Id" > @__value_1))
                ORDER BY b."CreatedOn", b."ModifiedOn", b."Id"
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_1_Ordering_By_Nullable_Columns_NULL_Cursor()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
            .ToBatchPageAsync(t => t.GroupId, arguments);
        var fourth = map[1];

        // Assert

        /*
        1. Aetherix
        2. Brightex
        3. Celestara
        4. Dynamova     <- NULL Cursor
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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='4'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn", b0."ModifiedOn", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND b0."CreatedOn" >= @__value_0 AND (b0."CreatedOn" > @__value_0 OR (b0."ModifiedOn" IS NULL AND b0."Id" > @__value_1))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn", b3."ModifiedOn", b3."Id"
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Ordering_By_Nullable_Columns_NULL_Cursor()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 5) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var fourthToLast = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
        11. Kinetiq     <- Selected - Item 1
        12. Luminara    <- Selected - Item 2
        13. Momentumix  <- Selected - Item 3
        14. Nebularis   <- Selected - Item 4
        15. Omniflex    <- Selected - Item 5
        16. Pulsarix    <- NULL Cursor
        17. Quantumis
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
                  "Page": 3,
                  "TotalCount": 20,
                  "Items": [
                    "Kinetiq",
                    "Luminara",
                    "Momentumix",
                    "Nebularis",
                    "Omniflex"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='01/04/2025' (DbType = Date)
                -- @__value_1='16'
                -- @__p_2='6'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" <= @__value_0 AND (b."CreatedOn" < @__value_0 OR b."ModifiedOn" IS NOT NULL OR (b."ModifiedOn" IS NULL AND b."Id" < @__value_1))
                ORDER BY b."CreatedOn" DESC, b."ModifiedOn" DESC, b."Id" DESC
                LIMIT @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 2) };
        var fourth = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                -- @__p_4='3'
                -- @__p_3='4'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" >= @__value_0 AND (b."CreatedOn" > @__value_0 OR ((b."ModifiedOn" >= @__value_1 OR b."ModifiedOn" IS NULL) AND (b."ModifiedOn" > @__value_1 OR b."ModifiedOn" IS NULL OR b."Id" > @__value_2)))
                ORDER BY b."CreatedOn", b."ModifiedOn", b."Id"
                LIMIT @__p_4 OFFSET @__p_3
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new { Page = fourth.Index, fourth.TotalCount, Items = fourth.Items.Select(t => t.Name).ToArray() })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Fourth_Page_With_Offset_2_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 2) };
        var map = await context.Brands.Where(t => t.GroupId == 1).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
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
                -- @__value_0='01/01/2025' (DbType = Date)
                -- @__value_1='01/02/2025' (DbType = Date)
                -- @__value_2='2'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 1
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn", b0."ModifiedOn", b0."Id") AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 1 AND b0."CreatedOn" >= @__value_0 AND (b0."CreatedOn" > @__value_0 OR ((b0."ModifiedOn" >= @__value_1 OR b0."ModifiedOn" IS NULL) AND (b0."ModifiedOn" > @__value_1 OR b0."ModifiedOn" IS NULL OR b0."Id" > @__value_2)))
                    ) AS b2
                    WHERE 4 < b2.row AND b2.row <= 7
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn", b3."ModifiedOn", b3."Id"
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = secondToLast.Index,
                secondToLast.TotalCount,
                Items = secondToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

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
                -- @__value_0='01/05/2025' (DbType = Date)
                -- @__value_1='01/06/2025' (DbType = Date)
                -- @__value_2='19'
                -- @__p_3='3'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" <= @__value_0 AND (b."CreatedOn" < @__value_0 OR (b."ModifiedOn" <= @__value_1 AND (b."ModifiedOn" < @__value_1 OR b."Id" < @__value_2)))
                ORDER BY b."CreatedOn" DESC, b."ModifiedOn" DESC, b."Id" DESC
                LIMIT @__p_3
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = secondToLast.Index,
                secondToLast.TotalCount,
                Items = secondToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Second_To_Last_Page_Offset_0_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
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
                -- @__value_0='01/05/2025' (DbType = Date)
                -- @__value_1='01/06/2025' (DbType = Date)
                -- @__value_2='19'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn" DESC, b0."ModifiedOn" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND b0."CreatedOn" <= @__value_0 AND (b0."CreatedOn" < @__value_0 OR (b0."ModifiedOn" <= @__value_1 AND (b0."ModifiedOn" < @__value_1 OR b0."Id" < @__value_2)))
                    ) AS b2
                    WHERE b2.row <= 3
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn" DESC, b3."ModifiedOn" DESC, b3."Id" DESC
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task Fetch_Third_To_Last_Page_Offset_Negative_1_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -1) };
        var thirdToLast = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

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
                -- @__value_0='01/05/2025' (DbType = Date)
                -- @__value_1='01/06/2025' (DbType = Date)
                -- @__value_2='19'
                -- @__p_4='3'
                -- @__p_3='2'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."CreatedOn" <= @__value_0 AND (b."CreatedOn" < @__value_0 OR (b."ModifiedOn" <= @__value_1 AND (b."ModifiedOn" < @__value_1 OR b."Id" < @__value_2)))
                ORDER BY b."CreatedOn" DESC, b."ModifiedOn" DESC, b."Id" DESC
                LIMIT @__p_4 OFFSET @__p_3
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_Offset_Negative_1_Ordering_By_Nullable_Date_Column()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, -1) };
        var fourthToLast = await context.Brands.OrderBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert

        /*
        11. Nebularis
        12. Omniflex
        13. Quantumis   <- Selected - Item 1
        14. Radiantum   <- Selected - Item 2
        15. Synerflux
        16. Dynamova
        17. Hyperionix  <- Cursor
        18. Luminara
        19. Pulsarix
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
                    "Quantumis",
                    "Radiantum"
                  ]
                }
                ---------------

                SQL 0
                ---------------
                -- @__value_0='8'
                -- @__p_2='3'
                -- @__p_1='2'
                SELECT b."Id", b."CreatedOn", b."GroupId", b."ModifiedOn", b."Name"
                FROM "Brands" AS b
                WHERE b."ModifiedOn" IS NOT NULL OR (b."ModifiedOn" IS NULL AND b."Id" < @__value_0)
                ORDER BY b."ModifiedOn" DESC, b."Id" DESC
                LIMIT @__p_2 OFFSET @__p_1
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
            })
            .AddSql(capture)
            .MatchSnapshot();
    }

    [Fact]
    public async Task BatchFetch_Third_To_Last_Page_Offset_Negative_1_Ordering_By_Nullable_Columns()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await context.Brands.OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -1) };
        var map = await context.Brands.Where(t => t.GroupId == 2).OrderBy(t => t.CreatedOn).ThenBy(t => t.ModifiedOn).ThenBy(t => t.Id)
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
                -- @__value_0='01/05/2025' (DbType = Date)
                -- @__value_1='01/06/2025' (DbType = Date)
                -- @__value_2='19'
                SELECT b1."GroupId", b3."Id", b3."CreatedOn", b3."GroupId", b3."ModifiedOn", b3."Name"
                FROM (
                    SELECT b."GroupId"
                    FROM "Brands" AS b
                    WHERE b."GroupId" = 2
                    GROUP BY b."GroupId"
                ) AS b1
                LEFT JOIN (
                    SELECT b2."Id", b2."CreatedOn", b2."GroupId", b2."ModifiedOn", b2."Name"
                    FROM (
                        SELECT b0."Id", b0."CreatedOn", b0."GroupId", b0."ModifiedOn", b0."Name", ROW_NUMBER() OVER(PARTITION BY b0."GroupId" ORDER BY b0."CreatedOn" DESC, b0."ModifiedOn" DESC, b0."Id" DESC) AS row
                        FROM "Brands" AS b0
                        WHERE b0."GroupId" = 2 AND b0."CreatedOn" <= @__value_0 AND (b0."CreatedOn" < @__value_0 OR (b0."ModifiedOn" <= @__value_1 AND (b0."ModifiedOn" < @__value_1 OR b0."Id" < @__value_2)))
                    ) AS b2
                    WHERE 2 < b2.row AND b2.row <= 5
                ) AS b3 ON b1."GroupId" = b3."GroupId"
                ORDER BY b1."GroupId", b3."GroupId", b3."CreatedOn" DESC, b3."ModifiedOn" DESC, b3."Id" DESC
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = thirdToLast.Index,
                thirdToLast.TotalCount,
                Items = thirdToLast.Items.Select(t => t.Name).ToArray()
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = fourthToLast.Index,
                fourthToLast.TotalCount,
                Items = fourthToLast.Items.Select(t => t.Name).ToArray()
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

        Snapshot.Create(postFix: TestEnvironment.TargetFramework)
            .Add(new
            {
                Page = fourthToLast.Index,
                fourthToLast.TotalCount,
                Items = fourthToLast.Items.Select(t => t.Name).ToArray()
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

    [Fact]
    public async Task RequestedSize_Not_Evenly_Divisible_By_TotalCount()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(12) { EnableRelativeCursors = true };

        // Act
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        Assert.Equal(20, first.TotalCount);
        Assert.Single(first.CreateRelativeForwardCursors());
    }

    [Fact]
    public async Task Nullable_Fallback_Cursor()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.ModifiedOn).ToPageAsync(arguments);

        // Act

        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };

        async Task Error()
        {
            await using var ctx = new TestContext(connectionString);
            await ctx.Brands.OrderBy(t => t.ModifiedOn).ToPageAsync(arguments);
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

        context.Brands.Add(new Brand { Name = "Aetherix", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 1), ModifiedOn = new DateOnly(2025, 1, 1) });
        context.Brands.Add(new Brand { Name = "Brightex", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 1), ModifiedOn = new DateOnly(2025, 1, 2) });
        context.Brands.Add(new Brand { Name = "Celestara", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 1), ModifiedOn = new DateOnly(2025, 1, 2) });
        context.Brands.Add(new Brand { Name = "Dynamova", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 1)  });
        context.Brands.Add(new Brand { Name = "Evolvance", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 2), ModifiedOn = new DateOnly(2025, 1, 2) });
        context.Brands.Add(new Brand { Name = "Futurova", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 2), ModifiedOn = new DateOnly(2025, 1, 3) });
        context.Brands.Add(new Brand { Name = "Glacient", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 2), ModifiedOn = new DateOnly(2025, 1, 3) });
        context.Brands.Add(new Brand { Name = "Hyperionix", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 2) });
        context.Brands.Add(new Brand { Name = "Innovexa", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 3), ModifiedOn = new DateOnly(2025, 1, 3) });
        context.Brands.Add(new Brand { Name = "Joventra", GroupId = 1, CreatedOn = new DateOnly(2025, 1, 3), ModifiedOn = new DateOnly(2025, 1, 4) });

        context.Brands.Add(new Brand { Name = "Kinetiq", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 3), ModifiedOn = new DateOnly(2025, 1, 4) });
        context.Brands.Add(new Brand { Name = "Luminara", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 3) });
        context.Brands.Add(new Brand { Name = "Momentumix", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 4), ModifiedOn = new DateOnly(2025, 1, 4) });
        context.Brands.Add(new Brand { Name = "Nebularis", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 4), ModifiedOn = new DateOnly(2025, 1, 5) });
        context.Brands.Add(new Brand { Name = "Omniflex", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 4), ModifiedOn = new DateOnly(2025, 1, 5) });
        context.Brands.Add(new Brand { Name = "Pulsarix", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 4) });
        context.Brands.Add(new Brand { Name = "Quantumis", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 5), ModifiedOn = new DateOnly(2025, 1, 5) });
        context.Brands.Add(new Brand { Name = "Radiantum", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 5), ModifiedOn = new DateOnly(2025, 1, 6) });
        context.Brands.Add(new Brand { Name = "Synerflux", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 5), ModifiedOn = new DateOnly(2025, 1, 6) });
        context.Brands.Add(new Brand { Name = "Vertexis", GroupId = 2, CreatedOn = new DateOnly(2025, 1, 5) });

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

        public DateOnly CreatedOn { get; set; }

        public DateOnly? ModifiedOn { get; set; }
    }
}
#endif

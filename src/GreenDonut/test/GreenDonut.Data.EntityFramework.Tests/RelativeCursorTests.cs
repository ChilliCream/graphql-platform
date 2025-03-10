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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_2
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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" > @__value_0 OR (b."Name" = @__value_0 AND b."Id" > @__value_1)
                ORDER BY b."Name", b."Id"
                LIMIT @__p_3 OFFSET @__p_2
                ---------------

                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        using var capture = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

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
            .MatchInline(
                """
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
                """);
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        using var capture = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        await using var context = new TestContext(connectionString);
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await context.Brands.OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

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
            .MatchInline(
                """
                {
                  "Page": 4,
                  "TotalCount": 20,
                  "Items": [
                    "Glacient",
                    "Hyperionix"
                  ]
                }
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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_2
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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
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
                SELECT b."Id", b."Name"
                FROM "Brands" AS b
                WHERE b."Name" < @__value_0 OR (b."Name" = @__value_0 AND b."Id" < @__value_1)
                ORDER BY b."Name" DESC, b."Id" DESC
                LIMIT @__p_3 OFFSET @__p_2
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

        context.Brands.Add(new Brand { Name = "Aetherix" });
        context.Brands.Add(new Brand { Name = "Brightex" });
        context.Brands.Add(new Brand { Name = "Celestara" });
        context.Brands.Add(new Brand { Name = "Dynamova" });
        context.Brands.Add(new Brand { Name = "Evolvance" });
        context.Brands.Add(new Brand { Name = "Futurova" });
        context.Brands.Add(new Brand { Name = "Glacient" });
        context.Brands.Add(new Brand { Name = "Hyperionix" });
        context.Brands.Add(new Brand { Name = "Innovexa" });
        context.Brands.Add(new Brand { Name = "Joventra" });
        context.Brands.Add(new Brand { Name = "Kinetiq" });
        context.Brands.Add(new Brand { Name = "Luminara" });
        context.Brands.Add(new Brand { Name = "Momentumix" });
        context.Brands.Add(new Brand { Name = "Nebularis" });
        context.Brands.Add(new Brand { Name = "Omniflex" });
        context.Brands.Add(new Brand { Name = "Pulsarix" });
        context.Brands.Add(new Brand { Name = "Quantumis" });
        context.Brands.Add(new Brand { Name = "Radiantum" });
        context.Brands.Add(new Brand { Name = "Synerflux" });
        context.Brands.Add(new Brand { Name = "Vertexis" });

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

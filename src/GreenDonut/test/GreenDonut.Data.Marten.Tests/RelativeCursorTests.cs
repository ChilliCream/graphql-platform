#if NET9_0_OR_GREATER
using System.ComponentModel.DataAnnotations;
using GreenDonut.Data.TestContext;
using Marten;
using Squadron;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class RelativeCursorTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
    {
        var dbName = $"db_{Guid.NewGuid():N}" ;

        Resource.CreateDatabaseAsync(dbName).GetAwaiter().GetResult();

        return Resource.GetConnectionString(dbName);
    }

    private static DocumentStore GetStore(string connectionString)
    {
        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);

            options.UseSystemTextJsonForSerialization();

            //options.AutoCreateSchemaObjects = AutoCreate.All;

            options.Schema.For<Product>().Identity(x => x.Id);
            options.Schema.For<ProductType>().Identity(x => x.Id);
            options.Schema.For<Brand>().Identity(x => x.Id);
            options.Schema.For<Test>().Identity(x => x.Id);
        });

        return store;
    }

    [Fact]
    public async Task Fetch_Second_Page()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Third_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 1) };
        var second = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { After = first.CreateCursor(first.Last!, 0) };
        var second = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = second.CreateCursor(second.Last!, 1) };
        var fourth = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Fourth_Page_With_Offset_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { After = first.CreateCursor(first.Last!, 2) };
        var fourth = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Second_To_Last_Page_Offset_0()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Third_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -1) };
        var thirdToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_Offset_Negative_2()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = last.CreateCursor(last.First!, -2) };
        var thirdToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Fourth_To_Last_Page_From_Second_To_Last_Page_Offset_Negative_1()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        using var capture = new CapturePagingQueryInterceptor();
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, -1) };
        var fourthToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Backward_With_Positive_Offset()
    {
        // Arrange

        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();
        var arguments = new PagingArguments(last: 2) { EnableRelativeCursors = true };
        var last = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = last.CreateCursor(last.First!, 0) };
        var secondToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        arguments = arguments with { Before = secondToLast.CreateCursor(secondToLast.First!, 0) };
        var thirdToLast = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act

        arguments = arguments with { Before = thirdToLast.CreateCursor(thirdToLast.First!, 1) };

        async Task Error()
        {
            await using var ctx = store.LightweightSession();
            await ctx.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);
        }

        // Assert

        await Assert.ThrowsAsync<ArgumentException>(Error);
    }

    private static async Task SeedAsync(string connectionString)
    {
        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();

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

        session.Store(new Brand { Name = "Aetherix", GroupId = 1 });
        session.Store(new Brand { Name = "Brightex", GroupId = 1 });
        session.Store(new Brand { Name = "Celestara", GroupId = 1 });
        session.Store(new Brand { Name = "Dynamova", GroupId = 1 });
        session.Store(new Brand { Name = "Evolvance", GroupId = 1 });
        session.Store(new Brand { Name = "Futurova", GroupId = 1 });
        session.Store(new Brand { Name = "Glacient", GroupId = 1 });
        session.Store(new Brand { Name = "Hyperionix", GroupId = 1 });
        session.Store(new Brand { Name = "Innovexa", GroupId = 1 });
        session.Store(new Brand { Name = "Joventra", GroupId = 1 });

        session.Store(new Brand { Name = "Kinetiq", GroupId = 2 });
        session.Store(new Brand { Name = "Luminara", GroupId = 2 });
        session.Store(new Brand { Name = "Momentumix", GroupId = 2 });
        session.Store(new Brand { Name = "Nebularis", GroupId = 2 });
        session.Store(new Brand { Name = "Omniflex", GroupId = 2 });
        session.Store(new Brand { Name = "Pulsarix", GroupId = 2 });
        session.Store(new Brand { Name = "Quantumis", GroupId = 2 });
        session.Store(new Brand { Name = "Radiantum", GroupId = 2 });
        session.Store(new Brand { Name = "Synerflux", GroupId = 2 });
        session.Store(new Brand { Name = "Vertexis", GroupId = 2 });

        await session.SaveChangesAsync();
    }

    public class Brand
    {
        public int GroupId { get; set; }

        public int Id { get; set; }

        [MaxLength(100)] public required string Name { get; set; }
    }
}
#endif

using GreenDonut.Data.TestContext;
using Marten;
using Squadron;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class PagingHelperTests(PostgreSqlResource resource)
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
    public async Task Fetch_First_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var arguments = new PagingArguments(2);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get first page
        var arguments = new PagingArguments(2);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_With_Offset_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get first page
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        var cursor = page.CreateCursor(page.Last!, 2);
        arguments = new PagingArguments(2, after: cursor);
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_With_Offset_Negative_2()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();

        // -> get first page
        var arguments = new PagingArguments(2) { EnableRelativeCursors = true };
        var first = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // -> get second page
        var cursor = first.CreateCursor(first.Last!, 0);
        arguments = new PagingArguments(2, after: cursor) { EnableRelativeCursors = true };
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // -> get third page
        cursor = page.CreateCursor(page.Last!, 0);
        arguments = new PagingArguments(2, after: cursor) { EnableRelativeCursors = true };
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
        cursor = page.CreateCursor(page.Last!, -1);
        arguments = new PagingArguments(last: 2, before: cursor);
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

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
        new {
            First = page.First!.Name,
            Last = page.Last!.Name,
            ItemsCount = page.Items.Length
        }.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Third_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get first page
        var arguments = new PagingArguments(2);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get first page
        var arguments = new PagingArguments(4);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(2, after: page.CreateCursor(page.First!), before: page.CreateCursor(page.Last!));
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var arguments = new PagingArguments(last: 2);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        var arguments = new PagingArguments(last: 2);

        await using var session = store.LightweightSession();

        await session.Query<Product>()
            .With(query)
            .ToPageAsync(arguments);

        // Assert
        CreateSnapshot()
            .AddQueries(interceptor.Queries)
            .MatchMarkdown();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Brand()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        query = query.Include(t => t.Brand);

        var arguments = new PagingArguments(last: 2);

        await using var session = store.LightweightSession();

        var page = await session.Query<Product>()
            .With(query)
            .ToPageAsync(arguments);

        // Assert
        CreateSnapshot()
            .AddQueries(interceptor.Queries)
            .MatchMarkdown();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Brand_Name()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var query = new QueryContext<Product>(
            Selector: t => new Product { Id = t.Id, Name = t.Name },
            Sorting: new SortDefinition<Product>().AddDescending(t => t.Id));

        query = query.Select(t => new Product { Brand = new Brand { Name = t.Brand!.Name } });

        var arguments = new PagingArguments(last: 2);

        await using var session = store.LightweightSession();

        var page = await session.Query<Product>()
            .With(query)
            .ToPageAsync(arguments);

        // Assert
        CreateSnapshot()
            .AddQueries(interceptor.Queries)
            .MatchMarkdown();
    }

    [Fact]
    public async Task QueryContext_Simple_Selector_Include_Product_List()
    {
        // Arrange
        using var interceptor = new CapturePagingQueryInterceptor();
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // Act
        var query = new QueryContext<Brand>(
            Selector: t => new Brand { Id = t.Id, Name = t.Name, Products = t.Products },
            Sorting: new SortDefinition<Brand>().AddDescending(t => t.Id));

        var arguments = new PagingArguments(last: 2);

        await using var session = store.LightweightSession();

        var page = await session.Query<Brand>()
            .With(query)
            .ToPageAsync(arguments);

        // Assert
        CreateSnapshot()
            .AddQueries(interceptor.Queries)
            .MatchMarkdown();
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Before_Last_Page()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get last page
        var arguments = new PagingArguments(last: 2);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = arguments with { Before = page.CreateCursor(page.First!), };
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_Last_2_Items_Between()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);

        var store = GetStore(connectionString);

        // -> get last page
        var arguments = new PagingArguments(last: 4);
        await using var session = store.LightweightSession();
        var page = await session.Query<Product>()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(arguments);

        // Act
        arguments = new PagingArguments(after: page.CreateCursor(page.First!), last: 2, before: page.CreateCursor(page.Last!));
        page = await session.Query<Product>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(arguments);

        // Assert
        page.MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Fetch_First_2_Items_Second_Page_Descending_AllTypes()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedTestAsync(connectionString);

        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();

        Dictionary<string, IOrderedQueryable<Test>> queries = new()
        {
            { "Bool", session.Query<Test>().OrderByDescending(t => t.Bool) },
            { "DateOnly", session.Query<Test>().OrderByDescending(t => t.DateOnly) },
            { "DateTimeOffset", session.Query<Test>().OrderByDescending(t => t.DateTimeOffset) },
            { "Decimal", session.Query<Test>().OrderByDescending(t => t.Decimal) },
            { "Double", session.Query<Test>().OrderByDescending(t => t.Double) },
            { "Float", session.Query<Test>().OrderByDescending(t => t.Float) },
            { "Guid", session.Query<Test>().OrderByDescending(t => t.Guid) },
            { "Int", session.Query<Test>().OrderByDescending(t => t.Int) },
            { "Long", session.Query<Test>().OrderByDescending(t => t.Long) },
            { "Short", session.Query<Test>().OrderByDescending(t => t.Short) },
            { "String", session.Query<Test>().OrderByDescending(t => t.String) },
            { "TimeOnly", session.Query<Test>().OrderByDescending(t => t.TimeOnly) },
            { "UInt", session.Query<Test>().OrderByDescending(t => t.UInt) },
        };

        // Act
        Dictionary<string, Page<Test>> pages = [];

        foreach (var (label, query) in queries)
        {
            // Get 1st page.
            var arguments = new PagingArguments(2);
            var page = await query.ThenByDescending(t => t.Id).ToPageAsync(arguments);

            // Get 2nd page.
            arguments = new PagingArguments(2, after: page.CreateCursor(page.Last!));
            pages.Add(label, await query.ThenByDescending(t => t.Id).ToPageAsync(arguments));
        }

        // Assert
        pages.MatchMarkdownSnapshot();
    }

    private static async Task SeedAsync(string connectionString)
    {
        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();

        var type = new ProductType { Name = "T-Shirt", };
        session.Store(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand" + i,
                DisplayName = i % 2 == 0 ? "BrandDisplay" + i : null,
                BrandDetails = new() { Country = new() { Name = "Country" + i } }
            };
            session.Store(brand);

            for (var j = 0; j < 100; j++)
            {
                var product = new Product
                {
                    Name = $"Product {i}-{j}",
                    Type = type,
                    Brand = brand,
                };
                session.Store(product);
            }
        }

        await session.SaveChangesAsync();
    }

    private static async Task SeedTestAsync(string connectionString)
    {
        var store = GetStore(connectionString);

        await using var session = store.LightweightSession();

        for (var i = 1; i <= 10; i++)
        {
            var test = new Test
            {
                Id = i,
                Bool = i % 2 == 0,
                DateOnly = DateOnly.FromDateTime(DateTime.UnixEpoch.AddDays(i - 1)),
                DateTimeOffset = DateTimeOffset.UnixEpoch.AddDays(i - 1),
                Decimal = i,
                Double = i,
                Float = i,
                Guid = Guid.ParseExact($"0000000000000000000000000000000{i - 1}", "N"),
                Int = i,
                Long = i,
                Short = (short)i,
                String = i.ToString(),
                TimeOnly = TimeOnly.MinValue.AddHours(i),
                TimeSpan = TimeSpan.FromHours(i),
                UInt = (uint)i
            };

            session.Store(test);
        }

        await session.SaveChangesAsync();
    }

    private static Snapshot CreateSnapshot()
    {
#if NET9_0_OR_GREATER
        return Snapshot.Create();
#else
        return Snapshot.Create("NET8_0");
#endif
    }
}

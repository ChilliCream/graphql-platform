using System.Linq.Expressions;
using GreenDonut.Data.TestContext;
using Marten;
using Squadron;
using Weasel.Core;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationPagingHelperTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
    {
        var dbName = $"db_{Guid.NewGuid():N}";

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
        });

        return store;
    }


    [Fact]
    public async Task Paging_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        var store = GetStore(connectionString);

        // Act
        await using var session = store.LightweightSession();

        var pagingArgs = new PagingArguments { First = 5 };
        var result = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await CreateSnapshot()
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_After_Id_13()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        var store = GetStore(connectionString);

        // Act
        await using var session = store.LightweightSession();

        var pagingArgs = new PagingArguments
        {
            First = 5,
            After = "QnJhbmQxMjoxMw=="
        };
        var result = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await CreateSnapshot()
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_Last_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        var store = GetStore(connectionString);

        // Act
        await using var session = store.LightweightSession();

        var pagingArgs = new PagingArguments { Last = 5 };
        var result = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await CreateSnapshot()
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstName = result.First?.Name,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastName = result.Last?.Name,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_First_5_Before_Id_96()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        var store = GetStore(connectionString);

        // Act
        await using var session = store.LightweightSession();

        var pagingArgs = new PagingArguments
        {
            Last = 5,
            Before = "QnJhbmQ5NTo5Ng=="
        };
        var result = await session.Query<Brand>().OrderBy(t => t.Name).ThenBy(t => t.Id).ToPageAsync(pagingArgs);

        // Assert
        await CreateSnapshot()
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    [Fact]
    public async Task Paging_WithChildCollectionProjectionExpression_First_5()
    {
        // Arrange
        var connectionString = CreateConnectionString();
        await SeedAsync(connectionString);
        using var capture = new CapturePagingQueryInterceptor();

        var store = GetStore(connectionString);

        // Act
        await using var session = store.LightweightSession();

        var pagingArgs = new PagingArguments
        {
            First = 5
        };

        var result = await session.Query<Brand>()
            .Select(BrandWithProductsDto.Projection)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(pagingArgs);

        // Assert
        await CreateSnapshot()
            .AddQueries(capture.Queries)
            .Add(
                new
                {
                    result.HasNextPage,
                    result.HasPreviousPage,
                    First = result.First?.Id,
                    FirstCursor = result.First is not null ? result.CreateCursor(result.First) : null,
                    Last = result.Last?.Id,
                    LastCursor = result.Last is not null ? result.CreateCursor(result.Last) : null
                })
            .Add(result.Items)
            .MatchMarkdownAsync();
    }

    private static async Task SeedAsync(string connectionString)
    {
        var store = DocumentStore.For(options =>
        {
            options.Connection(connectionString);

            options.UseSystemTextJsonForSerialization();
        });

        await using var session = store.LightweightSession();

        var type = new ProductType
        {
            Name = "T-Shirt",
        };
        session.Store(type);

        for (var i = 0; i < 100; i++)
        {
            var brand = new Brand
            {
                Name = "Brand:" + i,
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
                session.Store((product));
            }
        }

        await session.SaveChangesAsync();
    }

    public class BrandDto
    {
        public BrandDto(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    public class BrandWithProductsDto
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public required IReadOnlyCollection<ProductDto> Products { get; init; }

        public static Expression<Func<Brand, BrandWithProductsDto>> Projection
            => brand => new BrandWithProductsDto
            {
                Id = brand.Id,
                Name = brand.Name,
                Products = brand.Products.AsQueryable().Select(ProductDto.Projection).ToList()
            };
    }

    public class ProductDto
    {
        public required int Id { get; init; }

        public required string Name { get; init; }

        public static Expression<Func<Product, ProductDto>> Projection
            => product => new ProductDto
            {
                Id = product.Id,
                Name = product.Name
            };
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

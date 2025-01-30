using GreenDonut.Data.TestContext;
using Marten;
using Squadron;

namespace GreenDonut.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class IntegrationPagingHelperTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private async Task<string> CreateConnectionString()
    {
        var name = $"db_{Guid.NewGuid():N}";
        var connectionString = Resource.GetConnectionString(name);
        await Resource.CreateDatabaseAsync(name);
        return connectionString;
    }

    [Fact]
    public async Task Paging_Empty_PagingArgs()
    {
        // Arrange
        var connectionString = await CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = DocumentStore.For(connectionString);
        await using var session = context.QuerySession();

        var pagingArgs = new PagingArguments();
        var result = await session.Query<ProductType>()
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToPageAsync(pagingArgs);

        // Assert
        Assert.Equal(4, result.Items.Length);
    }

    [Fact]
    public async Task Paging_First_2()
    {
        // Arrange
        var connectionString = await CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = DocumentStore.For(connectionString);
        await using var session = context.QuerySession();

        var pagingArgs = new PagingArguments(first: 2);
        var result = await session.Query<ProductType>()
            // .OrderBy(t => t.Name)
            .OrderBy(t => t.Id)
            // .ThenBy(t => t.Id)
            .ToPageAsync(pagingArgs);

        // Assert
        Assert.Equal(2, result.Items.Length);
        Assert.NotNull(result.Last);
        Assert.Equal(2, result.Last.Id);
        Assert.Equal("UGFudHM6Mg==", result.CreateCursor(result.Last));
    }

    [Fact]
    public async Task Paging_First_2_After_2()
    {
        // Arrange
        var connectionString = await CreateConnectionString();
        await SeedAsync(connectionString);

        // Act
        await using var context = DocumentStore.For(connectionString);
        await using var session = context.QuerySession();

        var id = 1;
        var q = session.Query<ProductType>().Where(t => t.Id > id);
        var r = await session.Query<ProductType>().Where(t => t.Id > 1).ToListAsync();

        var pagingArgs = new PagingArguments(first: 2, after: "Mg==");
        var result = await session.Query<ProductType>()
            // .OrderBy(t => t.Name)
            .OrderBy(t => t.Id)
            .ToPageAsync(pagingArgs);

        // Assert
        Assert.Equal(2, result.Items.Length);
        Assert.Null(result.Last);
    }

    private static async Task SeedAsync(string connectionString)
    {
        await using var  store = DocumentStore.For(connectionString);
        await using var session = store.LightweightSession();

        session.Store(
            new ProductType
            {
                Name = "T-Shirt",
            });

        session.Store(
            new ProductType
            {
                Name = "Pants",
            });

        session.Store(
            new ProductType
            {
                Name = "Polos",
            });

        session.Store(
            new ProductType
            {
                Name = "Coats",
            });

        await session.SaveChangesAsync();
    }
}

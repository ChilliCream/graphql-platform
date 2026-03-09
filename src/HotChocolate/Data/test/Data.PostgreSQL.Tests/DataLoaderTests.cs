using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Migrations;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class DataLoaderTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Include_On_List_Results()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();
        using var cts = new CancellationTokenSource(2000);
        await using var services = CreateServer();
        await using var scope = services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync(cts.Token);
        await seeder.SeedAsync(context);

        var productByBrand = scope.ServiceProvider.GetRequiredService<IProductListByBrandDataLoader>();

        // act
        var products = await productByBrand
            .Select(t => new Product { BrandId = t.BrandId, Name = t.Name })
            .Include(t => t.Price)
            .LoadRequiredAsync(1, cts.Token);

        // assert
        Assert.Equal(10, products.Count);
        interceptor.MatchSnapshot();
    }

    [Fact]
    public async Task Include_On_Array_Results()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();
        using var cts = new CancellationTokenSource(2000);
        await using var services = CreateServer();
        await using var scope = services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync(cts.Token);
        await seeder.SeedAsync(context);

        var productByBrand = scope.ServiceProvider.GetRequiredService<IProductArrayByBrandDataLoader>();

        // act
        var products = await productByBrand
            .Select(t => new Product { BrandId = t.BrandId, Name = t.Name })
            .Include(t => t.Price)
            .LoadRequiredAsync(1, cts.Token);

        // assert
        Assert.Equal(10, products.Length);
        interceptor.MatchSnapshot();
    }

    [Fact]
    public async Task Include_On_Page_Results()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();
        using var cts = new CancellationTokenSource(2000);
        await using var services = CreateServer();
        await using var scope = services.CreateAsyncScope();
        await using var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync(cts.Token);
        await seeder.SeedAsync(context);

        var productByBrand = scope.ServiceProvider.GetRequiredService<IProductsByBrandDataLoader>();

        // act
        var products = await productByBrand
            .With(new PagingArguments { First = 5 })
            .Select(t => new Product { BrandId = t.BrandId, Name = t.Name })
            .Include(t => t.Price)
            .LoadRequiredAsync(1, cts.Token);

        // assert
        Assert.Equal(5, products.Items.Length);
        interceptor.MatchSnapshot();
    }

    private ServiceProvider CreateServer()
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);

        var services = new ServiceCollection();

        services
            .AddLogging()
            .AddDbContext<CatalogContext>(c => c.UseNpgsql(connectionString));

        services
            .AddSingleton<BrandService>()
            .AddSingleton<ProductService>();

        services
            .AddGraphQLServer()
            .AddCustomTypes()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .AddFiltering()
            .AddSorting()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true);

        services.AddSingleton<IDbSeeder<CatalogContext>, CatalogContextSeed>();

        return services.BuildServiceProvider();
    }
}

file static class Extensions
{
    public static void MatchSnapshot(
        this TestQueryInterceptor queryInterceptor)
    {
        var snapshot = Snapshot.Create(postFix: TestEnvironment.TargetFramework);

        for (var i = 0; i < queryInterceptor.Queries.Count; i++)
        {
            var sql = queryInterceptor.Queries[i];
            snapshot.Add(sql, $"Query {i + 1}", MarkdownLanguages.Sql);
        }

        snapshot.MatchMarkdown();
    }
}

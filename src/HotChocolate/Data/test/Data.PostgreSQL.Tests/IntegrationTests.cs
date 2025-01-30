using HotChocolate.Data.Data;
using HotChocolate.Data.Migrations;
using HotChocolate.Data.Services;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class IntegrationTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task CreateSchema()
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();

        services
            .AddLogging()
            .AddDbContext<CatalogContext>(c => c.UseNpgsql(resource.GetConnectionString(db)));

        services
            .AddSingleton<BrandService>()
            .AddSingleton<ProductService>();

        services
            .AddGraphQLServer()
            .AddCustomTypes()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .AddFiltering()
            .AddSorting();

        services.AddSingleton<IDbSeeder<CatalogContext>, CatalogContextSeed>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);

        var executor = await provider.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Query_Brands()
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();

        services
            .AddLogging()
            .AddDbContext<CatalogContext>(c => c.UseNpgsql(resource.GetConnectionString(db)));

        services
            .AddSingleton<BrandService>()
            .AddSingleton<ProductService>();

        services
            .AddGraphQLServer()
            .AddCustomTypes()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .AddFiltering()
            .AddSorting();

        services.AddSingleton<IDbSeeder<CatalogContext>, CatalogContextSeed>();

        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);

        var executor = await provider.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();

        var result = await executor.ExecuteAsync(
            @"
            {
                brands {
                    nodes {
                        id
                        name
                    }
                }
            }
            ");

        result.MatchSnapshot();
    }
}

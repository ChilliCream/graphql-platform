using System.Text.Json;
using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Migrations;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Data.Types.Brands;
using HotChocolate.Execution;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using static CookieCrumble.TestEnvironment;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed class IgnoredObjectFieldIntegrationTests(PostgreSqlResource resource)
{
    [Fact]
    public async Task Sort_Query_Should_Work_When_ObjectType_Ignores_Field()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                hiddenNameProductTypes(order: { id: DESC }) {
                    nodes {
                        id
                    }
                }
            }
            """);

        // assert
        using var document = JsonDocument.Parse(result.ToJson());
        Assert.False(document.RootElement.TryGetProperty("errors", out _), result.ToJson());

        var ids = document.RootElement
            .GetProperty("data")
            .GetProperty("hiddenNameProductTypes")
            .GetProperty("nodes")
            .EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32())
            .ToArray();

        Assert.Equal(ids.OrderByDescending(t => t).ToArray(), ids);

        var sortType = Assert.IsAssignableFrom<InputObjectType>(executor.Schema.Types["ProductTypeSortInput"]);
        Assert.DoesNotContain(sortType.Fields, field => field.Name == "name");

        Assert.DoesNotContain(
            interceptor.Queries,
            query => query.Contains("\"Name\"", StringComparison.Ordinal));

        MatchSnapshot(result, interceptor, Postfix([NET8_0, NET9_0]));
    }

    [Fact]
    public async Task Filter_Query_Should_Work_When_ObjectType_Ignores_Field()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        // act
        var result = await executor.ExecuteAsync(
            """
            {
                hiddenNameProductTypes(where: { id: { eq: 1 } }) {
                    nodes {
                        id
                    }
                }
            }
            """);

        // assert
        using var document = JsonDocument.Parse(result.ToJson());
        Assert.False(document.RootElement.TryGetProperty("errors", out _), result.ToJson());

        var ids = document.RootElement
            .GetProperty("data")
            .GetProperty("hiddenNameProductTypes")
            .GetProperty("nodes")
            .EnumerateArray()
            .Select(t => t.GetProperty("id").GetInt32())
            .ToArray();

        Assert.Contains(1, ids);

        var filterType = Assert.IsAssignableFrom<InputObjectType>(executor.Schema.Types["ProductTypeFilterInput"]);
        Assert.DoesNotContain(filterType.Fields, field => field.Name == "name");

        Assert.DoesNotContain(
            interceptor.Queries,
            query => query.Contains("\"Name\"", StringComparison.Ordinal));

        MatchSnapshot(result, interceptor, Postfix([NET8_0, NET9_0], [NET10_0]));
    }

    private static ServiceProvider CreateServer(string connectionString)
    {
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

    private static void MatchSnapshot(
        IExecutionResult result,
        TestQueryInterceptor queryInterceptor,
        string? postfix)
    {
        var snapshot = Snapshot.Create(postfix);

        snapshot.Add(result.ToJson(), "Result", MarkdownLanguages.Json);

        for (var i = 0; i < queryInterceptor.Queries.Count; i++)
        {
            var sql = queryInterceptor.Queries[i];
            snapshot.Add(sql, $"Query {i + 1}", MarkdownLanguages.Sql);
        }

        snapshot.MatchMarkdown();
    }
}

[QueryType]
public static partial class HiddenNameProductTypeQueries
{
    [UseFiltering]
    [UseSorting]
    public static async Task<CatalogConnection<ProductType>> GetHiddenNameProductTypesAsync(
        PagingArguments pagingArgs,
        QueryContext<ProductType> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        var page = await context.ProductTypes
            .AsNoTracking()
            .With(query, DefaultOrder)
            .ToPageAsync(pagingArgs, cancellationToken);

        return new CatalogConnection<ProductType>(page);
    }

    private static SortDefinition<ProductType> DefaultOrder(SortDefinition<ProductType> sort)
        => sort.IfEmpty(o => o.AddAscending(t => t.Id));
}

[ObjectType<ProductType>]
public static partial class HiddenNameProductTypeNode
{
    static partial void Configure(IObjectTypeDescriptor<ProductType> descriptor)
    {
        descriptor.Ignore(t => t.Name);
    }
}

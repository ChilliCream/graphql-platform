using GreenDonut;
using HotChocolate.Data.Data;
using HotChocolate.Data.Migrations;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Execution;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
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
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        executor.Schema.MatchSnapshot();
    }

    [Fact]
    public async Task Query_Brands()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands {
                    nodes {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_First_2()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 2) {
                    nodes {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_First_2_And_Products_First_2()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 2) {
                    nodes {
                        id
                        name
                        products(first: 2) {
                            nodes {
                                id
                                name
                            }
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_First_2_And_Products_First_2_Name_Desc()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 2) {
                    nodes {
                        id
                        name
                        products(first: 2, order: { name: DESC }) {
                            nodes {
                                id
                                name
                            }
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_First_2_And_Products_First_2_Name_Desc_Brand_Name()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 2) {
                    nodes {
                        id
                        products(first: 2, order: { name: DESC }) {
                            nodes {
                                id
                                name
                                brand {
                                    name
                                }
                            }
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Node_Product()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                product: node(id: "UHJvZHVjdDox") {
                    ... on Product {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Node_Brand()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brand: node(id: "QnJhbmQ6MQ==") {
                    ... on Brand {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Products_First_2_And_Brand()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                products(first: 2) {
                    nodes {
                        name
                        brand {
                            name
                        }
                    }
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Products_First_2_With_4_EndCursors()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                products(first: 2) {
                    nodes {
                        name
                        brand {
                            name
                        }
                    }
                    endCursors(count: 4)
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Products_First_2_With_4_EndCursors_Skip_4()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                products(first: 2, after: "ezN8MHwxMDF9WmVuaXRoIEN5Y2xpbmcgSmVyc2V5OjQ2") {
                    nodes {
                        name
                        brand {
                            name
                        }
                    }
                    endCursors(count: 4)
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_First_2_Products_First_2_ForwardCursors()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 1) {
                    nodes {
                        name
                        products(first: 2) {
                            nodes {
                                name
                            }
                            pageInfo {
                                forwardCursors { page cursor }
                            }
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Verify_That_PageInfo_Flag_Is_Correctly_Inferred()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 1) {
                    nodes {
                        products(first: 2) {
                            pageInfo {
                                endCursor
                            }
                        }
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Products_Include_TotalCount()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                productsNonRelative(first: 2) {
                    nodes {
                        name
                    }
                    totalCount
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Products_Exclude_TotalCount()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                productsNonRelative(first: 2) {
                    nodes {
                        name
                    }
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Ensure_That_Self_Requirement_Is_Honored()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                singleProperties {
                    id
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Fallback_To_Runtime_Properties_When_No_Field_Is_Bindable()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                singleProperties {
                    __typename
                }
            }

            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task SecondLevelCache_Is_Used()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);

        // act
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        await executor.ExecuteAsync(
            """
            {
                node(id: "QnJhbmQ6MQ==") {
                    ... on Brand {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        var cache = services.GetRequiredService<IMemoryCache>();
        var entry = cache.Get<Promise<Brand>>(new PromiseCacheKey("HotChocolate.Data.Services.BrandByIdDataLoader", 1));
        var brand = await entry.Task;
        Assert.Equal("Daybird", brand.Name);
    }

    [Fact]
    public async Task SecondLevelCache_Resolve_Entry()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);

        var cache = services.GetRequiredService<IMemoryCache>();
        cache.Set(
            new PromiseCacheKey("HotChocolate.Data.Services.BrandByIdDataLoader", 1),
            new Promise<Brand>(new Brand { Id = 1, Name = "Test" }));

        // act
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        var result = await executor.ExecuteAsync(
            """
            {
                node(id: "QnJhbmQ6MQ==") {
                    ... on Brand {
                        id
                        name
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "node": {
                  "id": "QnJhbmQ6MQ==",
                  "name": "Test"
                }
              }
            }
            """);
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
            .AddMemoryCache()
            .AddSingleton<IPromiseCacheInterceptor, DataLoaderSecondLevelCache>();

        services
            .AddGraphQLServer()
            .AddCustomTypes()
            .AddGlobalObjectIdentification()
            .AddPagingArguments()
            .AddFiltering()
            .AddSorting()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ModifyPagingOptions(o => o.RelativeCursorFields = o.RelativeCursorFields.Add("endCursors"));

        services.AddSingleton<IDbSeeder<CatalogContext>, CatalogContextSeed>();

        return services.BuildServiceProvider();
    }

    private async Task<IExecutionResult> ExecuteAsync(string sourceText)
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);
        var executor = await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();
        return await executor.ExecuteAsync(sourceText);
    }

    private static void MatchSnapshot(
        IExecutionResult result,
        TestQueryInterceptor queryInterceptor)
    {
        var snapshot = Snapshot.Create(postFix: TestEnvironment.TargetFramework);

        snapshot.Add(result.ToJson(), "Result", MarkdownLanguages.Json);

        for (var i = 0; i < queryInterceptor.Queries.Count; i++)
        {
            var sql = queryInterceptor.Queries[i];
            snapshot.Add(sql, $"Query {i + 1}", MarkdownLanguages.Sql);
        }

        snapshot.MatchMarkdown();
    }

    private class DataLoaderSecondLevelCache : IPromiseCacheInterceptor
    {
        private readonly IMemoryCache _memoryCache;

        public DataLoaderSecondLevelCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Promise<T> GetOrAddPromise<T>(PromiseCacheKey key, Func<PromiseCacheKey, Promise<T>> createPromise)
        {
            return _memoryCache.GetOrCreate(key, k => createPromise((PromiseCacheKey)k.Key));
        }

        public bool TryAdd<T>(PromiseCacheKey key, Promise<T> promise)
        {
            _memoryCache.Set(key, promise);
            return true;
        }
    }
}

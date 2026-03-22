using GreenDonut;
using HotChocolate.Data.Data;
using HotChocolate.Data.Migrations;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Execution;
using HotChocolate.Resolvers;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Squadron;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace HotChocolate.Data;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public sealed partial class IntegrationTests(PostgreSqlResource resource)
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
    public async Task Query_Brands_With_BatchResolver_ProductCount()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 5) {
                    nodes {
                        id
                        name
                        productCount
                    }
                }
            }
            """);

        // assert
        MatchSnapshot(result, interceptor);
    }

    [Fact]
    public async Task Query_Brands_With_BatchResolver_Supplier()
    {
        // arrange
        using var interceptor = new TestQueryInterceptor();

        // act
        var result = await ExecuteAsync(
            """
            {
                brands(first: 5) {
                    nodes {
                        id
                        name
                        supplier {
                            name
                            website
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
    public async Task Project_Into_1to1_Relation()
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
                        brandName
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
    public async Task Query_InterfaceType_Derived_Implementation_Is_Resolved()
    {
        // act
        var result = await ExecuteAsync(
            """
            {
                statementTransaction {
                    __typename
                    id
                    ... on DepositStatementTransaction {
                        collectionAmount
                    }
                }
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "statementTransaction": {
                  "__typename": "DepositStatementTransaction",
                  "id": 1,
                  "collectionAmount": 42
                }
              }
            }
            """);
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
        var entry = cache.Get<Promise<Brand>>(
            new PromiseCacheKey(
                "HotChocolate.Data.Services.BrandByIdDataLoader:1a50fe619de69da54111d7525dc67ff9",
                1));
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
            new PromiseCacheKey("HotChocolate.Data.Services.BrandByIdDataLoader:1a50fe619de69da54111d7525dc67ff9", 1),
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

    [Fact]
    public async Task Generated_BrandKey_NodeIdValueSerializer_RoundTrip()
    {
        // arrange
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);

        // We need to initialize the executor so that all services are registered.
        await services.GetRequiredService<IRequestExecutorProvider>().GetExecutorAsync();

        var serializer = services.GetRequiredService<INodeIdSerializer>();
        var original = new BrandKey(42, 7);

        // act
        var formatted = serializer.Format("BrandKey", original);
        var parsed = serializer.Parse(formatted, typeof(BrandKey));

        // assert
        Assert.Equal("BrandKey", parsed.TypeName);
        Assert.IsType<BrandKey>(parsed.InternalId);
        Assert.Equal(original, (BrandKey)parsed.InternalId);
    }

    [Fact]
    public async Task Query_ScopeState_With_Derived_ScopedState_Attribute()
    {
        // act
        var result = await ExecuteAsync(
            """
            {
                scopeState
            }
            """);

        // assert
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "scopeState": "Hello World"
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
            .AddNodeIdValueSerializerFrom<BrandKey>()
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
        var queries = NormalizeBrandLookupBatching(queryInterceptor.Queries);

        snapshot.Add(result.ToJson(), "Result", MarkdownLanguages.Json);

        for (var i = 0; i < queries.Count; i++)
        {
            var sql = queries[i];
            snapshot.Add(sql, $"Query {i + 1}", MarkdownLanguages.Sql);
        }

        snapshot.MatchMarkdown();
    }

    private static IReadOnlyList<string> NormalizeBrandLookupBatching(IReadOnlyList<string> queries)
    {
        var indices = new List<int>();
        var ids = new HashSet<int>();
        string? body = null;

        for (var i = 0; i < queries.Count; i++)
        {
            var query = queries[i];
            if (!IsBrandLookupQuery(query, out var currentIds, out var currentBody))
            {
                continue;
            }

            if (body is not null && !string.Equals(body, currentBody, StringComparison.Ordinal))
            {
                return queries;
            }

            body = currentBody;
            indices.Add(i);

            foreach (var id in currentIds)
            {
                ids.Add(id);
            }
        }

        if (indices.Count <= 1 || body is null || ids.Count == 0)
        {
            return queries;
        }

        var orderedIds = ids.OrderBy(t => t).Select(t => $"'{t}'");
        var merged = CurlyBraceBlockRegex().Replace(queries[indices[0]], "{ " + string.Join(", ", orderedIds) + " }");

        var normalized = new List<string>(queries.Count - indices.Count + 1);
        var first = indices[0];
        var indexSet = indices.ToHashSet();

        for (var i = 0; i < queries.Count; i++)
        {
            if (i == first)
            {
                normalized.Add(merged);
            }

            if (!indexSet.Contains(i))
            {
                normalized.Add(queries[i]);
            }
        }

        return normalized;
    }

    private static bool IsBrandLookupQuery(
        string query,
        out IReadOnlyList<int> ids,
        out string body)
    {
        ids = [];
        body = query;

        var lines = query
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length < 4)
        {
            return false;
        }

        if (!lines[0].StartsWith("-- @", StringComparison.Ordinal)
            || !query.Contains("SELECT b.\"Name\", b.\"Id\"", StringComparison.Ordinal)
            || !query.Contains("FROM \"Brands\" AS b", StringComparison.Ordinal)
            || !query.Contains("WHERE b.\"Id\" = ANY (", StringComparison.Ordinal))
        {
            return false;
        }

        var matches = QuotedNumericIdRegex().Matches(lines[0]);
        if (matches.Count == 0)
        {
            return false;
        }

        var parsed = new List<int>(matches.Count);
        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups["id"].Value, out var id))
            {
                parsed.Add(id);
            }
        }

        if (parsed.Count == 0)
        {
            return false;
        }

        ids = parsed;
        body = string.Join('\n', lines.Skip(1));
        return true;
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

    [GeneratedRegex(@"\{[^}]*\}", RegexOptions.CultureInvariant)]
    private static partial Regex CurlyBraceBlockRegex();

    [GeneratedRegex(@"'(?<id>\d+)'", RegexOptions.CultureInvariant)]
    private static partial Regex QuotedNumericIdRegex();
}

[QueryType]
public static partial class ScopeStateQuery
{
    [UseScopeStateMiddleware]
    public static string ScopeState([ScopeState] string scope)
        => scope;
}

[AttributeUsage(AttributeTargets.Parameter)]
public sealed class ScopeStateAttribute()
    : ScopedStateAttribute(LookupKey)
{
    public const string LookupKey = "ScopeState";
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
public sealed class UseScopeStateMiddlewareAttribute : ObjectFieldDescriptorAttribute
{
    public UseScopeStateMiddlewareAttribute([CallerLineNumber] int order = 0)
        => Order = order;

    protected override void OnConfigure(
        IDescriptorContext context,
        IObjectFieldDescriptor descriptor,
        MemberInfo? member) =>
        descriptor.Use<ScopeStateMiddleware>();

    private sealed class ScopeStateMiddleware(FieldDelegate next)
    {
        public async Task InvokeAsync(IMiddlewareContext context)
        {
            context.SetScopedState(ScopeStateAttribute.LookupKey, "Hello World");

            await next(context);

            context.RemoveScopedState(ScopeStateAttribute.LookupKey);
        }
    }
}

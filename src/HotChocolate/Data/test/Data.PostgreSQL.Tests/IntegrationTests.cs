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
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
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
            """,
            interceptor);

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
            """,
            interceptor);

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
            """,
            interceptor);

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
            """,
            interceptor);

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
            """,
            interceptor);

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
            """,
            interceptor);

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
            """,
            interceptor);

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

            """,
            interceptor);

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

            """,
            interceptor);

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
                products(first: 2) {
                    nodes {
                        name
                    }
                    totalCount
                }
            }

            """,
            interceptor);

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
                products(first: 2) {
                    nodes {
                        name
                    }
                }
            }

            """,
            interceptor);

        // assert
        MatchSnapshot(result, interceptor);
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
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ModifyPagingOptions(o => o.AllowRelativeCursors = true);

        services.AddSingleton<IDbSeeder<CatalogContext>, CatalogContextSeed>();

        return services.BuildServiceProvider();
    }

    private async Task<IExecutionResult> ExecuteAsync(
        string sourceText,
        TestQueryInterceptor queryInterceptor)
    {
        var db = "db_" + Guid.NewGuid().ToString("N");
        var connectionString = resource.GetConnectionString(db);
        await using var services = CreateServer(connectionString);
        await using var scope = services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();
        var seeder = scope.ServiceProvider.GetRequiredService<IDbSeeder<CatalogContext>>();
        await context.Database.EnsureCreatedAsync();
        await seeder.SeedAsync(context);
        var executor = await services.GetRequiredService<IRequestExecutorResolver>().GetRequestExecutorAsync();
        return await executor.ExecuteAsync(sourceText);
    }

    private static void MatchSnapshot(
        IExecutionResult result,
        TestQueryInterceptor queryInterceptor)
    {
#if NET9_0_OR_GREATER
        var snapshot = Snapshot.Create();
#else
        var snapshot = Snapshot.Create(postFix: "_net_8_0");
#endif

        snapshot.Add(result.ToJson(), "Result", MarkdownLanguages.Json);

        for (var i = 0; i < queryInterceptor.Queries.Count; i++)
        {
            var sql = queryInterceptor.Queries[i];
            snapshot.Add(sql, $"Query {i + 1}", MarkdownLanguages.Sql);
        }

        snapshot.MatchMarkdown();
    }
}

using System.Data.Common;
using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Execution;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using IOPath = System.IO.Path;

namespace HotChocolate.Data;

public class IsProjectedSelectorTests : IDisposable
{
    private readonly string _fileName = IOPath.Combine(
        IOPath.GetTempPath(),
        $"is-projected-selector-{Guid.NewGuid():N}.db");

    [Fact]
    public async Task QueryContext_Should_ProjectField_When_FieldIsAlwaysProjected()
    {
        // arrange
        var queries = new List<string>();
        await SeedAsync();
        var executor = await CreateExecutorAsync(queries);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              products {
                name
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Single(queries).MatchInlineSnapshot(
            """
            SELECT "p"."AlwaysFetched", "p"."Name"
            FROM "Products" AS "p"
            """);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "name": "Widget"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task QueryContext_Should_NotProjectField_When_FieldIsNeverProjected()
    {
        // arrange
        var queries = new List<string>();
        await SeedAsync();
        var executor = await CreateExecutorAsync(queries);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              products {
                name
                neverFetched
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Single(queries).MatchInlineSnapshot(
            """
            SELECT "p"."AlwaysFetched", "p"."Name"
            FROM "Products" AS "p"
            """);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "name": "Widget",
                    "neverFetched": null
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task QueryContext_Should_ProjectAlwaysProjectedField_When_OnlyTypenameIsSelected()
    {
        // arrange
        var queries = new List<string>();
        await SeedAsync();
        var executor = await CreateExecutorAsync(queries);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              products {
                __typename
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Single(queries).MatchInlineSnapshot(
            """
            SELECT "p"."AlwaysFetched"
            FROM "Products" AS "p"
            """);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "products": [
                  {
                    "__typename": "Product"
                  }
                ]
              }
            }
            """);
    }

    [Fact]
    public async Task DataLoaderSelect_Should_ProjectField_When_FieldIsAlwaysProjected()
    {
        // arrange
        var queries = new List<string>();
        await SeedAsync();
        var executor = await CreateExecutorAsync(queries);

        // act
        var result = await executor.ExecuteAsync(
            """
            {
              productById(id: 1) {
                name
              }
            }
            """,
            Xunit.TestContext.Current.CancellationToken);

        // assert
        Assert.Single(queries).MatchInlineSnapshot(
            """
            SELECT "p"."AlwaysFetched", "p"."Name", "p"."Id"
            FROM "Products" AS "p"
            WHERE "p"."Id" = @keys1
            """);
        result.MatchInlineSnapshot(
            """
            {
              "data": {
                "productById": {
                  "name": "Widget"
                }
              }
            }
            """);
    }

    private async Task<IRequestExecutor> CreateExecutorAsync(List<string> queries)
        => await new ServiceCollection()
            .AddDbContext<ProductDbContext>(
                b => b.UseSqlite("Data Source=" + _fileName)
                    .AddInterceptors(new SqlCaptureInterceptor(queries)))
            .AddGraphQL()
            .AddQueryContext()
            .AddQueryType<ProductQuery>()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .BuildRequestExecutorAsync(
                cancellationToken: Xunit.TestContext.Current.CancellationToken);

    private async Task SeedAsync()
    {
        var options = new DbContextOptionsBuilder<ProductDbContext>()
            .UseSqlite("Data Source=" + _fileName)
            .Options;

        await using var context = new ProductDbContext(options);
        await context.Database.EnsureCreatedAsync(Xunit.TestContext.Current.CancellationToken);

        context.Products.Add(
            new Product
            {
                Id = 1,
                Name = "Widget",
                AlwaysFetched = "always",
                NeverFetched = "never"
            });

        await context.SaveChangesAsync(Xunit.TestContext.Current.CancellationToken);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(_fileName))
        {
            File.Delete(_fileName);
        }
    }

    public class Product
    {
        public int Id { get; set; }

        public string? Name { get; set; }

        [IsProjected]
        public string? AlwaysFetched { get; set; }

        [IsProjected(false)]
        public string? NeverFetched { get; set; }
    }

    public class ProductDbContext(DbContextOptions<ProductDbContext> options) : DbContext(options)
    {
        public DbSet<Product> Products => Set<Product>();
    }

    public class ProductQuery
    {
        public IQueryable<Product> GetProducts(
            ProductDbContext context,
            QueryContext<Product> query)
            => context.Products.With(query);

        public async Task<Product?> GetProductById(
            int id,
            ISelection selection,
            ProductByIdDataLoader productById,
            CancellationToken cancellationToken)
            => await productById.Select(selection).LoadAsync(id, cancellationToken);
    }

    public class ProductByIdDataLoader(
        IServiceProvider services,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Product>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product> context,
            CancellationToken cancellationToken)
        {
            await using var scope = services.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();

            return await dbContext.Products
                .Where(t => keys.Contains(t.Id))
                .Select(t => t.Id, context.GetSelector())
                .ToDictionaryAsync(t => t.Id, cancellationToken);
        }
    }

    private sealed class SqlCaptureInterceptor(List<string> queries) : DbCommandInterceptor
    {
        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            queries.Add(command.CommandText);

            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            queries.Add(command.CommandText);

            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }
    }
}

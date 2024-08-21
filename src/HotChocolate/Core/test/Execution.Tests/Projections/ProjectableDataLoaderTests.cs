#if NET8_0_OR_GREATER
using CookieCrumble;
using GreenDonut;
using GreenDonut.Projections;
using HotChocolate.Execution.Processing;
using HotChocolate.Execution.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Squadron;

namespace HotChocolate.Execution.Projections;

[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class ProjectableDataLoaderTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task Brand_With_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddDataLoader<BrandByIdDataLoader>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                """
                {
                    brandById(id: 1) {
                        name
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    [Fact]
    public async Task Product_With_Name_And_Brand_With_Name()
    {
        // Arrange
        var queries = new List<string>();
        var connectionString = CreateConnectionString();
        await CatalogContext.SeedAsync(connectionString);

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddScoped(_ => new CatalogContext(connectionString))
            .AddDataLoader<BrandByIdDataLoader>()
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ExecuteRequestAsync(
                """
                {
                    productById(id: 1) {
                        name
                        brand {
                            name
                        }
                    }
                }
                """);

        Snapshot.Create()
            .AddSql(queries)
            .AddResult(result)
            .MatchMarkdownSnapshot();
    }

    public class Query
    {
        public async Task<Brand> GetBrandByIdAsync(
            int id,
            ISelection selection,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.Select(selection).LoadAsync(id, cancellationToken);

        public async Task<Product> GetProductByIdAsync(
            int id,
            ISelection selection,
            ProductByIdDataLoader productById,
            CancellationToken cancellationToken)
            => await productById.Select(selection).LoadAsync(id, cancellationToken);
    }

    public class BrandByIdDataLoader(
        CatalogContext catalogContext,
        List<string> queries,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Brand>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Brand>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Brand> context,
            CancellationToken cancellationToken)
        {
            var query = catalogContext.Brands
                .Where(t => keys.Contains(t.Id))
                .Select(context.GetSelector())
                .SelectKey(b => b.Id);

            queries.Add(query.ToQueryString());

            var x = await query.ToDictionaryAsync(t => t.Id, cancellationToken);

            return x;
        }
    }

    public class ProductByIdDataLoader(
        CatalogContext catalogContext,
        List<string> queries,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, Product>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, Product>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product> context,
            CancellationToken cancellationToken)
        {
            var query = catalogContext.Products
                .Where(t => keys.Contains(t.Id))
                .Select(context.GetSelector())
                .SelectKey(b => b.Id);

            queries.Add(query.ToQueryString());

            var x = await query.ToDictionaryAsync(t => t.Id, cancellationToken);

            return x;
        }
    }
}

file static class Extensions
{
    public static Snapshot AddSql(this Snapshot snapshot, List<string> queries)
    {
        snapshot.Add(string.Join("\n", queries), "SQL");
        return snapshot;
    }

    public static Snapshot AddResult(this Snapshot snapshot, IExecutionResult result)
    {
        snapshot.Add(result, "Result");
        return snapshot;
    }
}
#endif

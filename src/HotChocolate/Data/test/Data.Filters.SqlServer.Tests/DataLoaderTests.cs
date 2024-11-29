using System.Linq.Expressions;
using GreenDonut;
using GreenDonut.Predicates;
using HotChocolate.Execution;
using HotChocolate.Data.Filters;
using HotChocolate.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Data.Predicates;

public sealed class DataLoaderTests
{
    [Fact]
    public async Task Filter_With_Expression()
    {
        // Arrange
        var queries = new List<string>();
        var context = new CatalogContext();
        await context.SeedAsync();

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    filterExpression(id: 1) {
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
    public async Task Filter_With_Multi_Expression()
    {
        // Arrange
        var queries = new List<string>();
        var context = new CatalogContext();
        await context.SeedAsync();

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    multiFilterExpression(id: 1) {
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
    public async Task Filter_With_Filtering()
    {
        // Arrange
        var queries = new List<string>();
        var context = new CatalogContext();
        await context.SeedAsync();

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => context)
            .AddGraphQL()
            .AddFiltering()
            .AddQueryType<Query>()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    filterContext(brandId: 1, where: { name: { startsWith: "Product" } }) {
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
    public async Task Filter_With_Expression_Null()
    {
        // Arrange
        var queries = new List<string>();
        var context = new CatalogContext();
        await context.SeedAsync();

        // Act
        var result = await new ServiceCollection()
            .AddScoped(_ => queries)
            .AddTransient(_ => context)
            .AddGraphQL()
            .AddQueryType<Query>()
            .AddFiltering()
            .AddPagingArguments()
            .ModifyRequestOptions(o => o.IncludeExceptionDetails = true)
            .ExecuteRequestAsync(
                """
                {
                    brandByIdFilterNull(id: 1) {
                        name
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
        public async Task<Brand?> FilterExpression(
            int id,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById
                .Where(x => x.Name.StartsWith("Brand"))
                .LoadAsync(id, cancellationToken);

        public async Task<Brand?> MultiFilterExpression(
            int id,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById
                .Where(x => x.Name.StartsWith("Brand"))
                .Where(x => x.Name.EndsWith("0"))
                .LoadAsync(id, cancellationToken);

        [UseFiltering]
        public async Task<Product[]?> FilterContext(
            int brandId,
            IFilterContext context,
            ProductsByBrandIdDataLoader productsByBrandId,
            CancellationToken cancellationToken)
            => await productsByBrandId
                .Where(context)
                .LoadAsync(brandId, cancellationToken);

        public async Task<Brand?> GetBrandByIdFilterNullAsync(
            int id,
            BrandByIdDataLoader brandById,
            CancellationToken cancellationToken)
            => await brandById.Where(default(Expression<Func<Brand, bool>>))
                .LoadAsync(id, cancellationToken);
    }

    public class BrandByIdDataLoader : StatefulBatchDataLoader<int, Brand>
    {
        private readonly IServiceProvider _services;
        private readonly List<string> _queries;

        public BrandByIdDataLoader(IServiceProvider services,
            List<string> queries,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options) : base(batchScheduler, options)
        {
            _services = services;
            _queries = queries;

            PromiseCacheObserver
                .Create<int, Brand, Product>(
                    p =>
                    {
                        if (p.Brand is not null)
                        {
                            return new KeyValuePair<int, Brand>(p.Brand.Id, p.Brand);
                        }

                        return null;
                    },
                    this)
                .Accept(this);
        }

        protected override async Task<IReadOnlyDictionary<int, Brand>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Brand> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = _services.GetRequiredService<CatalogContext>();

            var query = catalogContext.Brands
                .Where(t => keys.Contains(t.Id))
                .Where(context.GetPredicate());

            lock (_queries)
            {
                _queries.Add(query.ToQueryString());
            }

            var x = await query.ToDictionaryAsync(t => t.Id, cancellationToken);

            return x;
        }
    }

    public class ProductsByBrandIdDataLoader : StatefulBatchDataLoader<int, Product[]>
    {
        private readonly IServiceProvider _services;
        private readonly List<string> _queries;

        public ProductsByBrandIdDataLoader(IServiceProvider services,
            List<string> queries,
            IBatchScheduler batchScheduler,
            DataLoaderOptions options) : base(batchScheduler, options)
        {
            _services = services;
            _queries = queries;
        }

        protected override async Task<IReadOnlyDictionary<int, Product[]>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<Product[]> context,
            CancellationToken cancellationToken)
        {
            var catalogContext = _services.GetRequiredService<CatalogContext>();

            var query = catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .Where(context.GetPredicate());

            lock (_queries)
            {
                _queries.Add(query.ToQueryString());
            }

            var x = await query.ToListAsync(cancellationToken);
            return x.GroupBy(t => t.Id).ToDictionary(t => t.Key, t => t.ToArray());
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

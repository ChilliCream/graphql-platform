#if NET9_0_OR_GREATER
using System.Data.Common;
using CookieCrumble.Resources;
using GreenDonut.Data.TestContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace GreenDonut.Data;

// Verifies a DataLoader<int, StreamPage<T>> built on ToBatchStreamPageAsync streams pages from
// one shared context lifetime.
[Collection(PostgresCacheCollectionFixture.DefinitionName)]
public class StreamPagingDataLoaderTests(PostgreSqlResource resource)
{
    public PostgreSqlResource Resource { get; } = resource;

    private string CreateConnectionString()
        => Resource.GetConnectionString($"db_{Guid.NewGuid():N}");

    [Fact]
    public async Task ProductsByBrandStreamDataLoader_Should_StreamFromOneSharedContext_When_TwoBrandsLoadInOneBatch()
    {
        // arrange
        var connectionString = CreateConnectionString();
        var brandIds = await SeedAsync(connectionString, ("Brand A", 3), ("Brand B", 3));
        var cancellationToken = Xunit.TestContext.Current.CancellationToken;

        var interceptor = new RecordingReaderInterceptor();
        var connectionDisposal = new ConnectionDisposalInterceptor();
        var dataLoader = new ProductsByBrandStreamDataLoader(
            connectionString,
            [interceptor, connectionDisposal],
            AutoBatchScheduler.Default,
            new DataLoaderOptions());
        var pagingArgs = new PagingArguments(3);

        // act
        var pages = await dataLoader.With(pagingArgs).LoadAsync(brandIds, cancellationToken);
        var firstBrandItems = await NamesAsync(pages[0]!);
        var rowsReadAfterFirstBrand = interceptor.Events.Count;
        var secondBrandItems = await NamesAsync(pages[1]!);

        // assert
        Assert.Equal(["Brand A-Item01", "Brand A-Item02", "Brand A-Item03"], firstBrandItems);
        Assert.Equal(["Brand B-Item01", "Brand B-Item02", "Brand B-Item03"], secondBrandItems);
        Assert.True(rowsReadAfterFirstBrand < interceptor.Events.Count);
        Assert.Single(interceptor.CommandTexts);
        Assert.Equal(1, connectionDisposal.DisposedCount);
    }

    private static async ValueTask<string[]> NamesAsync(StreamPage<Product> page)
    {
        var names = new List<string>();

        await foreach (var product in page)
        {
            names.Add(product.Name);
        }

        return [.. names];
    }

    private static async Task<int[]> SeedAsync(string connectionString, params (string BrandName, int Count)[] brands)
    {
        await using var context = new CatalogContext(connectionString);
        await context.Database.EnsureCreatedAsync();

        var type = new ProductType { Name = "T-Shirt" };
        context.ProductTypes.Add(type);

        foreach (var (brandName, count) in brands)
        {
            var brand = new Brand { Name = brandName, BrandDetails = new() { Country = new() { Name = "Country" } } };
            context.Brands.Add(brand);

            for (var i = 1; i <= count; i++)
            {
                context.Products.Add(new Product { Name = $"{brandName}-Item{i:D2}", Type = type, Brand = brand });
            }
        }

        await context.SaveChangesAsync();

        return await context.Brands.OrderBy(b => b.Name).Select(b => b.Id).ToArrayAsync();
    }

    private sealed class ConnectionDisposalInterceptor : DbConnectionInterceptor
    {
        public int DisposedCount { get; private set; }

        public override void ConnectionDisposed(DbConnection connection, ConnectionEndEventData eventData)
            => DisposedCount++;

        public override Task ConnectionDisposedAsync(DbConnection connection, ConnectionEndEventData eventData)
        {
            DisposedCount++;
            return Task.CompletedTask;
        }
    }

    // Loads a StreamPage<Product> per brand from one flat batch query, using the DbContext as
    // the shared lifetime.
    public class ProductsByBrandStreamDataLoader(
        string connectionString,
        IEnumerable<IInterceptor>? interceptors,
        IBatchScheduler batchScheduler,
        DataLoaderOptions options)
        : StatefulBatchDataLoader<int, StreamPage<Product>>(batchScheduler, options)
    {
        protected override async Task<IReadOnlyDictionary<int, StreamPage<Product>>> LoadBatchAsync(
            IReadOnlyList<int> keys,
            DataLoaderFetchContext<StreamPage<Product>> context,
            CancellationToken cancellationToken)
        {
            var pagingArgs = context.GetPagingArguments();
            var catalogContext = new CatalogContext(connectionString, interceptors);

            return await catalogContext.Products
                .Where(t => keys.Contains(t.BrandId))
                .OrderBy(t => t.Name)
                .ThenBy(t => t.Id)
                .ToBatchStreamPageAsync(
                    t => t.BrandId,
                    pagingArgs,
                    lifetime: catalogContext,
                    cancellationToken: cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
#endif

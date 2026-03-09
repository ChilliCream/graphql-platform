using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HotChocolate.Data.Services;

[DataLoaderGroup("ProductBatchingContext")]
internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> ids,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        ids = ids.EnsureOrdered();
        var queryable = context.Products
            .Where(t => ids.Contains(t.Id))
            .With(query);
        PagingQueryInterceptor.Publish(queryable);
        return await queryable.ToDictionaryAsync(t => t.Id, cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        brandIds = brandIds.EnsureOrdered();
        return await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .With(query, s => s.AddAscending(t => t.Id))
            .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<int, List<Product>>> GetProductListByBrandAsync(
        IReadOnlyList<int> brandIds,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        brandIds = brandIds.EnsureOrdered();

        var queryable = context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .With(query, s => s.AddAscending(t => t.Id))
            .GroupBy(t => t.BrandId);
        PagingQueryInterceptor.Publish(queryable);

        return await queryable.ToDictionaryAsync(t => t.Key, t => t.ToList(), cancellationToken);
    }

    [DataLoader]
    public static async Task<Dictionary<int, Product[]>> GetProductArrayByBrandAsync(
        IReadOnlyList<int> brandIds,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        brandIds = brandIds.EnsureOrdered();

        var queryable = context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .With(query, s => s.AddAscending(t => t.Id))
            .GroupBy(t => t.BrandId);
        PagingQueryInterceptor.Publish(queryable);

        return await queryable.ToDictionaryAsync(t => t.Key, t => t.ToArray(), cancellationToken);
    }
}

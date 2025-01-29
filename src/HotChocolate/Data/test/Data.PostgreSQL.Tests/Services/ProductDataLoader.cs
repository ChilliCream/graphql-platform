using eShop.Catalog.Data;
using eShop.Catalog.Models;
using GreenDonut;
using GreenDonut.Data;

namespace eShop.Catalog.Services;

internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArgs,
        QueryContext<Product> queryContext,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .With(queryContext)
            .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
    }
}

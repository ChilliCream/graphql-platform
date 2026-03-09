using static eShop.Catalog.Services.Ordering;

namespace eShop.Catalog.Services;

internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Product>> GetProductByIdAsync(
        IReadOnlyList<int> productIds,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => productIds.Contains(t.Id))
            .MapToProduct()
            .With(query)
            .ToDictionaryAsync(t => t.Id, cancellationToken);

    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandIdAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .MapToProduct()
            .With(query, DefaultOrder)
            .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
}

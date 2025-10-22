namespace eShop.Catalog.Services;

internal static class BrandDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Brand>> GetBrandByIdAsync(
        IReadOnlyList<int> brandIds,
        CatalogContext context,
        CancellationToken cancellationToken)
        => await context.Brands
            .Where(t => brandIds.Contains(t.Id))
            .MapToBrand()
            .ToDictionaryAsync(t => t.Id, cancellationToken);
}

namespace eShop.Catalog.Types;

[QueryType]
public static partial class BrandQueries
{
    public static async Task<Brand?> GetBrandByIdAsync(
        int id,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(id, cancellationToken);

    [UsePaging(IncludeTotalCount = true)]
    public static async Task<Connection<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService
            .GetBrandsAsync(pagingArgs, cancellationToken)
            .ToConnectionAsync();
}

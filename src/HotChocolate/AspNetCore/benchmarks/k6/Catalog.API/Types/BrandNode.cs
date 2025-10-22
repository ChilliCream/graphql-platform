namespace eShop.Catalog.Types;

[ObjectType<Brand>]
public static partial class BrandNode
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static async Task<Connection<Product>> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
        => await productService
            .GetProductsByBrandAsync(brand.Id, pagingArgs, query, cancellationToken)
            .ToConnectionAsync();
}

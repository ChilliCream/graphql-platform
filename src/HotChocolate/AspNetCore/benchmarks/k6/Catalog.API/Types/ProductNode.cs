namespace eShop.Catalog.Types;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(product.BrandId, cancellationToken);
}

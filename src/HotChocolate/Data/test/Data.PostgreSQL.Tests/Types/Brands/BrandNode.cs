using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Data.Types.Products;
using HotChocolate.Types;

namespace HotChocolate.Data.Types.Brands;

[ObjectType<Brand>]
public static partial class BrandNode
{
    [UseConnection]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductsConnection> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsByBrandAsync(brand.Id, pagingArgs, query, cancellationToken);
        return new ProductsConnection(page);
    }
}

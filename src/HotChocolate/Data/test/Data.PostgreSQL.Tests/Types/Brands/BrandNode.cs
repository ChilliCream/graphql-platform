using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Brands;

[ObjectType<Brand>]
public static partial class BrandNode
{
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public static Task<Connection<Product>> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
        => productService.GetProductsByBrandAsync(brand.Id, pagingArgs, query, cancellationToken).ToConnectionAsync();
}

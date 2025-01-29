using eShop.Catalog.Models;
using eShop.Catalog.Services;
using GreenDonut.Data;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace eShop.Catalog.Types;

[ObjectType<Brand>]
public static partial class BrandExtensions
{
    [UsePaging]
    [UseFiltering]
    public static Task<Connection<Product>> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> queryContext,
        ProductService productService,
        CancellationToken cancellationToken)
        => productService
            .GetProductsByBrandAsync(
                brand.Id,
                pagingArgs,
                queryContext,
                cancellationToken)
            .ToConnectionAsync();
}

using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Types;

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

using eShop.Catalog.Models;
using eShop.Catalog.Services;
using GreenDonut.Data;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace eShop.Catalog.Types;

[QueryType]
public static partial class BrandQueries
{
    [UsePaging]
    [UseFiltering]
    public static async Task<Connection<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        QueryContext<Brand> queryContext,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService
            .GetBrandsAsync(
                pagingArgs,
                queryContext,
                cancellationToken)
            .ToConnectionAsync();
}

using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Types;

[QueryType]
public static class BrandQueries
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

    [NodeResolver]
    public static async Task<Brand?> GetBrandAsync(
        int id,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(id, cancellationToken);
}

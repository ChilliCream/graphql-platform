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
        QueryContext<Brand> query,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandsAsync(pagingArgs, query, cancellationToken).ToConnectionAsync();

    [NodeResolver]
    public static async Task<Brand?> GetBrandAsync(
        int id,
        QueryContext<Brand> query,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(id, query, cancellationToken);
}

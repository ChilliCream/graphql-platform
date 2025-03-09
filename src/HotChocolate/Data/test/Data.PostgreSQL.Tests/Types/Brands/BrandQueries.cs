using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Types.Brands;

[QueryType]
public static partial class BrandQueries
{
    [UseFiltering]
    public static async Task<CatalogConnection<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        QueryContext<Brand> query,
        BrandService brandService,
        CancellationToken cancellationToken)
    {
        var page = await brandService.GetBrandsAsync(pagingArgs, query, cancellationToken);
        return new CatalogConnection<Brand>(page);
    }

    [NodeResolver]
    public static async Task<Brand?> GetBrandByIdAsync(
        int id,
        QueryContext<Brand> query,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(id, query, cancellationToken);
}

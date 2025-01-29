using eShop.Catalog.Data;
using eShop.Catalog.Models;
using GreenDonut.Data;

namespace eShop.Catalog.Services;

public class BrandService(CatalogContext context)
{
    public async Task<Page<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        QueryContext<Brand>? queryContext = null,
        CancellationToken cancellationToken = default)
        => await context.Brands
            .With(queryContext, s => s.AddAscending(t => t.Id).AddDescending(t => t.Name))
            .ToPageAsync(pagingArgs, cancellationToken);
}

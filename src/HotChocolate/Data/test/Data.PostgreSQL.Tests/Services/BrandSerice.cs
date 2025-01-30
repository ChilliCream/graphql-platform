using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;

namespace HotChocolate.Data.Services;

public class BrandService(CatalogContext context)
{
    public async Task<Page<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        QueryContext<Brand>? queryContext = null,
        CancellationToken cancellationToken = default)
        => await context.Brands
            .With(queryContext, s => s.AddAscending(t => t.Id).AddDescending(t => t.Name))
            .ToPageAsync(pagingArgs, cancellationToken);

    public async Task<Brand?> GetBrandByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
        => await context.Brands.FindAsync([id], cancellationToken);
}

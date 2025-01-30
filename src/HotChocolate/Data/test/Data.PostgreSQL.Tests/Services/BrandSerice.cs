using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;

namespace HotChocolate.Data.Services;

public class BrandService(CatalogContext context, IBrandByIdDataLoader brandById)
{
    public async Task<Page<Brand>> GetBrandsAsync(
        PagingArguments pagingArgs,
        QueryContext<Brand>? query = null,
        CancellationToken cancellationToken = default)
        => await context.Brands
            .With(query, s => s.AddAscending(t => t.Id).AddDescending(t => t.Name))
            .ToPageAsync(pagingArgs, cancellationToken);

    public async Task<Brand?> GetBrandByIdAsync(
        int id,
        QueryContext<Brand>? query = null,
        CancellationToken cancellationToken = default)
        => await brandById.LoadAsync(id, cancellationToken);
}

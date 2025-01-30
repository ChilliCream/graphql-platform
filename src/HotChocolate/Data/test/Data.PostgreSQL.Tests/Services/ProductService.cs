using GreenDonut.Data;
using HotChocolate.Data.Models;

namespace HotChocolate.Data.Services;

public class ProductService(IProductsByBrandDataLoader productsByBrand)
{
    public async Task<Page<Product>> GetProductsByBrandAsync(
        int brandId,
        PagingArguments pagingArgs,
        QueryContext<Product>? queryContext = null,
        CancellationToken cancellationToken = default)
        => await productsByBrand
            .With(pagingArgs, queryContext)
            .LoadAsync(brandId, cancellationToken) ?? Page<Product>.Empty;
}

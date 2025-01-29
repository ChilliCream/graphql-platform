using eShop.Catalog.Models;
using GreenDonut.Data;

namespace eShop.Catalog.Services;

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

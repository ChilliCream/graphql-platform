using static eShop.Catalog.Services.Ordering;

namespace eShop.Catalog.Services;

public class ProductService(
    CatalogContext context,
    IProductByIdDataLoader productById,
    IProductsByBrandIdDataLoader productsByBrandId)
{
    public async Task<Product?> GetProductByIdAsync(
        int id,
        QueryContext<Product>? query = default,
        CancellationToken cancellationToken = default)
        => await productById
            .With(query)
            .LoadAsync(id, cancellationToken);

    public async Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product>? query = default,
        CancellationToken cancellationToken = default)
        => await context.Products
            .MapToProduct()
            .With(query, DefaultOrder)
            .ToPageAsync(pagingArgs, cancellationToken);

    public async Task<Page<Product>> GetProductsByBrandAsync(
        int brandId,
        PagingArguments pagingArgs,
        QueryContext<Product>? query = default,
        CancellationToken cancellationToken = default)
        => await productsByBrandId
            .With(pagingArgs, query)
            .LoadAsync(brandId, cancellationToken)
            ?? Page<Product>.Empty;
}

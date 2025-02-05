using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;

namespace HotChocolate.Data.Services;

public class ProductService(CatalogContext context, IProductBatchingContext batchingContext)
{
    public async Task<Product?> GetProductByIdAsync(
        int id,
        QueryContext<Product>? query = null,
        CancellationToken cancellationToken = default)
        => await batchingContext.ProductById
            .With(query)
            .LoadAsync(id, cancellationToken);

    public async Task<Page<Product>> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product>? query = null,
        CancellationToken cancellationToken = default)
        => await context.Products.With(query, DefaultOrder).ToPageAsync(pagingArgs, cancellationToken);

    public async Task<Page<Product>> GetProductsByBrandAsync(
        int brandId,
        PagingArguments pagingArgs,
        QueryContext<Product>? query = null,
        CancellationToken cancellationToken = default)
        => await batchingContext.ProductsByBrand
            .With(pagingArgs, query)
            .LoadAsync(brandId, cancellationToken)
            ?? Page<Product>.Empty;

    private static SortDefinition<Product> DefaultOrder(SortDefinition<Product> sort)
        => sort.IfEmpty(o => o.AddDescending(t => t.Name)).AddAscending(t => t.Id);
}

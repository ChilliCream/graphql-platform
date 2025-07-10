using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;

namespace HotChocolate.Data.Types.Products;

[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true, EnableRelativeCursors = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsAsync(pagingArgs, query, cancellationToken);
        return new ProductConnection(page);
    }

    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductConnection> GetProductsNonRelativeAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        var page = await productService.GetProductsAsync(pagingArgs, query, cancellationToken);
        return new ProductConnection(page);
    }
}

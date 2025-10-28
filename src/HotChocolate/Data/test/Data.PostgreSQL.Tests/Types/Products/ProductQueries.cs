using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Composite;
using HotChocolate.Types.Relay;

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

    [Lookup, Internal]
    public static async Task<Product?> GetProductAsync(
        [ID] int id,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        return await productService.GetProductByIdAsync(id, query, cancellationToken);
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

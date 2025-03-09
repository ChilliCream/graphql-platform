using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Data.Types.Brands;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Types.Products;

[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true, AllowRelativeCursors = true)]
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

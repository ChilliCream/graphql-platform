using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Data.Types.Products;

[QueryType]
public static partial class ProductQueries
{
    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<ProductsConnection> GetProductsAsync(
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        CancellationToken cancellationToken)
    {
        pagingArgs =  pagingArgs with { EnableRelativeCursors = true };
        var page = await productService.GetProductsAsync(pagingArgs, query, cancellationToken);
        return new ProductsConnection(page);
    }
}

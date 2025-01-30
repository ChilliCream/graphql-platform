using GreenDonut;
using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;

namespace HotChocolate.Data.Services;

internal static class ProductDataLoader
{
    [DataLoader]
    public static async Task<Dictionary<int, Page<Product>>> GetProductsByBrandAsync(
        IReadOnlyList<int> brandIds,
        PagingArguments pagingArgs,
        QueryContext<Product> queryContext,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        return await context.Products
            .Where(t => brandIds.Contains(t.BrandId))
            .With(queryContext)
            .ToBatchPageAsync(t => t.BrandId, pagingArgs, cancellationToken);
    }
}

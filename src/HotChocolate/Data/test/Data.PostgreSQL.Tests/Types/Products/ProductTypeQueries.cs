using GreenDonut.Data;
using HotChocolate.Data.Data;
using HotChocolate.Data.Models;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Products;

[QueryType]
public static partial class ProductTypeQueries
{
    [UseConnection(IncludeTotalCount = true)]
    [UseFiltering]
    [UseSorting]
    public static async ValueTask<PageConnection<ProductType>> GetProductTypesAsync(
        PagingArguments pagingArguments,
        QueryContext<ProductType> queryContext,
        CatalogContext context,
        CancellationToken cancellationToken)
    {
        return new PageConnection<ProductType>(
            await context.ProductTypes
                .With(queryContext)
                .ToPageAsync(pagingArguments, cancellationToken));
    }
}

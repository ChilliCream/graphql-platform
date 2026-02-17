using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Data.Types.Brands;

[ObjectType<Brand>]
public static partial class BrandNode
{
    [UseConnection(Name = "BrandProducts", EnableRelativeCursors = true)]
    [UseFiltering]
    [UseSorting]
    public static async Task<PageConnection<Product>> GetProductsAsync(
        [Parent(requires: nameof(Brand.Id))] Brand brand,
        PagingArguments pagingArgs,
        QueryContext<Product> query,
        ProductService productService,
        ConnectionFlags connectionFlags,
        ISelection selection,
        CancellationToken cancellationToken)
    {
        // we for test purposes only return an empty page if the connection flags are set to PageInfo
        if (connectionFlags == ConnectionFlags.PageInfo)
        {
            return new PageConnection<Product>(Page<Product>.Empty);
        }

        var page = await productService.GetProductsByBrandAsync(brand.Id, pagingArgs, query, cancellationToken);
        return new PageConnection<Product>(page);
    }
}

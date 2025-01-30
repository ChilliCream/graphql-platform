using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Types;

[ObjectType<Product>]
public static partial class ProductNode
{
    public static async Task<Brand?> GetBrandAsync(
        [Parent] Product product,
        BrandService brandService,
        QueryContext<Brand> query,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(product.BrandId, query, cancellationToken);

    [NodeResolver]
    public static async Task<Product?> GetProductAsync(
        int id,
        IProductByIdDataLoader productById,
        QueryContext<Product> query,
        CancellationToken cancellationToken)
        => await productById.With(query).LoadAsync(id, cancellationToken);
}

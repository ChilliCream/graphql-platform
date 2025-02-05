using GreenDonut.Data;
using HotChocolate.Data.Models;
using HotChocolate.Data.Services;
using HotChocolate.Execution.Processing;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace HotChocolate.Data.Types.Products;

[ObjectType<Product>]
public static partial class ProductNode
{
    // [BindMember(nameof(Product.Brand))]
    public static async Task<Brand?> GetBrandAsync(
        [Parent(requires: nameof(Product.BrandId))] Product product,
        QueryContext<Brand> query,
        ISelection selection,
        BrandService brandService,
        CancellationToken cancellationToken)
        => await brandService.GetBrandByIdAsync(product.BrandId, query, cancellationToken);

    [NodeResolver]
    public static async Task<Product?> GetProductAsync(
        int id,
        ProductService productService,
        QueryContext<Product> query,
        CancellationToken cancellationToken)
        => await productService.GetProductByIdAsync(id, query, cancellationToken);
}

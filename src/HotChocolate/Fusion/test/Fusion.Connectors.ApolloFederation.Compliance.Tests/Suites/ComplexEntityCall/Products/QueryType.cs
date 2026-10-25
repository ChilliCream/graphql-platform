using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.Products;

/// <summary>
/// Root <c>Query</c> for the <c>products</c> subgraph. Exposes
/// <c>topProducts: ProductList!</c> returning the full seeded list.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("topProducts")
            .Type<NonNullType<ProductListType>>()
            .Resolve(_ => new ProductList { Products = ProductsData.Items });
    }
}

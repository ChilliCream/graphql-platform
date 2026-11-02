using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Products;

/// <summary>
/// Root <c>Query</c> for the <c>products</c> subgraph. Exposes the
/// <c>products: [Product]</c> list field.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<ListType<ProductType>>()
            .Resolve(_ => ProductsData.Products);
    }
}

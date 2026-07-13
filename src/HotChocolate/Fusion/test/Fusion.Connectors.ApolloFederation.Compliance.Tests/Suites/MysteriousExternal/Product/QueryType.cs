using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.MysteriousExternal.Product;

/// <summary>
/// Root <c>Query</c> type for the <c>product</c> subgraph. Exposes
/// <c>products: [Product!]!</c> returning all seeded products.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("products")
            .Type<NonNullType<ListType<NonNullType<ProductType>>>>()
            .Resolve(_ => ProductData.Products);
    }
}

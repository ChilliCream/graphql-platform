using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.MysteriousExternal.Price;

/// <summary>
/// Root <c>Query</c> type for the <c>price</c> subgraph. Exposes
/// <c>cheapestProduct: Product</c> returning the product with the lowest price.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("cheapestProduct")
            .Type<ProductType>()
            .Resolve(_ => PriceData.CheapestProduct);
    }
}

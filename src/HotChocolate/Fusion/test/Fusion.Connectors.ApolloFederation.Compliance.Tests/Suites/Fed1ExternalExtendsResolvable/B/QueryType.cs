using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>productInB: Product</c> returning the seeded product.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("productInB")
            .Type<ProductType>()
            .Resolve(_ => BData.Items[0]);
    }
}

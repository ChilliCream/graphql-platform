using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Fed1ExternalExtendsResolvable.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>productInA: Product</c> returning the seeded product.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);
        descriptor
            .Field("productInA")
            .Type<ProductType>()
            .Resolve(_ => AData.Items[0]);
    }
}

using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.IncludeSkip.A;

/// <summary>
/// Root <c>Query</c> for the <c>a</c> subgraph. Exposes
/// <c>product: Product</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<ProductType>()
            .Resolve(_ => AData.Products[0]);
    }
}

using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c> with the <c>product</c> field
/// that returns the first product.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("product")
            .Type<ProductType>()
            .Resolve(_ => ProductData.Products[0]);
    }
}

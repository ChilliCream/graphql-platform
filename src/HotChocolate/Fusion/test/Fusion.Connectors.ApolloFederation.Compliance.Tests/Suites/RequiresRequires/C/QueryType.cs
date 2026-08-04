using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.C;

/// <summary>
/// Root <c>Query</c> placeholder for subgraph <c>c</c>. No user-facing
/// query fields; the subgraph only provides the <c>Product</c> entity.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor
            .ExtendServiceType()
            .Name(OperationTypeNames.Query);
    }
}

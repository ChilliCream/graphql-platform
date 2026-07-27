using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresRequires.A;

/// <summary>
/// Root <c>Query</c> placeholder for subgraph <c>a</c>. No user-facing
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

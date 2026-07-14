using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Node.NodeTwo;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>node-two</c> subgraph. The
/// subgraph declares no user-facing query fields.
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

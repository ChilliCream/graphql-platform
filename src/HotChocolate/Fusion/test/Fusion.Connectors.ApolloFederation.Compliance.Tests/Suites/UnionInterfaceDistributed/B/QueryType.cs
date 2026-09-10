using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.UnionInterfaceDistributed.B;

/// <summary>
/// Root <c>Query</c> placeholder for subgraph <c>b</c>. This subgraph
/// is entity-only (no user-facing query fields).
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

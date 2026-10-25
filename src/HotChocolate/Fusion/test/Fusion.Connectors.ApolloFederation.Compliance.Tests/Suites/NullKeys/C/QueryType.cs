using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NullKeys.C;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>c</c> subgraph. The subgraph
/// declares no user-facing query fields.
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

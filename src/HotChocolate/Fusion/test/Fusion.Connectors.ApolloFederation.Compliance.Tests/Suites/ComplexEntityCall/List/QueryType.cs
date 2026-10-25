using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ComplexEntityCall.List;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>list</c> subgraph. The subgraph
/// exposes no user-facing root fields; <c>ExtendServiceType</c> marks this type
/// as extending the federated <c>Query</c>.
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

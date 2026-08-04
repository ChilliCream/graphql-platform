using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.Mutations.B;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>b</c> subgraph. The subgraph
/// declares no user-facing query fields; <see cref="ApolloFederationObjectTypeDescriptorExtensions.ExtendServiceType"/>
/// marks this type as extending the federated <c>Query</c>.
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

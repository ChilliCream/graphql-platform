using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.ParentEntityCallComplex.A;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>a</c> subgraph. The subgraph
/// exposes no user-facing root fields; HotChocolate requires a <c>Query</c>
/// type so the Apollo Federation interceptor can attach <c>_service</c> and
/// <c>_entities</c>. <see cref="ApolloFederationObjectTypeDescriptorExtensions.ExtendServiceType"/>
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

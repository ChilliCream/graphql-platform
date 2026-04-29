using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleEntityCall.Nickname;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>nickname</c> subgraph. The subgraph's
/// SDL declares no user-facing query fields, but HotChocolate requires a Query type
/// to exist so the Apollo Federation type interceptor can attach
/// <c>_service { sdl }</c> and <c>_entities(...)</c> fields. Applying
/// <see cref="ApolloFederationObjectTypeDescriptorExtensions.ExtendServiceType"/>
/// marks this type as extending the federated <c>Query</c>, so composition treats it
/// as a pure entity provider.
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

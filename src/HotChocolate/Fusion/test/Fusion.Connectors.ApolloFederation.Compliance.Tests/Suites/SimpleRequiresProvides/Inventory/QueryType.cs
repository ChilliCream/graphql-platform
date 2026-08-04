using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleRequiresProvides.Inventory;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>inventory</c> subgraph. The
/// audit Schema Definition Language (SDL) declares no user-facing query
/// fields, so the type extends the federated <c>Query</c> as a pure
/// entity provider.
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

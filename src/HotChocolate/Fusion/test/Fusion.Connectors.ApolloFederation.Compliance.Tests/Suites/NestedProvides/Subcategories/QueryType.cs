using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.NestedProvides.Subcategories;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>subcategories</c> subgraph.
/// The audit SDL declares no user-facing query fields, so the type
/// extends the federated <c>Query</c> as a pure entity provider.
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

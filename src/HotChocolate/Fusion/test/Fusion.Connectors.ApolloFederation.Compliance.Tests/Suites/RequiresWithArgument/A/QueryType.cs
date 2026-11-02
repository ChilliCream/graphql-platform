using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.RequiresWithArgument.A;

/// <summary>
/// Root <c>Query</c> placeholder for the <c>a</c> subgraph. No
/// user-facing query fields are declared; the type extends the
/// federated <c>Query</c> as a pure entity provider.
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

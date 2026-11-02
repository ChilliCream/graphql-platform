using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.SimpleInterfaceObject.B;

/// <summary>
/// Root <c>Query</c> for the <c>b</c> subgraph. Exposes
/// <c>anotherUsers: [NodeWithName]</c> and <c>accounts: [Account] @shareable</c>,
/// both returning the <c>@interfaceObject</c> declarations contributed by this
/// subgraph.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("anotherUsers")
            .Type<ListType<NodeWithNameType>>()
            .Resolve(_ => BData.Users);

        descriptor
            .Field("accounts")
            .Shareable()
            .Type<ListType<AccountType>>()
            .Resolve(_ => BData.Accounts);
    }
}

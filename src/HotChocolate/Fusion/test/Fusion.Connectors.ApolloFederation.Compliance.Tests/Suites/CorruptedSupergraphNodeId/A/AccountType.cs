using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;

/// <summary>
/// Apollo Federation descriptor for <c>Account</c> in subgraph <c>a</c>
/// (<c>@key(fields: "id")</c>).
/// </summary>
public sealed class AccountType : ObjectType<Account>
{
    protected override void Configure(IObjectTypeDescriptor<Account> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(a => a.Id).Type<NonNullType<IdType>>();
        descriptor.Field(a => a.Username).Type<NonNullType<StringType>>();
    }

    private static Account? ResolveById(string id)
        => SubgraphAData.AccountsById.TryGetValue(id, out var a) ? a : null;
}

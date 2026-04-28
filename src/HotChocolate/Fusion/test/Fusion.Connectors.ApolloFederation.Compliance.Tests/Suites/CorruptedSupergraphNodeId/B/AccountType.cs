using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// Apollo Federation descriptor for <c>Account</c> in subgraph <c>b</c>.
/// The <c>id</c> field is external; this subgraph contributes
/// <c>chats: [Chat!]!</c>.
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

        descriptor
            .Field("chats")
            .Type<NonNullType<ListType<NonNullType<ChatType>>>>()
            .Resolve(ctx =>
            {
                var account = ctx.Parent<Account>();
                return SubgraphBData.Chats
                    .Where(c => c.AccountId == account.Id)
                    .ToList();
            });
    }

    private static Account? ResolveById(string id)
        => SubgraphBData.AccountsById.TryGetValue(id, out var a) ? a : null;
}

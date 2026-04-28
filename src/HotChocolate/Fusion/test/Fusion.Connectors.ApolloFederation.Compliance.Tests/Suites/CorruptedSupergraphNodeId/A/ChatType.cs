using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;

/// <summary>
/// Apollo Federation descriptor for <c>Chat</c> in subgraph <c>a</c>.
/// The <c>id</c> field is external; this subgraph contributes
/// <c>account: Account!</c>.
/// </summary>
public sealed class ChatType : ObjectType<Chat>
{
    protected override void Configure(IObjectTypeDescriptor<Chat> descriptor)
    {
        descriptor
            .Implements<NodeType>()
            .Key("id")
            .ResolveReferenceWith(_ => ResolveById(default!));

        descriptor.Field(c => c.Id).Type<NonNullType<IdType>>();

        descriptor
            .Field("account")
            .Type<NonNullType<AccountType>>()
            .Resolve(ctx =>
            {
                var chat = ctx.Parent<Chat>();
                return SubgraphAData.AccountsById.GetValueOrDefault(chat.AccountId);
            });

        // Hide the internal AccountId property from the schema.
        descriptor.Field(c => c.AccountId).Ignore();
    }

    private static Chat? ResolveById(string id)
        => SubgraphAData.ChatsById.TryGetValue(id, out var c) ? c : null;
}

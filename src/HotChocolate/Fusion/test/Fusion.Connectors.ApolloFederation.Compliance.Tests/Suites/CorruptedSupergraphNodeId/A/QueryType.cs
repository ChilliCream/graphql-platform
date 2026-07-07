using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.A;

/// <summary>
/// Root <c>Query</c> for subgraph <c>a</c>. Exposes
/// <c>node(id: ID!): Node @shareable</c> and
/// <c>account(id: String!): Account</c>.
/// </summary>
public sealed class QueryType : ObjectType
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name(OperationTypeNames.Query);

        descriptor
            .Field("node")
            .Argument("id", a => a.Type<NonNullType<IdType>>())
            .Type<NodeType>()
            .Shareable()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");

                if (SubgraphAData.AccountsById.TryGetValue(id, out var account))
                {
                    return (INode)account;
                }

                if (SubgraphAData.ChatsById.TryGetValue(id, out var chat))
                {
                    // Intentionally corrupted: Chat.id is @external in this
                    // subgraph, so we return "never" as the id to test
                    // gateway handling of corrupted node IDs.
                    return (INode)new Chat { Id = "never", AccountId = chat.AccountId };
                }

                return null;
            });

        descriptor
            .Field("account")
            .Argument("id", a => a.Type<NonNullType<StringType>>())
            .Type<AccountType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");
                return SubgraphAData.AccountsById.GetValueOrDefault(id);
            });
    }
}

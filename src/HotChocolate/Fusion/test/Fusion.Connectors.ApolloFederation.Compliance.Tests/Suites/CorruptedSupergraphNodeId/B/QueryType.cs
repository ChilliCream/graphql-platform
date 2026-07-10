using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// Root <c>Query</c> for subgraph <c>b</c>. Exposes
/// <c>node(id: ID!): Node @shareable</c> and
/// <c>chat(id: String!): Chat</c>.
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

                if (SubgraphBData.AccountsById.TryGetValue(id, out _))
                {
                    // Intentionally corrupted: Account.id is @external in
                    // this subgraph, so we return "never" as the id.
                    return (INode)new Account { Id = "never" };
                }

                if (SubgraphBData.ChatsById.TryGetValue(id, out var chat))
                {
                    return (INode)chat;
                }

                return null;
            });

        descriptor
            .Field("chat")
            .Argument("id", a => a.Type<NonNullType<StringType>>())
            .Type<ChatType>()
            .Resolve(ctx =>
            {
                var id = ctx.ArgumentValue<string>("id");
                return SubgraphBData.ChatsById.GetValueOrDefault(id);
            });
    }
}

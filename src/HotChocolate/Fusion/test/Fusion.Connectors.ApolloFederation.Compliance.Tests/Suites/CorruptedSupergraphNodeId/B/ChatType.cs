using HotChocolate.ApolloFederation.Types;
using HotChocolate.Types;

namespace HotChocolate.Fusion.Suites.CorruptedSupergraphNodeId.B;

/// <summary>
/// Apollo Federation descriptor for <c>Chat</c> in subgraph <c>b</c>.
/// This subgraph owns <c>id</c> and <c>text</c>.
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
        descriptor.Field(c => c.Text).Type<NonNullType<StringType>>();

        // Hide the internal AccountId property from the schema.
        descriptor.Field(c => c.AccountId).Ignore();
    }

    private static Chat? ResolveById(string id)
        => SubgraphBData.ChatsById.TryGetValue(id, out var c) ? c : null;
}

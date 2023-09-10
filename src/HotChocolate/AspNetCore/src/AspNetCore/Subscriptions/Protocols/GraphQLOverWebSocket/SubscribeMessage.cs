using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class SubscribeMessage : OperationMessage<GraphQLRequest>
{
    public SubscribeMessage(string id, GraphQLRequest payload)
        : base(Messages.Subscribe, payload)
    {
        Id = id;
    }

    public string Id { get; }
}

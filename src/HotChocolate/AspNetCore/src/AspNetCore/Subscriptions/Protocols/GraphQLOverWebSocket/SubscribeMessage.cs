using HotChocolate.Language;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed  class SubscribeMessage : OperationMessage<GraphQLRequest>
{
    public SubscribeMessage(GraphQLRequest payload, string id)
        : base(Messages.Subscribe, payload)
    {
        Id = id;
    }

    public string Id { get; }
}

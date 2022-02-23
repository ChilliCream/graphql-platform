namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed  class PongMessage : OperationMessage<IReadOnlyDictionary<string, object?>?>
{
    public PongMessage(IReadOnlyDictionary<string, object?>? payload = null)
        : base(Messages.Pong, payload)
    {
    }
}

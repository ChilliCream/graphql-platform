namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed  class PingMessage : OperationMessage<IReadOnlyDictionary<string, object?>?>
{
    public PingMessage(IReadOnlyDictionary<string, object?>? payload = null)
        : base(Messages.Ping, payload)
    {
    }
}

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed  class ConnectionAckMessage
    : OperationMessage<IReadOnlyDictionary<string, object?>?>
{
    public ConnectionAckMessage(IReadOnlyDictionary<string, object?>? payload = null)
        : base(Messages.ConnectionAccept, payload)
    {
    }
}

using static HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class ConnectionInitMessage
    : OperationMessage<IReadOnlyDictionary<string, object?>?>
{
    public ConnectionInitMessage(IReadOnlyDictionary<string, object?>? payload = null)
        : base(ConnectionInitialize, payload)
    {
    }
}

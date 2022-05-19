using System.Text.Json;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class ConnectionInitMessage : JsonOperationMessage
{
    public ConnectionInitMessage(JsonElement? payload = null)
        : base(ConnectionInitialize, payload)
    {
    }

    public static ConnectionInitMessage Default { get; } = new();
}

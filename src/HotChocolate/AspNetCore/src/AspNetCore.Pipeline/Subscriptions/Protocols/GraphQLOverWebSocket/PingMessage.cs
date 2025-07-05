using System.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class PingMessage : JsonOperationMessage
{
    public PingMessage(JsonElement? payload = null)
        : base(Messages.Ping, payload)
    {
    }

    public static PingMessage Default { get; } = new();
}

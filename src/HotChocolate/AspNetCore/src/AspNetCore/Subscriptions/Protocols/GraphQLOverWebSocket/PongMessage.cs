using System.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.GraphQLOverWebSocket;

internal sealed class PongMessage : JsonOperationMessage
{
    public PongMessage(JsonElement? payload = null)
        : base(Messages.Pong, payload)
    {
    }

    public static PongMessage Default { get; } = new();
}

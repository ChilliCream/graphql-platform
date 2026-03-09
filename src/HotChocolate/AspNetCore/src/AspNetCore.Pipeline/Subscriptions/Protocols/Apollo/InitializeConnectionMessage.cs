using System.Text.Json;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo.Messages;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols.Apollo;

public sealed class InitializeConnectionMessage : JsonOperationMessage
{
    public InitializeConnectionMessage(JsonElement? payload = null)
        : base(ConnectionInitialize, payload)
    {
    }

    public static InitializeConnectionMessage Default { get; } = new();
}

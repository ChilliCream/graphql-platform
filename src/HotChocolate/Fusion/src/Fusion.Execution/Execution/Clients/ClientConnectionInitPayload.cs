using System.Text.Json;

namespace HotChocolate.Fusion.Execution.Clients;

internal sealed class ClientConnectionInitPayload(JsonElement payload)
{
    public JsonElement Payload { get; } = payload;
}

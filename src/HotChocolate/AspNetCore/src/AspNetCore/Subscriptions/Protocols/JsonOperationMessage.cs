using System.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

public abstract class JsonOperationMessage
    : OperationMessage<JsonElement?>
    , IOperationMessagePayload
{
    protected JsonOperationMessage(string typeName, JsonElement? payload = null)
        : base(typeName, payload)
    {
    }

    public T? As<T>() where T : class
        => Payload?.Deserialize<T>();
}

using System.Text.Json;
using static HotChocolate.AspNetCore.Subscriptions.Protocols.MessageUtilities;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// A base class for JSON operation messages.
/// </summary>
public abstract class JsonOperationMessage
    : OperationMessage<JsonElement?>
    , IOperationMessagePayload
{
    protected JsonOperationMessage(string typeName, JsonElement? payload = null)
        : base(typeName, payload)
    {
    }

#if NET6_0_OR_GREATER
    /// <inheritdoc />
    public T? As<T>() where T : class
        => Payload?.Deserialize<T>(SerializerOptions);
#else
    /// <inheritdoc />
    public T? As<T>() where T : class
    {
        return Payload is null
            ? null
            : JsonSerializer.Deserialize<T>(Payload.Value.GetRawText(), SerializerOptions);
    }
#endif
}

using System.Text.Json;

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
}

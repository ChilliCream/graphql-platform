using System.Text.Json;

namespace HotChocolate.AspNetCore.Subscriptions.Protocols;

/// <summary>
/// The operation message with a custom payload.
/// </summary>
public interface IOperationMessagePayload
{
    /// <summary>
    /// Gets the JSON payload.
    /// </summary>
    JsonElement? Payload { get; }
}

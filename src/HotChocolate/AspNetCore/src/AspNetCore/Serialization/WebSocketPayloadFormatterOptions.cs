using HotChocolate.Transport.Formatters;

namespace HotChocolate.AspNetCore.Serialization;

/// <summary>
/// Represents the GraphQL over WebSocket payload formatter options.
/// </summary>
public struct WebSocketPayloadFormatterOptions
{
    /// <summary>
    /// Gets or sets the JSON result formatter options.
    /// </summary>
    public JsonResultFormatterOptions Json { get; set; }
}

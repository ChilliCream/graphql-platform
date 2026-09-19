using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The opencode shim event fields used to identify a session and its server.
/// The aliases retain compatibility with early shim payloads.
/// </summary>
internal sealed class OpencodeHookPayload
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; set; }

    [JsonPropertyName("session_id")]
    public string? SessionIdAlias
    {
        set => SessionId = value;
    }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; set; }

    [JsonPropertyName("serverUrl")]
    public string? ServerUrl { get; set; }

    [JsonPropertyName("serverPassword")]
    public string? ServerPassword { get; set; }

    [JsonPropertyName("password")]
    public string? ServerPasswordAlias
    {
        set => ServerPassword = value;
    }

    /// <summary>
    /// Whether the shim reports a bound HTTP server. Missing or false prevents
    /// <see cref="ServerUrl"/> from being registered as a push endpoint.
    /// </summary>
    [JsonPropertyName("serverBound")]
    public bool ServerBound { get; set; }

    [JsonPropertyName("harnessVersion")]
    public string? HarnessVersion { get; set; }

    [JsonPropertyName("nitroPushed")]
    public bool NitroPushed { get; set; }

    /// <summary>
    /// Whether the shim confirmed the previous response, including a response with no parts.
    /// False requests a delivery retry; null means no delivery report was supplied.
    /// </summary>
    [JsonPropertyName("nitroDelivered")]
    public bool? Delivered { get; set; }

    [JsonPropertyName("version")]
    public string? HarnessVersionAlias
    {
        set => HarnessVersion = value;
    }
}

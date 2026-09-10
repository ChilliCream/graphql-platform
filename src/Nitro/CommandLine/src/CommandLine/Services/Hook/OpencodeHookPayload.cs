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
    /// Whether the shim proved, by reading its plugin input's
    /// <c>serverUrl</c> getter twice and comparing the results by
    /// reference, that opencode actually bound an HTTP server in this
    /// process: opencode returns the same URL object on every read once a
    /// server is bound, and a fresh placeholder URL on every read
    /// otherwise. Missing (an older shim that predates this field) defaults
    /// to <c>false</c>, the safe reading: it demotes an unproven
    /// <see cref="ServerUrl"/> to <c>endpoint_kind = 'none'</c> rather than
    /// trusting a placeholder. See
    /// <see cref="ChilliCream.Nitro.CommandLine.Services.Workspace.EndpointAddress.IsTrustedOpencodeServerUrl"/>.
    /// </summary>
    [JsonPropertyName("serverBound")]
    public bool ServerBound { get; set; }

    [JsonPropertyName("harnessVersion")]
    public string? HarnessVersion { get; set; }

    [JsonPropertyName("nitroPushed")]
    public bool NitroPushed { get; set; }

    /// <summary>
    /// Whether the shim's PREVIOUS chat-message response actually reached
    /// the model: true when it pushed at least one part, false when
    /// appending threw, and null when the previous turn had no parts to
    /// append or when this is the shim's first-ever report for the session.
    /// </summary>
    [JsonPropertyName("nitroDelivered")]
    public bool? Delivered { get; set; }

    [JsonPropertyName("version")]
    public string? HarnessVersionAlias
    {
        set => HarnessVersion = value;
    }
}

using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The <c>type</c>, <c>thread-id</c>, and <c>cwd</c> fields read from the notify
/// command's JSON argument. Other fields are ignored.
/// </summary>
internal sealed record CodexNotifyPayload
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("thread-id")]
    public string? ThreadId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    /// <summary>
    /// The notify event type handled by this adapter; missing or other types yield a neutral outcome.
    /// </summary>
    public const string AgentTurnComplete = "agent-turn-complete";
}

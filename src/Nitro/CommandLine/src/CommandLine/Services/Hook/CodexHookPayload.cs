using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Codex CLI <c>hooks.json</c> event's
/// stdin JSON, across <c>SessionStart</c>, <c>UserPromptSubmit</c>, and
/// <c>SessionEnd</c>: <c>session_id</c> and <c>cwd</c>. Other fields are
/// left unparsed.
/// </summary>
internal sealed record CodexHookPayload
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }
}

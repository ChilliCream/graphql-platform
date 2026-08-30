using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Claude Code hook's stdin JSON,
/// across every event: <c>SessionStart</c>, <c>UserPromptSubmit</c>,
/// <c>Stop</c>, and <c>SessionEnd</c> all carry <c>session_id</c> and
/// <c>cwd</c>; <c>stop_hook_active</c> is only meaningful on <c>Stop</c>.
/// Fields this adapter does not read (transcript path, the event name
/// itself, prompt text) are left unparsed by design: only charset-validated
/// identifiers ever influence adapter behavior or reach the digest.
/// </summary>
internal sealed record ClaudeHookPayload
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("stop_hook_active")]
    public bool StopHookActive { get; init; }
}

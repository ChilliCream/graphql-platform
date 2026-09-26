using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Codex CLI <c>hooks.json</c> event's
/// stdin JSON, across <c>SessionStart</c>, <c>UserPromptSubmit</c>, and
/// <c>SessionEnd</c>. All three carry <c>session_id</c> and <c>cwd</c>. The
/// wire shape uses snake_case fields and PascalCase event names. Fields this adapter does not read
/// (<c>transcript_path</c>, <c>hook_event_name</c>, <c>model</c>,
/// <c>permission_mode</c>, <c>source</c>, <c>turn_id</c>, <c>prompt</c>,
/// <c>reason</c>) are left unparsed by design: only charset-validated
/// identifiers ever influence adapter behavior or reach the digest.
/// </summary>
internal sealed record CodexHookPayload
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }
}

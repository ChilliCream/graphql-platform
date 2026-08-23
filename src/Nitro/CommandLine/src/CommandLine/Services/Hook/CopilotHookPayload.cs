using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Copilot CLI hook's stdin JSON, across
/// <c>sessionStart</c>, <c>userPromptSubmitted</c>, and <c>sessionEnd</c>:
/// all three carry <c>sessionId</c> and <c>cwd</c> (spike S5 redo,
/// perles-net-k3j.4 - live-verified camelCase field names and an
/// epoch-milliseconds numeric <c>timestamp</c>, the canonical SDK payload
/// shape, distinct from the snake_case payload the Claude-Code-compat
/// PascalCase alias event keys produce). This adapter only ever registers
/// the canonical camelCase event keys, so the alias payload shape never
/// applies here. Fields this adapter does not read (<c>timestamp</c>,
/// <c>source</c>, <c>initialPrompt</c>, <c>prompt</c>, <c>reason</c>) are
/// left unparsed by design: only charset-validated identifiers ever
/// influence adapter behavior or reach the digest.
/// </summary>
internal sealed record CopilotHookPayload
{
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }
}

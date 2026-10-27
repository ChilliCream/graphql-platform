using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Claude Code hook's stdin JSON:
/// <c>session_id</c> and <c>cwd</c> on every event, <c>agent_id</c> when the
/// event fires inside a subagent session, plus <c>stop_hook_active</c> on
/// <c>Stop</c>, <c>notification_type</c> on <c>Notification</c>, and
/// <c>agent_kind</c> on Venti's <c>AgentNaming</c>. Other fields are left unparsed.
/// </summary>
internal sealed record ClaudeHookPayload
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; init; }

    [JsonPropertyName("stop_hook_active")]
    public bool StopHookActive { get; init; }

    [JsonPropertyName("notification_type")]
    public string? NotificationType { get; init; }

    [JsonPropertyName("agent_kind")]
    public string? AgentKind { get; init; }
}

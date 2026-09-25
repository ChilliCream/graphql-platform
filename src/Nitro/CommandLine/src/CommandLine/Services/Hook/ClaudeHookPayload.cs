using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Claude Code hook's stdin JSON:
/// <c>session_id</c> and <c>cwd</c> on every event, <c>agent_id</c> and
/// <c>agent_type</c> when the event fires inside a subagent session, plus
/// <c>stop_hook_active</c> on <c>Stop</c> and <c>notification_type</c> on
/// <c>Notification</c>. Other fields are left unparsed.
/// </summary>
internal sealed record ClaudeHookPayload
{
    [JsonPropertyName("session_id")]
    public string? SessionId { get; init; }

    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    [JsonPropertyName("agent_id")]
    public string? AgentId { get; init; }

    [JsonPropertyName("agent_type")]
    public string? AgentType { get; init; }

    [JsonPropertyName("stop_hook_active")]
    public bool StopHookActive { get; init; }

    [JsonPropertyName("notification_type")]
    public string? NotificationType { get; init; }
}

using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Codex CLI <c>notify</c> program's
/// single JSON argument (spike S2/S4, perles-net-k3j.2): kebab-case field
/// names, argv[1], NOT stdin - a materially different wire shape from
/// <see cref="CodexHookPayload"/>'s <c>hooks.json</c> events. Only
/// <c>type</c>, <c>thread-id</c>, and <c>cwd</c> are read; <c>turn-id</c>,
/// <c>client</c>, <c>input-messages</c>, and <c>last-assistant-message</c>
/// are left unparsed by design, same rationale as
/// <see cref="CodexHookPayload"/>.
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
    /// The only <c>type</c> value spike S2 ever observed. A notify payload
    /// of a different (or missing) type is not a boundary this adapter
    /// understands and is treated as fail-open no-op.
    /// </summary>
    public const string AgentTurnComplete = "agent-turn-complete";
}

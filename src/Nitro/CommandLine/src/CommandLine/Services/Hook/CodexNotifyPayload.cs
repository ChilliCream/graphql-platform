using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The fields this adapter reads from a Codex CLI <c>notify</c> program's
/// single JSON argument, argv[1] rather than stdin: <c>type</c>,
/// <c>thread-id</c>, and <c>cwd</c>. Other fields are left unparsed.
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
    /// The supported <c>type</c> value. A notify payload
    /// of a different (or missing) type is not a boundary this adapter
    /// understands and is treated as fail-open no-op.
    /// </summary>
    public const string AgentTurnComplete = "agent-turn-complete";
}

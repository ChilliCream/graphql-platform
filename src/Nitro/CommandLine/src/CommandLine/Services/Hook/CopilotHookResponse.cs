using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The Copilot CLI hook event response envelope written to stdout: a flat
/// <c>{"additionalContext": "..."}</c>, live-verified against the running
/// 1.0.80 binary (spike S5 redo, perles-net-k3j.4) - unlike Claude Code and
/// Codex, Copilot does NOT nest this under a <c>hookSpecificOutput</c>
/// wrapper. The property serializes as camelCase and is omitted when null,
/// so the neutral, fail-open response is exactly <c>{}</c>.
/// </summary>
internal sealed record CopilotHookResponse
{
    public string? AdditionalContext { get; init; }
}

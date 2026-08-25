using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The Codex CLI <c>hooks.json</c> event response envelope written to
/// stdout: <see cref="HookSpecificOutput"/> for context injection, or
/// nothing at all. Codex uses the same
/// <c>hookSpecificOutput.additionalContext</c> shape Claude Code uses for
/// <c>SessionStart</c> and <c>UserPromptSubmit</c>; <c>SessionEnd</c> has no
/// response contract, so this adapter never returns one for it). Every
/// property serializes as camelCase and every null property is omitted, so
/// the neutral, fail-open response is exactly <c>{}</c>.
/// </summary>
internal sealed record CodexHookResponse
{
    public CodexHookSpecificOutput? HookSpecificOutput { get; init; }
}

internal sealed record CodexHookSpecificOutput
{
    public required string HookEventName { get; init; }

    public required string AdditionalContext { get; init; }
}

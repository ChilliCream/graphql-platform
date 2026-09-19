namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The Codex CLI <c>hooks.json</c> event response envelope written to stdout:
/// <see cref="HookSpecificOutput"/> for context injection, or nothing at all.
/// Every property serializes as camelCase and every null property is omitted, so
/// the neutral, fail-open response is exactly <c>{}</c>.
/// </summary>
internal sealed record CodexHookResponse
{
    public CodexHookSpecificOutput? HookSpecificOutput { get; init; }
}

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The Claude Code hook response envelope written to stdout: either
/// <see cref="HookSpecificOutput"/> (context injection) or
/// <see cref="Decision"/>/<see cref="Reason"/> (the Stop gate), never both.
/// Every property serializes as camelCase and every null property is
/// omitted, so the neutral, fail-open response is exactly <c>{}</c>.
/// </summary>
internal sealed record ClaudeHookResponse
{
    public ClaudeHookSpecificOutput? HookSpecificOutput { get; init; }

    public string? Decision { get; init; }

    public string? Reason { get; init; }
}

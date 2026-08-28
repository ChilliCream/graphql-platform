namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record ClaudeHookSpecificOutput
{
    public required string HookEventName { get; init; }

    public required string AdditionalContext { get; init; }
}

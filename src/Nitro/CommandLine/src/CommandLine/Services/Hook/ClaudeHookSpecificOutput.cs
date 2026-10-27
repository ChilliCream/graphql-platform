namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed record ClaudeHookSpecificOutput
{
    public required string HookEventName { get; init; }

    public string? AdditionalContext { get; init; }

    public string? AgentName { get; init; }
}

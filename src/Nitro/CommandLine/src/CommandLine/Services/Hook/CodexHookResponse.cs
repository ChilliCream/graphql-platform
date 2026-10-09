namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// The Codex hook response containing optional additional context.
/// Properties use camelCase, and null properties are omitted, yielding <c>{}</c> for a neutral response.
/// </summary>
internal sealed record CodexHookResponse
{
    public CodexHookSpecificOutput? HookSpecificOutput { get; init; }
}

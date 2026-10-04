namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Claude settings path to <c>~/.claude/settings.json</c> for user
/// scope or <c>&lt;checkout-root&gt;/.claude/settings.json</c> for project scope.
/// </summary>
internal interface IClaudeSettingsPathResolver
{
    string Resolve(string scope);
}

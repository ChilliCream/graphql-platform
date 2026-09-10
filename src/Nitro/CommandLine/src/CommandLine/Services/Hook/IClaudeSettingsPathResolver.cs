namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Claude Code <c>settings.json</c> path for a scope:
/// <c>~/.claude/settings.json</c> for <see cref="HookInstallScopes.User"/>,
/// <c>&lt;workspace&gt;/.claude/settings.json</c> for
/// <see cref="HookInstallScopes.Project"/>.
/// </summary>
internal interface IClaudeSettingsPathResolver
{
    string Resolve(string scope);
}

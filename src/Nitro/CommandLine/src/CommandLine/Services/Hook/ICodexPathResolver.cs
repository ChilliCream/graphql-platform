namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Codex CLI config paths this installer writes:
/// <c>~/.codex/hooks.json</c> and <c>~/.codex/config.toml</c>. Unlike Claude
/// Code (user vs project scope) and the plan's Copilot row, the install-flow
/// table has no project-scope row for Codex - <c>CODEX_HOME</c> is a
/// per-user, not per-repo, concept.
/// </summary>
internal interface ICodexPathResolver
{
    string ResolveHooksJson();

    string ResolveConfigToml();
}

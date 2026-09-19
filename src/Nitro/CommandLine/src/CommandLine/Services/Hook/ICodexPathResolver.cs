namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Codex CLI config paths this installer writes:
/// <c>~/.codex/hooks.json</c> and <c>~/.codex/config.toml</c>. <c>CODEX_HOME</c> is
/// a per-user, not per-repo, concept, so there is no project-scope variant.
/// </summary>
internal interface ICodexPathResolver
{
    string ResolveHooksJson();

    string ResolveConfigToml();
}

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves <c>hooks.json</c> and <c>config.toml</c> under a nonempty
/// <c>CODEX_HOME</c>, or under <c>~/.codex</c> when it is unset or empty.
/// </summary>
internal interface ICodexPathResolver
{
    string ResolveHooksJson();

    string ResolveConfigToml();
}

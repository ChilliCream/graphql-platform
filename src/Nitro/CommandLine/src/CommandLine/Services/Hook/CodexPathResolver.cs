namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexPathResolver(IEnvironmentVariableProvider environmentVariables)
    : ICodexPathResolver
{
    public string ResolveHooksJson() => Path.Combine(ResolveCodexHome(), "hooks.json");

    public string ResolveConfigToml() => Path.Combine(ResolveCodexHome(), "config.toml");

    /// <summary>
    /// <c>CODEX_HOME</c> when set (Codex's own override, honored the same
    /// way the real Codex CLI resolves it), otherwise <c>~/.codex</c>. Reads
    /// through <see cref="IEnvironmentVariableProvider"/>, not
    /// <see cref="Environment.GetEnvironmentVariable(string)"/> directly, so
    /// a test can redirect this away from a real <c>CODEX_HOME</c> the same
    /// way it can already fake <c>NITRO_HOOK_SUPPRESS</c>.
    /// </summary>
    private string ResolveCodexHome()
    {
        var overrideHome = environmentVariables.GetEnvironmentVariable("CODEX_HOME");

        if (!string.IsNullOrEmpty(overrideHome))
        {
            return overrideHome;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(home))
        {
            throw new ExitException("Could not resolve the current user's home directory.");
        }

        return Path.Combine(home, ".codex");
    }
}

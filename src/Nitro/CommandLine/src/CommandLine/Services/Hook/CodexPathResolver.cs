namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class CodexPathResolver(IEnvironmentVariableProvider environmentVariables)
    : ICodexPathResolver
{
    public string ResolveHooksJson() => Path.Combine(ResolveCodexHome(), "hooks.json");

    public string ResolveConfigToml() => Path.Combine(ResolveCodexHome(), "config.toml");

    /// <summary>
    /// Returns nonempty <c>CODEX_HOME</c>, or <c>~/.codex</c> when it is unset or empty.
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

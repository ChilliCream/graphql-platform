namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Copilot CLI hooks directory file this installer writes:
/// <c>~/.copilot/hooks/nitro-mail.json</c>. Copilot loads JSON files under
/// <c>$COPILOT_HOME/hooks</c> without assigning significance to the filename.
/// A dedicated, Nitro-owned filename means this installer
/// never has to merge its entries into a config file Copilot itself, or
/// another tool, also writes to, unlike Claude's <c>settings.json</c> or
/// Codex's <c>hooks.json</c>).
/// </summary>
internal interface ICopilotPathResolver
{
    string ResolveHooksFile();
}

internal sealed class CopilotPathResolver(IEnvironmentVariableProvider environmentVariables) : ICopilotPathResolver
{
    private const string FileName = "nitro-mail.json";

    public string ResolveHooksFile() => Path.Combine(ResolveCopilotHome(), "hooks", FileName);

    /// <summary>
    /// <c>COPILOT_HOME</c> when set, otherwise <c>~/.copilot</c>. Reads through
    /// <see cref="IEnvironmentVariableProvider"/>, not
    /// <see cref="Environment.GetEnvironmentVariable(string)"/> directly, so
    /// a test can redirect this away from a real <c>COPILOT_HOME</c> the
    /// same way <see cref="CodexPathResolver"/> can redirect <c>CODEX_HOME</c>.
    /// </summary>
    private string ResolveCopilotHome()
    {
        var overrideHome = environmentVariables.GetEnvironmentVariable("COPILOT_HOME");

        if (!string.IsNullOrEmpty(overrideHome))
        {
            return overrideHome;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(home))
        {
            throw new ExitException("Could not resolve the current user's home directory.");
        }

        return Path.Combine(home, ".copilot");
    }
}

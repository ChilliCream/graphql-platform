namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeSettingsPathResolver(IFileSystem fileSystem) : IClaudeSettingsPathResolver
{
    public string Resolve(string scope) => scope switch
    {
        HookInstallScopes.Project => Path.Combine(ResolveProjectRoot(), ".claude", "settings.json"),
        _ => Path.Combine(ResolveUserHome(), ".claude", "settings.json")
    };

    /// <summary>
    /// Returns the checkout directory containing the current directory, or throws
    /// <see cref="ExitException"/> when no agent workspace is found.
    /// </summary>
    private string ResolveProjectRoot()
        => Workspace.AgentWorkspace.FindLocation(fileSystem, fileSystem.GetCurrentDirectory())
            ?.CheckoutDirectory
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");

    private static string ResolveUserHome()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(home))
        {
            throw new ExitException("Could not resolve the current user's home directory.");
        }

        return home;
    }
}

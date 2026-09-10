namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class ClaudeSettingsPathResolver(IFileSystem fileSystem) : IClaudeSettingsPathResolver
{
    public string Resolve(string scope) => scope switch
    {
        HookInstallScopes.Project => Path.Combine(ResolveProjectRoot(), ".claude", "settings.json"),
        _ => Path.Combine(ResolveUserHome(), ".claude", "settings.json")
    };

    /// <summary>
    /// The checkout root that contains the current directory (the directory
    /// whose <c>.nitro</c> or <c>.git</c> entry resolved the workspace), not
    /// the workspace directory itself: Claude Code's project-scope config
    /// lives at <c>&lt;checkout-root&gt;/.claude/settings.json</c>, so a
    /// linked worktree gets its own settings, not the main checkout's.
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

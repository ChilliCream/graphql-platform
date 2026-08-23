namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal static class HookInstallScopes
{
    public const string User = "user";
    public const string Project = "project";
}

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

internal sealed class ClaudeSettingsPathResolver(IFileSystem fileSystem) : IClaudeSettingsPathResolver
{
    public string Resolve(string scope) => scope switch
    {
        HookInstallScopes.Project => Path.Combine(ResolveProjectRoot(), ".claude", "settings.json"),
        _ => Path.Combine(ResolveUserHome(), ".claude", "settings.json")
    };

    /// <summary>
    /// The directory CONTAINING <c>.nitro</c>, not
    /// <see cref="Workspace.AgentWorkspace.Find"/>'s own return value (that
    /// resolves to the nested <c>.nitro/agents</c> database directory):
    /// Claude Code's project-scope config lives at
    /// <c>&lt;repo-root&gt;/.claude/settings.json</c>, a sibling of
    /// <c>.nitro</c>, not underneath it. Walks the same nearest-ancestor
    /// search <c>AgentWorkspace.Find</c> uses, stopping one level higher.
    /// </summary>
    private string ResolveProjectRoot()
    {
        for (var directory = fileSystem.GetCurrentDirectory();
            !string.IsNullOrEmpty(directory);
            directory = Path.GetDirectoryName(directory))
        {
            var workspaceDirectory = Workspace.AgentWorkspace.GetDirectory(directory);

            if (fileSystem.FileExists(Workspace.AgentWorkspace.GetDatabasePath(workspaceDirectory)))
            {
                return directory;
            }
        }

        throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
    }

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

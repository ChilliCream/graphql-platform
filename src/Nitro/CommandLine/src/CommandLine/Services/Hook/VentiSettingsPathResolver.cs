namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves <c>~/.venti/settings.json</c> for user scope or <c>.venti/settings.json</c> under
/// the current directory for project scope, which Venti reads only when launched from that
/// directory. Project scope requires the current directory to be inside an agent workspace.
/// </summary>
internal sealed class VentiSettingsPathResolver(IFileSystem fileSystem) : IClaudeSettingsPathResolver
{
    public string Resolve(string scope) => scope switch
    {
        HookInstallScopes.Project => Path.Combine(ResolveProjectDirectory(), ".venti", "settings.json"),
        _ => Path.Combine(ResolveUserHome(), ".venti", "settings.json")
    };

    public IReadOnlyList<string> ResolveForCurrentDirectory()
        =>
        [
            Resolve(HookInstallScopes.User),
            Path.Combine(fileSystem.GetCurrentDirectory(), ".venti", "settings.json")
        ];

    private string ResolveProjectDirectory()
    {
        var currentDirectory = fileSystem.GetCurrentDirectory();

        return Workspace.AgentWorkspace.FindLocation(fileSystem, currentDirectory) is not null
            ? currentDirectory
            : throw ThrowHelper.NoAgentWorkspaceFound();
    }

    private static string ResolveUserHome()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        return string.IsNullOrEmpty(home)
            ? throw ThrowHelper.UserHomeDirectoryNotResolved()
            : home;
    }
}

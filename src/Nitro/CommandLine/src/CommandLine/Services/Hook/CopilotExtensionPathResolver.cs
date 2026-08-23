namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Copilot CLI project-scope extension paths this installer
/// writes: <c>&lt;repo-root&gt;/.github/extensions/nitro-mail/extension.mjs</c>
/// and its sibling <c>nitro-mail.config.json</c> (the launch descriptor the
/// asset reads at runtime, kept out of the versioned asset file itself so
/// the asset's bytes, and therefore its content hash, stay identical across
/// machines). Project scope only (perles-net-k3j.16 non-goal): spike S5
/// (perles-net-k3j.4 redo, comment #94) found the Copilot CLI's
/// <c>EXTENSIONS</c> feature flag reports false on the machine it ran on and
/// could not live-verify a user-scope extensions directory actually loading
/// anything, so there is no user-scope resolution path here at all, unlike
/// <see cref="IClaudeSettingsPathResolver"/>'s user/project split.
/// </summary>
internal interface ICopilotExtensionPathResolver
{
    string ResolveExtensionFile();

    string ResolveConfigFile();
}

internal sealed class CopilotExtensionPathResolver(IFileSystem fileSystem) : ICopilotExtensionPathResolver
{
    private const string DirectoryName = "nitro-mail";
    private const string ExtensionFileName = "extension.mjs";
    private const string ConfigFileName = "nitro-mail.config.json";

    public string ResolveExtensionFile() => Path.Combine(ResolveExtensionDirectory(), ExtensionFileName);

    public string ResolveConfigFile() => Path.Combine(ResolveExtensionDirectory(), ConfigFileName);

    private string ResolveExtensionDirectory()
        => Path.Combine(ResolveProjectRoot(), ".github", "extensions", DirectoryName);

    /// <summary>
    /// The directory CONTAINING <c>.nitro</c>, mirroring
    /// <c>ClaudeSettingsPathResolver.ResolveProjectRoot</c>: Copilot's own
    /// project-scope extensions directory
    /// (<c>&lt;repo-root&gt;/.github/extensions/</c>) is a sibling of
    /// <c>.nitro</c>, not underneath it.
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
}

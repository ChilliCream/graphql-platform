namespace ChilliCream.Nitro.CommandLine.Services.Hook;

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
    /// The checkout root that contains the current directory, mirroring
    /// <c>ClaudeSettingsPathResolver.ResolveProjectRoot</c>: Copilot's own
    /// project-scope extensions directory
    /// (<c>&lt;checkout-root&gt;/.github/extensions/</c>) belongs to the
    /// checkout the user works in, so a linked worktree gets its own.
    /// </summary>
    private string ResolveProjectRoot()
        => Workspace.AgentWorkspace.FindLocation(fileSystem, fileSystem.GetCurrentDirectory())
            ?.CheckoutDirectory
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
}

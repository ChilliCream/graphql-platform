namespace ChilliCream.Nitro.CommandLine.Services.Hook;

/// <summary>
/// Resolves the Copilot CLI project-scope extension paths this installer
/// writes: <c>&lt;repo-root&gt;/.github/extensions/nitro-mail/extension.mjs</c>
/// and its sibling <c>nitro-mail.config.json</c> (the launch descriptor the
/// asset reads at runtime, kept out of the versioned asset file itself so
/// the asset's bytes, and therefore its content hash, stay identical across
/// machines). There is no user-scope resolution path, unlike
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

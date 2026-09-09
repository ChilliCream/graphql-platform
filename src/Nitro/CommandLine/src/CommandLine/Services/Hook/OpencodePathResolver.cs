using ChilliCream.Nitro.CommandLine.Services;

namespace ChilliCream.Nitro.CommandLine.Services.Hook;

internal sealed class OpencodePathResolver(
    IFileSystem fileSystem,
    IEnvironmentVariableProvider environmentVariables) : IOpencodePathResolver
{
    public string Resolve(string scope) => scope switch
    {
        HookInstallScopes.User => Path.Combine(ResolveConfigHome(), "opencode", "plugins", "nitro-hooks.js"),
        HookInstallScopes.Project => Path.Combine(ResolveProjectRoot(), ".opencode", "plugin", "nitro-hooks.js"),
        _ => throw new ExitException($"'{scope}' is not a supported hook installation scope.")
    };

    private string ResolveConfigHome()
    {
        var xdgConfigHome = environmentVariables.GetEnvironmentVariable("XDG_CONFIG_HOME");

        if (!string.IsNullOrWhiteSpace(xdgConfigHome))
        {
            return xdgConfigHome;
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrEmpty(home))
        {
            throw new ExitException("Could not resolve the current user's home directory.");
        }

        return Path.Combine(home, ".config");
    }

    private string ResolveProjectRoot()
        => Workspace.AgentWorkspace.FindLocation(fileSystem, fileSystem.GetCurrentDirectory())
            ?.CheckoutDirectory
            ?? throw new ExitException("No agent workspace found. Run `nitro agent init` first.");
}

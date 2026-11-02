namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the Nitro directory under the current user's application data directory.
/// </summary>
internal sealed class GlobalConfigDirectoryProvider : IGlobalConfigDirectoryProvider
{
    public string GetDirectory()
        => AgentWorkspace.GetGlobalConfigDirectory(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create));
}

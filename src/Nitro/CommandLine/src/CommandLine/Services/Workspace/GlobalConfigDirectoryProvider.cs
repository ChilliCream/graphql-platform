namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the global Nitro directory under the real machine's application
/// data directory, mirroring the <c>ApplicationData/nitro</c> convention
/// <c>ConfigurationService</c> uses.
/// </summary>
internal sealed class GlobalConfigDirectoryProvider : IGlobalConfigDirectoryProvider
{
    public string GetDirectory()
        => AgentWorkspace.GetGlobalConfigDirectory(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create));
}

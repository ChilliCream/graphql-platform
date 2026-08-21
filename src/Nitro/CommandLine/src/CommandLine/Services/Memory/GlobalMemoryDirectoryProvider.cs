using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Resolves the global memory root directory under the platform's
/// application data directory, mirroring the <c>ApplicationData/nitro</c>
/// convention <c>ConfigurationService</c> uses.
/// </summary>
internal sealed class GlobalMemoryDirectoryProvider : IGlobalMemoryDirectoryProvider
{
    public string GetDirectory()
        => AgentWorkspace.GetGlobalMemoryDirectory(
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData, Environment.SpecialFolderOption.Create));
}

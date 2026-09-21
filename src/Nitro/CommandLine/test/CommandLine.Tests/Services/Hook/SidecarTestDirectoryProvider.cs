using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Tests.Hook;

internal sealed class SidecarTestDirectoryProvider(string directory) : IGlobalConfigDirectoryProvider
{
    public string GetDirectory() => directory;
}

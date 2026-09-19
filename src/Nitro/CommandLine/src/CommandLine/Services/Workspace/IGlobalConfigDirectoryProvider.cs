namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the machine-local Nitro root directory under the platform's
/// application data directory.
/// </summary>
internal interface IGlobalConfigDirectoryProvider
{
    string GetDirectory();
}

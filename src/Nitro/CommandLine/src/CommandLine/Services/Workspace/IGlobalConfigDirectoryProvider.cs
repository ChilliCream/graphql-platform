namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the machine-local Nitro root directory under the platform's
/// application data directory. A seam for dependency injection so tests can
/// point global, machine-scoped state (the instance id fallback file, among
/// others) at a temporary directory instead of the real machine's.
/// </summary>
internal interface IGlobalConfigDirectoryProvider
{
    string GetDirectory();
}

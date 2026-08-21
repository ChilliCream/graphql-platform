namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Resolves the machine-local global memory root directory, independent of
/// any project workspace. A seam for dependency injection so tests can
/// point the global store at a temporary directory instead of the real
/// machine's application data directory.
/// </summary>
internal interface IGlobalMemoryDirectoryProvider
{
    string GetDirectory();
}

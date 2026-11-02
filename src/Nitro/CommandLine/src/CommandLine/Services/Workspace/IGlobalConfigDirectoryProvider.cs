namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// Resolves the Nitro directory under the current user's application data directory.
/// </summary>
internal interface IGlobalConfigDirectoryProvider
{
    string GetDirectory();
}

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcome of restoring an agent workspace archive: the restored roots, the number of restored
/// files (excluding the manifest), and the replaced folders that could not be deleted.
/// </summary>
internal sealed record AgentWorkspaceRestoreSummary(
    string Archive,
    IReadOnlyList<string> Roots,
    int FileCount,
    IReadOnlyList<string> LeftoverDirectories);

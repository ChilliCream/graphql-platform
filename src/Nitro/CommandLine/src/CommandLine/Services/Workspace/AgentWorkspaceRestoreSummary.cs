namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcome of restoring an agent workspace archive. <c>Roots</c> are the archive roots that
/// were restored and <c>FileCount</c> excludes the manifest. <c>LeftoverDirectories</c> lists
/// replaced folders that could not be deleted after the restore succeeded.
/// </summary>
internal sealed record AgentWorkspaceRestoreSummary(
    string Archive,
    IReadOnlyList<string> Roots,
    int FileCount,
    IReadOnlyList<string> LeftoverDirectories);

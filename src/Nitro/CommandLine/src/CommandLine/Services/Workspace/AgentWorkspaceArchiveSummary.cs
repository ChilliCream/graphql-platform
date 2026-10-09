namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The outcome of writing an agent workspace archive. <c>FileCount</c> excludes the manifest and
/// <c>Size</c> is the archive size in bytes.
/// </summary>
internal sealed record AgentWorkspaceArchiveSummary(
    string Archive,
    IReadOnlyList<string> Roots,
    int FileCount,
    long Size);

namespace ChilliCream.Nitro.CommandLine.Services.Workspace;

/// <summary>
/// The <c>manifest.json</c> at the root of an agent workspace archive. <c>DatabaseVersion</c> is
/// the schema version of the archived <c>agents.db</c>, or null when the archive has none.
/// </summary>
internal sealed record AgentWorkspaceArchiveManifest(
    int FormatVersion,
    string CliVersion,
    DateTimeOffset CreatedAt,
    long? DatabaseVersion,
    IReadOnlyList<string> Roots);

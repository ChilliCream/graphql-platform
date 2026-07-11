namespace ChilliCream.Nitro.Client;

/// <summary>
/// GitHub-specific source metadata payload.
/// </summary>
public sealed record SourceGitHubMetadata(
    string Actor,
    string CommitHash,
    string WorkflowName,
    string RunNumber,
    string RunId,
    string? JobId,
    Uri RepositoryUrl);

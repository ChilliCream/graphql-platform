namespace ChilliCream.Nitro.Client;

/// <summary>
/// Azure DevOps-specific source metadata payload.
/// </summary>
public sealed record SourceAzureDevOpsMetadata(
    AzureDevOpsActor Actor,
    string PipelineName,
    string RunNumber,
    string RunId,
    Uri ProjectUrl,
    string? CommitHash = null,
    string? JobId = null,
    string? TaskId = null,
    Uri? RepositoryUrl = null);

public sealed record AzureDevOpsActor(
    string Name,
    string? Email = null);

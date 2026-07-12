namespace ChilliCream.Nitro.Client;

internal static class SourceMetadataMapper
{
    public static SourceMetadataInput? Map(SourceMetadata? source)
    {
        if (source?.GitHub is { } github)
        {
            return new SourceMetadataInput
            {
                Github = new GitHubSourceMetadataInput
                {
                    Actor = github.Actor,
                    CommitHash = github.CommitHash,
                    WorkflowName = github.WorkflowName,
                    RunNumber = github.RunNumber,
                    RunId = github.RunId,
                    JobId = github.JobId,
                    RepositoryUrl = github.RepositoryUrl
                }
            };
        }

        if (source?.AzureDevOps is { } azureDevOps)
        {
            return new SourceMetadataInput
            {
                AzureDevOps = new AzureDevOpsSourceMetadataInput
                {
                    Actor = new AzureDevOpsActorInput
                    {
                        Name = azureDevOps.Actor.Name,
                        Email = azureDevOps.Actor.Email
                    },
                    PipelineName = azureDevOps.PipelineName,
                    RunNumber = azureDevOps.RunNumber,
                    RunId = azureDevOps.RunId,
                    ProjectUrl = azureDevOps.ProjectUrl,
                    CommitHash = azureDevOps.CommitHash,
                    JobId = azureDevOps.JobId,
                    TaskId = azureDevOps.TaskId,
                    RepositoryUrl = azureDevOps.RepositoryUrl
                }
            };
        }

        return null;
    }
}

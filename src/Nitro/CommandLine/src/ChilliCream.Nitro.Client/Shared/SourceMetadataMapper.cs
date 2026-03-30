namespace ChilliCream.Nitro.Client;

internal static class SourceMetadataMapper
{
    public static SourceMetadataInput? Map(SourceMetadata? source)
    {
        if (source?.GitHub is not { } github)
        {
            return null;
        }

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
}

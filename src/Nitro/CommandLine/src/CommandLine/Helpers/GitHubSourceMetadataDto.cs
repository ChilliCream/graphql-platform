using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed record GitHubSourceMetadataDto(
    [property: JsonRequired] string Actor,
    [property: JsonRequired] string CommitHash,
    [property: JsonRequired] string WorkflowName,
    [property: JsonRequired] string RunNumber,
    [property: JsonRequired] string RunId,
    string? JobId,
    [property: JsonRequired] string RepositoryUrl);

using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal sealed record AzureDevOpsSourceMetadataDto(
    [property: JsonRequired] AzureDevOpsActorDto Actor,
    [property: JsonRequired] string PipelineName,
    [property: JsonRequired] string RunNumber,
    [property: JsonRequired] string RunId,
    [property: JsonRequired] string ProjectUrl,
    string? CommitHash,
    string? JobId,
    string? TaskId,
    string? RepositoryUrl);

using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class SourceMetadataParser
{
    public static SourceMetadata? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        GitHubSourceMetadataDto dto;

        try
        {
            dto = JsonSerializer.Deserialize(json, SourceMetadataJsonContext.Default.GitHubSourceMetadataDto)
                ?? throw new ExitException("Failed to parse --source-metadata: deserialized value was null.");

            return new SourceMetadata(
                new SourceGitHubMetadata(
                    dto.Actor,
                    dto.CommitHash,
                    dto.WorkflowName,
                    dto.RunNumber,
                    dto.RunId,
                    dto.JobId,
                    new Uri(dto.RepositoryUrl)));
        }
        catch (Exception ex)
        {
            throw new ExitException($"Failed to parse --source-metadata: {ex.Message}");
        }
    }
}

internal sealed record GitHubSourceMetadataDto(
    [property: JsonRequired] string Actor,
    [property: JsonRequired] string CommitHash,
    [property: JsonRequired] string WorkflowName,
    [property: JsonRequired] string RunNumber,
    [property: JsonRequired] string RunId,
    string? JobId,
    [property: JsonRequired] string RepositoryUrl);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubSourceMetadataDto))]
internal partial class SourceMetadataJsonContext : JsonSerializerContext;

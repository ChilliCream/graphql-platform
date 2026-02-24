using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.CommandLine.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class SourceMetadataHelper
{
    public static SourceMetadataInput? Parse(string? json)
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
        }
        catch (Exception ex)
        {
            throw new ExitException($"Failed to parse --source-metadata: {ex.Message}");
        }

        return new SourceMetadataInput
        {
            Github = new GitHubSourceMetadataInput
            {
                Actor = dto.Actor,
                CommitHash = dto.CommitHash,
                WorkflowName = dto.WorkflowName,
                RunNumber = dto.RunNumber,
                RunId = dto.RunId,
                JobId = dto.JobId,
                RepositoryUrl = new Uri(dto.RepositoryUrl)
            }
        };
    }
}

internal sealed record GitHubSourceMetadataDto(
    string Actor,
    string CommitHash,
    string WorkflowName,
    string RunNumber,
    string RunId,
    string? JobId,
    string RepositoryUrl);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubSourceMetadataDto))]
internal partial class SourceMetadataJsonContext : JsonSerializerContext;

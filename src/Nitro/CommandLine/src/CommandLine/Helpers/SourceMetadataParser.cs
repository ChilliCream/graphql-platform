using System.Text.Json;
using System.Text.Json.Serialization;
using ChilliCream.Nitro.Client;

namespace ChilliCream.Nitro.CommandLine.Helpers;

internal static class SourceMetadataParser
{
    private const string TypePropertyName = "type";
    private const string GitHubType = "github";
    private const string AzureDevOpsType = "azure-devops";

    public static SourceMetadata? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException("Expected a JSON object.");
            }

            // When no 'type' marker is present we assume GitHub for backwards compatibility.
            var type = GitHubType;
            if (document.RootElement.TryGetProperty(TypePropertyName, out var typeElement))
            {
                if (typeElement.ValueKind != JsonValueKind.String)
                {
                    throw new InvalidOperationException(
                        $"'{TypePropertyName}' must be a string. Expected '{GitHubType}' or '{AzureDevOpsType}'.");
                }

                type = typeElement.GetString()!;
            }

            return type switch
            {
                GitHubType => new SourceMetadata(GitHub: ParseGitHub(document.RootElement)),
                AzureDevOpsType => new SourceMetadata(AzureDevOps: ParseAzureDevOps(document.RootElement)),
                _ => throw new InvalidOperationException(
                    $"Unsupported '{TypePropertyName}' value '{type}'. "
                    + $"Expected '{GitHubType}' or '{AzureDevOpsType}'.")
            };
        }
        catch (Exception ex)
        {
            throw new ExitException($"Failed to parse --source-metadata: {ex.Message.EscapeMarkup()}");
        }
    }

    private static SourceGitHubMetadata ParseGitHub(JsonElement element)
    {
        var dto = element.Deserialize(SourceMetadataJsonContext.Default.GitHubSourceMetadataDto)
            ?? throw new InvalidOperationException("Could not deserialize GitHub source metadata.");

        return new SourceGitHubMetadata(
            dto.Actor,
            dto.CommitHash,
            dto.WorkflowName,
            dto.RunNumber,
            dto.RunId,
            dto.JobId,
            new Uri(dto.RepositoryUrl));
    }

    private static SourceAzureDevOpsMetadata ParseAzureDevOps(JsonElement element)
    {
        var dto = element.Deserialize(SourceMetadataJsonContext.Default.AzureDevOpsSourceMetadataDto)
            ?? throw new InvalidOperationException("Could not deserialize Azure DevOps source metadata.");

        return new SourceAzureDevOpsMetadata(
            new AzureDevOpsActor(dto.Actor.Name, dto.Actor.Email),
            dto.PipelineName,
            dto.RunNumber,
            dto.RunId,
            new Uri(dto.ProjectUrl),
            dto.CommitHash,
            dto.JobId,
            dto.TaskId,
            dto.RepositoryUrl is null ? null : new Uri(dto.RepositoryUrl));
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

internal sealed record AzureDevOpsActorDto(
    [property: JsonRequired] string Name,
    string? Email);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubSourceMetadataDto))]
[JsonSerializable(typeof(AzureDevOpsSourceMetadataDto))]
internal partial class SourceMetadataJsonContext : JsonSerializerContext;

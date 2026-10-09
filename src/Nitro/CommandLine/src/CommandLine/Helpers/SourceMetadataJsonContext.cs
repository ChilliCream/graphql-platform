using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Helpers;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GitHubSourceMetadataDto))]
[JsonSerializable(typeof(AzureDevOpsSourceMetadataDto))]
internal partial class SourceMetadataJsonContext : JsonSerializerContext;

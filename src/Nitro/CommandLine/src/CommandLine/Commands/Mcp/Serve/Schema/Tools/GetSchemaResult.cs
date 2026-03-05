using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

internal sealed class GetSchemaResult
{
    [JsonPropertyName("filePath")]
    public string FilePath { get; init; } = string.Empty;

    [JsonPropertyName("apiId")]
    public string ApiId { get; init; } = string.Empty;

    [JsonPropertyName("apiName")]
    public string ApiName { get; init; } = string.Empty;

    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }
}

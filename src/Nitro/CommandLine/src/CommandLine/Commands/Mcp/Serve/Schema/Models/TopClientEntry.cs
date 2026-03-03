using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class TopClientEntry
{
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    [JsonPropertyName("totalRequests")]
    public long TotalRequests { get; init; }

    [JsonPropertyName("totalOperations")]
    public long TotalOperations { get; init; }

    [JsonPropertyName("totalVersions")]
    public long TotalVersions { get; init; }
}

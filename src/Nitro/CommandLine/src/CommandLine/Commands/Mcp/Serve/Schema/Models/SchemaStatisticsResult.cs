using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class SchemaStatisticsResult
{
    [JsonPropertyName("statistics")]
    public IReadOnlyList<CoordinateStatisticsEntry> Statistics { get; init; } =
        Array.Empty<CoordinateStatisticsEntry>();

    [JsonPropertyName("window")]
    public StatisticsWindow Window { get; init; } = new();

    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("apiId")]
    public string ApiId { get; init; } = string.Empty;
}

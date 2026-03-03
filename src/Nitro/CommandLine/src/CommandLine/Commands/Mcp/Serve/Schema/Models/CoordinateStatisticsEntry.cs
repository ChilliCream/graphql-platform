using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class CoordinateStatisticsEntry
{
    [JsonPropertyName("coordinate")]
    public string Coordinate { get; init; } = string.Empty;

    [JsonPropertyName("found")]
    public bool Found { get; init; }

    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; init; }

    [JsonPropertyName("deprecationReason")]
    public string? DeprecationReason { get; set; }

    [JsonPropertyName("usage")]
    public CoordinateUsageEntry? Usage { get; init; }

    [JsonPropertyName("topClients")]
    public IReadOnlyList<TopClientEntry> TopClients { get; init; } = Array.Empty<TopClientEntry>();
}

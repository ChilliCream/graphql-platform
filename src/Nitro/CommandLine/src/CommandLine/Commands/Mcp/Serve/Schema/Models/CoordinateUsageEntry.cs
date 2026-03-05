using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class CoordinateUsageEntry
{
    [JsonPropertyName("clientCount")]
    public long ClientCount { get; init; }

    [JsonPropertyName("operationCount")]
    public long OperationCount { get; init; }

    [JsonPropertyName("totalReferences")]
    public long TotalReferences { get; init; }

    [JsonPropertyName("totalRequests")]
    public long? TotalRequests { get; init; }

    [JsonPropertyName("totalUsages")]
    public long? TotalUsages { get; init; }

    [JsonPropertyName("opm")]
    public double? Opm { get; init; }

    [JsonPropertyName("errorRate")]
    public double? ErrorRate { get; init; }

    [JsonPropertyName("meanDuration")]
    public double? MeanDuration { get; init; }

    [JsonPropertyName("firstSeen")]
    public string? FirstSeen { get; init; }

    [JsonPropertyName("lastSeen")]
    public string? LastSeen { get; init; }
}

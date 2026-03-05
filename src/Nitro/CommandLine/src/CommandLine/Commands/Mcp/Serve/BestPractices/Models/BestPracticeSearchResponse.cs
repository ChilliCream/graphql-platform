using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.BestPractices.Models;

internal sealed class BestPracticeSearchResponse
{
    [JsonPropertyName("results")]
    public IReadOnlyList<BestPracticeSearchResult> Results { get; init; } = Array.Empty<BestPracticeSearchResult>();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}

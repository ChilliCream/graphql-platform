using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class SearchResult
{
    [JsonPropertyName("results")]
    public IReadOnlyList<SearchResultItem> Results { get; init; } = Array.Empty<SearchResultItem>();

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }
}

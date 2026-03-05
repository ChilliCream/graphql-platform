using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class PageInfoEntry
{
    [JsonPropertyName("hasNextPage")]
    public bool HasNextPage { get; init; }

    [JsonPropertyName("endCursor")]
    public string? EndCursor { get; init; }
}

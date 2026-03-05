using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class ListApisResult
{
    [JsonPropertyName("apis")]
    public required IReadOnlyList<ApiEntry> Apis { get; init; }

    [JsonPropertyName("pageInfo")]
    public required PageInfoEntry PageInfo { get; init; }
}

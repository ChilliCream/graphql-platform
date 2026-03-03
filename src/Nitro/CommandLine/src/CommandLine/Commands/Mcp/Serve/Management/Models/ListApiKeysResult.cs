using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class ListApiKeysResult
{
    [JsonPropertyName("apiKeys")]
    public required IReadOnlyList<ApiKeyEntry> ApiKeys { get; init; }

    [JsonPropertyName("pageInfo")]
    public required PageInfoEntry PageInfo { get; init; }
}

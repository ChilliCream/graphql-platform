using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class ListClientsResult
{
    [JsonPropertyName("clients")]
    public required IReadOnlyList<ClientEntry> Clients { get; init; }

    [JsonPropertyName("pageInfo")]
    public required PageInfoEntry PageInfo { get; init; }
}

using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class CreateClientResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("client")]
    public ClientEntry? Client { get; init; }
}

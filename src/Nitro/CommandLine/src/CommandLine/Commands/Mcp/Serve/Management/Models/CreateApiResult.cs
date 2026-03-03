using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class CreateApiResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("api")]
    public ApiEntry? Api { get; init; }
}

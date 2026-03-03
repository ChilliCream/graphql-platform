using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

internal sealed class FusionInfoError
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}

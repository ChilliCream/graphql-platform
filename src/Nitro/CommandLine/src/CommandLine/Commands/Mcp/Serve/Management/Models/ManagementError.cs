using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class ManagementError
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}

using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class UpdateApiSettingsResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("apiName")]
    public string? ApiName { get; init; }
}

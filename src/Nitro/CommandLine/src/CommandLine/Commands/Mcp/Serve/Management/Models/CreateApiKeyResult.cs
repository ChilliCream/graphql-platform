using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Management.Models;

internal sealed class CreateApiKeyResult
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("apiKey")]
    public ApiKeyEntry? ApiKey { get; init; }

    [JsonPropertyName("secret")]
    public string? Secret { get; init; }
}

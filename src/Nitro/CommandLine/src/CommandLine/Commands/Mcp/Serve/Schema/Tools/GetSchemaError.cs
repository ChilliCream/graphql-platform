using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Tools;

internal sealed class GetSchemaError
{
    [JsonPropertyName("error")]
    public string Error { get; init; } = string.Empty;
}

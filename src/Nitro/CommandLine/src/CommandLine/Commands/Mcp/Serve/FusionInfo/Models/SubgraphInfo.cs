using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

internal sealed class SubgraphInfo
{
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    [JsonPropertyName("endpointUrl")]
    public string? EndpointUrl { get; init; }

    [JsonPropertyName("schemaCoordinateCount")]
    public required int SchemaCoordinateCount { get; init; }

    [JsonPropertyName("rootTypes")]
    public required SubgraphRootTypes RootTypes { get; init; }
}

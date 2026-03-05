using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

internal sealed class FusionInfoResult
{
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }

    [JsonPropertyName("subgraphs")]
    public required IReadOnlyList<SubgraphInfo> Subgraphs { get; init; }

    [JsonPropertyName("composedSchemaPath")]
    public required string ComposedSchemaPath { get; init; }

    [JsonPropertyName("subgraphSchemaPaths")]
    public required IReadOnlyDictionary<string, string> SubgraphSchemaPaths { get; init; }

    [JsonPropertyName("totalTypes")]
    public required int TotalTypes { get; init; }

    [JsonPropertyName("totalFields")]
    public required int TotalFields { get; init; }
}

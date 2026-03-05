using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.FusionInfo.Models;

internal sealed class SubgraphRootTypes
{
    [JsonPropertyName("query")]
    public IReadOnlyList<string> Query { get; init; } = [];

    [JsonPropertyName("mutation")]
    public IReadOnlyList<string> Mutation { get; init; } = [];

    [JsonPropertyName("subscription")]
    public IReadOnlyList<string> Subscription { get; init; } = [];
}

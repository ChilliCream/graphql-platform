using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class SearchResultItem
{
    [JsonPropertyName("coordinate")]
    public string Coordinate { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("typeName")]
    public string? TypeName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; init; }

    [JsonPropertyName("deprecationReason")]
    public string? DeprecationReason { get; init; }

    [JsonPropertyName("pathsToRoot")]
    public IReadOnlyList<IReadOnlyList<string>> PathsToRoot { get; init; } = Array.Empty<IReadOnlyList<string>>();
}

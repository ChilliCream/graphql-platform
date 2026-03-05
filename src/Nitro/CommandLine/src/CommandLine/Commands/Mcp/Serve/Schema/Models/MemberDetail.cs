using System.Text.Json.Serialization;

namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class MemberDetail
{
    [JsonPropertyName("coordinate")]
    public string Coordinate { get; init; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("parentType")]
    public string? ParentType { get; init; }

    [JsonPropertyName("typeName")]
    public string? TypeName { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("isDeprecated")]
    public bool IsDeprecated { get; init; }

    [JsonPropertyName("deprecationReason")]
    public string? DeprecationReason { get; init; }

    [JsonPropertyName("arguments")]
    public IReadOnlyList<ArgumentEntry>? Arguments { get; init; }

    [JsonPropertyName("directives")]
    public IReadOnlyList<DirectiveEntry>? Directives { get; init; }

    [JsonPropertyName("interfaces")]
    public IReadOnlyList<string>? Interfaces { get; init; }

    [JsonPropertyName("possibleTypes")]
    public IReadOnlyList<string>? PossibleTypes { get; init; }

    [JsonPropertyName("enumValues")]
    public IReadOnlyList<EnumValueEntry>? EnumValues { get; init; }
}

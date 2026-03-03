namespace ChilliCream.Nitro.CommandLine.Commands.Mcp.Serve.Schema.Models;

internal sealed class SchemaIndexEntry
{
    public required string Coordinate { get; init; }
    public required SchemaIndexMemberKind Kind { get; init; }
    public required string Name { get; init; }
    public string? ParentTypeName { get; init; }
    public string? TypeName { get; init; }
    public string? Description { get; init; }
    public bool IsDeprecated { get; init; }
    public string? DeprecationReason { get; init; }
    public string? DefaultValue { get; init; }
    public IReadOnlyList<ArgumentEntry>? Arguments { get; init; }
    public IReadOnlyList<DirectiveEntry>? Directives { get; init; }
    public IReadOnlyList<EnumValueEntry>? EnumValues { get; init; }
    public IReadOnlyList<string>? PossibleTypes { get; init; }
    public IReadOnlyList<string>? Interfaces { get; init; }
}

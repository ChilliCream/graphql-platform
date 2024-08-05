using System.Collections.Immutable;

namespace HotChocolate.Fusion.Planning;

public sealed class SourceObjectType(
    string name,
    string schemaName,
    ImmutableArray<Lookup> lookups)
    : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ImmutableArray<Lookup> Lookups { get; } = lookups;
}

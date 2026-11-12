using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Represents source schema metadata for a union type in a composite schema.
/// </summary>
/// <param name="name">The name of the union type as it appears in the source schema.</param>
/// <param name="schemaName">The name of the source schema containing this union type.</param>
/// <param name="lookups">The entity lookups available for this union type in the source schema.</param>
/// <param name="contexts">The names of the contexts that this union type sets in the source schema.</param>
public sealed class SourceUnionType(
    string name,
    string schemaName,
    ImmutableArray<Lookup> lookups,
    ImmutableHashSet<string> contexts) : ISourceMember
{
    public string Name { get; } = name;

    public string SchemaName { get; } = schemaName;

    public ImmutableArray<Lookup> Lookups { get; } = lookups;

    /// <summary>
    /// Gets the names of the contexts that this union type sets in the source schema.
    /// </summary>
    public ImmutableHashSet<string> Contexts { get; } = contexts;
}

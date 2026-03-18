using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Represents source schema metadata for an interface type in a composite schema.
/// This class captures how a composite interface type appears and behaves within
/// a specific source schema.
/// </summary>
/// <param name="name">The name of the interface type as it appears in the source schema.</param>
/// <param name="schemaName">The name of the source schema containing this interface type.</param>
/// <param name="lookups">The entity lookups available for this interface type in the source schema.</param>
/// <param name="implements">The names of interfaces implemented by this interface type in the source schema.</param>
public sealed class SourceInterfaceType(
    string name,
    string schemaName,
    ImmutableArray<Lookup> lookups,
    ImmutableHashSet<string> implements)
    : ISourceComplexType
{
    /// <summary>
    /// Gets the name of the interface type as it appears in the source schema.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the name of the source schema containing this interface type.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the entity lookups available for this interface type in the source schema.
    /// </summary>
    public ImmutableArray<Lookup> Lookups { get; } = lookups;

    /// <summary>
    /// Gets the names of interfaces implemented by this interface type in the source schema.
    /// </summary>
    public ImmutableHashSet<string> Implements { get; } = implements;
}

using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Represents source schema metadata for an object type in a composite schema.
/// This class captures how a composite object type appears and behaves within
/// a specific source schema.
/// </summary>
/// <param name="name">The name of the object type as it appears in the source schema.</param>
/// <param name="schemaName">The name of the source schema containing this object type.</param>
/// <param name="lookups">The entity lookups available for this object type in the source schema.</param>
/// <param name="implements">The names of interfaces implemented by this object type in the source schema.</param>
/// <param name="memberOf">The names of union types that this object type is a member of in the source schema.</param>
public sealed class SourceObjectType(
    string name,
    string schemaName,
    ImmutableArray<Lookup> lookups,
    ImmutableHashSet<string> implements,
    ImmutableHashSet<string> memberOf)
    : ISourceComplexType
{
    /// <summary>
    /// Gets the name of the object type as it appears in the source schema.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// Gets the name of the source schema containing this object type.
    /// </summary>
    public string SchemaName { get; } = schemaName;

    /// <summary>
    /// Gets the entity lookups available for this object type in the source schema.
    /// </summary>
    public ImmutableArray<Lookup> Lookups { get; } = lookups;

    /// <summary>
    /// Gets the names of interfaces implemented by this object type in the source schema.
    /// </summary>
    public ImmutableHashSet<string> Implements { get; } = implements;

    /// <summary>
    /// Gets the names of union types that this object type is a member of in the source schema.
    /// </summary>
    public ImmutableHashSet<string> MemberOf { get; } = memberOf;
}

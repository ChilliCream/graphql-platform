using System.Collections.Immutable;

namespace HotChocolate.Fusion.Types.Metadata;

/// <summary>
/// Represents source schema metadata for a complex types (Interface Type or Object Type)
/// in a composite schema. Complex types can have entity lookups and implement different interfaces,
/// with potentially different characteristics across source schemas.
/// </summary>
public interface ISourceComplexType : ISourceMember
{
    /// <summary>
    /// Gets the entity lookups available for this complex type in the current source schema.
    /// </summary>
    /// <value>
    /// The lookup definitions that enable entity resolution for this complex type from
    /// this specific source schema. These are used by the fusion gateway to determine
    /// how to fetch entity data during query execution.
    /// </value>
    ImmutableArray<Lookup> Lookups { get; }

    /// <summary>
    /// Gets the names of interfaces implemented by this complex type in the current source schema.
    /// </summary>
    /// <value>
    /// The set of interface names that this complex type implements within this source schema.
    /// The same composite complex type may implement different interfaces in different
    /// source schemas.
    /// </value>
    ImmutableHashSet<string> Implements { get; }
}

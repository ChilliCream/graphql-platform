namespace HotChocolate.Fusion.Metadata;

/// <summary>
/// Represents the metadata about a named type for the purpose of query planning.
/// </summary>
internal interface INamedTypeMetadata
{
    /// <summary>
    /// Gets the name of the named type.
    /// </summary>
    string Name { get; }

    MemberBindingCollection Bindings { get; }
}

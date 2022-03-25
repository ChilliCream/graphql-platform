namespace HotChocolate.Stitching.Types;

/// <summary>
/// <see cref="ITypeDefinitionMerger"/> will merge a <see cref="ITypeDefinition"/> into another
/// <see cref="ITypeDefinition"/>.
/// </summary>
internal interface ITypeDefinitionMerger
{
    /// <summary>
    /// Merges the <paramref name="source"/> definition into the
    /// <paramref name="target"/> definition.
    /// </summary>
    /// <param name="source">
    /// The type definition that shall be merged into the
    /// <paramref name="target"/> definition.
    /// </param>
    /// <param name="target">
    /// The type definition into which the <paramref name="source"/>
    /// definition shall be merged into.
    /// </param>
    void MergeInto(ITypeDefinition source, ITypeDefinition target);
}

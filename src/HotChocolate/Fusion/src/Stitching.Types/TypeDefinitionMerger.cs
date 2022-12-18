using System;

namespace HotChocolate.Stitching.Types;

internal abstract class TypeDefinitionMerger<T> : ITypeDefinitionMerger where T : ITypeDefinition
{
    /// <inheritdoc cref="ITypeDefinitionMerger"/>
    public void MergeInto(ITypeDefinition source, ITypeDefinition target)
    {
        if (source is null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        if (source is not T a || target is not T b)
        {
            throw new ArgumentException(
                $"The source and target definition must be {typeof(T).FullName}",
                nameof(target));
        }

        MergeInto(a, b);
    }

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
    protected abstract void MergeInto(T source, T target);
}

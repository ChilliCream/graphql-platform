using System;

namespace HotChocolate.Execution;

/// <summary>
/// A implementation of <see cref="PathFactory"/> that creates segments of a <see cref="Path"/>.
/// </summary>
public class PathFactory
{
    protected PathFactory() { }

    /// <summary>
    /// Appends an element.
    /// </summary>
    /// <param name="parent">The parent</param>
    /// <param name="index">The index of the element.</param>
    /// <returns>Returns a new path segment pointing to an element in a list.</returns>
    public IndexerPathSegment Append(Path parent, int index)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        IndexerPathSegment indexer = CreateIndexer();

        indexer.Parent = parent;
        indexer.Index = index;
        indexer.Depth = parent.Depth + 1;

        return indexer;
    }

    /// <summary>
    /// Appends a new path segment.
    /// </summary>
    /// <param name="parent">The parent</param>
    /// <param name="name">The name of the path segment.</param>
    /// <returns>Returns a new path segment.</returns>
    public NamePathSegment Append(Path parent, NameString name)
    {
        name.EnsureNotEmpty(nameof(name));

        NamePathSegment indexer = CreateNamed();

        indexer.Parent = parent;
        indexer.Name = name;
        indexer.Depth = parent.Depth + 1;

        return indexer;
    }

    /// <summary>
    /// Creates a new instance of <see cref="IndexerPathSegment"/>
    /// </summary>
    protected virtual IndexerPathSegment CreateIndexer() => new();

    /// <summary>
    /// Creates a new instance of <see cref="NamePathSegment"/>
    /// </summary>
    protected virtual NamePathSegment CreateNamed() => new();

    public static PathFactory Instance { get; } = new();
}

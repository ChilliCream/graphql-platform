#nullable enable

using System;

namespace HotChocolate;

/// <summary>
/// An <see cref="IndexerPathSegment" /> represents a pointer to
/// an list element in the result structure.
/// </summary>
public sealed class IndexerPathSegment : Path
{
    /// <summary>
    /// Gets the <see cref="Index"/> which represents the position an element in a
    /// list of the result structure.
    /// </summary>
    public int Index { get; internal set; }

    /// <inheritdoc />
    public override string Print()
    {
        return $"{Parent.Print()}[{Index}]";
    }

    /// <inheritdoc />
    public override bool Equals(Path? other)
    {
        if (ReferenceEquals(other, null))
        {
            return false;
        }

        if (other is IndexerPathSegment indexer &&
            Length.Equals(indexer.Length) &&
            Index.Equals(indexer.Index) &&
            Parent.Equals(indexer.Parent))
        {
            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public override Path Clone()
        => new IndexerPathSegment
        {
            Length = Length,
            Index = Index,
            Parent = Parent.Clone()
        };

    /// <inheritdoc />
    public override int GetHashCode()
        // ReSharper disable NonReadonlyMemberInGetHashCode
        => HashCode.Combine(Parent, Length, Index);
        // ReSharper restore NonReadonlyMemberInGetHashCode
}

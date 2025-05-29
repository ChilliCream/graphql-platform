namespace HotChocolate;

/// <summary>
/// An <see cref="IndexerPathSegment" /> represents a pointer to
/// an list element in the result structure.
/// </summary>
public sealed class IndexerPathSegment : Path
{
    internal IndexerPathSegment(Path parent, int index)
        : base(parent)
    {
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
    }

    /// <summary>
    /// Gets the <see cref="Index"/> which represents the position an element in a
    /// list of the result structure.
    /// </summary>
    public int Index { get; }

    /// <inheritdoc />
    public override bool Equals(Path? other)
        => base.Equals(other) &&
            other is IndexerPathSegment indexer &&
            Index.Equals(indexer.Index);

    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(base.GetHashCode(), Index);
}

#nullable enable

namespace HotChocolate
{
    /// <summary>
    /// An <see cref="IndexerPathSegment" /> represents a pointer to 
    /// an list element in the result structure.
    /// </summary>
    public sealed class IndexerPathSegment : Path
    {
        internal IndexerPathSegment(Path parent, int index)
        {
            Parent = parent;
            Depth = parent.Depth + 1;
            Index = index;
        }

        /// <inheritdoc />
        public override Path Parent { get; }

        /// <inheritdoc />
        public override int Depth { get; }

        /// <summary>
        /// Gets the <see cref="Index"/> which represents the position an element in a 
        /// list of the result structure.
        /// </summary>
        public int Index { get; }

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
                Depth.Equals(indexer.Depth) &&
                Index.Equals(indexer.Index) &&
                Parent.Equals(indexer.Parent))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hash = Parent.GetHashCode() * 3;
                hash ^= Depth.GetHashCode() * 7;
                hash ^= Index.GetHashCode() * 11;
                return hash;
            }
        }
    }
}
